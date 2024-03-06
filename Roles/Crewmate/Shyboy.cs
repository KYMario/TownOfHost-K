using AmongUs.GameOptions;
using UnityEngine;
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
            22015,
            SetupOptionItem,
            "Sy",
            "#00fa9a",
            introSound: () => GetIntroSound(RoleTypes.Crewmate)
        );
    public Shyboy(PlayerControl player)
    : base(
        RoleInfo,
        player
    )
    {
        Shytime = OptionShytime.GetFloat();
        Notshy = OptionNotShy.GetFloat();
    }
    private static OptionItem OptionShytime;
    private static OptionItem OptionNotShy;
    float Shydeath;
    float Cool;
    float AfterMeeting;
    private static float Notshy;
    enum OptionName
    {
        Shytime,
        NotShy
    }
    private static float Shytime;
    private static void SetupOptionItem()
    {
        OptionShytime = FloatOptionItem.Create(RoleInfo, 10, OptionName.Shytime, new(0f, 15f, 0.5f), 5f, false);
        OptionNotShy = FloatOptionItem.Create(RoleInfo, 11, OptionName.NotShy, new(0f, 30f, 1f), 10f, false);
    }
    public override void ApplyGameOptions(IGameOptions opt)
    {
        AURoleOptions.EngineerCooldown = Shytime + 1 / 4 - Shydeath;
        AURoleOptions.EngineerInVentMaxTime = 0;
    }
    public override bool OnEnterVent(PlayerPhysics physics, int ventId)
    {
        return false;
    }
    public override void OnFixedUpdate(PlayerControl player)
    {
        if (!AmongUsClient.Instance.AmHost) return;
        Cool += Time.fixedDeltaTime;
        if (Player.IsAlive() && Cool >= 0.25)
        {
            Cool = 0;
            player.RpcResetAbilityCooldown();
            Player.SyncSettings();
        }
        AfterMeeting += Time.fixedDeltaTime;
        var Shydeathdi = 5 * Main.DefaultCrewmateVision;
        if (GameStates.IsInTask && Player.IsAlive() && Notshy <= AfterMeeting - 5)
        {
            Vector2 GSpos = player.transform.position;
            PlayerControl Hito = null;
            foreach (var pc in Main.AllAlivePlayerControls)
            {
                if (pc != player)
                {
                    float HitoDistance = Vector2.Distance(GSpos, pc.transform.position);
                    if (HitoDistance <= Shydeathdi && player.CanMove && pc.CanMove)
                    {
                        Hito = pc;
                        break;
                    }
                }
            }
            if (Hito != null)//周囲に人がいる状況
            {
                Shydeath += Time.fixedDeltaTime;
            }
            else
            {
                Shydeath -= Time.fixedDeltaTime * 1 / 4;//周囲に人がいないとカウントをちょっとずつ減らす
            }

            if (Shydeath <= -1)//値がマイナスにならないようにする
            {
                Shydeath = 0;
            }
            /*if (Shydeath >= Shytime * 0.75 && Hito != null)//&& Shydeath <= Shytime * 0.85 &&
            {
                KillFlash += Time.fixedDeltaTime; ;
                if (KillFlash >= Options.KillFlashDuration.GetFloat() * 4)//連続で撃ってたらうっとおしいからキルフラ*4に一回
                {
                    var seer = Player;
                    seer.KillFlash(false);
                    KillFlash = 0;
                }
            }*/
            //可視化したから消す。

            if (Shytime <= Shydeath)
            {
                PlayerState.GetByPlayerId(Player.PlayerId).DeathReason = CustomDeathReason.Suicide;
                Player.RpcMurderPlayer(Player);//一定時間周囲に人がいたら恥ずかしくて死ぬ。
                Shydeath = 0;//0sの無限キル防止(おきないだろうけど)
            }
        }
    }
    public override void AfterMeetingTasks()
    {
        Logger.Info("シャイクールを直す", "Shyboy");
        Shydeath = 0;//会議明け修正
        AfterMeeting = 0;
    }
    //いつか可視化を顔文字でしたい!!!
}
