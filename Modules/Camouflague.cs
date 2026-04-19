using System.Collections.Generic;
using System.Linq;
using TownOfHost.Roles.Core;

namespace TownOfHost
{
    static class PlayerOutfitExtension
    {
        public static NetworkedPlayerInfo.PlayerOutfit Set(this NetworkedPlayerInfo.PlayerOutfit instance, string playerName, int colorId, string hatId, string skinId, string visorId, string petId)
        {
            instance.PlayerName = playerName;
            instance.ColorId = colorId;
            instance.HatId = hatId;
            instance.SkinId = skinId;
            instance.VisorId = visorId;
            instance.PetId = petId;
            return instance;
        }
        public static bool Compare(this NetworkedPlayerInfo.PlayerOutfit instance, NetworkedPlayerInfo.PlayerOutfit targetOutfit)
        {
            return instance.ColorId == targetOutfit.ColorId &&
                    instance.HatId == targetOutfit.HatId &&
                    instance.SkinId == targetOutfit.SkinId &&
                    instance.VisorId == targetOutfit.VisorId &&
                    instance.PetId == targetOutfit.PetId;

        }
        public static string GetString(this NetworkedPlayerInfo.PlayerOutfit instance)
        {
            return $"{instance.PlayerName} Color:{instance.ColorId} {instance.HatId} {instance.SkinId} {instance.VisorId} {instance.PetId}";
        }
    }
    public static class Camouflage
    {
        static NetworkedPlayerInfo.PlayerOutfit CamouflageOutfit = new NetworkedPlayerInfo.PlayerOutfit().Set("", 15, "", "", "", "");

        public static bool IsCamouflage;
        public static Dictionary<byte, NetworkedPlayerInfo.PlayerOutfit> PlayerSkins = new();

        public static void Init()
        {
            IsCamouflage = false;
            PlayerSkins.Clear();
        }
        public static void CheckCamouflage()
        {
            if (!(AmongUsClient.Instance.AmHost && Options.CommsCamouflage.GetBool())) return;

            var oldIsCamouflage = IsCamouflage;

            IsCamouflage = Utils.IsActive(SystemTypes.Comms);

            if (oldIsCamouflage != IsCamouflage)
            {
                foreach (var pc in PlayerCatch.AllPlayerControls)
                {
                    RpcSetSkin(pc);
                    // The code is intended to remove pets at dead players to combat a vanilla bug
                    if (!IsCamouflage && !pc.IsAlive())
                    {
                        pc.RpcSetPet("");
                    }
                }
                UtilsNotifyRoles.NotifyRoles(NoCache: true);
                if (!IsCamouflage)
                {
                    foreach (var role in CustomRoleManager.AllActiveRoles.Values)
                    {
                        role.ChangeColor();
                    }
                }
            }
        }
        public static List<byte> ventplayr = new();
        public static void RpcSetSkin(PlayerControl target, bool ForceRevert = false, bool RevertToDefault = false, bool? force = false)
        {
            if ((!AmongUsClient.Instance.AmHost && !(Options.CommsCamouflage.GetBool() || (force is null or true)))
            || (GameStates.IsLobby)
            || (target == null)) return;

            if (Options.CurrentGameMode != CustomGameMode.Standard) return;

            var id = target.PlayerId;

            if (IsCamouflage && force is true)
            {
                //コミュサボ中

                //死んでいたら処理しない
                if (PlayerState.GetByPlayerId(id).IsDead) return;
            }

            var newOutfit = CamouflageOutfit;

            if (!IsCamouflage || ForceRevert || force is true)
            {
                //コミュサボ解除または強制解除

                if (Main.CheckShapeshift.TryGetValue(id, out var shapeshifting) && shapeshifting && !RevertToDefault && force is not null)
                {
                    //シェイプシフターなら今の姿のidに変更
                    id = Main.ShapeshiftTarget[id];
                }

                newOutfit = PlayerSkins[id];
            }

            if (target.inVent)
            {
                if (force is not null)
                {
                    ventplayr.Add(target.PlayerId);
                    Logger.Info($"{target.Data.GetLogPlayerName()} : invent", "camouflague");
                    return;
                }
            }

            if (newOutfit.Compare(target.Data.DefaultOutfit) && force is false) return;

            //Logger.Info($"newOutfit={newOutfit.GetString()}", "RpcSetSkin");

            var sender = CustomRpcSender.Create(name: $"Camouflage.RpcSetSkin({target.Data.GetLogPlayerName()})");

            target.SetColor(newOutfit.ColorId);
            sender.AutoStartRpc(target.NetId, (byte)RpcCalls.SetColor)
                .Write(target.Data.NetId)
                .Write(newOutfit.ColorId)
                .EndRpc();

            target.SetHat(newOutfit.HatId, newOutfit.ColorId);
            sender.AutoStartRpc(target.NetId, (byte)RpcCalls.SetHatStr)
                .Write(newOutfit.HatId)
                .Write(target.GetNextRpcSequenceId(RpcCalls.SetHatStr))
                .EndRpc();

            target.SetSkin(newOutfit.SkinId, newOutfit.ColorId);
            sender.AutoStartRpc(target.NetId, (byte)RpcCalls.SetSkinStr)
                .Write(newOutfit.SkinId)
                .Write(target.GetNextRpcSequenceId(RpcCalls.SetSkinStr))
                .EndRpc();

            target.SetVisor(newOutfit.VisorId, newOutfit.ColorId);
            sender.AutoStartRpc(target.NetId, (byte)RpcCalls.SetVisorStr)
                .Write(newOutfit.VisorId)
                .Write(target.GetNextRpcSequenceId(RpcCalls.SetVisorStr))
                .EndRpc();

            sender.SendMessage();
            if (ventplayr.Contains(target.PlayerId)) ventplayr.Remove(target.PlayerId);
        }
    }
}