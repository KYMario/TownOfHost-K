using System;
using AmongUs.GameOptions;
using HarmonyLib;

using TownOfHost.Modules;
using TownOfHost.Roles.Core;

using static TownOfHost.Translator;

namespace TownOfHost.Roles.Impostor;

public sealed class EvilGuesser : RoleBase
{
    public static readonly SimpleRoleInfo RoleInfo =
        SimpleRoleInfo.Create(
            typeof(EvilGuesser),
            player => new EvilGuesser(player),
            CustomRoles.EvilGuesser,
            () => RoleTypes.Impostor,
            CustomRoleTypes.Impostor,
            73001,
            SetupOptionItem,
            "eg",
            "#ff1919",
            (2, 1)
        );

    public EvilGuesser(PlayerControl player)
        : base(
            RoleInfo,
            player
        )
    {
    }

    private static OptionItem CanGuessTime;
    private static OptionItem OwnCanGuessTime;
    private static OptionItem CanGuessVanilla;
    private static OptionItem CanGuessNakama;
    private static OptionItem CanGuessTaskDoneSnitch;
    private static OptionItem CanGuessWhiteCrew;

    private enum OptionName
    {
        CanGuessTime,
        OwnCanGuessTime,
        CanGuessVanilla,
        CanGuessNakama,
        CanGuessTaskDoneSnitch,
        CanWhiteCrew
    }

    private static void SetupOptionItem()
    {
        CanGuessTime = IntegerOptionItem.Create(RoleInfo, 110, OptionName.CanGuessTime, new(1, 15, 1), 3, false)
            .SetValueFormat(OptionFormat.Times);
        OwnCanGuessTime = IntegerOptionItem.Create(RoleInfo, 111, OptionName.OwnCanGuessTime, new(1, 15, 1), 1, false)
            .SetValueFormat(OptionFormat.Times);
        CanGuessVanilla = BooleanOptionItem.Create(RoleInfo, 112, OptionName.CanGuessVanilla, true, false);
        CanGuessNakama = BooleanOptionItem.Create(RoleInfo, 131, OptionName.CanGuessNakama, true, false);
        CanGuessTaskDoneSnitch = BooleanOptionItem.Create(RoleInfo, 114, OptionName.CanGuessTaskDoneSnitch, false, false);
        CanGuessWhiteCrew = BooleanOptionItem.Create(RoleInfo, 115, OptionName.CanWhiteCrew, false, false);
    }

    private static bool IsBtCommand(string msg)
    {
        if (string.IsNullOrWhiteSpace(msg)) return false;

        var args = msg.Split(' ', StringSplitOptions.RemoveEmptyEntries);
        if (args.Length < 2) return false;
        if (!args[0].Equals("/cmd", StringComparison.OrdinalIgnoreCase)) return false;

        var cmd = args[1].StartsWith("/") ? args[1] : $"/{args[1]}";
        return cmd.Equals("/bt", StringComparison.OrdinalIgnoreCase);
    }

    [HarmonyPatch(typeof(GuessManager), nameof(GuessManager.GuesserMsg))]
    private static class GuessManagerGuesserMsgPatch
    {
        private static void Prefix(PlayerControl pc, string msg)
        {
            if (pc == null || !pc.Is(CustomRoles.EvilGuesser) || !IsBtCommand(msg)) return;

            var state = PlayerState.GetByPlayerId(pc.PlayerId);
            state?.SetSubRole(CustomRoles.Guesser);
        }

        private static void Postfix(PlayerControl pc, string msg, ref bool __result)
        {
            if (pc == null || !pc.Is(CustomRoles.EvilGuesser) || !IsBtCommand(msg)) return;

            __result = true;
        }
    }

    [HarmonyPatch(typeof(GuessManager), nameof(GuessManager.GuessCountImp))]
    private static class GuessCountImpPatch
    {
        private static bool Prefix(PlayerControl pc, ref bool __result)
        {
            if (pc == null || !pc.Is(CustomRoles.EvilGuesser))
                return true;

            if (!GuessManager.GuesserGuessed.ContainsKey(pc.PlayerId)) GuessManager.GuesserGuessed[pc.PlayerId] = 0;
            if (!GuessManager.OneMeetingGuessed.ContainsKey(pc.PlayerId)) GuessManager.OneMeetingGuessed[pc.PlayerId] = 0;

            var shotLimit = CanGuessTime.GetInt();
            var oneMeetingShotLimit = OwnCanGuessTime.GetInt();

            if (GuessManager.GuesserGuessed[pc.PlayerId] >= shotLimit)
            {
                Utils.SendMessage(GetString("GuessercountError"), pc.PlayerId, Utils.ColorString(Palette.AcceptedGreen, GetString("GuessercountErrorT")));
                __result = true;
                return false;
            }

            if (GuessManager.OneMeetingGuessed[pc.PlayerId] >= oneMeetingShotLimit)
            {
                Utils.SendMessage(GetString("GuesserMTGcountError"), pc.PlayerId, Utils.ColorString(Palette.AcceptedGreen, GetString("GuesserMTGcountErrorT")));
                __result = true;
                return false;
            }

            __result = false;
            return false;
        }
    }

    [HarmonyPatch(typeof(GuessManager), nameof(GuessManager.GetImpostorGuessResult))]
    private static class GetImpostorGuessResultPatch
    {
        private static bool Prefix(PlayerControl pc, PlayerControl target, CustomRoles guessrole, ref bool __result)
        {
            if (pc == null || !pc.Is(CustomRoles.EvilGuesser))
                return true;

            if (guessrole is CustomRoles.Snitch && target.AllTasksCompleted() && !CanGuessTaskDoneSnitch.GetBool())
            {
                Utils.SendMessage(string.Format(GetString("GuessSnitch"), GetString("Impostor")), pc.PlayerId, Utils.ColorString(Palette.ImpostorRed, GetString("GuessSnitchTitle")));
                __result = true;
                return false;
            }

            if (guessrole.IsImpostor() && target.Is(CustomRoleTypes.Impostor) && !CanGuessNakama.GetBool())
            {
                Utils.SendMessage(GetString("GuessTeamMate"), pc.PlayerId, Utils.ColorString(UtilsRoleText.GetRoleColor(CustomRoles.Impostor), GetString("GuessTeamMateTitle")));
                __result = true;
                return false;
            }

            if (guessrole.IsWhiteCrew() && !CanGuessWhiteCrew.GetBool())
            {
                Utils.SendMessage(string.Format(GetString("GuessWhiteRole"), GetString("Impostor")), pc.PlayerId, Utils.ColorString(UtilsRoleText.GetRoleColor(CustomRoles.UltraStar), GetString("GuessWhiteRoleTitle")));
                __result = true;
                return false;
            }

            if (guessrole.IsVanilla() && !CanGuessVanilla.GetBool())
            {
                Utils.SendMessage(GetString("GuessVanillaRoleTitle"), pc.PlayerId, Utils.ColorString(UtilsRoleText.GetRoleColor(guessrole), GetString("GuessVanillaRole")));
                __result = true;
                return false;
            }

            __result = false;
            return false;
        }
    }

    [HarmonyPatch(typeof(CustomRolesHelper), "CheckGuesser")]
    private static class CheckGuesserPatch
    {
        private static void Postfix(ref bool __result)
        {
            if (__result) return;
            if (CustomRoles.EvilGuesser.IsPresent()) __result = true;
        }
    }
}