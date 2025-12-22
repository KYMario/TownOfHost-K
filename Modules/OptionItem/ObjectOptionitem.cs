using System;
using System.Linq;
using TownOfHost.Roles.Core;

namespace TownOfHost
{
    public class ObjectOptionitem : OptionItem
    {
        public bool IsHedderObject;
        public Action ClickAction = null;
        // コンストラクタ
        public ObjectOptionitem(int id, string name, bool IsHeader, Action ClickAction, TabGroup tab)
        : base(id, name, 0, tab, false)
        {
            this.IsHedderObject = IsHeader;
            this.ClickAction = ClickAction;
        }
        public static ObjectOptionitem Create(int id, string name, bool IsHeader, Action ClickAction, TabGroup tab)
        {
            return new ObjectOptionitem(
                id, name, IsHeader, ClickAction, tab
            );
        }
        public static ObjectOptionitem Create(SimpleRoleInfo roleInfo, int idOffset, Enum name, bool IsHeader, Action ClickAction, TabGroup tab, OptionItem parent = null)
        {
            var opt = new ObjectOptionitem(
                roleInfo.ConfigId + idOffset, name.ToString(), IsHeader, ClickAction, roleInfo.Tab
            );
            opt.SetParent(parent ?? roleInfo.RoleOption);
            opt.SetParentRole(roleInfo.RoleName);
            return opt;
        }
        public static ObjectOptionitem Create(SimpleRoleInfo roleInfo, int idOffset, string name, bool IsHeader, Action ClickAction, TabGroup tab, OptionItem parent = null)
        {
            var opt = new ObjectOptionitem(
                roleInfo.ConfigId + idOffset, name.ToString(), IsHeader, ClickAction, roleInfo.Tab
            );
            opt.SetParent(parent ?? roleInfo.RoleOption);
            opt.SetParentRole(roleInfo.RoleName);
            return opt;
        }

        public override bool GetBool() => (Tag == CustomOptionTags.All || GameModeManager.GetTags(Options.CurrentGameMode).Contains(Tag))
                    && (GameModeManager.GetTags(Options.CurrentGameMode).Any(tag => DisableTag.Contains(tag)) is false);
    }
}