using System.Numerics;

using Content.Shared.Actions.Components;
using Robust.Shared.Map;
using Robust.Shared.Network;

using Content.Shared.Chat;
using Content.Server.Chat.Systems;
using Timer = Robust.Shared.Timing.Timer;

using Content.Shared._LocalDuty.f1x4Blades;

using Content.Shared.Hands.Components;
using Robust.Shared.Containers;

using Robust.Shared.Physics.Components;
using Robust.Shared.Physics.Systems;

using Content.Shared.Projectiles;

using Robust.Shared.Prototypes;

using Content.Shared.Movement.Components;
using Content.Shared.Movement.Systems;

namespace Content.Server._LocalDuty.f1x4Blades;

public sealed class f1x4BladesActionSystem : EntitySystem
{
    [Dependency] private readonly SharedTransformSystem _xform = default!;
    [Dependency] private readonly INetManager _net = default!;

    [Dependency] private readonly ChatSystem _chat = default!;

    [Dependency] private readonly SharedContainerSystem _containers = default!;


    [Dependency] private readonly IPrototypeManager _protos = default!;
    [Dependency] private readonly SharedPhysicsSystem _physics = default!;

    [Dependency] private readonly MovementSpeedModifierSystem _movement = default!;



    private const string MassInfectionProjectileProto = "f1x4MassInfectionProjectile";
    private const string EntanglementProjectileProto = "f1x4EntanglementProjectile";

    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<ActionsComponent, f1x4BladesMassInfectionEvent>(OnMassInfection);
        SubscribeLocalEvent<ActionsComponent, f1x4BladesEntanglementEvent>(OnEntanglement);
    }

    private void OnMassInfection(Entity<ActionsComponent> ent, ref f1x4BladesMassInfectionEvent args)
    {
        //if (_net.IsClient // PredictedSpawn doesn't support spawning entities without initializing them yet...
        //  || args.Handled
        //|| _xform.GetGrid(args.Target) == null)
        //return;

        if (args.Handled
            || !args.Target.IsValid(EntityManager))
            return;

        var user = args.Performer;

        if (!TryComp<MovementSpeedModifierComponent>(user, out var speed))
            return;


        var originalWalk = speed.BaseWalkSpeed;
        var originalSprint = speed.BaseSprintSpeed;
        var originalAcceleration = speed.Acceleration;

        _movement.ChangeBaseSpeed(user, 0f, 0f, 0f);

        // координаты спавна
        var userXform = Transform(user);
        var spawnCoords = userXform.Coordinates;

        // направление
        //var direction = GetDirection(user, args.Target);

        // сохраняем исходный поворот
        var originalRotation = userXform.WorldRotation;

        var animationOffset = Angle.FromDegrees(-90);

        // поворачиваем вправо
        userXform.WorldRotation += animationOffset;


        _chat.TrySendInGameICMessage(user, "THIS IS...", InGameICChatType.Speak, true, true, checkRadioPrefix: false);
        Timer.Spawn(TimeSpan.FromSeconds(2.5), () =>
        {
            _chat.TrySendInGameICMessage(user, "MASS INFECTION!", InGameICChatType.Speak, true, true, checkRadioPrefix: false);

            var direction = GetDirectionFromRotation(user);

            // спавним проджектайл
            var projectile = EntityManager.SpawnEntity(
                MassInfectionProjectileProto,
                spawnCoords
            );

            // поворот
            var projXform = Transform(projectile);

            //if (AngleAlmostEqual((originalRotation /*- animationOffset*/), direction.ToAngle()))
            //{
              //  projXform.WorldRotation = originalRotation;
            //} else
            //{
                projXform.WorldRotation = direction.ToAngle();//Transform(user).WorldRotation;//direction.ToAngle();
            //}

            // скорость
            if (TryComp<PhysicsComponent>(projectile, out var physics))
            {
                const float speed = 15f;
                _physics.SetLinearVelocity(projectile, direction * speed);
            }

            // стрелок
            if (TryComp<ProjectileComponent>(projectile, out var proj))
            {
                proj.Shooter = user;
            }

            // возвращаем поворот
            if (EntityManager.EntityExists(user))
            {
                Transform(user).WorldRotation = originalRotation;
                _movement.ChangeBaseSpeed(user, originalWalk, originalSprint, originalAcceleration);
            }
        });

        args.Handled = true;
    }

    private void OnEntanglement(Entity<ActionsComponent> ent, ref f1x4BladesEntanglementEvent args)
    {
        if (args.Handled
            || !args.Target.IsValid(EntityManager))
            return;


        var user = args.Performer;

        if (!TryComp<MovementSpeedModifierComponent>(user, out var speed))
            return;


        var originalWalk = speed.BaseWalkSpeed;
        var originalSprint = speed.BaseSprintSpeed;
        var originalAcceleration = speed.Acceleration;

        _movement.ChangeBaseSpeed(user, 0f, 0f, 0f);

 
        var userXform = Transform(user);
        var spawnCoords = userXform.Coordinates;


        var direction = GetDirection(user, args.Target);


        var originalRotation = userXform.WorldRotation;

        for (int i = 0; i < 4; i++)
        {
            var iCopy = i;
            Timer.Spawn(TimeSpan.FromMilliseconds(120 * iCopy), () =>
            {
                userXform.WorldRotation -= 90;
                if (iCopy == 3)
                {
                    userXform.WorldRotation += 90;
                    var projectile = EntityManager.SpawnEntity(
                    EntanglementProjectileProto,
                    spawnCoords
                    );

                    var projXform = Transform(projectile);
                    projXform.WorldRotation = direction.ToAngle();

                    if (TryComp<PhysicsComponent>(projectile, out var physics))
                    {
                        const float speedV = 10f;
                        _physics.SetLinearVelocity(projectile, direction * speedV);
                    }

                    if (TryComp<ProjectileComponent>(projectile, out var proj))
                    {
                        proj.Shooter = user;
                    }

                    if (EntityManager.EntityExists(user))
                    {
                        Transform(user).WorldRotation = originalRotation;
                        _movement.ChangeBaseSpeed(user, originalWalk, originalSprint, originalAcceleration);
                    }
                }
            });
        }

        args.Handled = true;
    }

    private Vector2 GetDirection(EntityUid user, EntityCoordinates targetCoords)
    {
        var userPos = _xform.GetWorldPosition(user);
        var targetPos = _xform.ToMapCoordinates(targetCoords).Position;

        Logger.GetSawmill("getDirection").Info($"her {(targetPos - userPos).Normalized()}");

        return (targetPos - userPos).Normalized();
    }

    private Vector2 GetDirectionFromRotation(EntityUid user)
    {
        var rot = Transform(user).WorldRotation;
        return rot.RotateVec(-Vector2.UnitY).Normalized();
    }

    private bool AngleAlmostEqual(Angle a, Angle b, float epsilon = 0.1f)
    {
        Logger.GetSawmill("anglealmostequal").Info($"her {a} {b}");

        return MathF.Abs((float) (MathHelper.DegreesToRadians(a - b))) < epsilon;
    }
}
