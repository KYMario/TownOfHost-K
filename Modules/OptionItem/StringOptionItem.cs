using System;
using TownOfHost.Roles.Core;

namespace TownOfHost
{
    public class StringOptionItem : OptionItem
    {
        // 必須情報
        public IntegerValueRule Rule;
        public string[] Selections;

        // コンストラクタ
        public StringOptionItem(int id, string name, int defaultValue, TabGroup tab, bool isSingleValue, string[] selections)
        : base(id, name, defaultValue, tab, isSingleValue)
        {
            Rule = (0, selections.Length - 1, 1);
            Selections = selections;
        }
        public static StringOptionItem Create(
            int id, string name, string[] selections, int defaultIndex, TabGroup tab, bool isSingleValue
        )
        {
            return new StringOptionItem(
                id, name, defaultIndex, tab, isSingleValue, selections
            );
        }
        public static StringOptionItem Create(
            int id, Enum name, string[] selections, int defaultIndex, TabGroup tab, bool isSingleValue
        )
        {
            return new StringOptionItem(
                id, name.ToString(), defaultIndex, tab, isSingleValue, selections
            );
        }
        public static StringOptionItem Create(
            SimpleRoleInfo roleInfo, int idOffset, Enum name, string[] selections, int defaultIndex, bool isSingleValue, OptionItem parent = null
        )
        {
            var opt = new StringOptionItem(
                roleInfo.ConfigId + idOffset, name.ToString(), defaultIndex, roleInfo.Tab, isSingleValue, selections
            );
            opt.SetParent(parent ?? roleInfo.RoleOption);
            opt.SetParentRole(roleInfo.RoleName);
            return opt;
        }
        public static StringOptionItem Create(
            SimpleRoleInfo roleInfo, int idOffset, string name, string[] selections, int defaultIndex, bool isSingleValue, OptionItem parent = null
        )
        {
            var opt = new StringOptionItem(
                roleInfo.ConfigId + idOffset, name, defaultIndex, roleInfo.Tab, isSingleValue, selections
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
            var text = Translator.GetString(Selections[Rule.GetValueByIndex(CurrentValue)]);
            if (UtilsRoleInfo.GetRoleByInputName(text.RemoveHtmlTags(), out var r, true))
            {
                return $"<b>{Utils.ColorString(UtilsRoleText.GetRoleColor(r, true), text.RemoveColorTags())}</b>";
            }
            return text;
        }
        public string GetString(int value)
        {
            return Translator.GetString(Selections[Rule.GetValueByIndex(value)]);
        }
        public int GetChance()
        {
            //0%or100%の場合
            if (Selections.Length == 2) return CurrentValue * 100;

            //0%～100%or5%～100%の場合
            var offset = 12 - Selections.Length;
            var index = CurrentValue + offset;
            var rate = index <= 1 ? index * 5 : (index - 1) * 10;
            return rate;
        }
        public override int GetValue()
            => Rule.RepeatIndex(base.GetValue());

        // Setter
        public override void SetValue(int value, bool doSync = true)
        {
            base.SetValue(Rule.RepeatIndex(value), doSync);
        }
    }
}