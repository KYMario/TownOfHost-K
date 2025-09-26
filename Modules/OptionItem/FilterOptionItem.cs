using System;
using System.Collections.Generic;
using System.Linq;
using HarmonyLib;
using TownOfHost.Roles.Core;

namespace TownOfHost
{
    public class FilterOptionItem : OptionItem
    {
        // 必須情報
        public IntegerValueRule Rule;
        public Dictionary<CustomRoles, int> Selections;
        public List<CustomRoles> Sele;
        public static Dictionary<CustomRoles, int> Selection;
        public Func<CustomRoles[]> NotAssin;
        public (bool impostor, bool madmate, bool crewmate, bool neutral) roles;

        // コンストラクタ
        public FilterOptionItem(int id, string name, int defaultValue, TabGroup tab, bool isSingleValue, bool imp = false, bool mad = false, bool crew = false, bool neu = false, Func<CustomRoles[]> notassing = null)
        : base(id, name, defaultValue, tab, isSingleValue)
        {
            Selections = new Dictionary<CustomRoles, int>
            {
                { CustomRoles.NotAssigned ,0 }
            };
            Sele = [CustomRoles.NotAssigned];
            EnumHelper.GetAllValues<CustomRoles>().Where(role => role < CustomRoles.NotAssigned).Do(role =>
            {
                Selections.Add(role, Selections.Count);
                Sele.Add(role);
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
        public static FilterOptionItem Create(
            int id, string name, int defaultIndex, TabGroup tab, bool isSingleValue, bool imp = false, bool mad = false, bool crew = false, bool neu = false, params CustomRoles[] notassing
        )
        {
            return new FilterOptionItem(
                id, name, defaultIndex, tab, isSingleValue, imp, mad, crew, neu, () => notassing
            );
        }
        public static FilterOptionItem Create(
            int id, Enum name, int defaultIndex, TabGroup tab, bool isSingleValue, bool imp = false, bool mad = false, bool crew = false, bool neu = false, Func<CustomRoles[]> notassing = null
        )
        {
            return new FilterOptionItem(
                id, name.ToString(), defaultIndex, tab, isSingleValue, imp, mad, crew, neu, notassing
            );
        }
        public static FilterOptionItem Create(
            SimpleRoleInfo roleInfo, int idOffset, Enum name, int defaultIndex, bool isSingleValue, OptionItem parent = null, bool imp = false, bool mad = false, bool crew = false, bool neu = false, Func<CustomRoles[]> notassing = null
        )
        {
            var opt = new FilterOptionItem(
                roleInfo.ConfigId + idOffset, name.ToString(), defaultIndex, roleInfo.Tab, isSingleValue, imp, mad, crew, neu, notassing
            );
            opt.SetParent(parent ?? roleInfo.RoleOption);
            opt.SetParentRole(roleInfo.RoleName);
            return opt;
        }
        public static FilterOptionItem Create(
            SimpleRoleInfo roleInfo, int idOffset, string name, int defaultIndex, bool isSingleValue, OptionItem parent = null, bool imp = false, bool mad = false, bool crew = false, bool neu = false, Func<CustomRoles[]> notassing = null
        )
        {
            var opt = new FilterOptionItem(
                roleInfo.ConfigId + idOffset, name, defaultIndex, roleInfo.Tab, isSingleValue, imp, mad, crew, neu, notassing
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
            var role = Sele[Rule.GetValueByIndex(CurrentValue)];

            if (role is CustomRoles.NotAssigned)
            {
                return Translator.GetString("Unsettled");
            }

            return UtilsRoleText.GetRoleColorAndtext(role);
        }
        public override string GetValueString(bool coloroff)
        {
            var role = Sele[Rule.GetValueByIndex(CurrentValue)];

            if (role is CustomRoles.NotAssigned)
            {
                return Translator.GetString("Unsettled");
            }

            var name = UtilsRoleText.GetRoleColorAndtext(role);
            return coloroff ? name.RemoveColorTags() : name;
        }
        public void SetRoleValue(CustomRoles role)
        {
            if (!Selections.TryGetValue(role, out var index))
            {
                SetValue(0);
                return;
            }
            var notassings = NotAssin?.Invoke() ?? [];
            if (notassings.Contains(role) && notassings.Count() > 0)
            {
                SetValue(0);
                return;
            }

            switch (role.GetCustomRoleTypes())
            {
                case CustomRoleTypes.Impostor:
                    if (!roles.impostor)
                    {
                        SetValue(0);
                        return;
                    }
                    break;
                case CustomRoleTypes.Madmate:
                    if (!roles.madmate)
                    {
                        SetValue(0);
                        return;
                    }
                    break;
                case CustomRoleTypes.Crewmate:
                    if (!roles.crewmate)
                    {
                        SetValue(0);
                        return;
                    }
                    break;
                case CustomRoleTypes.Neutral:
                    if (!roles.neutral)
                    {
                        SetValue(0);
                        return;
                    }
                    break;
            }
            SetValue(index);
        }
        public override int GetValue()
            => Rule.RepeatIndex(base.GetValue());

        public CustomRoles GetRole()
        {
            return Sele[Rule.GetNearestIndex(CurrentValue)];
        }

        // Setter
        public override void SetValue(int value, bool doSync = true)
        {
            base.SetValue(Rule.RepeatIndex(value), doSync);
        }
    }
}