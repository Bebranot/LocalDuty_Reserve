//using Content.Shared._vg.TileMovement;
//using Content.Shared.Alert;
//using Content.Shared.Movement.Systems;
//using Robust.Shared.Prototypes;
using Content.Server._LocalDuty.FuckingSvistok.components;
using Content.Shared.Chat;
using Content.Server.Chat.Systems;
using Content.Shared.Interaction.Events;
using Content.Shared._LocalDuty.FuckingSvistok.components;

using Timer = Robust.Shared.Timing.Timer;

using Robust.Shared.Random;

using Robust.Server.Audio;
using Robust.Shared.Audio;
using Robust.Shared.Audio.Systems;

namespace Content.Server._LocalDuty.FuckingSvistok;

public sealed class FuckingSvistokSystem : EntitySystem
{
    [Dependency] private readonly ChatSystem _chat = default!;
    [Dependency] private readonly IRobustRandom _random = default!;

    [Dependency] private readonly SharedAudioSystem _audio = default!;

    private static readonly SoundSpecifier[] UseSounds =
    {
        new SoundPathSpecifier("/Audio/_Lavaland/Mobs/Bosses/hiero_blast.ogg"),
        new SoundPathSpecifier("/Audio/Animals/cat_hiss.ogg"),
        new SoundPathSpecifier("/Audio/Effects/Fluids/glug.ogg"),
    };

    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<FuckingSvistokComponent, UseInHandEvent>(OnFuckingUse);
    }

    private void OnFuckingUse(EntityUid uid, FuckingSvistokComponent comp, ref UseInHandEvent args)
    {
        //if (args.H)
        //_alertsSystem.ShowAlert(uid, component.HierophantBeatAlertId);
        EnsureComp<FuckingSvistokEffectComponent>(args.User);
        int randomMessageNumber = _random.Next(1, 7);
        string randomMessage = "";

        switch (randomMessageNumber)
        {
            case 1:
                randomMessage = "IT'S A FUCKING SVISTOK PUSI BOY!!!";
                break;
            case 2:
                randomMessage = "OHH SHIT THAT THE HELL";
                break;
            case 3:
                randomMessage = "FUCK U MAMA";
                break;
            case 4:
                randomMessage = "ВПЕРЁД, БРАТЬЯ, ВПЕРЁЁЁЁЁД!!";
                break;
            case 5:
                randomMessage = "ВПЕРЁЁЁД, В ААТААКУУУ!!";
                break;
            case 6:
                randomMessage = "АТАКУЕМ, АТАКУУУЕМ!!";
                break;
            default:
                randomMessage = "ВПЕРЁЁЁЁЁД, В АТААКУ!!";
                break;
        }

        var sound = _random.Pick(UseSounds);

        _audio.PlayPvs(sound, uid);

        _chat.TrySendInGameICMessage(args.User, randomMessage, InGameICChatType.Speak, true, true, checkRadioPrefix: false);
        EntityUid user = args.User;
        Timer.Spawn(TimeSpan.FromSeconds(30), () =>
        {
            if (HasComp<FuckingSvistokEffectComponent>(user)) {
                RemComp<FuckingSvistokEffectComponent>(user);
            }
        });
    }
}
