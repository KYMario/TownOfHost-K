using System.Text;
using UnityEngine;
using AmongUs.GameOptions;

using TownOfHost.Roles.Core;
using TownOfHost.Roles.Core.Interfaces;
using System.Collections.Generic;

namespace TownOfHost.Roles.Impostor
{
    public sealed class Insider : RoleBase, IImpostor
    {
        public static readonly SimpleRoleInfo RoleInfo =
            SimpleRoleInfo.Create(
                typeof(Insider),
                player => new Insider(player),
                CustomRoles.Insider,
                () => RoleTypes.Impostor,
                CustomRoleTypes.Impostor,
                5400,
                SetupOptionItem,
                "ins",
                OptionSort: (5, 2),
                from: From.TownOfHost
            );
        public Insider(PlayerControl player)
        : base(
            RoleInfo,
            player
        )
        {
            canSeeImpostorAbilities = optionCanSeeImpostorAbilities.GetBool();
            canSeeAllGhostsRoles = optionCanSeeAllGhostsRoles.GetBool();
            canSeeMadmates = optionCanSeeMadmates.GetBool();
            killCountToSeeMadmates = optionKillCountToSeeMadmates.GetInt();
            CanSeeRoles = optionCanSeeRoles.GetNowRoleValue();
            IsSetCanSeeRole = optionSetCanSeeRoles.GetBool();
        }
        private static OptionItem optionCanSeeAllGhostsRoles;
        private static OptionItem optionCanSeeImpostorAbilities;
        private static OptionItem optionCanSeeMadmates;
        private static OptionItem optionKillCountToSeeMadmates;
        private static OptionItem optionSetCanSeeRoles;
        private static AssignOptionItem optionCanSeeRoles;
        private enum OptionName
        {
            InsiderCanSeeAllGhostsRoles,
            InsiderCanSeeImpostorAbilities,
            InsiderCanSeeMadmates,
            InsiderKillCountToSeeMadmates,
            InsiderCanSeeRoles,
            InsiderSetCanSeeRoles
        }
        private static bool canSeeAllGhostsRoles;
        private static bool canSeeImpostorAbilities;
        private static bool canSeeMadmates;
        private static int killCountToSeeMadmates;
        private static bool IsSetCanSeeRole;
        static List<CustomRoles> CanSeeRoles;

        private static void SetupOptionItem()
        {
            optionCanSeeAllGhostsRoles = BooleanOptionItem.Create(RoleInfo, 10, OptionName.InsiderCanSeeAllGhostsRoles, false, false);
            optionCanSeeImpostorAbilities = BooleanOptionItem.Create(RoleInfo, 11, OptionName.InsiderCanSeeImpostorAbilities, true, false);
            optionCanSeeMadmates = BooleanOptionItem.Create(RoleInfo, 12, OptionName.InsiderCanSeeMadmates, false, false);
            optionKillCountToSeeMadmates = IntegerOptionItem.Create(RoleInfo, 13, OptionName.InsiderKillCountToSeeMadmates, new(0, 15, 1), 2, false, optionCanSeeMadmates)
                .SetValueFormat(OptionFormat.Times);
            optionSetCanSeeRoles = BooleanOptionItem.Create(RoleInfo, 14, OptionName.InsiderSetCanSeeRoles, false, false);
            optionCanSeeRoles = AssignOptionItem.Create(RoleInfo, 15, OptionName.InsiderCanSeeRoles, 0, false, optionSetCanSeeRoles, true, true, true, true);
        }

        ///<summary>
        ///役職を見る前提条件
        ///</summary>
        private bool IsAbilityAvailable(PlayerControl target)
        {
            if (Player == null || target == null) return false;
            if (Player == target) return false;
            if (target.Is(CustomRoles.GM)) return false;
            if (!Player.IsAlive() && (Options.GhostCanSeeOtherRoles.GetBool() || !Options.GhostOptions.GetBool())) return false;
            return true;
        }
        ///<summary>
        ///Impostor, Madmateの内通能力
        ///</summary>
        private bool KnowAllyRole(PlayerControl target)
            => IsAbilityAvailable(target)
            && target.GetCustomRole().GetCustomRoleTypes() switch
            {
                CustomRoleTypes.Impostor => canSeeImpostorAbilities,
                CustomRoleTypes.Madmate => canSeeMadmates && MyState.GetKillCount(true) >= killCountToSeeMadmates,
                _ => false,
            };
        ///<summary>
        ///幽霊の役職が見えるケース
        ///</summary>
        private bool KnowDeadRole(PlayerControl target)
            => IsAbilityAvailable(target)
            && !target.IsAlive()
            && (canSeeAllGhostsRoles //全員見える
            || target.GetRealKiller() == Player); //自分でキルした相手
        ///<summary>
        ///内通or幽霊
        ///</summary>
        private bool KnowTargetRole(PlayerControl target)
            => KnowDeadRole(target) || KnowAllyRole(target);

        private bool CanSeeRole(CustomRoles role)
            => !IsSetCanSeeRole || CanSeeRoles.Contains(role);

        public override void OverrideDisplayRoleNameAsSeer(PlayerControl seen, ref bool enabled, ref Color roleColor, ref string roleText, ref bool addon)
        {
            addon = false;
            enabled |= KnowTargetRole(seen);
            if (Player.IsAlive() && !CanSeeRole(seen.GetCustomRole()))
            {
                roleColor = ModColors.NeutralGray;
                roleText = GetString("Unknown");
            }
        }
        public override void OverrideProgressTextAsSeer(PlayerControl seen, ref bool enabled, ref string text)
        {
            enabled |= KnowAllyRole(seen);
        }
        public override string GetProgressText(bool isComms = false, bool gamelog = false)
        {
            if (!canSeeMadmates) return "";

            int killCount = MyState.GetKillCount(true);
            string mark = killCount >= killCountToSeeMadmates ? "★" : $"({killCount}/{killCountToSeeMadmates})";
            return Utils.ColorString(Palette.ImpostorRed.ShadeColor(0.5f), mark);
        }
        public override string GetMark(PlayerControl seer, PlayerControl seen, bool isForMeeting = false)
        {
            //seenが省略の場合seer
            seen ??= seer;
            var mark = new StringBuilder(50);

            // 死亡したLoversのマーク追加
            if (seen.GetLoverRole() != seer.GetLoverRole() && seen.IsLovers() && KnowDeadRole(seen))
                mark.Append(Utils.ColorString(UtilsRoleText.GetRoleColor(seen.GetLoverRole()), "♥"));

            if (canSeeImpostorAbilities)
            {
                foreach (var impostor in PlayerCatch.AllPlayerControls)
                {
                    if (seer == impostor || impostor.Is(CustomRoles.Insider) || !impostor.Is(CustomRoleTypes.Impostor)) continue;
                    mark.Append(impostor.GetRoleClass()?.GetMark(impostor, seen, isForMeeting));
                }
            }
            return mark.ToString();
        }
    }
}