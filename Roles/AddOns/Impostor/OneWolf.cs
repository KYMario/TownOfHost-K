using System.Collections.Generic;
using UnityEngine;
using TownOfHost.Roles.Core;
using static TownOfHost.Options;
using System.Linq;
using AmongUs.GameOptions;

namespace TownOfHost.Roles.AddOns.Common
{
    public static class OneWolf
    {
        private static readonly int Id = 22700;
        public static List<byte> playerIdList = new();
        public static OptionItem OptionImpostorkillMe;
        public static OptionItem OptionMeCanKillImpostor;
        static string[] killmode =
        {
            "OneWolf.NoChange" ,
            "OneWolf.Guard",
            "OneWolf.Remove",
            "OneWolf.GuardAndRemove"
        };
        public static void SetupCustomOption()
        {
            var OneWolfAssingOption = SetupRoleOptions(Id, TabGroup.Addons, CustomRoles.OneWolf, new(1, 3, 1));
            AddOnsAssignData.Create(Id + 10, CustomRoles.OneWolf, false, false, true, false);
            OptionImpostorkillMe = StringOptionItem.Create(Id + 20, "OneWolfImpostorKillMe", killmode, 2, TabGroup.Addons, false)
                .SetParent(OneWolfAssingOption);
            OptionMeCanKillImpostor = StringOptionItem.Create(Id + 21, "OneWolfMeCanKillImpostor", killmode, 2, TabGroup.Addons, false)
                .SetParent(OneWolfAssingOption);
        }
        public static void Init()
        {
            playerIdList = new();
        }
        public static void Add(byte playerId)
        {
            playerIdList.Add(playerId);
        }
        public static void Remove(byte playerId)
        {
            playerIdList.Remove(playerId);
            playerId.GetPlayerState().RemoveSubRole(CustomRoles.OneWolf);
            var player = playerId.GetPlayerControl();

            foreach (var imp in PlayerCatch.AllPlayerControls.Where(pc => pc.GetCustomRole().IsImpostor() && playerIdList.Contains(pc.PlayerId) is false))
            {
                if (playerId == PlayerControl.LocalPlayer.PlayerId)
                {
                    imp.Data.Role.NameColor = Palette.ImpostorRed;
                }
                imp.RpcSetRoleDesync(imp.IsAlive() ? RoleTypes.Impostor : RoleTypes.ImpostorGhost, player.GetClientId());
                player.RpcSetRoleDesync(player.IsAlive() ? RoleTypes.Impostor : RoleTypes.ImpostorGhost, imp.GetClientId());
            }
            UtilsNotifyRoles.NotifyRoles(true, true);
        }
        public static void OnCheckMurder(MurderInfo info)
        {
            //0:キル通る 1:ガードのみ 2:キル後仲間自覚 3:ガードし仲間自覚
            var (killer, target) = info.AppearanceTuple;

            // 一匹狼 => 仲間のインポスター
            if (playerIdList.Contains(killer.PlayerId) && target.GetCustomRole().IsImpostor())
            {
                switch (OptionMeCanKillImpostor.GetValue())
                {
                    case 0:
                    case 2:
                        break;
                    case 1:
                        info.GuardPower = 1;
                        break;
                    case 3:
                        info.GuardPower = 1;
                        Remove(killer.PlayerId);
                        break;
                }
            }
            // 仲間のインポスター => 一匹狼
            if (playerIdList.Contains(target.PlayerId) && killer.GetCustomRole().IsImpostor())
            {
                switch (OptionImpostorkillMe.GetValue())
                {
                    case 0:
                    case 2:
                        break;
                    case 1:
                        info.GuardPower = 1;
                        break;
                    case 3:
                        info.GuardPower = 1;
                        Remove(target.PlayerId);
                        break;
                }
            }
        }
        public static void OnMurderPlayer(MurderInfo info)
        {
            //0:キル通る 1:ガードのみ 2:キル後仲間自覚 3:ガードし仲間自覚
            var (killer, target) = info.AppearanceTuple;

            // 一匹狼 => 仲間のインポスター
            if (playerIdList.Contains(killer.PlayerId) && target.GetCustomRole().IsImpostor() && OptionMeCanKillImpostor.GetValue() is 2)
            {
                Remove(killer.PlayerId);
            }
            // 仲間のインポスター => 一匹狼
            if (playerIdList.Contains(target.PlayerId) && killer.GetCustomRole().IsImpostor() && OptionImpostorkillMe.GetValue() is 2)
            {
                Remove(target.PlayerId);
            }
        }
    }
}