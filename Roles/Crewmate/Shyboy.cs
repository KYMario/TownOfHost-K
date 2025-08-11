using AmongUs.GameOptions;
using UnityEngine;
using System;
using TownOfHost.Roles.Core;
namespace TownOfHost.Roles.Crewmate;

public sealed class Shyboy : RoleBase
{
    public static readonly SimpleRoleInfo RoleInfo =
        SimpleRoleInfo.Create(
            typeof(Shyboy),
            player => new Shyboy(player),
            CustomRoles.Shyboy,
            () => RoleTypes.Engineer,
            CustomRoleTypes.Crewmate,
            11900,
            SetupOptionItem,
            "Sy",
            "#00fa9a",
            (8, 0),
            introSound: () => GetIntroSound(RoleTypes.Crewmate)
        );
    public Shyboy(PlayerControl player)
    : base(
        RoleInfo,
        player
    )
    {
        Notify = true;
        Shytime = OptionShytime.GetFloat();
        Notshy = OptionNotShy.GetFloat();
        Shydeath = 0;
        AfterMeeting = 0;
    }
    private static float Shytime; private static OptionItem OptionShytime;
    private static float Notshy; private static OptionItem OptionNotShy;
    public static OptionItem OptionShyDieBom;
    float Shydeath;
    float Cool;
    float AfterMeeting;
    bool Notify;
    float Last;
    float Shydeathdi;
    enum OptionName
    {
        ShyboyShytime,
        ShyboyAfterMeetingNotShytime,
        ShyboyBooooom
    }

    public override bool CanClickUseVentButton => false;
    private static void SetupOptionItem()
    {
        OptionShytime = FloatOptionItem.Create(RoleInfo, 10, OptionName.ShyboyShytime, new(0f, 15f, 0.5f), 5f, false);
        OptionNotShy = FloatOptionItem.Create(RoleInfo, 11, OptionName.ShyboyAfterMeetingNotShytime, new(0f, 30f, 1f), 10f, false);
        OptionShyDieBom = BooleanOptionItem.Create(RoleInfo, 12, OptionName.ShyboyBooooom, false, false)
        .SetInfo(GetString("AprilfoolOnly")).SetEnabled(() => Event.April || Event.Special);
    }
    public override void ApplyGameOptions(IGameOptions opt)
    {
        //ししゃごにゅー
        double Coold = Math.Round(Shytime + 1 / 4 - Shydeath);
        AURoleOptions.EngineerCooldown = (float)Coold;
        AURoleOptions.EngineerInVentMaxTime = 0;
    }
    public override void StartGameTasks() => Shydeathdi = MathF.Min(Player.Is(CustomRoles.Lighting) ? 4.5f * Main.DefaultImpostorVision : 4.5f * Main.DefaultCrewmateVision, 4);
    public override void OnStartMeeting()
    {
        Notify = true;
        Shydeath = 0;
        AfterMeeting = 0;
        StartGameTasks();
    }
    public override void OnFixedUpdate(PlayerControl player)
    {
        if (!AmongUsClient.Instance.AmHost) return;
        if (GameStates.CalledMeeting || GameStates.ExiledAnimate || !MyState.HasSpawned) return;
        if (!Player.IsAlive()) return;
        Cool += Time.fixedDeltaTime;
        if (0.25 < Cool)
        {
            Cool = 0;
            //シャイのクールｳﾙｾｪからログださない()()バグ起こったらここtrueか削除して探そう!!((((
            //30回に1回だけとかlog残すか考えたけど余計重くなりそう。
            var cooldown = (float)Math.Round(Shytime + 1 / 4 - Shydeath);
            if (Last != cooldown) //必要な時だけ送る
            {
                Last = cooldown;
                Player.MarkDirtySettings();
            }
            Player.RpcResetAbilityCooldown(log: false);
        }
        AfterMeeting += Time.fixedDeltaTime;

        if (GameStates.IsInTask && Notshy <= AfterMeeting - 5)
        {
            if (Notify)
            {
                Notify = false;
                Player.RpcProtectedMurderPlayer();
            }

            Vector2 GSpos = player.transform.position;
            bool Hito = false;
            foreach (var pc in PlayerCatch.AllAlivePlayerControls)
            {
                if (pc != player)
                {
                    float HitoDistance = Vector2.Distance(GSpos, pc.transform.position);
                    var vector = (Vector2)pc.transform.position - GSpos;
                    float dis = vector.magnitude;
                    if (HitoDistance <= Shydeathdi && !PhysicsHelpers.AnyNonTriggersBetween(GSpos, pc.transform.position, dis, Constants.ShadowMask))
                    {
                        Hito = true;
                        break;
                    }
                }
            }
            if (Hito)//周囲に人がいる状況
            {
                Shydeath += Time.fixedDeltaTime;
            }
            else
            {
                Shydeath -= Time.fixedDeltaTime * 1 / 4;//周囲に人がいないとカウントをちょっとずつ減らす
            }

            if (Shydeath <= -0.25f)//値がマイナスにならないようにする
            {
                Shydeath = 0;
            }

            if (Shytime <= Shydeath)
            {
                Logger.Info("もぉみんなかまうからシャイ君しんぢゃったぁ～!", "Shyboy");
                MyState.DeathReason = CustomDeathReason.Suicide;
                Player.RpcMurderPlayer(Player);//一定時間周囲に人がいたら恥ずかしくて死ぬ。
                Shydeath = -1;//0sの無限キル防止(おきないだろうけど)
                if ((Event.April || Event.Special) && OptionShyDieBom.GetBool())
                    foreach (var pc in PlayerCatch.AllAlivePlayerControls)
                    {
                        if (pc != player)
                        {
                            float HitoDistance = Vector2.Distance(GSpos, pc.transform.position);
                            var vector = (Vector2)pc.transform.position - GSpos;
                            float dis = vector.magnitude;
                            if (HitoDistance <= Shydeathdi && !PhysicsHelpers.AnyNonTriggersBetween(GSpos, pc.transform.position, dis, Constants.ShipAndObjectsMask))
                            {
                                if (CustomRoleManager.OnCheckMurder(Player, pc, pc, pc, true, true, 10))
                                    pc.PlayerId.GetPlayerState().DeathReason = CustomDeathReason.Bombed;

                                Logger.Info($"Booooooooooooom! => {pc.Data.GetLogPlayerName()}", "ShyboyDie");
                            }
                        }
                    }
            }
        }
    }
    public override bool AllEnabledColor => true;
    public override bool OnEnterVent(PlayerPhysics physics, int ventId) => false;
    public override string GetAbilityButtonText() => GetString("ShyBoyText");
    public override bool OverrideAbilityButton(out string text)
    {
        text = "ShyBoy_Ability";
        return true;
    }
}