using System.Collections.Generic;
using System.Linq;
using AmongUs.GameOptions;
using HarmonyLib;
using TownOfHost.Modules;

namespace TownOfHost;

class VanillaOptionHolder
{
    public static Dictionary<VanillaOptionName, OptionItem> VanillaOptions;
    public enum VanillaOptionName//仮に追加することがある場合、各グループの最後に追加する。
    {
        //float/int
        GameKillCooldown, GamePlayerSpeed, GameNumImpostors,
        GameEmergencyCooldown, GameDiscussTime, GameVotingTime,
        GameKillDistance, GameImpostorLight, GameCrewLight,
        GameCommonTasks, GameLongTasks, GameShortTasks, NumEmergencyMeetings,
        MapId,
        //bool
        GameAnonymousVotes = 200
    }
    public static void Initialize()
    {
        var id = 115600;
        VanillaOptions = new();
        foreach (var key in EnumHelper.GetAllValues<VanillaOptionName>())
        {
            OptionItem option = null;
            if ((int)key < 200)
            {
                var Rulu = new FloatValueRule(0, 180, 0.5f);
                var defo = 30f;

                switch (key)
                {
                    case VanillaOptionName.GamePlayerSpeed: Rulu = new(0f, 5, 0.25f); defo = 1.25f; break;
                    case VanillaOptionName.GameEmergencyCooldown: Rulu = new(0, 60, 1); defo = 15; break;
                    case VanillaOptionName.GameVotingTime: Rulu = new(0, 300, 1); defo = 180; break;
                    case VanillaOptionName.GameDiscussTime: Rulu = new(0, 300, 1); defo = 0; break;
                    case VanillaOptionName.GameCrewLight: Rulu = new(0f, 5, 0.01f); defo = 0.5f; break;
                    case VanillaOptionName.GameImpostorLight: Rulu = new(0f, 5, 0.01f); defo = 1f; break;
                    case VanillaOptionName.GameNumImpostors:
                    case VanillaOptionName.NumEmergencyMeetings: Rulu = new(0, 15, 1); defo = 1f; break;
                    case VanillaOptionName.GameKillDistance: Rulu = new(0, 3, 1); defo = 0f; break;
                    case VanillaOptionName.MapId: Rulu = new(0, 5, 1); defo = 1f; break;
                    case VanillaOptionName.GameLongTasks: Rulu = new(0, 99, 1); defo = 2f; break;
                    case VanillaOptionName.GameShortTasks: Rulu = new(0, 99, 1); defo = 4f; break;
                    case VanillaOptionName.GameCommonTasks: Rulu = new(0, 99, 1); defo = 2f; break;
                }
                option = FloatOptionItem.Create(id, key, Rulu, defo, TabGroup.MainSettings, false).SetEnabled(() => false);
            }
            else
            {
                option = BooleanOptionItem.Create(id, key, true, TabGroup.MainSettings, false).SetEnabled(() => false);
            }
            id++;
            VanillaOptions.Add(key, option);
        }
    }
    public static void SetVanillaValue()
    {
        if (Main.NormalOptions is null) return;
        if (AmongUsClient.Instance?.AmHost is not true) return;
        if (VanillaOptions.Values.All(option =>
        {
            return option.DefaultValue == option.CurrentValue;
        }))
        {
            SetOptinItem();
            return;
        }

        foreach (var data in VanillaOptions)
        {
            switch (data.Key)
            {
                case VanillaOptionName.GameKillCooldown: Main.NormalOptions.SetFloat(FloatOptionNames.KillCooldown, data.Value.GetFloat()); break;
                case VanillaOptionName.GamePlayerSpeed: Main.NormalOptions.SetFloat(FloatOptionNames.PlayerSpeedMod, data.Value.GetFloat()); break;
                case VanillaOptionName.GameNumImpostors: Main.NormalOptions.SetInt(Int32OptionNames.NumImpostors, data.Value.GetInt()); break;
                case VanillaOptionName.GameEmergencyCooldown: Main.NormalOptions.SetInt(Int32OptionNames.EmergencyCooldown, data.Value.GetInt()); break;
                case VanillaOptionName.GameDiscussTime: Main.NormalOptions.SetInt(Int32OptionNames.DiscussionTime, data.Value.GetInt()); break;
                case VanillaOptionName.GameVotingTime: Main.NormalOptions.SetInt(Int32OptionNames.VotingTime, data.Value.GetInt()); break;
                case VanillaOptionName.GameKillDistance: Main.NormalOptions.SetInt(Int32OptionNames.KillDistance, data.Value.GetInt()); break;
                case VanillaOptionName.GameImpostorLight: Main.NormalOptions.SetFloat(FloatOptionNames.ImpostorLightMod, data.Value.GetFloat()); break;
                case VanillaOptionName.GameCrewLight: Main.NormalOptions.SetFloat(FloatOptionNames.CrewLightMod, data.Value.GetFloat()); break;
                case VanillaOptionName.GameCommonTasks: Main.NormalOptions.SetInt(Int32OptionNames.NumCommonTasks, data.Value.GetInt()); break;
                case VanillaOptionName.GameLongTasks: Main.NormalOptions.SetInt(Int32OptionNames.NumLongTasks, data.Value.GetInt()); break;
                case VanillaOptionName.GameShortTasks: Main.NormalOptions.SetInt(Int32OptionNames.NumShortTasks, data.Value.GetInt()); break;
                case VanillaOptionName.NumEmergencyMeetings: Main.NormalOptions.SetInt(Int32OptionNames.NumEmergencyMeetings, data.Value.GetInt()); break;
                case VanillaOptionName.GameAnonymousVotes: Main.NormalOptions.SetBool(BoolOptionNames.AnonymousVotes, data.Value.GetBool()); break;
                case VanillaOptionName.MapId: Main.NormalOptions.SetByte(ByteOptionNames.MapId, (byte)data.Value.GetInt()); break;
            }
        }
        StringOptionStartPatch.all.Do(x =>
        {
            x.Value = Main.NormalOptions.GetInt(x.stringOptionName);
            x.ValueText.text = Translator.GetString(x.Values[x.Value]);
        });
        NumberOptionStartPatch.all.Do(x =>
        {
            var opt = x.intOptionName is Int32OptionNames.Invalid ? Main.NormalOptions.GetFloat(x.floatOptionName) : Main.NormalOptions.GetInt(x.intOptionName);
            x.Value = opt;
            x.ValueText.text = x.data.GetValueString(opt);
        });
        ToggleOptionStartPatch.all.Do(x =>
        {
            try
            {
                x.CheckMark.enabled = Main.NormalOptions.GetBool(x.boolOptionName);
            }
            catch { }
        });
        GameOptionsSender.RpcSendOptions();
    }
    public static void SetOptinItem()
    {
        var normaloption = Main.NormalOptions;

        if (normaloption is null) return;

        foreach (var data in VanillaOptions)
        {
            switch (data.Key)
            {
                case VanillaOptionName.GameKillCooldown: data.Value.SetValue((int)(normaloption.GetFloat(FloatOptionNames.KillCooldown) / ((FloatOptionItem)data.Value).Rule.Step), false, false); break;
                case VanillaOptionName.GamePlayerSpeed: data.Value.SetValue((int)(normaloption.GetFloat(FloatOptionNames.PlayerSpeedMod) / ((FloatOptionItem)data.Value).Rule.Step), false, false); break;
                case VanillaOptionName.GameNumImpostors: data.Value.SetValue((int)(normaloption.GetInt(Int32OptionNames.NumImpostors) / ((FloatOptionItem)data.Value).Rule.Step), false, false); break;
                case VanillaOptionName.GameEmergencyCooldown: data.Value.SetValue((int)(normaloption.GetInt(Int32OptionNames.EmergencyCooldown) / ((FloatOptionItem)data.Value).Rule.Step), false, false); break;
                case VanillaOptionName.GameDiscussTime: data.Value.SetValue((int)(normaloption.GetInt(Int32OptionNames.DiscussionTime) / ((FloatOptionItem)data.Value).Rule.Step), false, false); break;
                case VanillaOptionName.GameVotingTime: data.Value.SetValue((int)(normaloption.GetInt(Int32OptionNames.VotingTime) / ((FloatOptionItem)data.Value).Rule.Step), false, false); break;
                case VanillaOptionName.GameKillDistance: data.Value.SetValue((int)(normaloption.GetInt(Int32OptionNames.KillDistance) / ((FloatOptionItem)data.Value).Rule.Step), false, false); break;
                case VanillaOptionName.GameImpostorLight: data.Value.SetValue((int)(normaloption.GetFloat(FloatOptionNames.ImpostorLightMod) / ((FloatOptionItem)data.Value).Rule.Step), false, false); break;
                case VanillaOptionName.GameCrewLight: data.Value.SetValue((int)(normaloption.GetFloat(FloatOptionNames.CrewLightMod) / ((FloatOptionItem)data.Value).Rule.Step), false, false); break;
                case VanillaOptionName.GameCommonTasks: data.Value.SetValue((int)(normaloption.GetInt(Int32OptionNames.NumCommonTasks) / ((FloatOptionItem)data.Value).Rule.Step), false, false); break;
                case VanillaOptionName.GameLongTasks: data.Value.SetValue((int)(normaloption.GetInt(Int32OptionNames.NumLongTasks) / ((FloatOptionItem)data.Value).Rule.Step), false, false); break;
                case VanillaOptionName.GameShortTasks: data.Value.SetValue((int)(normaloption.GetInt(Int32OptionNames.NumShortTasks) / ((FloatOptionItem)data.Value).Rule.Step), false, false); break;
                case VanillaOptionName.NumEmergencyMeetings: data.Value.SetValue((int)(normaloption.GetInt(Int32OptionNames.NumEmergencyMeetings) / ((FloatOptionItem)data.Value).Rule.Step), false, false); break;
                case VanillaOptionName.GameAnonymousVotes: data.Value.SetValue(normaloption.GetBool(BoolOptionNames.AnonymousVotes) ? 1 : 0, false); break;
                case VanillaOptionName.MapId: data.Value.SetValue(normaloption.GetByte(ByteOptionNames.MapId)); break;
            }
        }
        OptionSaver.Save();
    }
    public static void ResetVanilla()
    {
        foreach (var data in EnumHelper.GetAllValues<VanillaOptionName>())
        {
            switch (data)
            {
                case VanillaOptionName.GameKillCooldown: Main.NormalOptions.SetFloat(FloatOptionNames.KillCooldown, 30); break;
                case VanillaOptionName.GamePlayerSpeed: Main.NormalOptions.SetFloat(FloatOptionNames.PlayerSpeedMod, 1.25f); break;
                case VanillaOptionName.GameNumImpostors: Main.NormalOptions.SetInt(Int32OptionNames.NumImpostors, 2); break;
                case VanillaOptionName.GameEmergencyCooldown: Main.NormalOptions.SetInt(Int32OptionNames.EmergencyCooldown, 15); break;
                case VanillaOptionName.GameDiscussTime: Main.NormalOptions.SetInt(Int32OptionNames.DiscussionTime, 0); break;
                case VanillaOptionName.GameVotingTime: Main.NormalOptions.SetInt(Int32OptionNames.VotingTime, 180); break;
                case VanillaOptionName.GameKillDistance: Main.NormalOptions.SetInt(Int32OptionNames.KillDistance, 0); break;
                case VanillaOptionName.GameImpostorLight: Main.NormalOptions.SetFloat(FloatOptionNames.ImpostorLightMod, 1.25f); break;
                case VanillaOptionName.GameCrewLight: Main.NormalOptions.SetFloat(FloatOptionNames.CrewLightMod, 0.5f); break;
                case VanillaOptionName.GameCommonTasks: Main.NormalOptions.SetInt(Int32OptionNames.NumCommonTasks, 2); break;
                case VanillaOptionName.GameLongTasks: Main.NormalOptions.SetInt(Int32OptionNames.NumLongTasks, 2); break;
                case VanillaOptionName.GameShortTasks: Main.NormalOptions.SetInt(Int32OptionNames.NumShortTasks, 4); break;
                case VanillaOptionName.NumEmergencyMeetings: Main.NormalOptions.SetInt(Int32OptionNames.NumEmergencyMeetings, 1); break;
                case VanillaOptionName.GameAnonymousVotes: Main.NormalOptions.SetBool(BoolOptionNames.AnonymousVotes, true); break;
                case VanillaOptionName.MapId: Main.NormalOptions.SetByte(ByteOptionNames.MapId, 0); break;
            }
        }
        StringOptionStartPatch.all.Do(x =>
        {
            x.Value = Main.NormalOptions.GetInt(x.stringOptionName);
            x.ValueText.text = Translator.GetString(x.Values[x.Value]);
        });
        NumberOptionStartPatch.all.Do(x =>
        {
            var opt = x.intOptionName is Int32OptionNames.Invalid ? Main.NormalOptions.GetFloat(x.floatOptionName) : Main.NormalOptions.GetInt(x.intOptionName);
            x.Value = opt;
            x.ValueText.text = x.data.GetValueString(opt);
        });
        ToggleOptionStartPatch.all.Do(x =>
        {
            try
            {
                x.CheckMark.enabled = Main.NormalOptions.GetBool(x.boolOptionName);
            }
            catch { }
        });
        GameOptionsSender.RpcSendOptions();
    }
}