// SPDX-FileCopyrightText: 2023 AJCM-git <60196617+AJCM-git@users.noreply.github.com>
// SPDX-FileCopyrightText: 2023 Kara <lunarautomaton6@gmail.com>
// SPDX-FileCopyrightText: 2023 Pieter-Jan Briers <pieterjan.briers@gmail.com>
// SPDX-FileCopyrightText: 2023 metalgearsloth <31366439+metalgearsloth@users.noreply.github.com>
// SPDX-FileCopyrightText: 2024 Pieter-Jan Briers <pieterjan.briers+git@gmail.com>
// SPDX-FileCopyrightText: 2024 Plykiya <58439124+Plykiya@users.noreply.github.com>
// SPDX-FileCopyrightText: 2024 Tayrtahn <tayrtahn@gmail.com>
// SPDX-FileCopyrightText: 2024 plykiya <plykiya@protonmail.com>
// SPDX-FileCopyrightText: 2025 Aiden <28298836+Aidenkrz@users.noreply.github.com>
// SPDX-FileCopyrightText: 2025 Aidenkrz <aiden@djkraz.com>
// SPDX-FileCopyrightText: 2025 Aineias1 <dmitri.s.kiselev@gmail.com>
// SPDX-FileCopyrightText: 2025 FaDeOkno <143940725+FaDeOkno@users.noreply.github.com>
// SPDX-FileCopyrightText: 2025 GoobBot <uristmchands@proton.me>
// SPDX-FileCopyrightText: 2025 McBosserson <148172569+McBosserson@users.noreply.github.com>
// SPDX-FileCopyrightText: 2025 Milon <plmilonpl@gmail.com>
// SPDX-FileCopyrightText: 2025 Piras314 <p1r4s@proton.me>
// SPDX-FileCopyrightText: 2025 Rouden <149893554+Roudenn@users.noreply.github.com>
// SPDX-FileCopyrightText: 2025 Roudenn <romabond091@gmail.com>
// SPDX-FileCopyrightText: 2025 SX-7 <sn1.test.preria.2002@gmail.com>
// SPDX-FileCopyrightText: 2025 SolsticeOfTheWinter <solsticeofthewinter@gmail.com>
// SPDX-FileCopyrightText: 2025 TheBorzoiMustConsume <197824988+TheBorzoiMustConsume@users.noreply.github.com>
// SPDX-FileCopyrightText: 2025 Unlumination <144041835+Unlumy@users.noreply.github.com>
// SPDX-FileCopyrightText: 2025 coderabbitai[bot] <136622811+coderabbitai[bot]@users.noreply.github.com>
// SPDX-FileCopyrightText: 2025 deltanedas <39013340+deltanedas@users.noreply.github.com>
// SPDX-FileCopyrightText: 2025 deltanedas <@deltanedas:kde.org>
// SPDX-FileCopyrightText: 2025 gluesniffler <159397573+gluesniffler@users.noreply.github.com>
// SPDX-FileCopyrightText: 2025 gluesniffler <linebarrelerenthusiast@gmail.com>
// SPDX-FileCopyrightText: 2025 username <113782077+whateverusername0@users.noreply.github.com>
// SPDX-FileCopyrightText: 2025 whateverusername0 <whateveremail>
//
// SPDX-License-Identifier: AGPL-3.0-or-later

using Content.Shared.Damage;
using Content.Shared.Projectiles;
using Content.Shared.Weapons.Melee.Events;
using Content.Shared.Whitelist;
using Robust.Shared.Audio.Systems;
using Robust.Shared.Network;
using Robust.Shared.Physics.Events;
using Robust.Shared.Timing;
using Content.Shared._Shitmed.Targeting; // Shitmed Change
// Lavaland Change
using Content.Shared._Lavaland.Weapons.Marker;
using Content.Shared._Lavaland.Mobs;
using Content.Shared._Shitmed.Damage; // Shitmed Change



using Content.Shared.Containers.ItemSlots;
using Content.Shared.Weapons.Melee.Components;
using Content.Shared._Lavaland.Weapons.Crusher.Upgrades.Components;
using Content.Shared._Lavaland.Weapons.Crusher;

//using Content.Shared.Actions;

using Content.Shared._Lavaland.Damage;

using Robust.Shared.Prototypes;

using Robust.Shared.Map;

using Content.Shared.Coordinates.Helpers;


using Content.Shared._Lavaland.Weapons.Crusher.Crests.Components;


namespace Content.Shared.Weapons.Marker;

public abstract class SharedDamageMarkerSystem : EntitySystem
{
    [Dependency] private readonly IGameTiming _timing = default!;
    [Dependency] private readonly INetManager _netManager = default!;
    [Dependency] private readonly SharedAudioSystem _audio = default!;
    [Dependency] private readonly DamageableSystem _damageable = default!;
    [Dependency] private readonly EntityWhitelistSystem _whitelistSystem = default!;


    [Dependency] private readonly IMapManager _mapMan = default!;

    private readonly EntProtoId _chaserPrototype = "LavalandHierophantChaser";

    public override void Initialize()
    {
        base.Initialize();
        SubscribeLocalEvent<DamageMarkerOnCollideComponent, StartCollideEvent>(OnMarkerCollide);
        SubscribeLocalEvent<DamageMarkerComponent, AttackedEvent>(OnMarkerAttacked);
    }

    private void OnMarkerAttacked(EntityUid uid, DamageMarkerComponent component, AttackedEvent args)
    {
        if (component.Marker != args.Used)
            return;

        args.BonusDamage += component.Damage;
        _audio.PlayPredicted(component.Sound, uid, args.User);

        if (TryComp<LeechOnMarkerComponent>(args.Used, out var leech))
            _damageable.TryChangeDamage(args.User, leech.Leech, true, false, origin: args.Used, targetPart: TargetBodyPart.All, splitDamage: SplitDamageBehavior.SplitEnsureAll); // Shitmed Change

        if (HasComp<DamageBoostOnMarkerComponent>(args.Used))
        {
            RaiseLocalEvent(uid, new ApplyMarkerBonusEvent(args.Used, args.User)); // For effects on the target
            RaiseLocalEvent(args.Used, new ApplyMarkerBonusEvent(args.Used, args.User)); // For effects on the weapon
        }

        RemCompDeferred<DamageMarkerComponent>(uid);

        if (TryComp<ItemSlotsComponent>(component.Marker, out var slots))
        {
            foreach (var slot in slots.Slots.Values)
            {
                if (slot.Whitelist?.Tags?.Contains("CrusherCrest") != true)
                    continue;

                if (slot.Item is not EntityUid crestEntity)
                    continue;

                if (!TryComp<ItemSlotsComponent>(crestEntity, out var crestSlots))
                    continue;

                foreach (var innerSlot in crestSlots.Slots.Values)
                {
                    if (innerSlot.Item is not EntityUid upgradeEntity)
                        continue;

                    if (TryComp<CrusherUpgradeHierophantComponent>(upgradeEntity, out var hierophant))
                    {
                        // var damage = (int) Math.Round(damageable.TotalDamage.Float() / 10.0);
                        var finalMaxSteps = 7; // club.ChaserMaxSteps; // + damage;

                        //AddImmunity(args.User, 70f);

                        var xform = Transform(uid);
                        var targetCoords = xform.Coordinates.SnapToGrid(EntityManager, _mapMan);

                        var dummy = Spawn(null, targetCoords);


                        var chaser = Spawn(_chaserPrototype, Transform(args.User).Coordinates);

                        if (TryComp<HierophantChaserSharedComponent>(chaser, out var chasercomp))
                        {
                            chasercomp.Target = dummy;
                            chasercomp.MaxSteps *= finalMaxSteps;
                            chasercomp.Speed += 0.5f;
                        }

                        Timer.Spawn(TimeSpan.FromSeconds(finalMaxSteps + 100000), () =>
                        {
                            QueueDel(dummy);
                        });
                    }

                    if (TryComp<CrusherUpgradeVigilanteComponent>(upgradeEntity, out var vigilante))
                    {
                        if (!HasComp<VigilanteEyeComponent>(args.User))
                        {
                            EnsureComp<VigilanteEyeComponent>(args.User);
                            float bonus = (float) (component.EndTime - _timing.CurTime).TotalSeconds / 10f;
                            Timer.Spawn(TimeSpan.FromSeconds(vigilante.Lifetime + bonus), () =>
                            {
                                if (HasComp<VigilanteEyeComponent>(args.User))
                                    RemComp<VigilanteEyeComponent>(args.User);
                            });
                        }
                    }
                }
            }
        }
    }

    public override void Update(float frameTime)
    {
        base.Update(frameTime);

        var query = EntityQueryEnumerator<DamageMarkerComponent>();

        while (query.MoveNext(out var uid, out var comp))
        {
            if (comp.EndTime > _timing.CurTime)
                continue;

            RemCompDeferred<DamageMarkerComponent>(uid);
        }
    }

    private void OnMarkerCollide(EntityUid uid, DamageMarkerOnCollideComponent component, ref StartCollideEvent args)
    {
        if (!args.OtherFixture.Hard ||
            args.OurFixtureId != SharedProjectileSystem.ProjectileFixture ||
            component.Amount <= 0 ||
            _whitelistSystem.IsWhitelistFail(component.Whitelist, args.OtherEntity) ||
            !TryComp<ProjectileComponent>(uid, out var projectile) ||
            projectile.Weapon == null ||
            component.OnlyWorkOnFauna && // Lavaland Change
            !HasComp<FaunaComponent>(args.OtherEntity)) // Lavaland Change
        {
            return;
        }

        // Markers are exclusive, deal with it.
        var marker = EnsureComp<DamageMarkerComponent>(args.OtherEntity);
        marker.Damage = new DamageSpecifier(component.Damage);
        marker.Marker = projectile.Weapon.Value;
        marker.EndTime = _timing.CurTime + component.Duration;
        marker.Effect = component.Effect; // Pass the effect to the marker
        marker.Sound = component.Sound; // Pass the effect to the marker
        component.Amount--;

        Dirty(args.OtherEntity, marker);

        if (_netManager.IsServer)
        {
            if (component.Amount <= 0)
            {
                QueueDel(uid);
            }
            else
            {
                Dirty(uid, component);
            }
        }


        if (projectile.Weapon is { } weapon)
        {
            if (TryComp<ItemSlotsComponent>(weapon, out var slots))
            {
                foreach (var slot in slots.Slots.Values)
                {
                    if (slot.Whitelist?.Tags?.Contains("CrusherCrest") != true)
                        continue;

                    if (slot.Item is not EntityUid crestEntity)
                        continue;

                    if (!TryComp<ItemSlotsComponent>(crestEntity, out var crestSlots))
                        continue;

                    if (TryComp<CrusherCrestHunterComponent>(crestEntity, out var crestHunter))
                    {
                        marker.EndTime += TimeSpan.FromSeconds(5);
                    }

                    foreach (var innerSlot in crestSlots.Slots.Values)
                    {
                        if (innerSlot.Item is not EntityUid upgradeEntity)
                            continue;

                        if (TryComp<CrusherUpgradeDrakeComponent>(upgradeEntity, out var drake))
                        {
                            EnsureComp<MeleeThrowOnHitComponent>(weapon);
                            /*
                            var throwComp = EnsureComp<MeleeThrowOnHitComponent>(weapon);

                            throwComp.Speed = drake.Speed;
                            throwComp.Lifetime = drake.Lifetime;

                            Dirty(weapon, throwComp);
                            */
                            // мы делаем прикольчики

                            Timer.Spawn(marker.EndTime - _timing.CurTime,
                                () =>
                                {
                                    //Deferred
                                    RemComp<MeleeThrowOnHitComponent>(weapon);
                                });
                        }

                        if (TryComp<CrusherUpgradeHivelordComponent>(upgradeEntity, out var hivelord))
                        {
                            marker.EndTime += TimeSpan.FromSeconds(5);
                        }

                        if (TryComp<CrusherUpgradeWatcherComponent>(upgradeEntity, out var watcher))
                        {
                            var target = args.OtherEntity;
                            var lifetime = watcher.Lifetime + ((int) marker.EndTime.TotalSeconds - (int) _timing.CurTime.TotalSeconds);

                            EnsureComp<IcyLookComponent>(target);

                            Timer.Spawn(TimeSpan.FromSeconds(lifetime), () =>
                            {
                                RemComp<IcyLookComponent>(target);
                            });
                        }





                        if (TryComp<CrusherUpgradeCarpComponent>(upgradeEntity, out var carp))


                        {


                            var target = args.OtherEntity;


                            var lifetime = (int) marker.EndTime.TotalSeconds - (int) _timing.CurTime.TotalSeconds;





                            EnsureComp<CarpBloodComponent>(target);





                            Timer.Spawn(TimeSpan.FromSeconds(lifetime), () =>


                            {


                                RemComp<CarpBloodComponent>(target);


                            });


                        }
                    }
                }
            }
        }
    }

//    private void AddImmunity(EntityUid uid, float time = 3f)
  //  {
    //    EnsureComp<DamageSquareImmunityComponent>(uid).HasImmunityUntil = _timing.CurTime + TimeSpan.FromSeconds(time);
    //}
}
