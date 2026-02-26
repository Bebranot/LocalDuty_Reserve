// SPDX-FileCopyrightText: 2025 LocalDuty <https://github.com/Bebranot/LocalDuty_Reserve>
//
// SPDX-License-Identifier: AGPL-3.0-or-later

using Robust.Shared.Configuration;

namespace Content.Shared.CCVar;

/// <summary>
/// LocalDuty-specific CVars.
/// </summary>
[CVarDefs]
public sealed partial class DutyCCVars
{
    #region DynamicAmbientMusic

    /// <summary>
    /// Включить/выключить систему динамической фоновой музыки.
    /// </summary>
    public static readonly CVarDef<bool> DynamicAmbientMusicEnabled =
        CVarDef.Create("duty.dynamic_ambient_music_enabled", true, CVar.CLIENTONLY | CVar.ARCHIVE);

    /// <summary>
    /// Громкость динамической фоновой музыки (0.0 - 1.0).
    /// </summary>
    public static readonly CVarDef<float> DynamicAmbientMusicVolume =
        CVarDef.Create("duty.dynamic_ambient_music_volume", 0.12f, CVar.CLIENTONLY | CVar.ARCHIVE);

    #endregion
}
