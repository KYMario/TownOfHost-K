using HarmonyLib;
using Hazel;
using TownOfHost.Attributes;
using TownOfHost.Modules;
using TownOfHost.Roles.Core;
using TownOfHost.Roles.Core.Interfaces;

namespace TownOfHost.Patches.ISystemType;

[HarmonyPatch(typeof(SabotageSystemType), nameof(SabotageSystemType.UpdateSystem))]
public static class SabotageSystemTypeUpdateSystemPatch
{
    private static bool isCooldownModificationEnabled;
    private static float modifiedCooldownSec;
    private static readonly LogHandler logger = Logger.Handler(nameof(SabotageSystemType));

    [GameModuleInitializer]
    public static void Initialize()
    {
        isCooldownModificationEnabled = Options.ModifySabotageCooldown.GetBool();
        modifiedCooldownSec = Options.SabotageCooldown.GetFloat();
    }
    static byte amount;
    public static bool Prefix([HarmonyArgument(0)] PlayerControl player, [HarmonyArgument(1)] MessageReader msgReader)
    {
        var newReader = MessageReader.Get(msgReader);
        amount = newReader.ReadByte();
        newReader.Recycle();

        var nextSabotage = (SystemTypes)amount;
        logger.Info($"PlayerName: {player.GetNameWithRole().RemoveHtmlTags()}, SabotageType: {nextSabotage}");

        //HASモードではサボタージュ不可
        if (Options.CurrentGameMode == CustomGameMode.HideAndSeek || Options.IsStandardHAS) return false;

        if (Options.SuddenDeathMode.GetBool()) return false;
        if (!ExileControllerWrapUpPatch.AllSpawned && !MeetingStates.FirstMeeting) return false;

        if (!CustomRoleManager.OnSabotage(player, nextSabotage))
        {
            return false;
        }
        var roleClass = player.GetRoleClass();
        if (roleClass is IKiller killer)
        {
            //そもそもサボタージュボタン使用不可ならサボタージュ不可
            if (!killer.CanUseSabotageButton()) return false;
            //その他処理が必要であれば処理
            if (roleClass.OnInvokeSabotage(nextSabotage))
            {
                if (AmongUsClient.Instance.AmHost)
                {
                    Main.SabotageType = (SystemTypes)amount;
                    var sb = Translator.GetString($"sb.{(SystemTypes)amount}");
                    if (!Main.NowSabotage)
                        Utils.AddGameLog($"Sabotage", string.Format(Translator.GetString("Log.Sabotage"), Utils.GetPlayerColor(player, true) + $"({Utils.GetTrueRoleName(player.PlayerId, false)})", sb));
                    Main.NowSabotage = true;
                    Main.LastSab = player.PlayerId;
                }
            }
            return roleClass.OnInvokeSabotage(nextSabotage);
        }
        else
        {
            return CanSabotage(player);
        }
    }
    private static bool CanSabotage(PlayerControl player)
    {
        //サボタージュ出来ないキラー役職はサボタージュ自体をキャンセル
        if (!player.Is(CustomRoleTypes.Impostor))
        {
            return false;
        }
        if (AmongUsClient.Instance.AmHost)
        {
            if (!Main.NowSabotage)
            {
                Main.SabotageType = (SystemTypes)amount;
                var sb = Translator.GetString($"sb.{(SystemTypes)amount}");

                Utils.AddGameLog($"Sabotage", string.Format(Translator.GetString("Log.Sabotage"), Utils.GetPlayerColor(player, true) + $"({Utils.GetTrueRoleName(player.PlayerId, false)})", sb));
                Main.NowSabotage = true;
                Main.LastSab = player.PlayerId;
            }
        }
        return true;
    }
    public static void Postfix(SabotageSystemType __instance, bool __runOriginal /* Prefixの結果，本体処理が実行されたかどうか */ )
    {
        if (!__runOriginal || !isCooldownModificationEnabled || !AmongUsClient.Instance.AmHost)
        {
            return;
        }
        // サボタージュクールダウンを変更
        __instance.Timer = modifiedCooldownSec;
        __instance.IsDirty = true;
    }
}

[HarmonyPatch(typeof(ElectricTask), nameof(ElectricTask.Initialize))]
public static class ElectricTaskInitializePatch
{
    public static void Postfix()
    {
        Utils.MarkEveryoneDirtySettings();
        if (!GameStates.IsMeeting)
            Utils.NotifyRoles(ForceLoop: true);
    }
}
[HarmonyPatch(typeof(ElectricTask), nameof(ElectricTask.Complete))]
public static class ElectricTaskCompletePatch
{
    public static void Postfix()
    {
        Utils.MarkEveryoneDirtySettings();
        if (!GameStates.IsMeeting)
            Utils.NotifyRoles(ForceLoop: true);
    }
}
