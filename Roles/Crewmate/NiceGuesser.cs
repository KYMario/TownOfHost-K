using System;
using System.Collections.Generic;
using System.Reflection.Emit;
using AmongUs.GameOptions;
using HarmonyLib;

using TownOfHost.Modules;
using TownOfHost.Roles.Core;

using static TownOfHost.Translator;

namespace TownOfHost.Roles.Crewmate;

public sealed class NiceGuesser : RoleBase
{
    public static readonly SimpleRoleInfo RoleInfo =
        SimpleRoleInfo.Create(
            typeof(NiceGuesser),
            player => new NiceGuesser(player),
            CustomRoles.NiceGuesser,
            () => RoleTypes.Crewmate,
            CustomRoleTypes.Crewmate,
            70330,
            SetupOptionItem,
            "ng",
            "#dcf500",
            (7, 0)
        );

    public NiceGuesser(PlayerControl player)
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
    private static OptionItem CanGuessWhiteCrew;

    private enum OptionName
    {
        CanGuessTime,
        OwnCanGuessTime,
        CanGuessVanilla,
        CanGuessNakama,
        CanWhiteCrew
    }

    private static void SetupOptionItem()
    {
        CanGuessTime = IntegerOptionItem.Create(RoleInfo, 10, OptionName.CanGuessTime, new(1, 15, 1), 3, false)
            .SetValueFormat(OptionFormat.Times);
        OwnCanGuessTime = IntegerOptionItem.Create(RoleInfo, 11, OptionName.OwnCanGuessTime, new(1, 15, 1), 1, false)
            .SetValueFormat(OptionFormat.Times);
        CanGuessVanilla = BooleanOptionItem.Create(RoleInfo, 12, OptionName.CanGuessVanilla, true, false);
        CanGuessNakama = BooleanOptionItem.Create(RoleInfo, 13, OptionName.CanGuessNakama, true, false);
        CanGuessWhiteCrew = BooleanOptionItem.Create(RoleInfo, 14, OptionName.CanWhiteCrew, false, false);
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
        private static readonly System.Reflection.MethodInfo PcIsCustomRoleMethod =
            AccessTools.Method(typeof(ExtendedPlayerControl), nameof(ExtendedPlayerControl.Is), new[] { typeof(PlayerControl), typeof(CustomRoles) });

        private static readonly System.Reflection.MethodInfo IsGuesserLikeMethod =
            AccessTools.Method(typeof(GuessManagerGuesserMsgPatch), nameof(IsGuesserLike));

        private static void Prefix(PlayerControl pc, string msg)
        {
            if (pc == null || !pc.Is(CustomRoles.NiceGuesser) || !IsBtCommand(msg)) return;

            var state = PlayerState.GetByPlayerId(pc.PlayerId);
            state?.SetSubRole(CustomRoles.Guesser);
        }

        // Guesser role check should treat NiceGuesser as Guesser only during /cmd bt handling.
        private static IEnumerable<CodeInstruction> Transpiler(IEnumerable<CodeInstruction> instructions)
        {
            foreach (var code in instructions)
            {
                if (code.Calls(PcIsCustomRoleMethod))
                {
                    yield return new CodeInstruction(OpCodes.Call, IsGuesserLikeMethod);
                }
                else
                {
                    yield return code;
                }
            }
        }

        private static bool IsGuesserLike(PlayerControl target, CustomRoles role)
        {
            if (target == null) return false;

            if (target.Is(role)) return true;

            return role == CustomRoles.Guesser && target.Is(CustomRoles.NiceGuesser);
        }

        private static void Postfix(PlayerControl pc, string msg, ref bool __result)
        {
            if (pc == null || !pc.Is(CustomRoles.NiceGuesser) || !IsBtCommand(msg)) return;

            // Mark command as handled so the raw /cmd bt line is not exposed in public chat.
            __result = true;
        }
    }

    [HarmonyPatch(typeof(GuessManager), nameof(GuessManager.GuessCountCrewandMad))]
    private static class GuessCountCrewAndMadPatch
    {
        private static bool Prefix(PlayerControl pc, ref bool __result)
        {
            if (pc == null || !pc.Is(CustomRoles.NiceGuesser))
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

    [HarmonyPatch(typeof(GuessManager), nameof(GuessManager.GetCrewmateGuessResult))]
    private static class GetCrewmateGuessResultPatch
    {
        private static bool Prefix(PlayerControl pc, PlayerControl target, CustomRoles guessrole, ref bool __result)
        {
            if (pc == null || !pc.Is(CustomRoles.NiceGuesser))
                return true;

            if (guessrole.IsCrewmate() && !CanGuessNakama.GetBool())
            {
                Utils.SendMessage(GetString("GuessTeamMate"), pc.PlayerId, Utils.ColorString(UtilsRoleText.GetRoleColor(CustomRoles.Crewmate), GetString("GuessTeamMateTitle")));
                __result = true;
                return false;
            }

            if (guessrole.IsWhiteCrew() && !CanGuessWhiteCrew.GetBool())
            {
                Utils.SendMessage(string.Format(GetString("GuessWhiteRole"), GetString("Crewmate")), pc.PlayerId, Utils.ColorString(UtilsRoleText.GetRoleColor(CustomRoles.UltraStar), GetString("GuessWhiteRoleTitle")));
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
            if (CustomRoles.NiceGuesser.IsPresent()) __result = true;
        }
    }
}