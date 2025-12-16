using System;
using System.Collections.Generic;
using System.Linq;
using HarmonyLib;
using Hazel;
using TownOfHost.Roles.Core;

namespace TownOfHost
{
    public class AssignOptionItem : OptionItem
    {
        // 必須情報
        public IntegerValueRule Rule;
        public Dictionary<CustomRoles, int> Selections;
        public static Dictionary<CustomRoles, int> Selection;
        public Func<CustomRoles[]> NotAssin;
        public Dictionary<int, List<CustomRoles>> RoleValues = new(7);
        public List<CustomRoles> GetNowRoleValue() => RoleValues[Getpresetid()];
        public static int Getpresetid() => PresetOptionItem.Preset.GetInt();
        public (bool impostor, bool madmate, bool crewmate, bool neutral, bool addon) roles;

        // コンストラクタ
        public AssignOptionItem(int id, string name, int defaultValue, TabGroup tab, bool isSingleValue, bool imp = false, bool mad = false, bool crew = false, bool neu = false, bool addon = false, Func<CustomRoles[]> notassing = null)
        : base(id, name, defaultValue, tab, isSingleValue)
        {
            for (var i = 0; i < NumPresets; i++)
            {
                RoleValues.Add(i, new());
            }
            Selections = new Dictionary<CustomRoles, int>
            {
                { CustomRoles.NotAssigned ,0 }
            };
            EnumHelper.GetAllValues<CustomRoles>().Where(role => role < CustomRoles.NotAssigned).Do(role =>
            {
                Selections.Add(role, Selections.Count);
            }
            );
            if (Selection == null)
            {
                Selection = Selections;
            }
            NotAssin = notassing;
            roles = (imp, mad, crew, neu, addon);
            Rule = (0, Selections.Count - 1, 1);
        }
        public static AssignOptionItem Create(
            int id, string name, int defaultIndex, TabGroup tab, bool isSingleValue, bool imp = false, bool mad = false, bool crew = false, bool neu = false, bool addon = false, params CustomRoles[] notassing
        )
        {
            return new AssignOptionItem(
                id, name, defaultIndex, tab, isSingleValue, imp, mad, crew, neu, addon, () => notassing
            );
        }
        public static AssignOptionItem Create(
            SimpleRoleInfo roleInfo, int idOffset, Enum name, int defaultIndex, bool isSingleValue, OptionItem parent = null, bool imp = false, bool mad = false, bool crew = false, bool neu = false, bool addon = false, params CustomRoles[] notassing
        )
        {
            var opt = new AssignOptionItem(
                roleInfo.ConfigId + idOffset, name.ToString(), defaultIndex, roleInfo.Tab, isSingleValue, imp, mad, crew, neu, addon, () => notassing
            );
            opt.SetParent(parent ?? roleInfo.RoleOption);
            opt.SetParentRole(roleInfo.RoleName);
            return opt;
        }

        // Getter
        public override int GetInt() => Rule.GetValueByIndex(CurrentValue);
        public override float GetFloat() => Rule.GetValueByIndex(CurrentValue);
        public override string GetString()
        {
            if (RoleValues[Getpresetid()].Count <= 0)
            {
                return Translator.GetString("Unsettled");
            }

            return $"{Translator.GetString("Setteled")}({RoleValues[Getpresetid()]?.Count ?? -1})";
        }
        public override string GetValueString(bool coloroff)
        {
            if (RoleValues[Getpresetid()].Count <= 0)
            {
                return Translator.GetString("Unsettled");
            }
            return $"{Translator.GetString("Setteled")}({RoleValues[Getpresetid()]?.Count ?? -1})";
        }
        public void SetRoleValue(List<CustomRoles> roles)
        {
            if (RoleValues.TryAdd(Getpresetid(), roles) is false)
            {
                RoleValues[Getpresetid()] = roles.Distinct().ToList();
            }
            Refresh();

            Modules.OptionSaver.Save();
            SendRpc(true);
        }

        void Clear(int presetid)
        {
            RoleValues[presetid] = new();
        }
        void Add(int presetid, CustomRoles role)
        {
            if (RoleValues.TryGetValue(presetid, out var list) is false)
            {
                list = new();
            }
            list.Add(role);
            if (RoleValues.TryAdd(presetid, list) is false)
            {
                RoleValues[presetid] = list.Distinct().ToList();
            }
        }
        public void SendRpc(bool Isoverride)
        {
            if (PlayerCatch.AnyModClient() is false) return;
            if (Name.Contains("SlotRole") is false) return;

            MessageWriter writer = AmongUsClient.Instance.StartRpcImmediately(PlayerControl.LocalPlayer.NetId, (byte)CustomRPC.SyncAssignOption, SendOption.None, -1);
            writer.Write(Id);//オプションid
            writer.Write(Getpresetid());//現在のプリセットid
            writer.Write(Isoverride);//上書きするか
            writer.WritePacked(GetNowRoleValue().Count > 8 ? 8 : GetNowRoleValue().Count);
            int i = 0;
            int index = 0;
            bool Sended = false;
            foreach (var role in GetNowRoleValue())
            {
                if (Sended)
                {
                    writer = AmongUsClient.Instance.StartRpcImmediately(PlayerControl.LocalPlayer.NetId, (byte)CustomRPC.SyncAssignOption, SendOption.None, -1);
                    writer.Write(Id);
                    writer.Write(Getpresetid());//現在のプリセットid
                    writer.Write(false);//上書きするか
                    writer.WritePacked((GetNowRoleValue().Count - i) > 8 ? 8 : GetNowRoleValue().Count);
                    Sended = false;
                }

                writer.Write((int)role);
                i++;
                index++;

                if (index > 8)
                {
                    index = 0;
                    Sended = true;
                    AmongUsClient.Instance.FinishRpcImmediately(writer);
                }
            }
            if (Sended is false)
            {
                AmongUsClient.Instance.FinishRpcImmediately(writer);
            }
        }
        public static void ReadRpc(MessageReader reader)
        {
            int optionid = reader.ReadInt32();
            int presetid = reader.ReadInt32();
            bool Isoverride = reader.ReadBoolean();
            AssignOptionItem optionItem = AllOptions.Where(x => x.Id == optionid).FirstOrDefault() is AssignOptionItem assignOption ? assignOption : null;

            if (optionItem is null)
            {
                Logger.Error($"{optionid} is null", "AssignOptionItemRead");
                return;
            }

            if (Isoverride)
            {
                optionItem.Clear(presetid);
            }
            var forcount = reader.ReadPackedUInt32();
            OptionShower.Update = true;
            if (forcount <= 0) return;
            try
            {
                for (var i = 0; i < forcount; i++)
                {
                    CustomRoles role = (CustomRoles)reader.ReadInt32();
                    optionItem.Add(presetid, role);
                }
            }
            catch (Exception ex)
            {
                Logger.Error($"{ex}", "AssignOptionRead");
            }
        }
        public override int GetValue()
            => RoleValues.Count;

        // Setter
        public override void SetValue(int value, bool doSync = true)
        {
            base.SetValue(RoleValues.Count, doSync);
        }
        public override bool GetBool() => RoleValues[Getpresetid()].Count > 0 && (Parent == null || Parent.GetBool() || CheckRoleOption(Parent)) && (GameMode == CustomGameMode.All || GameMode == Options.CurrentGameMode);
    }
}