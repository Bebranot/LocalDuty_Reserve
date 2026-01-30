using Content.Shared.Damage;
using Content.Shared.Projectiles;
using Content.Shared.Weapons.Melee.Events;
using Content.Shared.Whitelist;
using Robust.Shared.Audio.Systems;
using Robust.Shared.Network;
using Robust.Shared.Physics.Events;
using Robust.Shared.Timing;
using Content.Shared._Shitmed.Targeting;


using Content.Shared._Shitmed.Damage;


using Content.Shared.Containers.ItemSlots;
using Content.Shared.Weapons.Melee.Components;


using Robust.Shared.Prototypes;

using Robust.Shared.Map;

using Content.Shared.Coordinates.Helpers;


using Content.Shared.Movement.Components;
using Content.Shared.Movement.Systems;


namespace Content.Shared._LocalDuty.f1x4Blades;

public sealed class Sharedf1x4BladesEffectSystem : EntitySystem
{
    [Dependency] private readonly EntityWhitelistSystem _whitelistSystem = default!;

    [Dependency] private readonly MovementSpeedModifierSystem _movement = default!;


    public override void Initialize()
    {
        base.Initialize();
        SubscribeLocalEvent<f1x4BladesEffectComponent, ProjectileHitEvent>(OnProjectileHit);
    }

    private void OnProjectileHit(
        EntityUid uid,
        f1x4BladesEffectComponent component,
        ref ProjectileHitEvent args)
    {
       // Logger.GetSawmill("f1x4").Info("Projectile hit triggered");

        var target = args.Target;

        if (!TryComp<MovementSpeedModifierComponent>(target, out var speed))
            return;


        _movement.ChangeBaseSpeed(
            target,
            speed.BaseWalkSpeed - 1.5f,
            speed.BaseSprintSpeed - 1.5f,
            speed.Acceleration - 1.5f
        );

        Timer.Spawn(component.Duration, () =>
        {
            if (!Deleted(target))
            {
                _movement.ChangeBaseSpeed(
                    target,
                    speed.BaseWalkSpeed + 1.5f,
                    speed.BaseSprintSpeed + 1.5f,
                    speed.Acceleration + 1.5f
                );
            }
        });
    }

}
