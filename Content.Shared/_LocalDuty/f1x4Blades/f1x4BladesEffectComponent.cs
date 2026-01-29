
using Content.Shared.Whitelist;
using Robust.Shared.GameStates;
using Robust.Shared.Audio;
using Robust.Shared.Utility;

namespace Content.Shared._LocalDuty.f1x4Blades;

[RegisterComponent, NetworkedComponent, AutoGenerateComponentState, Access(typeof(Sharedf1x4BladesEffectSystem))]
public sealed partial class f1x4BladesEffectComponent : Component
{
    [DataField("whitelist"), AutoNetworkedField]
    public EntityWhitelist? Whitelist = new();

    [ViewVariables(VVAccess.ReadWrite), DataField("duration"), AutoNetworkedField]
    public TimeSpan Duration = TimeSpan.FromSeconds(5);
}
