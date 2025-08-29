using System.Linq;
using System.Collections.Generic;
using Hazel;
using AmongUs.GameOptions;

using TownOfHost.Roles.Core;
using static TownOfHost.Translator;
using TownOfHost.Roles.Crewmate;

namespace TownOfHost.Roles.Ghost
{
    public class GhostRoleAssingData
    {
        public static Dictionary<CustomRoles, GhostRoleAssingData> AllData = new();
        public static Dictionary<CustomRoles, int> GhostAssingCount = new();
        public CustomRoles Role { get; private set; }
        public CustomRoleTypes RoleType { get; private set; }
        public CustomRoleTypes SubRoleType { get; set; }
        public int IdStart { get; private set; }

        public GhostRoleAssingData(int idStart, CustomRoles role, CustomRoleTypes roleTypes, CustomRoleTypes k)
        {
            IdStart = idStart;
            Role = role;
            RoleType = roleTypes;
            SubRoleType = k;

            if (!AllData.ContainsKey(role)) AllData.Add(role, this);
            else Logger.Warn("重複したCustomRolesを対象とするGhostRoleAssingDataが作成されました", "GhostRoleAssingData");
        }
        /// <summary>
        /// 幽霊役職のアサイン
        /// </summary>
        /// <param name="idStart">Id</param>
        /// <param name="role">配布役職</param>
        /// <param name="roleTypes">配布する陣営</param>
        /// <param name="kottinimofuyo">配布陣営その二</param>
        /// <returns></returns>
        public static GhostRoleAssingData Create(int idStart, CustomRoles role, CustomRoleTypes roleTypes, CustomRoleTypes kottinimofuyo) => new(idStart, role, roleTypes, kottinimofuyo);

        /// <summary>
        /// 幽霊役職のアサイン
        /// </summary>
        /// <param name="idStart">Id</param>
        /// <param name="role">配布役職</param>
        /// <param name="roleTypes">配布する陣営</param>
        /// <returns></returns>
        public static GhostRoleAssingData Create(int idStart, CustomRoles role, CustomRoleTypes roleTypes) => new(idStart, role, roleTypes, roleTypes);
        ///<summary>
        ///GhostRoleAssingDataが存在する幽霊役職を一括で割り当て
        ///</summary>
        public static void AssignAddOnsFromList(bool IsDead = false)
        {
            foreach (var kvp in AllData)
            {
                var (role, data) = kvp;
                if (!role.IsPresent()) continue;
                var assignTargetList = AssignTargetList(data);

                foreach (var pc in assignTargetList)
                {
                    if (GhostAssingCount[data.Role] >= data.Role.GetRealCount()) continue;
                    GhostAssingCount[data.Role]++;

                    if (!Utils.RoleSendList.Contains(pc.PlayerId))
                        Utils.RoleSendList.Add(pc.PlayerId);

                    PlayerState.GetByPlayerId(pc.PlayerId).SetGhostRole(role);
                    //非クライアントにもRpcぶっぱ
                    if (PlayerCatch.AnyModClient())
                    {
                        MessageWriter writer = AmongUsClient.Instance.StartRpcImmediately(PlayerControl.LocalPlayer.NetId, (byte)CustomRPC.SetCustomRole, Hazel.SendOption.Reliable, -1);
                        writer.Write(pc.PlayerId);
                        writer.WritePacked((int)role);
                        AmongUsClient.Instance.FinishRpcImmediately(writer);
                    }

                    Logger.Info("役職設定:" + pc?.Data?.GetLogPlayerName() + " = " + pc.GetCustomRole().ToString() + " + " + role.ToString(), "GhostRoleAssingData");

                    UtilsGameLog.AddGameLog($"{role}", string.Format(GetString("GhostRole.log"), UtilsName.GetPlayerColor(pc), Utils.ColorString(UtilsRoleText.GetRoleColor(role), UtilsRoleText.GetRoleName(role))));
                    UtilsGameLog.LastLogRole[pc.PlayerId] += $"<size=45%>=> {Utils.ColorString(UtilsRoleText.GetRoleColor(role), UtilsRoleText.GetRoleName(role))}</size>";

                    if (role is CustomRoles.DemonicSupporter)
                    {
                        if (!IsDead)
                        {
                            pc.RpcSetRole(RoleTypes.ImpostorGhost, true);
                        }
                        else
                        {
                            _ = new LateTask(() =>
                                {
                                    if (!GameStates.CalledMeeting)
                                    {
                                        pc.RpcSetRole(RoleTypes.ImpostorGhost, true);
                                    }
                                }, 1.4f, "Fix sabotage");
                        }
                        return;
                    }
                    if (!IsDead)
                    {
                        pc.RpcSetRole(RoleTypes.GuardianAngel, true);
                        _ = new LateTask(() => pc.RpcResetAbilityCooldown(Sync: true), 0.5f, "GhostRoleResetAbilty");
                    }
                    else
                    {
                        _ = new LateTask(() =>
                            {
                                if (!GameStates.CalledMeeting)
                                {
                                    pc.RpcSetRole(RoleTypes.GuardianAngel, true);
                                    _ = new LateTask(() => pc.RpcResetAbilityCooldown(Sync: true), 0.5f, "GhostRoleResetAbilty");
                                }
                            }, 1.4f, "Fix sabotage");
                    }
                }
            }
        }
        ///<summary>
        ///アサインするプレイヤーのList
        ///</summary>
        private static List<PlayerControl> AssignTargetList(GhostRoleAssingData data)
        {
            var rnd = IRandom.Instance;
            var candidates = new List<PlayerControl>();
            var AP = new List<PlayerControl>(PlayerCatch.AllPlayerControls.Where(x => !x.IsGhostRole() && !x.IsAlive() && (x.Is(data.RoleType) || x.Is(data.SubRoleType))));
            var APc = new List<PlayerControl>(PlayerCatch.AllPlayerControls.Where(x => !x.IsGhostRole() && !x.IsAlive() && (x.Is(data.RoleType) || x.Is(data.SubRoleType))));

            if (!GhostAssingCount.ContainsKey(data.Role))//データ内なら0
            {
                GhostAssingCount[data.Role] = 0;
            }

            for (var i = 0; i < APc.Count; i++)
            {
                if (AP.Count == 0) continue;
                var pc = AP[rnd.Next(AP.Count)];

                //ラバー or 天邪鬼の場合、勝利条件がくっっっっっそややこしなるから付与しない
                if (pc.IsLovers() || pc.Is(CustomRoles.Amanojaku) || (pc.GetRoleClass() as Staff)?.EndedTaskInAlive is false)
                {
                    AP.Remove(pc);
                    continue;
                }
                //配布対象外ならサヨナラ...
                if (pc == null || pc.IsGhostRole() || pc.Is(CustomRoles.GM) || pc.IsAlive() || PlayerState.GetByPlayerId(pc.PlayerId) == null)
                {
                    AP.Remove(pc);
                    continue;
                }
                if (PlayerState.GetByPlayerId(pc.PlayerId).DeathReason == CustomDeathReason.Disconnected)
                {
                    AP.Remove(pc);
                    continue;
                }

                candidates.Add(pc);
                AP.Remove(pc);
            }

            while (candidates.Count > data.Role.GetRealCount())
                candidates.RemoveAt(rnd.Next(candidates.Count));

            return candidates;
        }
    }
}