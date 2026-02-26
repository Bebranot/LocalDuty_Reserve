// SPDX-FileCopyrightText: 2025 LocalDuty <https://github.com/Bebranot/LocalDuty_Reserve>
//
// SPDX-License-Identifier: AGPL-3.0-or-later

using Content.Client.Audio;
using Content.Client.Gameplay;
using Content.Shared.CCVar;
using Content.Shared.CombatMode;
using Content.Shared.Duty.Audio;
using Content.Shared.GameTicking;
using Robust.Client.Player;
using Robust.Client.State;
using Robust.Shared.Audio;
using Robust.Shared.Audio.Systems;
using Robust.Shared.Configuration;
using Robust.Shared.Player;
using Robust.Shared.Prototypes;
using Robust.Shared.Random;
using Robust.Shared.Timing;

namespace Content.Client.Duty.Audio;

/// <summary>
/// Система динамической фоновой музыки для LocalDuty.
///
/// Логика:
/// - Если игрок НЕ в боевом режиме → спокойная музыка играет периодически со случайными паузами.
/// - Если игрок ВХОДИТ в боевой режим → спокойная музыка мгновенно останавливается, боевая начинается.
/// - Если игрок ВЫХОДИТ из боевого режима → боевая музыка плавно затухает (fade-out), затем возобновляется логика спокойной музыки.
/// - Система активна только во время геймплея (не в лобби).
/// - Уважает CCVar включения/громкости.
/// </summary>
public sealed class DynamicAmbientMusicSystem : EntitySystem
{
    [Dependency] private readonly IConfigurationManager _config = default!;
    [Dependency] private readonly IPlayerManager _playerManager = default!;
    [Dependency] private readonly IPrototypeManager _protoManager = default!;
    [Dependency] private readonly IRobustRandom _random = default!;
    [Dependency] private readonly IGameTiming _timing = default!;
    [Dependency] private readonly IStateManager _stateManager = default!;
    [Dependency] private readonly SharedAudioSystem _audio = default!;

    // ── Зависимость на основную аудио-систему (для FadeOut) ──────────────────
    private ContentAudioSystem _contentAudio = default!;

    // ── Текущее состояние ────────────────────────────────────────────────────

    /// Был ли игрок в боевом режиме в прошлый тик
    private bool _wasInCombat;

    /// Тип музыки, которая сейчас воспроизводится
    private DutyMusicType _currentType = DutyMusicType.None;

    /// EntityUid текущего играющего трека
    private EntityUid? _currentStream;

    // ── Таймер для спокойной музыки ──────────────────────────────────────────

    /// Момент времени, после которого разрешено запустить следующий спокойный трек
    private TimeSpan _nextCalmPlayTime = TimeSpan.Zero;

    /// Играет ли сейчас спокойный трек (чтобы не запускать новый пока старый не кончился)
    private bool _calmTrackPlaying;

    // ── CCVar-кэш ────────────────────────────────────────────────────────────

    private bool _enabled;
    private float _volume;

    // ── ID прототипа ─────────────────────────────────────────────────────────

    /// ID прототипа с треками. Меняй если нужно несколько наборов по ситуации.
    private const string PrototypeId = "DutyAmbientMusic";

    // ─────────────────────────────────────────────────────────────────────────

    public override void Initialize()
    {
        base.Initialize();

        _contentAudio = IoCManager.Resolve<IEntitySystemManager>().GetEntitySystem<ContentAudioSystem>();

        // Читаем CCVars и подписываемся на изменения
        _config.OnValueChanged(DutyCCVars.DynamicAmbientMusicEnabled, OnEnabledChanged, true);
        _config.OnValueChanged(DutyCCVars.DynamicAmbientMusicVolume, OnVolumeChanged, true);

        // Сброс при рестарте раунда
        SubscribeNetworkEvent<RoundRestartCleanupEvent>(OnRoundRestart);
    }

    public override void Shutdown()
    {
        base.Shutdown();
        _config.UnsubValueChanged(DutyCCVars.DynamicAmbientMusicEnabled, OnEnabledChanged);
        _config.UnsubValueChanged(DutyCCVars.DynamicAmbientMusicVolume, OnVolumeChanged);
        StopCurrent(immediate: true);
    }

    // ── CCVar-обработчики ─────────────────────────────────────────────────────

    private void OnEnabledChanged(bool value)
    {
        _enabled = value;
        if (!_enabled)
            StopCurrent(immediate: true);
    }

    private void OnVolumeChanged(float value)
    {
        _volume = value;
        // Если трек уже играет — обновляем громкость на лету
        if (_currentStream != null && TryComp(_currentStream, out Robust.Shared.Audio.Components.AudioComponent? comp))
        {
            _audio.SetVolume(_currentStream, VolumeFromLinear(_volume), comp);
        }
    }

    // ── Рестарт раунда ────────────────────────────────────────────────────────

    private void OnRoundRestart(RoundRestartCleanupEvent ev)
    {
        StopCurrent(immediate: true);
        _wasInCombat = false;
        _calmTrackPlaying = false;
        _nextCalmPlayTime = TimeSpan.Zero;
    }

    // ── Update-цикл ───────────────────────────────────────────────────────────

    public override void Update(float frameTime)
    {
        base.Update(frameTime);

        // Работаем только на первом предсказанном тике
        if (!_timing.IsFirstTimePredicted)
            return;

        // Только если система включена и мы в геймплее (не в лобби)
        if (!_enabled || _stateManager.CurrentState is not GameplayState)
        {
            if (_currentStream != null)
                StopCurrent(immediate: true);
            return;
        }

        // Получаем локального игрока
        var player = _playerManager.LocalSession?.AttachedEntity;
        if (player == null)
        {
            StopCurrent(immediate: true);
            return;
        }

        // При нулевой громкости не тратим ресурсы на проигрывание музыки.
        if (_volume <= 0f)
        {
            StopCurrent(immediate: true);
            return;
        }

        var inCombat = IsInCombatMode(player.Value);

        // ── Переход В боевой режим ───────────────────────────────────────────
        if (inCombat && !_wasInCombat)
        {
            // Немедленно останавливаем спокойную музыку (без fade — по дизайну)
            StopCurrent(immediate: true);
            PlayCombatTrack();
        }
        // ── Выход ИЗ боевого режима ──────────────────────────────────────────
        else if (!inCombat && _wasInCombat)
        {
            // Плавно гасим боевую музыку
            if (_currentStream != null)
            {
                var proto = GetProto();
                _contentAudio.FadeOut(_currentStream, duration: proto?.CombatFadeOutDuration ?? 2.5f);
                _currentStream = null;
                _currentType = DutyMusicType.None;
            }

            // Небольшая пауза перед следующим спокойным треком
            ScheduleNextCalmTrack();
            _calmTrackPlaying = false;
        }

        _wasInCombat = inCombat;

        // ── Логика спокойной музыки ──────────────────────────────────────────
        if (!inCombat)
            UpdateCalmMusic();
    }

    // ── Спокойная музыка ──────────────────────────────────────────────────────

    private void UpdateCalmMusic()
    {
        // Если трек уже играет — проверяем, не закончился ли он
        if (_currentType == DutyMusicType.Calm && _currentStream != null)
        {
            // EntityUid больше не существует — трек завершился естественно
            if (!EntityManager.EntityExists(_currentStream.Value))
            {
                _currentStream = null;
                _currentType = DutyMusicType.None;
                _calmTrackPlaying = false;
                ScheduleNextCalmTrack();
            }
            return;
        }

        // Ждём таймера перед следующим треком
        if (_timing.CurTime < _nextCalmPlayTime)
            return;

        // Пора запустить следующий спокойный трек
        if (!_calmTrackPlaying)
            PlayCalmTrack();
    }

    private void PlayCalmTrack()
    {
        var proto = GetProto();
        if (proto == null || proto.CalmTracks.Count == 0)
            return;

        var track = _random.Pick(proto.CalmTracks);
        _currentStream = _audio.PlayGlobal(
            track,
            Filter.Local(),
            false,
            AudioParams.Default.WithVolume(VolumeFromLinear(_volume))
        )?.Entity;

        if (_currentStream != null)
        {
            _currentType = DutyMusicType.Calm;
            _calmTrackPlaying = true;

            // Плавный fade-in спокойной музыки.
            _contentAudio.FadeIn(_currentStream, duration: proto.CalmFadeInDuration);
        }
    }

    private void ScheduleNextCalmTrack()
    {
        var proto = GetProto();
        var minInterval = proto?.CalmMinInterval ?? 20f;
        var maxInterval = proto?.CalmMaxInterval ?? 60f;
        var delay = _random.NextFloat(minInterval, maxInterval);
        _nextCalmPlayTime = _timing.CurTime + TimeSpan.FromSeconds(delay);
    }

    // ── Боевая музыка ─────────────────────────────────────────────────────────

    private void PlayCombatTrack()
    {
        var proto = GetProto();
        if (proto == null || proto.CombatTracks.Count == 0)
            return;

        var track = _random.Pick(proto.CombatTracks);
        _currentStream = _audio.PlayGlobal(
            track,
            Filter.Local(),
            false,
            AudioParams.Default.WithVolume(VolumeFromLinear(_volume)).WithLoop(true)
        )?.Entity;

        if (_currentStream != null)
            _currentType = DutyMusicType.Combat;
    }

    // ── Вспомогательные методы ────────────────────────────────────────────────

    /// <summary>
    /// Останавливает текущий трек.
    /// </summary>
    /// <param name="immediate">Если true — мгновенная остановка. Если false — плавный fade-out.</param>
    private void StopCurrent(bool immediate = false)
    {
        if (_currentStream == null)
            return;

        if (immediate)
            _audio.Stop(_currentStream);
        else
        {
            var proto = GetProto();
            var duration = _currentType switch
            {
                DutyMusicType.Combat => proto?.CombatFadeOutDuration ?? 2.5f,
                DutyMusicType.Calm => proto?.CalmFadeOutDuration ?? 2.5f,
                _ => 2.5f
            };

            _contentAudio.FadeOut(_currentStream, duration: duration);
        }

        _currentStream = null;
        _currentType = DutyMusicType.None;
        _calmTrackPlaying = false;
    }

    /// <summary>
    /// Проверяет, находится ли entity в боевом режиме (нажата кнопка боевого режима).
    /// </summary>
    private bool IsInCombatMode(EntityUid entity)
    {
        return TryComp<CombatModeComponent>(entity, out var combat) && combat.IsInCombatMode;
    }

    /// <summary>
    /// Получает прототип с настройками треков.
    /// </summary>
    private DynamicAmbientMusicPrototype? GetProto()
    {
        if (_protoManager.TryIndex<DynamicAmbientMusicPrototype>(PrototypeId, out var proto))
            return proto;

        Logger.Warning($"[DynamicAmbientMusic] Прототип '{PrototypeId}' не найден!");
        return null;
    }

    /// <summary>
    /// Переводит линейное значение громкости (0.0–1.0) в децибелы для AudioParams.
    /// </summary>
    private static float VolumeFromLinear(float linear)
    {
        if (linear <= 0f)
            return -32f;
        return 20f * MathF.Log10(linear);
    }
}

/// <summary>
/// Тип музыки, которая сейчас воспроизводится системой.
/// </summary>
public enum DutyMusicType
{
    None,
    Calm,
    Combat,
}
