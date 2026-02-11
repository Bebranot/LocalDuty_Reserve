using Robust.Shared;
using Robust.Shared.Configuration;

namespace Content.Server._Reserve.LenaApi;

/// <summary>
///     Reserve API cvars
/// </summary>
[CVarDefs]
public sealed class LenaApiCVars : CVars
{
    public static readonly CVarDef<string> ApiKey =
        CVarDef.Create("lena.api_key", "", CVar.CONFIDENTIAL | CVar.SERVERONLY);

    public static readonly CVarDef<bool> ApiIntegration =
        CVarDef.Create("lena.api_integration", false, CVar.SERVERONLY);

    public static readonly CVarDef<bool> RequireAuth =
        CVarDef.Create("lena.require_auth", true, CVar.SERVERONLY);

    public static readonly CVarDef<string> AuthUri =
        CVarDef.Create("lena.auth_uri", "https://lena.reserve-station.space/v1/auth/login", CVar.SERVERONLY);

    public static readonly CVarDef<string> BaseUri =
        CVarDef.Create("lena.base_uri", "https://lena.reserve-station.space/v1", CVar.SERVERONLY);
}
