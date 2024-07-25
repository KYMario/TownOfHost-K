using System;
using System.Collections.Generic;
using TownOfHost.Roles.Core;
using static TownOfHost.Options;

namespace TownOfHost.Roles.AddOns.Common
{
    //いつかクソゲーにはなるけど全員の役職分からない状態で試合させたい。
    public static class Amnesia
    {
        private static readonly int Id = 71300;
        public static List<byte> playerIdList = new();
        public static OptionItem Modoru;
        public static OptionItem TriggerDay;
        public static OptionItem TriggerTask;
        public static OptionItem Task;
        public static OptionItem DontCanUseAbility;
        public static OptionItem defaultKillCool;
        public static OptionItem TriggerKill;
        public static OptionItem KillCount;
        public static void SetupCustomOption()
        {
            SetupRoleOptions(Id, TabGroup.Addons, CustomRoles.Amnesia);
            AddOnsAssignData.Create(Id + 10, CustomRoles.Amnesia, true, true, true, true);
            DontCanUseAbility = BooleanOptionItem.Create(Id + 40, "Am.DontCanUseAbility", true, TabGroup.Addons, false).SetParent(CustomRoleSpawnChances[CustomRoles.Amnesia]);
            defaultKillCool = BooleanOptionItem.Create(Id + 41, "Am.defaultKillCool", true, TabGroup.Addons, false).SetParent(DontCanUseAbility);
            TriggerDay = BooleanOptionItem.Create(Id + 50, "Am.TriggerDay", true, TabGroup.Addons, false).SetParent(CustomRoleSpawnChances[CustomRoles.Amnesia]);
            Modoru = IntegerOptionItem.Create(Id + 51, "Am.modru", new(1, 99, 1), 4, TabGroup.Addons, false).SetParent(TriggerDay).SetValueFormat(OptionFormat.day);
            TriggerTask = BooleanOptionItem.Create(Id + 52, "Am.TriggerTask", true, TabGroup.Addons, false).SetParent(CustomRoleSpawnChances[CustomRoles.Amnesia]);
            Task = IntegerOptionItem.Create(Id + 53, "Am.Task", new(1, 255, 1), 4, TabGroup.Addons, false).SetParent(TriggerTask);
            TriggerKill = BooleanOptionItem.Create(Id + 54, "Am.TriggerKill", true, TabGroup.Addons, false).SetParent(CustomRoleSpawnChances[CustomRoles.Amnesia]);
            KillCount = IntegerOptionItem.Create(Id + 55, "Am.KillCount", new(1, 15, 1), 2, TabGroup.Addons, false).SetParent(TriggerKill);
        }

        public static void Init()
        {
            playerIdList = new();
        }
        public static void Add(byte playerId)
        {
            playerIdList.Add(playerId);
        }
        public static void Kesu(byte playerId)
        {
            playerIdList.Remove(playerId);
            PlayerState.GetByPlayerId(playerId).RemoveSubRole(CustomRoles.Amnesia);
            //これはなにかって?英語名変えちゃったからめんどくさいんだっ!!!
            var langId = TranslationController.InstanceExists ? TranslationController.Instance.currentLanguage.languageID : SupportedLangs.English;
            if (Main.ForceJapanese.Value) langId = SupportedLangs.Japanese;
            var a = langId == SupportedLangs.English ? "Loss of memory" : "Amnesia";
            Main.gamelog += $"\n{DateTime.Now:HH.mm.ss} [{a}]　" + string.Format(Translator.GetString("Am.log"), Utils.GetPlayerColor(playerId));
        }
        public static bool IsEnable => playerIdList.Count > 0;
        public static bool IsThisRole(byte playerId) => playerIdList.Contains(playerId);

    }
}