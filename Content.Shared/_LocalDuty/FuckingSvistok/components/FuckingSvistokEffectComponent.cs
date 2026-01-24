using Content.Shared.Alert;
using Robust.Shared.GameStates;
using Robust.Shared.Prototypes;

namespace Content.Shared._LocalDuty.FuckingSvistok.components;

[RegisterComponent, NetworkedComponent]
public sealed partial class FuckingSvistokEffectComponent : Component
{
    [DataField]
    public float MovementSpeedBuff = 1.15f;

    [DataField]
    public ProtoId<AlertPrototype> FuckingSvistokAlertId = "FuckingSvistok";
}
