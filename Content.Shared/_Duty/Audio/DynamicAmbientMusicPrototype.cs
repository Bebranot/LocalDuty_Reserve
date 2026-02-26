// SPDX-FileCopyrightText: 2025 LocalDuty <https://github.com/Bebranot/LocalDuty_Reserve>
//
// SPDX-License-Identifier: AGPL-3.0-or-later

using Robust.Shared.Audio;
using Robust.Shared.Prototypes;

namespace Content.Shared.Duty.Audio;

/// <summary>
/// Прототип, описывающий набор треков для динамической фоновой музыки.
/// Задаёт списки спокойных и боевых треков, а также параметры воспроизведения.
/// </summary>
[Prototype("dynamicAmbientMusic")]
public sealed class DynamicAmbientMusicPrototype : IPrototype
{
    [IdDataField]
    public string ID { get; } = default!;

    /// <summary>
    /// Список спокойных треков. Один из них будет выбран случайно.
    /// </summary>
    [DataField(required: true)]
    public List<SoundSpecifier> CalmTracks = new();

    /// <summary>
    /// Список боевых треков. Один из них будет выбран случайно.
    /// </summary>
    [DataField(required: true)]
    public List<SoundSpecifier> CombatTracks = new();

    /// <summary>
    /// Минимальная пауза (в секундах) между спокойными треками.
    /// </summary>
    [DataField]
    public float CalmMinInterval = 20f;

    /// <summary>
    /// Максимальная пауза (в секундах) между спокойными треками.
    /// </summary>
    [DataField]
    public float CalmMaxInterval = 60f;

        /// <summary>
        /// Длительность fade-in спокойной музыки (в секундах) при старте трека.
        /// </summary>
        [DataField]
        public float CalmFadeInDuration = 2.5f;

        /// <summary>
        /// Длительность fade-out спокойной музыки (в секундах) при остановке трека.
        /// </summary>
        [DataField]
        public float CalmFadeOutDuration = 2.5f;

    /// <summary>
    /// Длительность fade-out боевой музыки (в секундах) при выходе из боевого режима.
    /// </summary>
    [DataField]
    public float CombatFadeOutDuration = 2.5f;

        /// <summary>
        /// Длительность fade-in боевой музыки (в секундах) при входе в боевой режим.
        /// </summary>
        [DataField]
        public float CombatFadeInDuration = 2.5f;
}
