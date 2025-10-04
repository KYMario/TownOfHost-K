using AmongUs.GameOptions;
using System.Linq;
using TownOfHost.Roles;
using TownOfHost.Roles.AddOns.Impostor;
using TownOfHost.Roles.AddOns.Neutral;
using TownOfHost.Roles.Core;
using TownOfHost.Roles.Madmate;

namespace TownOfHost
{
    static class CustomRolesHelper
    {
        /// <summary>すべての役職(属性は含まない)</summary>
        public static readonly CustomRoles[] AllRoles = EnumHelper.GetAllValues<CustomRoles>().Where(role => role < CustomRoles.NotAssigned).ToArray();
        /// <summary>すべての属性</summary>
        public static readonly CustomRoles[] AllAddOns = EnumHelper.GetAllValues<CustomRoles>().Where(role => role > CustomRoles.NotAssigned).ToArray();
        /// <summary>スタンダードモードで出現できるすべての役職</summary>
        public static readonly CustomRoles[] AllStandardRoles = AllRoles.Where(role => role is not (CustomRoles.HASFox or CustomRoles.HASTroll or CustomRoles.TaskPlayerB)).ToArray();
        /// <summary>HASモードで出現できるすべての役職</summary>
        public static readonly CustomRoles[] AllHASRoles = { CustomRoles.HASFox, CustomRoles.HASTroll };
        public static readonly CustomRoleTypes[] AllRoleTypes = EnumHelper.GetAllValues<CustomRoleTypes>();

        public static bool IsImpostor(this CustomRoles role)
        {
            var roleInfo = role.GetRoleInfo();
            if (roleInfo != null)
                return roleInfo.CustomRoleType == CustomRoleTypes.Impostor;

            return false;
        }
        public static bool IsMadmate(this CustomRoles role)
        {
            if (role is CustomRoles.MadBetrayer) return MadBetrayer.IsMadmate();
            var roleInfo = role.GetRoleInfo();
            if (roleInfo != null)
                return roleInfo.CustomRoleType == CustomRoleTypes.Madmate;
            return role == CustomRoles.SKMadmate;
        }
        public static bool IsImpostorTeam(this CustomRoles role) => role.IsImpostor() || role.IsMadmate();
        public static bool IsNeutral(this CustomRoles role)
        {
            if (role is CustomRoles.MadBetrayer) return MadBetrayer.IsMadmate() is false;
            var roleInfo = role.GetRoleInfo();
            if (roleInfo != null)
                return roleInfo.CustomRoleType == CustomRoleTypes.Neutral || role == CustomRoles.Jackaldoll;
            return role is CustomRoles.HASTroll or CustomRoles.HASFox;
        }
        public static bool IsCrewmate(this CustomRoles role) => role.GetRoleInfo()?.CustomRoleType == CustomRoleTypes.Crewmate || (role is not CustomRoles.Amanojaku and not CustomRoles.GM and not CustomRoles.Twins and not CustomRoles.Faction && !role.IsImpostorTeam() && role > 0 && !role.IsAddOn() && !role.IsGhostRole() && !role.IsLovers() && !role.IsNeutral());
        public static bool IsVanilla(this CustomRoles role)
        {
            return
                role is CustomRoles.Crewmate or
                CustomRoles.Engineer or
                CustomRoles.Scientist or
                CustomRoles.Noisemaker or
                CustomRoles.Tracker or
                CustomRoles.Detective or
                //CustomRoles.GuardianAngel or幽霊役職でやったからちょっと不都合になる
                CustomRoles.Impostor or
                CustomRoles.Shapeshifter or
                CustomRoles.Phantom or
                CustomRoles.Viper;
        }
        public static bool IsAddOn(this CustomRoles roles)
        {
            return
                roles is
                //ラスト系
                CustomRoles.LastImpostor or
                CustomRoles.LastNeutral or
                CustomRoles.Workhorse or
                CustomRoles.OneWolf or
                //バフ
                CustomRoles.Moon or
                CustomRoles.Guesser or
                CustomRoles.Speeding or
                CustomRoles.Guarding or
                CustomRoles.Watching or
                CustomRoles.Lighting or
                CustomRoles.Management or
                CustomRoles.Connecting or
                CustomRoles.Serial or
                CustomRoles.PlusVote or
                CustomRoles.Opener or
                //CustomRoles.AntiTeleporter or
                CustomRoles.Seeing or
                CustomRoles.Revenger or
                CustomRoles.Autopsy or
                CustomRoles.Tiebreaker or
                CustomRoles.MagicHand or
                //デバフ
                CustomRoles.NonReport or
                CustomRoles.Notvoter or
                CustomRoles.Elector or
                CustomRoles.Water or
                CustomRoles.Slacker or
                CustomRoles.Transparent or
                CustomRoles.Amnesia or
                CustomRoles.Clumsy or
                CustomRoles.SlowStarter or
                CustomRoles.InfoPoor
                ;
        }
        public static bool IsBuffAddon(this CustomRoles roles)
        {
            return roles is
                CustomRoles.Moon or
                CustomRoles.Guesser or
                CustomRoles.Speeding or
                CustomRoles.Guarding or
                CustomRoles.Watching or
                CustomRoles.Lighting or
                CustomRoles.Management or
                CustomRoles.Connecting or
                CustomRoles.Serial or
                CustomRoles.PlusVote or
                CustomRoles.Opener or
                //CustomRoles.AntiTeleporter or
                CustomRoles.Seeing or
                CustomRoles.Revenger or
                CustomRoles.Autopsy or
                CustomRoles.Tiebreaker or
                CustomRoles.MagicHand
                ;
        }
        public static bool IsDebuffAddon(this CustomRoles roles)
        {
            return roles is
                CustomRoles.NonReport or
                CustomRoles.Notvoter or
                CustomRoles.Elector or
                CustomRoles.Water or
                CustomRoles.Slacker or
                CustomRoles.Transparent or
                CustomRoles.Amnesia or
                CustomRoles.Clumsy or
                CustomRoles.SlowStarter or
                CustomRoles.InfoPoor
                ;
        }
        public static bool IsCrewmate(this RoleTypes role) =>
            role is RoleTypes.Crewmate or RoleTypes.CrewmateGhost or
                    RoleTypes.Engineer or RoleTypes.GuardianAngel or
                    RoleTypes.Noisemaker or RoleTypes.Scientist or RoleTypes.Tracker or RoleTypes.Detective;
        public static bool IsSubRole(this CustomRoles role) => role.IsAddOn() || role.IsLovers() || role.IsGhostRole() || role is CustomRoles.Amanojaku or CustomRoles.Twins or CustomRoles.Faction;
        public static bool IsLovers(this CustomRoles roles, bool checkonelover = true)
        {
            if (roles is CustomRoles.OneLove && checkonelover) return true;
            return roles is
            CustomRoles.Lovers or
            CustomRoles.RedLovers or
            CustomRoles.YellowLovers or
            CustomRoles.BlueLovers or
            CustomRoles.GreenLovers or
            CustomRoles.WhiteLovers or
            CustomRoles.PurpleLovers or
            CustomRoles.MadonnaLovers;
        }
        public static bool IsLovers(this PlayerControl pc, bool checkonelover = true)
        {
            return pc.Is(CustomRoles.Lovers) || pc.Is(CustomRoles.RedLovers) ||
            pc.Is(CustomRoles.YellowLovers) || pc.Is(CustomRoles.BlueLovers) ||
            pc.Is(CustomRoles.GreenLovers) || pc.Is(CustomRoles.WhiteLovers) ||
            pc.Is(CustomRoles.PurpleLovers) || pc.Is(CustomRoles.MadonnaLovers) ||
            (pc.Is(CustomRoles.OneLove) && checkonelover);
        }
        public static bool IsLovers(this CustomWinner winner)
        {
            return winner is CustomWinner.Lovers or
                            CustomWinner.RedLovers or
                            CustomWinner.BlueLovers or
                            CustomWinner.YellowLovers or
                            CustomWinner.GreenLovers or
                            CustomWinner.WhiteLovers or
                            CustomWinner.PurpleLovers or
                            CustomWinner.MadonnaLovers or
                            CustomWinner.OneLove;
        }
        public static CustomRoles GetLoverRole(this PlayerControl pc)
        {
            if (pc == null) return CustomRoles.NotAssigned;
            var state = PlayerState.GetByPlayerId(pc.PlayerId);
            foreach (var sub in state.SubRoles)
                switch (sub)
                {
                    case CustomRoles.Lovers: return CustomRoles.Lovers;
                    case CustomRoles.RedLovers: return CustomRoles.RedLovers;
                    case CustomRoles.YellowLovers: return CustomRoles.YellowLovers;
                    case CustomRoles.BlueLovers: return CustomRoles.BlueLovers;
                    case CustomRoles.GreenLovers: return CustomRoles.GreenLovers;
                    case CustomRoles.WhiteLovers: return CustomRoles.WhiteLovers;
                    case CustomRoles.PurpleLovers: return CustomRoles.PurpleLovers;
                    case CustomRoles.OneLove: return CustomRoles.OneLove;
                    case CustomRoles.MadonnaLovers: return CustomRoles.MadonnaLovers;
                }
            return CustomRoles.NotAssigned;
        }
        public static bool IsWhiteCrew(this CustomRoles roles)
        {
            return
                roles is
                CustomRoles.UltraStar or
                CustomRoles.TaskStar;
        }
        public static bool IsGhostRole(this PlayerControl pc) => PlayerState.GetByPlayerId(pc.PlayerId)?.GhostRole.IsGhostRole() ?? false;
        public static bool IsGhostRole(this CustomRoles role)
        {
            return role is CustomRoles.GuardianAngel
                        or CustomRoles.Ghostbuttoner
                        or CustomRoles.GhostNoiseSender
                        or CustomRoles.GhostReseter
                        or CustomRoles.GhostRumour
                        or CustomRoles.DemonicTracker
                        or CustomRoles.DemonicVenter
                        or CustomRoles.DemonicCrusher
                        or CustomRoles.DemonicSupporter
                        or CustomRoles.AsistingAngel
                        ;
        }
        public static bool CheckGuesser()
        {
            foreach (var role in AllStandardRoles.Where(r => r.IsEnable()))
            {
                if (RoleAddAddons.GetRoleAddon(role, out var op, subrole: CustomRoles.Guesser))
                    if (op.GiveGuesser.GetBool()) return true;
            }
            if (CustomRoles.LastImpostor.IsPresent() && LastImpostor.giveguesser) return true;
            if (CustomRoles.LastNeutral.IsPresent() && LastNeutral.GiveGuesser.GetBool()) return true;

            return false;
        }
        public static CustomRoleTypes GetCustomRoleTypes(this CustomRoles role)
        {
            CustomRoleTypes type = CustomRoleTypes.Crewmate;

            if (role is CustomRoles.MadBetrayer) return MadBetrayer.IsMadmate() ? type = CustomRoleTypes.Madmate : CustomRoleTypes.Neutral;
            var roleInfo = role.GetRoleInfo();
            if (roleInfo != null)
                return roleInfo.CustomRoleType;

            if (role.IsImpostor()) type = CustomRoleTypes.Impostor;
            if (role.IsNeutral()) type = CustomRoleTypes.Neutral;
            if (role.IsMadmate()) type = CustomRoleTypes.Madmate;
            return type;
        }
        public static int GetCount(this CustomRoles role)
        {
            if (role.IsVanilla())
            {
                var roleOpt = Main.NormalOptions.RoleOptions;
                return role switch
                {
                    CustomRoles.GuardianAngel => roleOpt.GetNumPerGame(RoleTypes.GuardianAngel),
                    CustomRoles.Crewmate => roleOpt.GetNumPerGame(RoleTypes.Crewmate),
                    _ => Options.GetRoleCount(role)
                };
            }
            else
            {
                return Options.GetRoleCount(role);
            }
        }
        public static int GetChance(this CustomRoles role)
        {
            if (role.IsVanilla())
            {
                var roleOpt = Main.NormalOptions.RoleOptions;
                return role switch
                {
                    CustomRoles.GuardianAngel => roleOpt.GetChancePerGame(RoleTypes.GuardianAngel),
                    CustomRoles.Crewmate => roleOpt.GetChancePerGame(RoleTypes.Crewmate),
                    _ => Options.GetRoleChance(role)
                };
            }
            else
            {
                return Options.GetRoleChance(role);
            }
        }
        public static bool IsEnable(this CustomRoles role) => role.GetCount() > 0;
        public static bool CanMakeMadmate(this CustomRoles role)
        {
            if (role.GetRoleInfo() is SimpleRoleInfo info)
            {
                return info.CanMakeMadmate;
            }

            return false;
        }
        public static RoleTypes GetRoleTypes(this CustomRoles role)
        {
            var roleInfo = role.GetRoleInfo();
            if (roleInfo != null)
                return roleInfo.BaseRoleType.Invoke();

            if (Options.CurrentGameMode == CustomGameMode.TaskBattle && TaskBattle.TaskBattleCanVent.GetBool() && role is CustomRoles.TaskPlayerB)
                return RoleTypes.Engineer;

            return role switch
            {
                CustomRoles.GM => RoleTypes.GuardianAngel,
                CustomRoles.Emptiness => RoleTypes.CrewmateGhost,
                CustomRoles.SKMadmate => Options.SkMadCanUseVent.GetBool() ? RoleTypes.Engineer : RoleTypes.Crewmate,

                _ => role.IsImpostor() ? RoleTypes.Impostor : RoleTypes.Crewmate,
            };
        }
    }
    public enum CountTypes
    {
        OutOfGame,
        None,
        Crew,
        Impostor,
        Jackal,
        Remotekiller,
        TaskPlayer,
        GrimReaper,
        Fox,
        MilkyWay
    }
}