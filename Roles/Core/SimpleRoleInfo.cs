using System;
using UnityEngine;
using AmongUs.GameOptions;
using TownOfHost.Roles.Core.Descriptions;

using static TownOfHost.Options;

namespace TownOfHost.Roles.Core;

public enum From
{
    None,
    AmongUs,
    TheOtherRoles,
    TOR_GM_Edition,
    TOR_GM_Haoming_Edition,
    SuperNewRoles,
    ExtremeRoles,
    NebulaontheShip,
    au_libhalt_net,
    FoolersMod,
    SheriffMod,
    Jester,
    TownOfUs,
    TownOfHost,
    TownOfHost_Y,
    TownOfHost_for_E,
    TownOfHost_E,
    Speyrp,
    RevolutionaryHostRoles,
    Love_Couple_Mod

}
public class SimpleRoleInfo
{
    public Type ClassType;
    public Func<PlayerControl, RoleBase> CreateInstance;
    public CustomRoles RoleName;
    public Func<RoleTypes> BaseRoleType;
    public CustomRoleTypes CustomRoleType;
    public CountTypes CountType;
    public Color RoleColor;
    public string RoleColorCode;
    public int ConfigId;
    public TabGroup Tab;
    public OptionItem RoleOption => CustomRoleSpawnChances[RoleName];
    public bool IsEnable = false;
    public OptionCreatorDelegate OptionCreator;
    public string ChatCommand;
    /// <summary>本人視点のみインポスターに見える役職</summary>
    public bool IsDesyncImpostor;
    private Func<AudioClip> introSound;
    public AudioClip IntroSound => introSound?.Invoke();
    private Func<bool> canMakeMadmate;
    public Func<string> Desc;
    public bool CanMakeMadmate => canMakeMadmate?.Invoke() == true;
    public RoleAssignInfo AssignInfo { get; }
    public From From;
    /// <summary>コンビネーション役職</summary>
    public CombinationRoles Combination;
    /// <summary>役職の説明関係</summary>
    public RoleDescription Description { get; private set; }
    /// <summary>チームを視認することができない</summary>
    public bool IsCantSeeTeammates;
    /// <summary>オプションの順序低い順に並べられる。(大まかな順番 , 細かな順番)</summary>
    public (int TabNumber, int SortNumber) OptionSort;
    private SimpleRoleInfo(
        Type classType,
        Func<PlayerControl, RoleBase> createInstance,
        CustomRoles roleName,
        Func<RoleTypes> baseRoleType,
        CustomRoleTypes customRoleType,
        CountTypes countType,
        int configId,
        OptionCreatorDelegate optionCreator,
        string chatCommand,
        string colorCode,
        (int TabNumber, int SortNumber)? OptionSort,
        bool isDesyncImpostor,
        TabGroup tab,
        Func<AudioClip> introSound,
        Func<bool> canMakeMadmate,
        RoleAssignInfo assignInfo,
        CombinationRoles combination,
        From from,
        bool isCantSeeTeammates,
        Func<string> Desc
    )
    {
        ClassType = classType;
        CreateInstance = createInstance;
        RoleName = roleName;
        BaseRoleType = baseRoleType;
        CustomRoleType = customRoleType;
        CountType = countType;
        ConfigId = configId;
        OptionCreator = optionCreator;
        this.OptionSort = OptionSort.HasValue ? OptionSort.Value : (0, 0);
        IsDesyncImpostor = isDesyncImpostor;
        this.introSound = introSound;
        this.canMakeMadmate = canMakeMadmate;
        ChatCommand = chatCommand;
        AssignInfo = assignInfo;
        From = from;
        Combination = combination;
        IsCantSeeTeammates = isCantSeeTeammates;
        this.Desc = Desc;

        if (colorCode == "")
            colorCode = customRoleType switch
            {
                CustomRoleTypes.Impostor or CustomRoleTypes.Madmate => "#ff1919",
                CustomRoleTypes.Crewmate => "#8cffff",
                _ => "#ffffff"
            };
        RoleColorCode = colorCode;

        _ = ColorUtility.TryParseHtmlString(colorCode, out RoleColor);

        if (tab == TabGroup.MainSettings)
            tab = CustomRoleType switch
            {
                CustomRoleTypes.Impostor => TabGroup.ImpostorRoles,
                CustomRoleTypes.Madmate => TabGroup.MadmateRoles,
                CustomRoleTypes.Crewmate => TabGroup.CrewmateRoles,
                CustomRoleTypes.Neutral => TabGroup.NeutralRoles,
                _ => tab
            };
        Tab = tab;

        CustomRoleManager.AllRolesInfo.Add(roleName, this);
        CustomRoleManager.CustomRoleIds.Add(configId, roleName);
    }
    public static SimpleRoleInfo Create(
        Type classType,
        Func<PlayerControl, RoleBase> createInstance,
        CustomRoles roleName,
        Func<RoleTypes> baseRoleType,
        CustomRoleTypes customRoleType,
        int configId,
        OptionCreatorDelegate optionCreator,
        string chatCommand,
        string colorCode = "",
        (int TabNumber, int SortNumber)? OptionSort = null,
        bool isDesyncImpostor = false,
        TabGroup tab = TabGroup.MainSettings,
        Func<AudioClip> introSound = null,
        Func<bool> canMakeMadmate = null,
        CountTypes? countType = null,
        RoleAssignInfo assignInfo = null,
        CombinationRoles combination = CombinationRoles.None,
        From from = From.None,
        bool isCantSeeTeammates = false,
        Func<string> Desc = null
    )
    {
        countType ??= customRoleType == CustomRoleTypes.Impostor ?
            CountTypes.Impostor :
            CountTypes.Crew;
        assignInfo ??= new RoleAssignInfo(roleName, customRoleType);

        var roleInfo = new SimpleRoleInfo(
            classType,
            createInstance,
            roleName,
            baseRoleType,
            customRoleType,
            countType.Value,
            configId,
            optionCreator,
            chatCommand,
            colorCode,
            OptionSort,
            isDesyncImpostor,
            tab,
            introSound,
            canMakeMadmate,
            assignInfo,
            combination,
            from,
            isCantSeeTeammates,
            Desc
            );
        roleInfo.Description = new SingleRoleDescription(roleInfo);
        return roleInfo;
    }
    public static SimpleRoleInfo CreateForVanilla(
        Type classType,
        Func<PlayerControl, RoleBase> createInstance,
        RoleTypes baseRoleType,
        OptionCreatorDelegate optionCreator,
        string colorCode = "",
        bool canMakeMadmate = false,
        RoleAssignInfo assignInfo = null,
        CombinationRoles combination = CombinationRoles.None,
        From from = From.None
    )
    {
        CustomRoles roleName;
        CustomRoleTypes customRoleType;
        CountTypes countType = CountTypes.Crew;
        int configId = -1;
        (int TabNumber, int SortNumber) OptionSort = (0, 0);

        switch (baseRoleType)
        {
            case RoleTypes.Engineer:
                roleName = CustomRoles.Engineer;
                customRoleType = CustomRoleTypes.Crewmate;
                configId = 200;
                OptionSort = (0, 1);
                break;
            case RoleTypes.Scientist:
                roleName = CustomRoles.Scientist;
                customRoleType = CustomRoleTypes.Crewmate;
                configId = 250;
                OptionSort = (0, 2);
                break;
            case RoleTypes.Tracker:
                roleName = CustomRoles.Tracker;
                customRoleType = CustomRoleTypes.Crewmate;
                configId = 300;
                OptionSort = (0, 3);
                break;
            case RoleTypes.Noisemaker:
                roleName = CustomRoles.Noisemaker;
                customRoleType = CustomRoleTypes.Crewmate;
                configId = 350;
                OptionSort = (0, 4);
                break;
            case RoleTypes.Detective:
                roleName = CustomRoles.Detective;
                customRoleType = CustomRoleTypes.Crewmate;
                configId = 23050;
                OptionSort = (0, 5);
                break;
            case RoleTypes.GuardianAngel:
                roleName = CustomRoles.GuardianAngel;
                customRoleType = CustomRoleTypes.Crewmate;
                configId = -2;
                break;
            case RoleTypes.Impostor:
                roleName = CustomRoles.Impostor;
                customRoleType = CustomRoleTypes.Impostor;
                countType = CountTypes.Impostor;
                configId = -3;
                OptionSort = (0, 0);
                break;
            case RoleTypes.Shapeshifter:
                roleName = CustomRoles.Shapeshifter;
                customRoleType = CustomRoleTypes.Impostor;
                countType = CountTypes.Impostor;
                configId = 30;
                OptionSort = (0, 1);
                break;
            case RoleTypes.Phantom:
                roleName = CustomRoles.Phantom;
                customRoleType = CustomRoleTypes.Impostor;
                countType = CountTypes.Impostor;
                configId = 40;
                break;
            case RoleTypes.Viper:
                roleName = CustomRoles.Viper;
                customRoleType = CustomRoleTypes.Impostor;
                countType = CountTypes.Impostor;
                configId = 23100;
                OptionSort = (0, 2);
                break;
            default:
                roleName = CustomRoles.Crewmate;
                customRoleType = CustomRoleTypes.Crewmate;
                configId = -1;
                OptionSort = (0, 0);
                break;
        }
        var roleInfo = new SimpleRoleInfo(
            classType,
            createInstance,
            roleName,
            () => baseRoleType,
            customRoleType,
            countType,
            configId,
            optionCreator,
            null,
            colorCode,
            OptionSort,
            false,
            TabGroup.MainSettings,
            null,
            () => canMakeMadmate,
            assignInfo ?? new(roleName, customRoleType),
            combination,
            from,
            false,
            null);
        roleInfo.Description = new VanillaRoleDescription(roleInfo, baseRoleType);
        return roleInfo;
    }
    public delegate void OptionCreatorDelegate();
}