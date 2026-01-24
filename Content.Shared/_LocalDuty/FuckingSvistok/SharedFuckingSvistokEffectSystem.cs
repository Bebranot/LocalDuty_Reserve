using Content.Shared.Alert;
using Content.Shared.Movement.Systems;
using Robust.Shared.Prototypes;
using Content.Shared._LocalDuty.FuckingSvistok.components;

namespace Content.Shared._LocalDuty.FuckingSvistok;

public sealed class FuckingSvistokEffectSystem : EntitySystem
{
    [Dependency] private readonly AlertsSystem _alertsSystem = default!;
    [Dependency] private readonly MovementSpeedModifierSystem _movementSpeed = default!;

    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<FuckingSvistokEffectComponent, ComponentStartup>(OnStartup);
        SubscribeLocalEvent<FuckingSvistokEffectComponent, ComponentRemove>(OnRemove);
        SubscribeLocalEvent<FuckingSvistokEffectComponent, RefreshMovementSpeedModifiersEvent>(OnRefreshSpeed);
    }

    private void OnStartup(EntityUid uid, FuckingSvistokEffectComponent component, ref ComponentStartup args)
    {
        _alertsSystem.ShowAlert(uid, component.FuckingSvistokAlertId);
        _movementSpeed.RefreshMovementSpeedModifiers(uid);
    }

    private void OnRemove(EntityUid uid, FuckingSvistokEffectComponent component, ref ComponentRemove args)
    {
        _alertsSystem.ClearAlert(uid, component.FuckingSvistokAlertId);
        _movementSpeed.RefreshMovementSpeedModifiers(uid);
    }

    private void OnRefreshSpeed(EntityUid uid, FuckingSvistokEffectComponent component, ref RefreshMovementSpeedModifiersEvent args)
        => args.ModifySpeed(component.MovementSpeedBuff, component.MovementSpeedBuff);
}
