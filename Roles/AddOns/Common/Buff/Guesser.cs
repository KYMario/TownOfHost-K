using System.Collections.Generic;
using UnityEngine;
using TownOfHost.Roles.Core;
using static TownOfHost.Options;
using TownOfHost.Attributes;

namespace TownOfHost.Roles.AddOns.Common;

public static class Guesser
{
    private static readonly int Id = 16900;
    private static Color RoleColor = UtilsRoleText.GetRoleColor(CustomRoles.Guesser);
    public static string SubRoleMark = Utils.ColorString(RoleColor, "∮");
    private static List<byte> playerIdList = new();

    public static OptionItem CanGuessTime;
    public static OptionItem OwnCanGuessTime;
    //crew
    public static OptionItem Crewmateset;
    public static OptionItem CCanGuessVanilla;
    public static OptionItem CCanGuessNakama;
    public static OptionItem CCanWhiteCrew;
    //imp
    public static OptionItem impset;
    public static OptionItem ICanGuessVanilla;
    public static OptionItem ICanGuessNakama;
    public static OptionItem ICanGuessTaskDoneSnitch;
    public static OptionItem ICanWhiteCrew;
    //mad
    public static OptionItem Madset;
    public static OptionItem MCanGuessVanilla;
    public static OptionItem MCanGuessNakama;
    public static OptionItem MCanGuessTaskDoneSnitch;
    public static OptionItem MCanWhiteCrew;
    //Neutral
    public static OptionItem Neuset;
    public static OptionItem NCanGuessVanilla;
    public static OptionItem NCanGuessTaskDoneSnitch;
    public static OptionItem NCanWhiteCrew;
    public static void SetupCustomOption()
    {
        SetupRoleOptions(Id, TabGroup.Addons, CustomRoles.Guesser, fromtext: "<color=#000000>From:</color><color=#ff0000>The Other Roles</color></size>");
        var assign = AddOnsAssignData.Create(Id + 10, CustomRoles.Guesser, true, true, true, true);
        impset = assign.ImpostorMaximum;
        Crewmateset = assign.CrewmateMaximum;
        Madset = assign.MadmateMaximum;
        Neuset = assign.NeutralMaximum;

        ObjectOptionitem.Create(Id + 70, "AddonOption", true, "", TabGroup.Addons).SetOptionName(() => "Role Option").SetSubRoleOptionItem(CustomRoles.Guesser);
        //共通設定
        CanGuessTime = IntegerOptionItem.Create(Id + 49, "CanGuessTime", new(1, 15, 1), 3, TabGroup.Addons, false).SetSubRoleOptionItem(CustomRoles.Guesser).SetValueFormat(OptionFormat.Players);
        OwnCanGuessTime = IntegerOptionItem.Create(Id + 50, "OwnCanGuessTime", new(1, 15, 1), 1, TabGroup.Addons, false).SetSubRoleOptionItem(CustomRoles.Guesser).SetValueFormat(OptionFormat.Players);
        //クルーメイト
        ObjectOptionitem.Create(Id + 52, "AddonOption", true, "", TabGroup.Addons).SetOptionName(() => "Crewmate Option").SetSubRoleOptionItem(CustomRoles.Guesser).SetColor(ModColors.CrewMateBlue);
        CCanGuessVanilla = BooleanOptionItem.Create(Id + 53, "CanGuessVanilla", true, TabGroup.Addons, false).SetParentRole(CustomRoles.Guesser).SetColor(ModColors.CrewMateBlue); ;
        CCanGuessNakama = BooleanOptionItem.Create(Id + 54, "CanGuessNakama", true, TabGroup.Addons, false).SetParentRole(CustomRoles.Guesser).SetColor(ModColors.CrewMateBlue); ;
        CCanWhiteCrew = BooleanOptionItem.Create(Id + 55, "CanWhiteCrew", false, TabGroup.Addons, false).SetParentRole(CustomRoles.Guesser).SetColor(ModColors.CrewMateBlue); ;
        //imp
        ObjectOptionitem.Create(Id + 51, "AddonOption", true, "", TabGroup.Addons).SetOptionName(() => "Imposotr Option").SetSubRoleOptionItem(CustomRoles.Guesser).SetColor(ModColors.ImpostorRed);
        ICanGuessVanilla = BooleanOptionItem.Create(Id + 57, "CanGuessVanilla", true, TabGroup.Addons, false).SetParentRole(CustomRoles.Guesser).SetColor(ModColors.ImpostorRed);
        ICanGuessNakama = BooleanOptionItem.Create(Id + 58, "CanGuessNakama", true, TabGroup.Addons, false).SetParentRole(CustomRoles.Guesser).SetColor(ModColors.ImpostorRed);
        ICanGuessTaskDoneSnitch = BooleanOptionItem.Create(Id + 59, "CanGuessTaskDoneSnitch", false, TabGroup.Addons, false).SetParentRole(CustomRoles.Guesser).SetColor(ModColors.ImpostorRed);
        ICanWhiteCrew = BooleanOptionItem.Create(Id + 60, "CanWhiteCrew", false, TabGroup.Addons, false).SetParentRole(CustomRoles.Guesser).SetColor(ModColors.ImpostorRed);
        //mad
        ObjectOptionitem.Create(Id + 61, "AddonOption", true, "", TabGroup.Addons).SetOptionName(() => "Madmate Option").SetSubRoleOptionItem(CustomRoles.Guesser).SetColor(ModColors.MadMateOrenge);
        MCanGuessVanilla = BooleanOptionItem.Create(Id + 62, "CanGuessVanilla", true, TabGroup.Addons, false).SetParentRole(CustomRoles.Guesser).SetColor(ModColors.MadMateOrenge);
        MCanGuessNakama = BooleanOptionItem.Create(Id + 63, "CanGuessNakama", true, TabGroup.Addons, false).SetParentRole(CustomRoles.Guesser).SetColor(ModColors.MadMateOrenge);
        MCanGuessTaskDoneSnitch = BooleanOptionItem.Create(Id + 64, "CanGuessTaskDoneSnitch", true, TabGroup.Addons, false).SetParentRole(CustomRoles.Guesser).SetColor(ModColors.MadMateOrenge);
        MCanWhiteCrew = BooleanOptionItem.Create(Id + 65, "CanWhiteCrew", false, TabGroup.Addons, false).SetParentRole(CustomRoles.Guesser).SetColor(ModColors.MadMateOrenge);
        //Neu
        ObjectOptionitem.Create(Id + 66, "AddonOption", true, "", TabGroup.Addons).SetOptionName(() => "Neutral Option").SetSubRoleOptionItem(CustomRoles.Guesser).SetColor(ModColors.NeutralGray);
        NCanGuessVanilla = BooleanOptionItem.Create(Id + 67, "CanGuessVanilla", true, TabGroup.Addons, false).SetParentRole(CustomRoles.Guesser).SetColor(ModColors.NeutralGray);
        NCanGuessTaskDoneSnitch = BooleanOptionItem.Create(Id + 68, "CanGuessTaskDoneSnitch", false, TabGroup.Addons, false).SetParentRole(CustomRoles.Guesser).SetColor(ModColors.NeutralGray);
        NCanWhiteCrew = BooleanOptionItem.Create(Id + 69, "CanWhiteCrew", false, TabGroup.Addons, false).SetParentRole(CustomRoles.Guesser).SetColor(ModColors.NeutralGray);
    }
    [GameModuleInitializer]

    public static void Init()
    {
        playerIdList = new();
    }
    public static void Add(byte playerId)
    {
        if (!playerIdList.Contains(playerId))
            playerIdList.Add(playerId);
    }
}