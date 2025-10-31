using System.Collections.Generic;
using System.Linq;
using TownOfHost.Roles.Core;
using TownOfHost.Roles.Crewmate;

namespace TownOfHost
{
    public class RoleAddAddons
    {
        public static Dictionary<CustomRoles, RoleAddAddons> AllData = new();
        public static Dictionary<CustomRoles, CustomRoles> RoleNames = new(); //1人までしか対応していない、
        public CustomRoles Role { get; private set; }
        public int IdStart { get; private set; }
        public bool IsImpostor;
        public OptionItem GiveAddons;
        //ゲッサー
        public OptionItem GiveGuesser;
        public OptionItem CanGuessTime; public OptionItem OwnCanGuessTime;
        public OptionItem ICanGuessVanilla; public OptionItem ICanGuessNakama; public OptionItem ICanGuessTaskDoneSnitch;
        public OptionItem ICanWhiteCrew; public OptionItem AddShotLimit;
        //マネジメント
        public OptionItem GiveManagement;
        public OptionItem ManagementCanSeeComms; public OptionItem PercentGage; public OptionItem ManagementCanSeeMeeting;
        public OptionItem RoughPercentage;
        //ウォッチング
        public OptionItem GiveWatching;
        //シーイング
        public OptionItem GiveSeeing;
        public OptionItem SeeingCanSeeComms;
        //オートプシー
        public OptionItem GiveAutopsy;
        public OptionItem AutopsyCanSeeComms;
        //タイブレーカー
        public OptionItem GiveTiebreaker;
        //プラスポート
        public OptionItem GivePlusVote;
        public OptionItem AdditionalVote;
        //リベンジャー
        public OptionItem GiveRevenger;
        public OptionItem RevengeToImpostor; public OptionItem RevengeToCrewmate;
        public OptionItem RevengeToMadmate; public OptionItem RevengeToNeutral;
        //オープナー
        public OptionItem GiveOpener;
        //アンチテレポーター
        //public OptionItem GiveAntiTeleporter;
        //スピーディング
        public OptionItem GiveSpeeding;
        public OptionItem Speed;
        //ガーディング
        public OptionItem GiveGuarding;
        public OptionItem Guard;
        //イレクター
        public OptionItem GiveElector;
        //ノンレポート
        public OptionItem GiveNonReport;
        public OptionItem OptionNonReportMode; public NonReportMode mode = NonReportMode.nullpo;
        public enum NonReportMode { NotButton, NotReport, NonReportModeAll, nullpo }
        //トランスパレント
        public OptionItem GiveTransparent;
        //ノットヴォウター
        public OptionItem GiveNotvoter;
        //インフォプアー
        public OptionItem GiveInfoPoor;

        //ウォーター
        public OptionItem GiveWater;
        //クラムシー
        public OptionItem GiveClumsy;
        //スラッカー
        public OptionItem GiveSlacker;
        //ムーン
        public OptionItem GiveMoon;
        //ライティング
        public OptionItem GiveLighting;
        public RoleAddAddons(int idStart, TabGroup tab, CustomRoles role, CustomRoles RoleName = CustomRoles.NotAssigned, bool NeutralKiller = false, bool MadMate = false, bool DefaaultOn = false)
        {
            this.IsImpostor = role.IsImpostor();
            this.IdStart = idStart;
            this.Role = role;
            GiveAddons = BooleanOptionItem.Create(idStart++, "addaddons", DefaaultOn || NeutralKiller, tab, false).SetParent(Options.CustomRoleSpawnChances[role]).SetParentRole(role)
                    .SetValueFormat(OptionFormat.None).SetParentRole(role);
            GiveGuesser = BooleanOptionItem.Create(idStart++, "GiveGuesser", false, tab, false).SetParent(GiveAddons).SetParentRole(role);
            CanGuessTime = IntegerOptionItem.Create(idStart++, "CanGuessTime", new(1, 15, 1), 3, tab, false).SetParent(GiveGuesser).SetParentRole(role)
                .SetValueFormat(OptionFormat.Players);
            AddShotLimit = BooleanOptionItem.Create(idStart++, "AddShotLimit", false, tab, false).SetParent(GiveGuesser).SetParentRole(role);
            OwnCanGuessTime = IntegerOptionItem.Create(idStart++, "OwnCanGuessTime", new(1, 15, 1), 1, tab, false).SetParent(GiveGuesser).SetParentRole(role)
                    .SetValueFormat(OptionFormat.Players);
            ICanGuessVanilla = BooleanOptionItem.Create(idStart++, "CanGuessVanilla", true, tab, false).SetParent(GiveGuesser).SetParentRole(role);
            ICanGuessNakama = BooleanOptionItem.Create(idStart++, "CanGuessNakama", true, tab, false).SetParent(GiveGuesser).SetParentRole(role);
            ICanGuessTaskDoneSnitch = BooleanOptionItem.Create(idStart++, "CanGuessTaskDoneSnitch", false, tab, false).SetParent(GiveGuesser).SetParentRole(role);
            ICanWhiteCrew = BooleanOptionItem.Create(idStart++, "CanWhiteCrew", false, tab, false).SetParent(GiveGuesser).SetParentRole(role);
            GiveWatching = BooleanOptionItem.Create(idStart++, "GiveWatching", false, tab, false).SetParent(GiveAddons).SetParentRole(role);
            GivePlusVote = BooleanOptionItem.Create(idStart++, "GivePlusVote", false, tab, false).SetParent(GiveAddons).SetParentRole(role);
            AdditionalVote = IntegerOptionItem.Create(idStart++, "MayorAdditionalVote", new(1, 99, 1), 1, tab, false).SetParentRole(role).SetValueFormat(OptionFormat.Votes).SetParent(GivePlusVote);
            GiveTiebreaker = BooleanOptionItem.Create(idStart++, "GiveTiebreaker", false, tab, false).SetParent(GiveAddons).SetParentRole(role);
            GiveAutopsy = BooleanOptionItem.Create(idStart++, "GiveAutopsy", false, tab, false).SetParent(GiveAddons).SetParentRole(role);
            AutopsyCanSeeComms = BooleanOptionItem.Create(idStart++, "CanUseActiveComms", true, tab, false).SetParent(GiveAutopsy).SetParentRole(role);
            GiveRevenger = BooleanOptionItem.Create(idStart++, "GiveRevenger", false, tab, false).SetParent(GiveAddons).SetParentRole(role);
            RevengeToImpostor = BooleanOptionItem.Create(idStart++, "NekoKabochaImpostorsGetRevenged", true, tab, false).SetParentRole(role).SetParent(GiveRevenger);
            RevengeToCrewmate = BooleanOptionItem.Create(idStart++, "RevengeToCrewmate", true, tab, false).SetParent(GiveRevenger).SetParentRole(role);
            RevengeToMadmate = BooleanOptionItem.Create(idStart++, "NekoKabochaMadmatesGetRevenged", true, tab, false).SetParent(GiveRevenger).SetParentRole(role);
            RevengeToNeutral = BooleanOptionItem.Create(idStart++, "RevengeToNeutral", true, tab, false).SetParent(GiveRevenger).SetParentRole(role);
            GiveSpeeding = BooleanOptionItem.Create(idStart++, "GiveSpeeding", false, tab, false).SetParent(GiveAddons).SetParentRole(role);
            Speed = FloatOptionItem.Create(idStart++, "AddSpeed", new(0.5f, 10f, 0.25f), 2f, tab, false).SetParent(GiveSpeeding).SetParentRole(role);
            GiveGuarding = BooleanOptionItem.Create(idStart++, "GiveGuarding", false, tab, false).SetParent(GiveAddons).SetParentRole(role);
            Guard = IntegerOptionItem.Create(idStart++, "AddGuardCount", new(1, 10, 1), 1, tab, false).SetParent(GiveGuarding).SetParentRole(role);
            GiveManagement = BooleanOptionItem.Create(idStart++, "GiveManagement", false, tab, false).SetParent(GiveAddons).SetParentRole(role);
            PercentGage = BooleanOptionItem.Create(idStart++, "PercentGage", false, tab, false).SetParent(GiveManagement).SetParentRole(role);
            RoughPercentage = BooleanOptionItem.Create(idStart++, "RoughPercentage", false, tab, false).SetParent(PercentGage).SetParentRole(role);
            ManagementCanSeeComms = BooleanOptionItem.Create(idStart++, "CanUseActiveComms", false, tab, false).SetParent(GiveManagement).SetParentRole(role);
            ManagementCanSeeMeeting = BooleanOptionItem.Create(idStart++, "CanseeMeeting", false, tab, false).SetParent(GiveManagement).SetParentRole(role);
            GiveSeeing = BooleanOptionItem.Create(idStart++, "GiveSeeing", false, tab, false).SetParent(GiveAddons).SetParentRole(role);
            SeeingCanSeeComms = BooleanOptionItem.Create(idStart++, "CanUseActiveComms", true, tab, false).SetParent(GiveSeeing).SetParentRole(role);
            GiveOpener = BooleanOptionItem.Create(idStart++, "GiveOpener", false, tab, false).SetParent(GiveAddons).SetParentRole(role);
            //GiveAntiTeleporter = BooleanOptionItem.Create(idStart++, "GiveAntiTeleporter", false, tab, false).SetParent(GiveAddons);
            if (!IsImpostor)
            {
                GiveLighting = BooleanOptionItem.Create(idStart++, "GiveLighting", NeutralKiller, tab, false).SetParentRole(role).SetParent(GiveAddons);
                GiveMoon = BooleanOptionItem.Create(idStart++, "GiveMoon", NeutralKiller || MadMate, tab, false).SetParentRole(role).SetParent(GiveAddons);
            }
            //デバフ
            GiveNotvoter = BooleanOptionItem.Create(idStart++, "GiveNotvoter", false, tab, false).SetParentRole(role).SetParent(GiveAddons);
            GiveElector = BooleanOptionItem.Create(idStart++, "GiveElector", false, tab, false).SetParentRole(role).SetParent(GiveAddons);
            GiveInfoPoor = BooleanOptionItem.Create(idStart++, "GiveInfoPoor", false, tab, false).SetParentRole(role).SetParent(GiveAddons);
            GiveNonReport = BooleanOptionItem.Create(idStart++, "GiveNonReport", false, tab, false).SetParentRole(role).SetParent(GiveAddons);
            OptionNonReportMode = StringOptionItem.Create(idStart++, "ConverMode", EnumHelper.GetAllNames<NonReportMode>().Where(name => name is not "nullpo").ToArray(), 0, tab, false).SetParentRole(role).SetParent(GiveNonReport);

            GiveTransparent = BooleanOptionItem.Create(idStart++, "GiveTransparent", false, tab, false).SetParentRole(role).SetParent(GiveAddons);
            GiveWater = BooleanOptionItem.Create(idStart++, "GiveWater", MadMate, tab, false).SetParentRole(role).SetParent(GiveAddons);
            GiveClumsy = BooleanOptionItem.Create(idStart++, "GiveClumsy", MadMate, tab, false).SetParentRole(role).SetParent(GiveAddons);
            GiveSlacker = BooleanOptionItem.Create(idStart++, "GiveSlacker", false, tab, false).SetParentRole(role).SetParent(GiveAddons);

            role = RoleName == CustomRoles.NotAssigned ? role : RoleName;

            if (!AllData.ContainsKey(role)) AllData.Add(role, this);
            else Logger.Warn("重複したCustomRolesを対象とするRoleAddAddonsが作成されました", "RoleAddAddons");
        }
        public static RoleAddAddons Create(SimpleRoleInfo roleInfo, int idOffset, CustomRoles rolename = CustomRoles.NotAssigned, bool NeutralKiller = false, bool MadMate = false, bool DefaaultOn = false)
        {
            return new RoleAddAddons(roleInfo.ConfigId + idOffset, roleInfo.Tab, roleInfo.RoleName, rolename, NeutralKiller, MadMate, DefaaultOn);
        }
        public static RoleAddAddons Create(int idStart, TabGroup tab, CustomRoles role)
        {
            return new RoleAddAddons(idStart, tab, role);
        }
        /// <summary>
        /// 役職付与の属性<br/>
        /// ストルナーとかが重いため必要な分だけ取り出す<br/>
        /// GiveAddonがfalseの場合、全てデフォルト値になるのでチェックは基本不要
        /// </summary>
        /// <param name="role">役職</param>
        /// <param name="data">返すデータ</param>
        /// <param name="player">付与されているプレイヤー</param>
        /// <param name="subrole">必要な役職</param>
        /// <returns></returns>
        public static bool GetRoleAddon(CustomRoles role, out RoleAddAddons data, PlayerControl player = null, params CustomRoles[] subrole)
        {
            var haveaddon = false;
            AllData.TryGetValue(CustomRoles.NotAssigned, out var nulldata);
            data = nulldata;

            switch (role)
            {
                case CustomRoles.Stolener:
                    if (player == null && AllData.TryGetValue(role, out data)) haveaddon = true;
                    else if ((player.GetRoleClass() as Stolener)?.ICanUseaddon == true && AllData.TryGetValue(role, out data) && data?.GiveAddons.GetBool() == true)
                        haveaddon = true;
                    break;
                default:
                    if (AllData.TryGetValue(role, out data) && data?.GiveAddons.GetBool() == true)
                        haveaddon = true;
                    break;
            }
            if (data is not null) data.mode = haveaddon ? (NonReportMode)data.OptionNonReportMode.GetValue() : NonReportMode.nullpo;

            if (player != null)
            {
                if (Stolener.Killers.Contains(player.PlayerId) && AllData.TryGetValue(CustomRoles.Stolener, out var ovdata) && ovdata?.GiveAddons.GetBool() == true)
                {
                    if (haveaddon)
                    {
                        Overridedata(ref data, ovdata, subrole);
                        if (subrole.Contains(CustomRoles.NonReport))
                        {
                            var oldd = (NonReportMode)data.OptionNonReportMode.GetValue();
                            var newd = (NonReportMode)ovdata.OptionNonReportMode.GetValue();
                            if (oldd != newd)
                            {
                                switch (oldd)
                                {
                                    case NonReportMode.NotButton:
                                        if (newd is NonReportMode.NonReportModeAll or NonReportMode.NotReport)
                                            data.mode = NonReportMode.NonReportModeAll;
                                        break;
                                    case NonReportMode.NotReport:
                                        if (newd is NonReportMode.NonReportModeAll or NonReportMode.NotButton)
                                            data.mode = NonReportMode.NonReportModeAll;
                                        break;
                                }
                            }
                        }
                    }
                    else data = ovdata;

                    haveaddon = true;
                }
            }
            //持ってなかったりするならぬるぽの奴に変える
            if (data is null || !haveaddon)
            {
                data = nulldata;
                haveaddon = false;
            }

            return haveaddon;
        }
        static void Overridedata(ref RoleAddAddons olddata, RoleAddAddons newdata, CustomRoles[] subrole)
        {
            olddata.GiveAddons = olddata.GiveAddons.InfoGetBool() == false ? newdata.GiveAddons : olddata.GiveAddons;

            if (!olddata.IsImpostor && !newdata.IsImpostor)
            {
                olddata.GiveMoon = olddata.GiveMoon.InfoGetBool() == false ? newdata.GiveMoon : olddata.GiveMoon;
                olddata.GiveLighting = olddata.GiveLighting.InfoGetBool() == false ? newdata.GiveLighting : olddata.GiveLighting;
            }
            else
            if (olddata.IsImpostor && !newdata.IsImpostor)
            {
                olddata.IsImpostor = false;
                olddata.GiveMoon = newdata.GiveMoon;
                olddata.GiveLighting = newdata.GiveLighting;
            }

            //必要な時だけ変更する
            if (subrole.Contains(CustomRoles.NotAssigned))
            {
                foreach (var sub in subrole)
                    switch (sub)
                    {
                        case CustomRoles.Guesser:
                            olddata.GiveGuesser = olddata.GiveGuesser.InfoGetBool() == false ? newdata.GiveGuesser : olddata.GiveGuesser;
                            if (newdata.GiveGuesser.InfoGetBool())
                            {
                                olddata.CanGuessTime = olddata.CanGuessTime.GetInt() <= newdata.CanGuessTime.GetInt() ? newdata.CanGuessTime : olddata.CanGuessTime;
                                olddata.OwnCanGuessTime = olddata.OwnCanGuessTime.GetInt() <= newdata.OwnCanGuessTime.GetInt() ? newdata.OwnCanGuessTime : olddata.OwnCanGuessTime;
                                olddata.ICanGuessVanilla = olddata.ICanGuessVanilla.InfoGetBool() == false ? newdata.ICanGuessVanilla : olddata.ICanGuessVanilla;
                                olddata.ICanGuessNakama = olddata.ICanGuessNakama.InfoGetBool() == false ? newdata.ICanGuessNakama : olddata.ICanGuessNakama;
                                olddata.ICanGuessTaskDoneSnitch = olddata.ICanGuessTaskDoneSnitch.InfoGetBool() == false ? newdata.ICanGuessTaskDoneSnitch : olddata.ICanGuessTaskDoneSnitch;
                                olddata.ICanWhiteCrew = olddata.ICanWhiteCrew.InfoGetBool() == false ? newdata.ICanWhiteCrew : olddata.ICanWhiteCrew;
                                olddata.AddShotLimit = olddata.AddShotLimit.InfoGetBool() == false ? newdata.AddShotLimit : olddata.AddShotLimit;
                            }
                            break;
                        case CustomRoles.Management:
                            olddata.GiveManagement = olddata.GiveManagement.InfoGetBool() == false ? newdata.GiveManagement : olddata.GiveManagement;
                            if (newdata.GiveManagement.InfoGetBool())
                            {
                                olddata.ManagementCanSeeComms = olddata.ManagementCanSeeComms.InfoGetBool() == false ? newdata.ManagementCanSeeComms : olddata.ManagementCanSeeComms;
                                olddata.PercentGage = olddata.PercentGage.InfoGetBool() == false ? newdata.PercentGage : olddata.PercentGage;
                                olddata.ManagementCanSeeMeeting = olddata.ManagementCanSeeMeeting.InfoGetBool() == false ? newdata.ManagementCanSeeMeeting : olddata.ManagementCanSeeMeeting;
                                olddata.RoughPercentage = olddata.RoughPercentage.InfoGetBool() == false ? newdata.RoughPercentage : olddata.RoughPercentage;
                            }
                            break;
                        case CustomRoles.Seeing:
                            olddata.GiveSeeing = olddata.GiveSeeing.InfoGetBool() == false ? newdata.GiveSeeing : olddata.GiveSeeing;
                            if (newdata.GiveSeeing.InfoGetBool()) olddata.SeeingCanSeeComms = olddata.SeeingCanSeeComms.InfoGetBool() == false ? newdata.SeeingCanSeeComms : olddata.SeeingCanSeeComms;
                            break;
                        case CustomRoles.Autopsy:
                            olddata.GiveAutopsy = olddata.GiveAutopsy.InfoGetBool() == false ? newdata.GiveAutopsy : olddata.GiveAutopsy;
                            if (newdata.GiveAutopsy.InfoGetBool()) olddata.AutopsyCanSeeComms = olddata.AutopsyCanSeeComms.InfoGetBool() == false ? newdata.AutopsyCanSeeComms : olddata.AutopsyCanSeeComms;
                            break;
                        case CustomRoles.PlusVote:
                            olddata.GivePlusVote = olddata.GivePlusVote.InfoGetBool() == false ? newdata.GivePlusVote : olddata.GivePlusVote;
                            if (newdata.GivePlusVote.InfoGetBool()) olddata.AdditionalVote = olddata.AdditionalVote.GetInt() <= newdata.AdditionalVote.GetInt() ? newdata.AdditionalVote : olddata.AdditionalVote;
                            break;
                        case CustomRoles.Revenger:
                            olddata.GiveRevenger = olddata.GiveRevenger.InfoGetBool() == false ? newdata.GiveRevenger : olddata.GiveRevenger;
                            if (newdata.GiveRevenger.InfoGetBool())
                            {
                                olddata.RevengeToImpostor = olddata.RevengeToImpostor.InfoGetBool() == false ? newdata.RevengeToImpostor : olddata.RevengeToImpostor;
                                olddata.RevengeToCrewmate = olddata.RevengeToCrewmate.InfoGetBool() == false ? newdata.RevengeToCrewmate : olddata.RevengeToCrewmate;
                                olddata.RevengeToNeutral = olddata.RevengeToNeutral.InfoGetBool() == false ? newdata.RevengeToNeutral : olddata.RevengeToNeutral;
                                olddata.RevengeToMadmate = olddata.RevengeToMadmate.InfoGetBool() == false ? newdata.RevengeToMadmate : olddata.RevengeToMadmate;
                            }
                            break;
                        case CustomRoles.Speeding:
                            olddata.GiveSpeeding = olddata.GiveSpeeding.InfoGetBool() == false ? newdata.GiveSpeeding : olddata.GiveSpeeding;
                            if (newdata.GiveSpeeding.InfoGetBool()) olddata.Speed = olddata.Speed.GetFloat() <= newdata.Speed.GetFloat() ? newdata.Speed : olddata.Speed;
                            break;
                        case CustomRoles.Guarding:
                            olddata.GiveGuarding = olddata.GiveGuarding.InfoGetBool() == false ? newdata.GiveGuarding : olddata.GiveGuarding;
                            if (newdata.GiveGuarding.InfoGetBool()) olddata.Guard = olddata.Guard.GetFloat() <= newdata.Guard.GetFloat() ? newdata.Guard : olddata.Guard;
                            break;
                        case CustomRoles.NonReport:
                            olddata.GiveNonReport = olddata.GiveNonReport.InfoGetBool() == false ? newdata.GiveNonReport : olddata.GiveNonReport;
                            if (!olddata.GiveNonReport.InfoGetBool())
                            {
                                olddata.OptionNonReportMode = newdata.OptionNonReportMode;
                                break;
                            }
                            break;
                        case CustomRoles.Watching: olddata.GiveWatching = olddata.GiveWatching.InfoGetBool() == false ? newdata.GiveWatching : olddata.GiveWatching; break;
                        case CustomRoles.Tiebreaker: olddata.GiveTiebreaker = olddata.GiveTiebreaker.InfoGetBool() == false ? newdata.GiveTiebreaker : olddata.GiveTiebreaker; break;
                        case CustomRoles.Opener: olddata.GiveOpener = olddata.GiveOpener.InfoGetBool() == false ? newdata.GiveOpener : olddata.GiveOpener; break;
                        case CustomRoles.Elector: olddata.GiveElector = olddata.GiveElector.InfoGetBool() == false ? newdata.GiveElector : olddata.GiveElector; break;
                        case CustomRoles.Transparent: olddata.GiveTransparent = olddata.GiveTransparent.InfoGetBool() == false ? newdata.GiveTransparent : olddata.GiveTransparent; break;
                        case CustomRoles.Notvoter: olddata.GiveNotvoter = olddata.GiveNotvoter.InfoGetBool() == false ? newdata.GiveNotvoter : olddata.GiveNotvoter; break;
                        case CustomRoles.Water: olddata.GiveWater = olddata.GiveWater.InfoGetBool() == false ? newdata.GiveWater : olddata.GiveWater; break;
                        case CustomRoles.Clumsy: olddata.GiveClumsy = olddata.GiveClumsy.InfoGetBool() == false ? newdata.GiveClumsy : olddata.GiveClumsy; break;
                        case CustomRoles.Slacker: olddata.GiveSlacker = olddata.GiveSlacker.InfoGetBool() == false ? newdata.GiveSlacker : olddata.GiveSlacker; break;
                        case CustomRoles.InfoPoor: olddata.GiveInfoPoor = olddata.GiveInfoPoor.InfoGetBool() == false ? newdata.GiveInfoPoor : olddata.GiveInfoPoor; break;
                    }
            }
            else
            {
                olddata.GiveGuesser = olddata.GiveGuesser.InfoGetBool() == false ? newdata.GiveGuesser : olddata.GiveGuesser;
                olddata.GiveManagement = olddata.GiveManagement.InfoGetBool() == false ? newdata.GiveManagement : olddata.GiveManagement;
                olddata.GiveWatching = olddata.GiveWatching.InfoGetBool() == false ? newdata.GiveWatching : olddata.GiveWatching;
                olddata.GiveSeeing = olddata.GiveSeeing.InfoGetBool() == false ? newdata.GiveSeeing : olddata.GiveSeeing;
                olddata.GiveAutopsy = olddata.GiveAutopsy.InfoGetBool() == false ? newdata.GiveAutopsy : olddata.GiveAutopsy;
                olddata.GiveTiebreaker = olddata.GiveTiebreaker.InfoGetBool() == false ? newdata.GiveTiebreaker : olddata.GiveTiebreaker;
                olddata.GivePlusVote = olddata.GivePlusVote.InfoGetBool() == false ? newdata.GivePlusVote : olddata.GivePlusVote;
                olddata.GiveRevenger = olddata.GiveRevenger.InfoGetBool() == false ? newdata.GiveRevenger : olddata.GiveRevenger;
                olddata.GiveOpener = olddata.GiveOpener.InfoGetBool() == false ? newdata.GiveOpener : olddata.GiveOpener;
                olddata.GiveSpeeding = olddata.GiveSpeeding.InfoGetBool() == false ? newdata.GiveSpeeding : olddata.GiveSpeeding;
                olddata.GiveGuarding = olddata.GiveGuarding.InfoGetBool() == false ? newdata.GiveGuarding : olddata.GiveGuarding;
                olddata.GiveElector = olddata.GiveElector.InfoGetBool() == false ? newdata.GiveElector : olddata.GiveElector;
                olddata.GiveNonReport = olddata.GiveNonReport.InfoGetBool() == false ? newdata.GiveNonReport : olddata.GiveNonReport;
                olddata.GiveTransparent = olddata.GiveTransparent.InfoGetBool() == false ? newdata.GiveTransparent : olddata.GiveTransparent;
                olddata.GiveNotvoter = olddata.GiveNotvoter.InfoGetBool() == false ? newdata.GiveNotvoter : olddata.GiveNotvoter;
                olddata.GiveWater = olddata.GiveWater.InfoGetBool() == false ? newdata.GiveWater : olddata.GiveWater;
                olddata.GiveClumsy = olddata.GiveClumsy.InfoGetBool() == false ? newdata.GiveClumsy : olddata.GiveClumsy;
                olddata.GiveSlacker = olddata.GiveSlacker.InfoGetBool() == false ? newdata.GiveSlacker : olddata.GiveSlacker;
                olddata.GiveInfoPoor = olddata.GiveInfoPoor.InfoGetBool() == false ? newdata.GiveInfoPoor : olddata.GiveInfoPoor;
            }
        }
    }
}