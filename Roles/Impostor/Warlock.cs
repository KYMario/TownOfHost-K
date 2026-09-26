using System.Collections.Generic;
using System.Linq;
using AmongUs.GameOptions;
using Hazel;
using Rewired.Utils.Classes.Data;
using TMPro;
using TownOfHost.Roles.Core;
using TownOfHost.Roles.Core.Interfaces;
using UnityEngine;
using static Sentry.MeasurementUnit;
using static UnityEngine.GraphicsBuffer;

namespace TownOfHost.Roles.Impostor;

public sealed class Warlock : RoleBase, IImpostor, IUsePhantomButton
{
    public static readonly SimpleRoleInfo RoleInfo =
        SimpleRoleInfo.Create(
            typeof(Warlock),
            player => new Warlock(player),
            CustomRoles.Warlock,
            () => RoleTypes.Phantom,
            CustomRoleTypes.Impostor,
            5000,
            SetUpOptionItem,
            "wa",
            OptionSort: (4, 4),
            Desc: () => string.Format(!OptionCantmove.GetBool() ? GetString("WarlockDesc") : GetString("WarlockDescKoutyoku"), OptionCantMovetime.GetFloat()),
            from: From.TheOtherRoles
        );
    public Warlock(PlayerControl player)
    : base(
        RoleInfo,
        player
    )
    {
    }
    public override void OnDestroy()
    {
        CursedPlayer = null;
    }

    PlayerControl CursedPlayer;
    public static OptionItem Optiondouki;
    public static OptionItem OptionAbilityCoolDown;
    public static OptionItem OptionCantMovetime;
    public static OptionItem OptionCantmove;
    static OptionItem OptionCanKillNakama;
    bool IsCursed;
    enum OptionName
    {
        WarlockDouki,
        WarlockCantMovetime,
        WarlockKoutyoku,
        WarlockCanKillNakama
    }


    private static void SetUpOptionItem()
    {
        OptionAbilityCoolDown = FloatOptionItem.Create(RoleInfo, 10, GeneralOption.Cooldown, OptionBaseCoolTime, 20f, false)
            .SetValueFormat(OptionFormat.Seconds);
        OptionCantmove = BooleanOptionItem.Create(RoleInfo, 11, OptionName.WarlockKoutyoku, true, false);
        OptionCantMovetime = FloatOptionItem.Create(RoleInfo, 12, OptionName.WarlockCantMovetime, OptionBaseCoolTime, 5f, false, OptionCantmove)
            .SetValueFormat(OptionFormat.Seconds);
        OptionCanKillNakama = BooleanOptionItem.Create(RoleInfo, 13, OptionName.WarlockCanKillNakama, true, false);
        Optiondouki = BooleanOptionItem.Create(RoleInfo, 14, OptionName.WarlockDouki, true, false);
    }

    public override void Add()
    {
        CursedPlayer = null;
        IsCursed = false;
        Main.AllPlayerSpeed[Player.PlayerId] = Main.NormalOptions.PlayerSpeedMod;
    }

    public override string GetAbilityButtonText() => GetString("WarlockCurseButtonText");

    public override void ApplyGameOptions(IGameOptions opt)
    {
        AURoleOptions.PhantomCooldown = OptionAbilityCoolDown.GetFloat();
        AURoleOptions.PhantomDuration = 0.1f;
    }

    public override void AfterMeetingTasks()
    {
        Main.AllPlayerSpeed[Player.PlayerId] = Main.NormalOptions.PlayerSpeedMod;
    }

    bool IUsePhantomButton.IsresetAfterKill => Optiondouki.GetBool();
    bool IUsePhantomButton.IsPhantomRole => true;

    void IUsePhantomButton.OnClick(ref bool AdjustKillCooldown, ref bool? ResetCooldown)
    {
        AdjustKillCooldown = false;

        if (IsCursed && AmongUsClient.Instance.AmHost)
        {
            if (CursedPlayer != null && CursedPlayer.IsAlive())
            {
                Vector2 cpPos = CursedPlayer.transform.position;
                Dictionary<PlayerControl, float> candidateList = new();
                float distance;
                foreach (PlayerControl candidatePC in PlayerCatch.AllAlivePlayerControls)
                {
                    if (OptionCanKillNakama.GetBool())
                    {
                        if (candidatePC != CursedPlayer && !candidatePC.Is(CustomRoles.King) && !candidatePC.Is(CustomRoles.Autocrat))
                        {
                            distance = Vector2.Distance(cpPos, candidatePC.transform.position);
                            candidateList.Add(candidatePC, distance);
                            Logger.Info($"{candidatePC?.Data?.GetLogPlayerName()}の位置{distance}", "Warlock");
                        }
                    }
                    else
                    {
                        if (candidatePC != CursedPlayer && !candidatePC.Is(CustomRoles.King) && !candidatePC.Is(CustomRoles.Autocrat) && !candidatePC.Is(CustomRoleTypes.Impostor))
                        {
                            distance = Vector2.Distance(cpPos, candidatePC.transform.position);
                            candidateList.Add(candidatePC, distance);
                            Logger.Info($"{candidatePC?.Data?.GetLogPlayerName()}の位置{distance}", "Warlock");
                        }
                    }
                }
                var nearest = candidateList.OrderBy(c => c.Value).FirstOrDefault();
                var killTarget = nearest.Key;
                if (CustomRoleManager.OnCheckMurder(Player, killTarget, CursedPlayer, killTarget, true, false, 2))
                {
                    Logger.Info($"{killTarget.GetNameWithRole().RemoveHtmlTags()}was killed", "Warlock");
                    RPC.PlaySoundRPC(Player.PlayerId, Sounds.KillSound);
                }
                CursedPlayer = null;

                Achievements.RpcCompleteAchievement(Player.PlayerId, 1, achievements[0]);
                if (killTarget.IsTeammate(Player))
                {
                    Achievements.RpcCompleteAchievement(Player.PlayerId, 0, achievements[1]);
                }
                if (Optiondouki.GetBool())
                {
                    Player.SetKillCooldown();
                }
                if (OptionCantmove.GetBool() && Player.IsAlive())
                {         
                    var tmpSpeed = Main.AllPlayerSpeed[Player.PlayerId];

                    Main.AllPlayerSpeed[Player.PlayerId] = Main.MinSpeed;
                    UtilsOption.MarkEveryoneDirtySettings();
                    _ = new LateTask(() =>
                    {
                        Logger.Info("硬直解除", "Warlock");

                        Main.AllPlayerSpeed[Player.PlayerId] = tmpSpeed;
                        UtilsOption.MarkEveryoneDirtySettings();
                    }, OptionCantMovetime.GetFloat(), "Warlock_koutyoku", true);
                }
            }       
            IsCursed = false;
            ResetCooldown = true;
            SendRPC();
        }
        else
        {
            ResetCooldown = false;

            CursedPlayer = Player.GetKillTarget(true);
            if (CursedPlayer != null)
            {
                IsCursed = true;
            }
            SendRPC();
        }
    }
    public override void OnReportDeadBody(PlayerControl reporter, NetworkedPlayerInfo target)
    {
        CursedPlayer = null;
        IsCursed = false;
        SendRPC();
    }
    public override bool OverrideAbilityButton(out string text)
    {
        text = "Warlock_Ability";
        return true;
    }
    public void SendRPC()
    {
        using var sender = CreateSender();
        sender.Writer.Write(IsCursed);
        sender.Writer.Write(CursedPlayer != null);
        if (CursedPlayer != null) sender.Writer.Write(CursedPlayer.PlayerId);
    }

    public override void ReceiveRPC(MessageReader reader)
    {
        IsCursed = reader.ReadBoolean();
        bool hasCursed = reader.ReadBoolean();
        CursedPlayer = hasCursed ? PlayerCatch.GetPlayerById(reader.ReadByte()) : null;
    }

    public override string GetMark(PlayerControl seer, PlayerControl seen, bool isForMeeting = false)
    {
        //seenが省略の場合seer
        seen ??= seer;
        if (CursedPlayer is null)
        {
            return "";
        }
        if (seen.PlayerId == CursedPlayer.PlayerId) 
            return Utils.ColorString(RoleInfo.RoleColor, "★");

        return "";
    }

    public static Dictionary<int, Achievement> achievements = new();
    [Attributes.PluginModuleInitializer]
    public static void Load()
    {
        var n1 = new Achievement(RoleInfo, 0, 5, 0, 0);
        var sp1 = new Achievement(RoleInfo, 1, 1, 2, 2, true);
        achievements.Add(0, n1);
        achievements.Add(1, sp1);
    }
}
