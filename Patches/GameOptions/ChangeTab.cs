using HarmonyLib;
using UnityEngine;

using TownOfHost.Modules;
using static TownOfHost.Translator;
using static TownOfHost.GameSettingMenuStartPatch;

namespace TownOfHost
{
    [HarmonyPatch(typeof(GameSettingMenu), nameof(GameSettingMenu.ChangeTab))]
    class GameSettingMenuChangeTabPatch
    {
        public static string meg;
        public static void Prefix(GameSettingMenu __instance, [HarmonyArgument(0)] ref int tabNum, [HarmonyArgument(1)] bool previewOnly)
        {
            var flag1 = previewOnly && Controller.currentTouchType is Controller.TouchType.Joystick;
            if (flag1 || !previewOnly)
            {
                ModSettingsTab?.gameObject?.SetActive(false);
                ModSettingsButton?.SelectButton(false);
            }

            if (tabNum == 3)
            {
                __instance.MenuDescriptionText.text = meg;
                ModSettingsTab?.gameObject?.SetActive(true);
                ModSettingsButton?.SelectButton(true);

                if (!flag1)
                {
                    ControllerManager.Instance?.OpenOverlayMenu(ModSettingsTab.name, ModSettingsTab.BackButton, ModSettingsTab.DefaultButtonSelected, ModSettingsTab.ControllerSelectable);
                    ModSettingsTab?.EnableTabControllerGlyphs(true);
                }
            }

            if (!previewOnly)
            {
                if (tabNum is 0 && !IsClick)
                {
                    __instance.ChangeTab(1, false);
                    return;
                }
                IsClick = false;
            }
            else
            {
                if (tabNum is 0)
                {
                    tabNum = 1;
                }
            }
        }
        static int Last = 0;
        public static int ClickCount = 0;
        public static void Postfix(GameSettingMenu __instance, [HarmonyArgument(0)] int tabNum, [HarmonyArgument(1)] bool previewOnly)
        {
            if (previewOnly) return;

            var l = Last;
            if (tabNum == Last && tabNum == 3)
            {
                ClickCount++;
                ShowModSetting = false;
            }
            if (tabNum != Last)
            {
                if (tabNum == 3) ShowModSetting = true;
                else ShowModSetting = false;
                if (Last == 1) VanillaOptionHolder.SetOptinItem();
                Last = tabNum;
            }
            if (100 > ClickCount)
            {
                var rand = IRandom.Instance;
                int rect = IRandom.Instance.Next(1, 101);
                if (rect < 40)
                    meg = GetString("ModSettingInfo0");
                else if (rect < 50)
                    meg = GetString("ModSettingInfo10");
                else if (rect < 60)
                    meg = GetString("ModSettingInfo1");
                else if (rect < 70)
                    meg = GetString("ModSettingInfo2");
                else if (rect < 80)
                    meg = GetString("ModSettingInfo3");
                else if (rect < 90)
                    meg = GetString("ModSettingInfo4");
                else if (rect < 95)
                    meg = GetString("ModSettingInfo5");
                else if (rect < 99)
                    meg = GetString("ModSettingInfo6");
                else
                    meg = GetString("ModSettingInfo7");
            }
            else meg = GetString("ModSettingInfo9");

            if (tabNum == 1)
            {
                _ = new LateTask(() =>
                {
                    if (__instance?.GameSettingsTab?.Children is null) return;
                    foreach (var ob in __instance.GameSettingsTab.Children)
                    {
                        switch (ob.Title)
                        {
                            case StringNames.GameShortTasks:
                            case StringNames.GameLongTasks:
                            case StringNames.GameCommonTasks:
                                ob.TryCast<NumberOption>().ValidRange = new FloatRange(0, 99);
                                break;
                            case StringNames.GameKillCooldown:
                                ob.TryCast<NumberOption>().Increment = 0.5f;
                                ob.TryCast<NumberOption>().ValidRange = new FloatRange(0, 180);
                                break;
                            case StringNames.GameNumImpostors:
                                if (DebugModeManager.IsDebugMode)
                                {
                                    ob.TryCast<NumberOption>().ValidRange.min = 0;
                                }
                                break;
                            case StringNames.GameTaskBarMode:
                            case StringNames.GameConfirmImpostor:
                                ob.transform.position = new Vector3(999f, 999f);
                                break;
                            case StringNames.GameVotingTime:
                            case StringNames.GameEmergencyCooldown:
                            case StringNames.GameDiscussTime:
                                ob.TryCast<NumberOption>().Increment = 1f;
                                break;
                            default:
                                break;
                        }
                    }
                    GameOptionsSender.RpcSendOptions();
                }, 0.02f, "", true);
            }
        }
    }
}