using Hazel;
using System.Linq;
using AmongUs.GameOptions;
using UnityEngine;

using TownOfHost.Modules;
using TownOfHost.Roles.Core;
using TownOfHost.Roles.Core.Interfaces;
using TownOfHost.Roles.Crewmate;
using static TownOfHost.Roles.Core.Interfaces.ISchrodingerCatOwner;

namespace TownOfHost.Roles.Neutral
{
    public sealed class BakeCat : RoleBase, IAdditionalWinner, IKiller, ISchrodingerCatOwner
    {
        public static readonly SimpleRoleInfo RoleInfo =
            SimpleRoleInfo.Create(
                typeof(BakeCat),
                player => new BakeCat(player),
                CustomRoles.BakeCat,
                () => RoleTypes.Crewmate,
                CustomRoleTypes.Neutral,
                15700,
                SetupOptionItem,
                "bk",
                "#ededc7",
                (7, 3),
                true,
                countType: CountTypes.Crew
            );
        public BakeCat(PlayerControl player)
        : base(
            RoleInfo,
            player,
            () => HasTask.ForRecompute
        )
        {
            CanKill = false;
            Killer = null;
        }
        public bool CanKill;
        private static OptionItem OptionKillCooldown;
        public static OptionItem OptionCanVent;
        public static OptionItem OptionCanUseSabotage;
        public static OptionItem OptionHasImpostorVision;
        public static OptionItem OptionDieKiller;
        public static OptionItem OptionDieKillerTIme;
        static OptionItem OptionCountChenge;
        static OptionItem OptionCanSeeKillableTeammate;
        PlayerControl Killer;
        /// <summary>
        /// 自分をキルしてきた人のロール
        /// </summary>
        private ISchrodingerCatOwner owner = null;
        private TeamType _team = TeamType.None;
        public TeamType SchrodingerCatChangeTo => Team;
        /// <summary>
        /// 現在の所属陣営<br/>
        /// 変更する際は特段の事情がない限り<see cref="RpcSetTeam"/>を使ってください
        /// </summary>
        public TeamType Team
        {
            get => _team;
            private set
            {
                logger.Info($"{Player.GetRealName()}の陣営を{value}に変更");
                _team = value;
            }
        }
        public Color DisplayRoleColor => GetCatColor(Team);
        private static LogHandler logger = Logger.Handler(nameof(BakeCat));
        enum Op
        {
            BakeCatDieKiller, BakeCatDieKillerTime, BakeCatCountChenge, SchrodingerCatCanSeeKillableTeammate
        }
        public static void SetupOptionItem()
        {
            OptionKillCooldown = FloatOptionItem.Create(RoleInfo, 10, GeneralOption.KillCooldown, new(0f, 180f, 0.5f), 30f, false)
                .SetValueFormat(OptionFormat.Seconds);
            OptionCanVent = BooleanOptionItem.Create(RoleInfo, 11, GeneralOption.CanVent, true, false);
            OptionCanUseSabotage = BooleanOptionItem.Create(RoleInfo, 12, GeneralOption.CanUseSabotage, false, false);
            OptionHasImpostorVision = BooleanOptionItem.Create(RoleInfo, 13, GeneralOption.ImpostorVision, true, false);
            OptionDieKiller = BooleanOptionItem.Create(RoleInfo, 14, Op.BakeCatDieKiller, true, false);
            OptionDieKillerTIme = FloatOptionItem.Create(RoleInfo, 15, Op.BakeCatDieKillerTime, new(0, 180, 1), 1, false, OptionDieKiller).SetValueFormat(OptionFormat.Seconds);
            OptionCountChenge = BooleanOptionItem.Create(RoleInfo, 16, Op.BakeCatCountChenge, false, false);
            OptionCanSeeKillableTeammate = BooleanOptionItem.Create(RoleInfo, 17, Op.SchrodingerCatCanSeeKillableTeammate, false, false);
        }
        public override void ApplyGameOptions(IGameOptions opt)
        {
            opt.SetVision(OptionHasImpostorVision.GetBool() && Team != TeamType.None);
        }
        void IKiller.OnCheckMurderAsKiller(MurderInfo info)
        {
            if (info.AttemptKiller.PlayerId == Player.PlayerId) return;

            // 親分はキル出来ないようにする
            if (info.AttemptTarget.PlayerId == (Killer?.PlayerId ?? byte.MaxValue))
            {
                info.DoKill = false;
            }
        }
        public override bool OnCheckMurderAsTarget(MurderInfo info)
        {
            var killer = info.AttemptKiller;

            //自殺ならスルー
            if (info.IsSuicide) return true;
            if (killer.GetRoleClass() is not ISchrodingerCatOwner) return true;

            if (killer.Is(CustomRoles.GrimReaper) || killer.Is(CustomRoles.BakeCat))
                return true;
            else
            if (Team == TeamType.None)
            {
                info.CanKill = false;
                ChangeTeamOnKill(killer);

                return false;
            }
            return true;
        }

        /// <summary>
        /// キルしてきた人に応じて陣営の状態を変える
        /// </summary>
        public void ChangeTeamOnKill(PlayerControl killer)
        {
            killer.RpcProtectedMurderPlayer(Player);
            Killer = killer;
            if (killer.GetRoleClass() is ISchrodingerCatOwner catOwner)
            {
                catOwner.OnBakeCatKill(this);
                RpcSetTeam((TeamType)catOwner.SchrodingerCatChangeTo);
                owner = catOwner;

                if (AmongUsClient.Instance.AmHost)
                {
                    Player.RpcSetRoleDesync(RoleTypes.Impostor, Player.GetClientId());
                    foreach (var pc in PlayerCatch.AllPlayerControls)
                    {
                        if (pc == PlayerControl.LocalPlayer)
                        {
                            Player.RpcSetRoleDesync(Player.IsAlive() ? RoleTypes.Crewmate : RoleTypes.CrewmateGhost, Player.GetClientId());
                            if (Player != pc) pc.RpcSetRoleDesync(pc.IsAlive() ? RoleTypes.Scientist : RoleTypes.CrewmateGhost, Player.GetClientId());
                        }
                        else
                        {
                            Player.RpcSetRoleDesync(pc == Player ? (Player.IsAlive() ? RoleTypes.Impostor : RoleTypes.ImpostorGhost) : (Player.IsAlive() ? RoleTypes.Crewmate : RoleTypes.CrewmateGhost), pc.GetClientId());
                            if (Player != pc) pc.RpcSetRoleDesync(pc.IsAlive() ? RoleTypes.Scientist : RoleTypes.CrewmateGhost, Player.GetClientId());
                        }
                    }
                }
                _ = new LateTask(() =>
                {
                    Player.SetKillCooldown(OptionKillCooldown.GetFloat(), force: true);
                    CanKill = true;
                    if (!Utils.RoleSendList.Contains(Player.PlayerId)) Utils.RoleSendList.Add(Player.PlayerId);

                    if (OptionCountChenge.GetBool())
                    {
                        MyState.SetCountType(killer.GetCustomRole().GetRoleInfo()?.CountType ?? CountTypes.Crew);
                        if (OptionDieKiller.GetBool())//死ぬならカウントが増えないようにキラーのカウントをクルーにしてやる
                            PlayerState.GetByPlayerId(killer.PlayerId).SetCountType(CountTypes.Crew);
                    }
                }, 0.3f, "ResetKillCooldown");
                if (OptionDieKiller.GetBool())
                    _ = new LateTask(() =>
                    {
                        if (!killer.IsAlive() || GameStates.CalledMeeting) return;
                        killer.RpcMurderPlayerV2(killer);
                    }, OptionDieKillerTIme.GetFloat(), "BakeCatKillerDie");
            }
            else
            {
                logger.Warn($"未知のキル役職からのキル: {killer.GetNameWithRole().RemoveHtmlTags()}");
                return;
            }

            RevealNameColors(killer);

            UtilsNotifyRoles.NotifyRoles();
            UtilsOption.MarkEveryoneDirtySettings();

            if (PlayerControl.LocalPlayer.PlayerId == Player.PlayerId)
            {
                PlayerControl.LocalPlayer.Data.Role.AffectedByLightAffectors = false;
            }
        }
        public override void OnReportDeadBody(PlayerControl repo, NetworkedPlayerInfo sitai)
        {
            if (OptionDieKiller.GetBool())
            {
                if (!Killer.IsAlive()) return;
                Killer.RpcMurderPlayerV2(Killer);
            }
        }
        public override RoleTypes? AfterMeetingRole => CanKill ? RoleTypes.Impostor : RoleTypes.Crewmate;

        private void RevealNameColors(PlayerControl killer)
        {
            if (OptionCanSeeKillableTeammate.GetBool())
            {
                var killerRoleId = killer.GetCustomRole();
                var killerTeam = PlayerCatch.AllPlayerControls.Where(player => (_team is TeamType.Mad && (player.Is(CustomRoleTypes.Impostor) || player.Is(CustomRoles.WolfBoy))) || player.Is(killerRoleId));
                foreach (var member in killerTeam)
                {
                    if (member.GetCustomRole().IsMadmate()) continue;
                    var rolecolor = RoleInfo.RoleColorCode;
                    if (member.Is(CustomRoles.WolfBoy))
                    {
                        rolecolor = WolfBoy.Shurenekodotti.GetBool() ? UtilsRoleText.GetRoleColorCode(CustomRoles.Impostor) : "#ffffff";
                    }
                    NameColorManager.Add(member.PlayerId, Player.PlayerId, rolecolor);
                    NameColorManager.Add(Player.PlayerId, member.PlayerId);
                }
            }
            else
            {
                var rolecolor = RoleInfo.RoleColorCode;
                if (killer.Is(CustomRoles.WolfBoy))
                {
                    rolecolor = WolfBoy.Shurenekodotti.GetBool() ? UtilsRoleText.GetRoleColorCode(CustomRoles.Impostor) : "#ffffff";
                }
                NameColorManager.Add(killer.PlayerId, Player.PlayerId, rolecolor);
                NameColorManager.Add(Player.PlayerId, killer.PlayerId);
            }

            UtilsGameLog.AddGameLog($"BakeNeko", UtilsName.GetPlayerColor(Player) + ":  " + string.Format(GetString("SchrodingerCat.Ch"), UtilsName.GetPlayerColor(killer, true) + $"(<b>{UtilsRoleText.GetTrueRoleName(killer.PlayerId, false)}</b>)"));
            UtilsGameLog.LastLogRole[Player.PlayerId] = UtilsGameLog.LastLogRole[Player.PlayerId].RemoveColorTags().Color(DisplayRoleColor);
        }
        public override CustomRoles Misidentify() => Team == TeamType.None ? CustomRoles.Crewmate : CustomRoles.NotAssigned;
        public override CustomRoles TellResults(PlayerControl player) => Team == TeamType.None ? CustomRoles.Crewmate : CustomRoles.NotAssigned;
        public override void OverrideTrueRoleName(ref Color roleColor, ref string roleText)
        {
            // 陣営変化前なら上書き不要
            if (Team == TeamType.None)
            {
                return;
            }
            roleColor = DisplayRoleColor;
        }
        public override void OverrideDisplayRoleNameAsSeen(PlayerControl seer, ref bool enabled, ref Color roleColor, ref string roleText, ref bool addon)
        {
            if (seer.IsAlive() is false && Team == TeamType.None)
            {
                roleText += $"{UtilsRoleText.GetRoleColorAndtext(CustomRoles.BakeCat)}";
            }
        }
        public bool CheckWin(ref CustomRoles winnerRole)
        {
            bool? won = Team switch
            {
                TeamType.None => CustomWinnerHolder.WinnerTeam == CustomWinner.Crewmate,
                TeamType.Mad => CustomWinnerHolder.WinnerTeam == CustomWinner.Impostor,
                TeamType.Crew => CustomWinnerHolder.WinnerTeam == CustomWinner.Crewmate,
                TeamType.Jackal => CustomWinnerHolder.WinnerTeam == CustomWinner.Jackal,
                TeamType.Egoist => CustomWinnerHolder.WinnerTeam == CustomWinner.Egoist,
                TeamType.CountKiller => CustomWinnerHolder.WinnerTeam == CustomWinner.CountKiller,
                TeamType.Remotekiller => CustomWinnerHolder.WinnerTeam == CustomWinner.Remotekiller,
                TeamType.DoppelGanger => CustomWinnerHolder.WinnerTeam == CustomWinner.DoppelGanger,
                TeamType.MilkyWay => CustomWinnerHolder.WinnerTeam == CustomWinner.MilkyWay,
                TeamType.Betrayer => CustomWinnerHolder.WinnerTeam == CustomWinner.MadBetrayer,
                _ => null,
            };
            if (!won.HasValue)
            {
                logger.Warn($"不明な猫の勝利チェック: {Team}");
                return false;
            }
            return won.Value;
        }
        public void RpcSetTeam(TeamType team)
        {
            Team = team;
            if (AmongUsClient.Instance.AmHost)
            {
                using var sender = CreateSender();
                sender.Writer.Write((byte)team);
            }
        }
        public override void ReceiveRPC(MessageReader reader)
        {
            Team = (TeamType)reader.ReadByte();
        }
        public static Color GetCatColor(TeamType catType)
        {
            Color? color = catType switch
            {
                TeamType.None => RoleInfo.RoleColor,
                TeamType.Mad => UtilsRoleText.GetRoleColor(CustomRoles.Madmate),
                TeamType.Crew => UtilsRoleText.GetRoleColor(CustomRoles.Crewmate),
                TeamType.Jackal => UtilsRoleText.GetRoleColor(CustomRoles.Jackal),
                TeamType.Egoist => UtilsRoleText.GetRoleColor(CustomRoles.Egoist),
                TeamType.Remotekiller => UtilsRoleText.GetRoleColor(CustomRoles.Remotekiller),
                TeamType.CountKiller => UtilsRoleText.GetRoleColor(CustomRoles.CountKiller),
                TeamType.DoppelGanger => UtilsRoleText.GetRoleColor(CustomRoles.DoppelGanger),
                TeamType.MilkyWay => StringHelper.CodeColor(Vega.TeamColor),
                TeamType.Betrayer => UtilsRoleText.GetRoleColor(CustomRoles.MadBetrayer),
                _ => null,
            };
            if (!color.HasValue)
            {
                logger.Warn($"不明な猫に対する色の取得: {catType}");
                return UtilsRoleText.GetRoleColor(CustomRoles.Crewmate);
            }
            return color.Value;
        }

        public bool CanUseSabotageButton() => OptionCanUseSabotage.GetBool() && Team != TeamType.None;
        public bool CanUseImpostorVentButton() => OptionCanVent.GetBool() && Team != TeamType.None;
        public bool CanUseKillButton() => Team != TeamType.None && CanKill;
        public float CalculateKillCooldown() => OptionKillCooldown.GetFloat();
    }
}