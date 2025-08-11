using Hazel;
using TownOfHost.Modules;
using TownOfHost.Roles.Core;
using TownOfHost.Roles.Impostor;

namespace TownOfHost
{
    public static class NameColorManager
    {
        public static string ApplyNameColorData(this string name, PlayerControl seer, PlayerControl target, bool isMeeting)
        {
            if (!AmongUsClient.Instance.IsGameStarted) return name;

            if (!seer || !target)
            {
                Logger.Error($"{seer?.Data?.GetLogPlayerName() ?? "seer"} => {target?.Data?.GetLogPlayerName() ?? "target"}がnull", "ApplyNameColorData");
                return name;
            }
            if (!TryGetData(seer, target, out var colorCode))
            {
                if (KnowTargetRoleColor(seer, target, isMeeting))
                    colorCode = target.GetRoleColorCode();
            }
            string openTag = "", closeTag = "";
            if (!SuddenDeathMode.NowSuddenDeathMode)
            {
                var roleClass = seer.GetRoleClass();
                if (seer.PlayerId == target.PlayerId && seer.Is(CustomRoles.Amnesia))
                {
                    colorCode = seer.Is(CustomRoleTypes.Crewmate) ? UtilsRoleText.GetRoleColorCode(CustomRoles.Crewmate) : (seer.Is(CustomRoleTypes.Impostor) ?
                    UtilsRoleText.GetRoleColorCode(CustomRoles.Impostor) : UtilsRoleText.GetRoleColorCode(CustomRoles.SchrodingerCat));
                }
                if (seer.PlayerId == target.PlayerId && seer.GetMisidentify(out var role))
                {
                    colorCode = UtilsRoleText.GetRoleColorCode(role);
                }

                var seerRole = seer.GetCustomRole();
                var targetRole = target.GetCustomRole();
                if (seer != target && seerRole.IsImpostor() && targetRole.IsImpostor())
                {
                    if (targetRole.GetRoleInfo()?.IsCantSeeTeammates == true)
                        colorCode = Roles.Vanilla.Impostor.RoleInfo.RoleColorCode;
                    if ((seerRole.GetRoleInfo()?.IsCantSeeTeammates == true && !(roleClass as Amnesiac).Realized) || seer.Is(CustomRoles.OneWolf) || target.Is(CustomRoles.OneWolf))
                        colorCode = "#ffffff"; //white
                }
            }
            //会議中で決まってない場合は白
            if (isMeeting && colorCode == "") colorCode = "#ffffff";
            if (colorCode != "")
            {
                if (!colorCode.StartsWith('#'))
                    colorCode = "#" + colorCode;
                openTag = $"<{colorCode}>";
                closeTag = "</color>";
            }
            return openTag + name + closeTag;
        }
        public static bool KnowTargetRoleColor(PlayerControl seer, PlayerControl target, bool isMeeting)
        {
            return seer == target
                || target.Is(CustomRoles.GM)
                || (seer.Is(CustomRoleTypes.Impostor) && target.Is(CustomRoleTypes.Impostor)
                && (!seer.Is(CustomRoles.Amnesiac) || ((PlayerControl.LocalPlayer.GetRoleClass() as Amnesiac)?.Realized ?? false)))
                || Mare.KnowTargetRoleColor(target, isMeeting)
                || ((seer.Is(CountTypes.Jackal) || seer.Is(CustomRoles.Jackaldoll)) && (target.Is(CountTypes.Jackal) || target.Is(CustomRoles.Jackaldoll)))
                || (seer.Is(CountTypes.MilkyWay) && target.Is(CountTypes.MilkyWay));
        }
        public static bool TryGetData(PlayerControl seer, PlayerControl target, out string colorCode)
        {
            colorCode = "";
            var state = PlayerState.GetByPlayerId(seer.PlayerId);
            if (!state.TargetColorData.TryGetValue(target.PlayerId, out var value)) return false;
            colorCode = value;
            return true;
        }

        public static void Add(byte seerId, byte targetId, string colorCode = "")
        {
            if (colorCode == "")
            {
                var target = PlayerCatch.GetPlayerById(targetId);
                if (target == null) return;
                colorCode = target.GetRoleColorCode();
            }

            var state = PlayerState.GetByPlayerId(seerId);
            if (state.TargetColorData.TryGetValue(targetId, out var value) && colorCode == value) return;
            if (!state.TargetColorData.TryAdd(targetId, colorCode))
            {
                state.TargetColorData[targetId] = colorCode;
            }

            SendRPC(seerId, targetId, colorCode);
        }
        public static void Remove(byte seerId, byte targetId)
        {
            var state = PlayerState.GetByPlayerId(seerId);
            if (!state.TargetColorData.ContainsKey(targetId)) return;
            state.TargetColorData.Remove(targetId);

            SendRPC(seerId, targetId);
        }
        public static void RemoveAll(byte seerId)
        {
            PlayerState.GetByPlayerId(seerId).TargetColorData.Clear();

            SendRPC(seerId);
        }
        private static void SendRPC(byte seerId, byte targetId = byte.MaxValue, string colorCode = "")
        {
            if (!AmongUsClient.Instance.AmHost) return;

            MessageWriter writer = AmongUsClient.Instance.StartRpcImmediately(PlayerControl.LocalPlayer.NetId, (byte)CustomRPC.SetNameColorData, SendOption.None, -1);
            writer.Write(seerId);
            writer.Write(targetId);
            writer.Write(colorCode);
            AmongUsClient.Instance.FinishRpcImmediately(writer);
        }
        public static void ReceiveRPC(MessageReader reader)
        {
            byte seerId = reader.ReadByte();
            byte targetId = reader.ReadByte();
            string colorCode = reader.ReadString();

            if (targetId == byte.MaxValue)
                RemoveAll(seerId);
            else if (colorCode == "")
                Remove(seerId, targetId);
            else
                Add(seerId, targetId, colorCode);
        }
        public static void RpcMeetingColorName(PlayerControl pc = null)
        {
            if (ChatUpdatePatch.BlockSendName) return;
            if (pc == null)//全員に反映させる(会議開始時)
            {
                foreach (var seer in PlayerCatch.AllPlayerControls)
                {
                    if (seer.IsModClient()) continue;
                    var clientid = seer.GetClientId();
                    if (clientid == -1) continue;

                    var sender = CustomRpcSender.Create("MeetingNameColor");
                    sender.StartMessage(clientid);
                    foreach (var seen in PlayerCatch.AllPlayerControls)
                    {
                        string playername = seen.GetRealName(isMeeting: true);
                        playername = playername.ApplyNameColorData(seer, seen, true);

                        sender.StartRpc(seen.NetId, (byte)RpcCalls.SetName)
                        .Write(seen.NetId)
                        .Write(playername)
                        .EndRpc();
                    }
                    sender.EndMessage();
                    sender.SendMessage();
                }
            }
            else
            {
                foreach (var seer in PlayerCatch.AllPlayerControls)
                {
                    if (seer.IsModClient()) continue;
                    var clientId = seer.GetClientId();
                    string playername = pc.GetRealName(isMeeting: true);
                    playername = playername.ApplyNameColorData(seer, pc, true);

                    var sender = CustomRpcSender.Create("MeetingNameColor", SendOption.None);
                    sender.StartMessage(clientId);
                    sender.AutoStartRpc(pc.NetId, RpcCalls.SetName, clientId)
                    .Write(pc.NetId)
                    .Write(playername)
                    .EndRpc();
                    sender.EndMessage();
                    sender.SendMessage();
                }
            }
        }
    }
}