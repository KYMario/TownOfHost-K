using System;
using System.Collections.Generic;
using System.Linq;
using HarmonyLib;
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
        public (bool impostor, bool madmate, bool crewmate, bool neutral) roles;

        // コンストラクタ
        public AssignOptionItem(int id, string name, int defaultValue, TabGroup tab, bool isSingleValue, bool imp = false, bool mad = false, bool crew = false, bool neu = false, Func<CustomRoles[]> notassing = null)
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
            roles = (imp, mad, crew, neu);
            Rule = (0, Selections.Count - 1, 1);
        }
        public static AssignOptionItem Create(
            int id, string name, int defaultIndex, TabGroup tab, bool isSingleValue, bool imp = false, bool mad = false, bool crew = false, bool neu = false, params CustomRoles[] notassing
        )
        {
            return new AssignOptionItem(
                id, name, defaultIndex, tab, isSingleValue, imp, mad, crew, neu, () => notassing
            );
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
        }
        public override int GetValue()
            => RoleValues.Count;

        // Setter
        public override void SetValue(int value, bool doSync = true)
        {
            this.CurrentValue = RoleValues.Count;
        }
        public override bool GetBool() => RoleValues[Getpresetid()].Count > 0 && (Parent == null || Parent.GetBool() || CheckRoleOption(Parent)) && (GameMode == CustomGameMode.All || GameMode == Options.CurrentGameMode);
    }
}