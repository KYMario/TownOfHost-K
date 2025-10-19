using System.Collections.Generic;
using TownOfHost.Roles.Core;

namespace TownOfHost
{
    static class SlotRoleAssign
    {
        public static List<SlotBaseOptionInfo> SlotRoles = new();
        public static void SetupOptionItem()
        {
            for (var i = 0; i < 14; i++)
            {
                var slotrole = (AssignOptionItem)AssignOptionItem.Create(114000 + i * 100, $"SlotRole{i + 1}", 0, TabGroup.Combinations, false, true, true, true, true)
                    .SetHeader(i is 0).SetColorcode("#efd87f");
                SlotRoles.Add(new SlotBaseOptionInfo(slotrole));
            }
        }

        public static bool IsSeted(CustomRoles role)
        {
            if (SlotRoles?.Count is 0 or null) return false;
            foreach (var info in SlotRoles)
            {
                if (info.AssignOption.GetNowRoleValue().Contains(role)) return true;
            }
            return false;
        }

        public static void Reset()
        {
            SlotRoles.Do(info => info.Reset());
        }
    }
    public class SlotBaseOptionInfo
    {
        public AssignOptionItem AssignOption;
        public CustomRoles AssignRole;
        public SlotBaseOptionInfo(AssignOptionItem assignOptionItem)
        {
            AssignOption = assignOptionItem;
            //AssignRole = CustomRoles.NotAssigned;
        }

        public void Reset()
        {
            //AssignRole = CustomRoles.NotAssigned;
        }
        /// <summary>
        /// 
        /// </summary>
        /// <param name="role"></param>
        /// <returns>0→変更なし 1→既に割り当て済み 2 →排他的アサイン</returns>
        public int CheckAssignRole(ref CustomRoles role)
        {
            if (AssignRole is not CustomRoles.NotAssigned)//既にこのグループは割り当て済みならおわり
            {
                if (AssignOption.GetBool())
                {
                    if (AssignOption.GetNowRoleValue().Contains(role)) return 1;
                }
                return 0;
            }

            if (AssignOption.GetBool())
            {
                if (AssignOption.GetNowRoleValue().Contains(role))
                {
                    List<CustomRoles> list = new();

                    foreach (var chancerole in AssignOption.GetNowRoleValue())
                    {
                        for (var i = 0; i < Options.GetRoleChance(chancerole); i++)
                        {
                            list.Add(chancerole);
                        }
                    }
                    if (list.Count <= 0) return 0;
                    var select = list[IRandom.Instance.Next(list.Count)];
                    AssignRole = role;
                    role = AssignRole;
                    return 2;
                }
            }
            return 0;
        }
        public string AssignChanceRolestring()
        {
            List<string> strings = new();
            foreach (var role in AssignOption.GetNowRoleValue())
            {
                strings.Add(UtilsRoleText.GetRoleColorAndtext(role));
            }
            return string.Join(", ", strings);
        }
    }
}