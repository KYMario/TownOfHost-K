using System;
using TownOfHost.Roles.Core;

namespace TownOfHost
{
    public class FloatOptionItem : OptionItem
    {
        // 必須情報
        public FloatValueRule Rule;

        // コンストラクタ
        public FloatOptionItem(int id, string name, float defaultValue, TabGroup tab, bool isSingleValue, FloatValueRule rule, bool? infinity)
        : base(id, name, rule.GetNearestIndex(defaultValue), tab, isSingleValue, infinity: infinity)
        {
            Rule = rule;
        }
        public static FloatOptionItem Create(
            int id, string name, FloatValueRule rule, float defaultValue, TabGroup tab, bool isSingleValue, bool? infinity = false
        )
        {
            return new FloatOptionItem(
                id, name, defaultValue, tab, isSingleValue, rule, infinity
            );
        }
        public static FloatOptionItem Create(
            int id, Enum name, FloatValueRule rule, float defaultValue, TabGroup tab, bool isSingleValue, bool? infinity = false
        )
        {
            return new FloatOptionItem(
                id, name.ToString(), defaultValue, tab, isSingleValue, rule, infinity
            );
        }
        public static FloatOptionItem Create(
            SimpleRoleInfo roleInfo, int idOffset, Enum name, FloatValueRule rule, float defaultValue, bool isSingleValue, OptionItem parent = null, bool? infinity = false
        )
        {
            var opt = new FloatOptionItem(
                roleInfo.ConfigId + idOffset, name.ToString(), defaultValue, roleInfo.Tab, isSingleValue, rule, infinity
            );
            opt.SetParent(parent ?? roleInfo.RoleOption);
            return opt;
        }
        public static FloatOptionItem Create(
            SimpleRoleInfo roleInfo, int idOffset, string name, FloatValueRule rule, float defaultValue, bool isSingleValue, OptionItem parent = null, bool? infinity = false
        )
        {
            var opt = new FloatOptionItem(
                roleInfo.ConfigId + idOffset, name.ToString(), defaultValue, roleInfo.Tab, isSingleValue, rule, infinity
            );
            opt.SetParent(parent ?? roleInfo.RoleOption);
            return opt;
        }

        // Getter
        public override int GetInt() => (int)Rule.GetValueByIndex(CurrentValue);
        public override float GetFloat() => Rule.GetValueByIndex(CurrentValue);
        public override string GetString()
        {
            return ApplyFormat(Rule.GetValueByIndex(CurrentValue).ToString());
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