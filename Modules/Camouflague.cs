using System.Collections.Generic;
using HarmonyLib;

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
                foreach (var pc in Main.AllPlayerControls)
                {
                    RpcSetSkin(pc);
                    // The code is intended to remove pets at dead players to combat a vanilla bug
                    if (!IsCamouflage && !pc.IsAlive())
                    {
                        pc.RpcSetPet("");
                    }
                }
                Utils.NotifyRoles(NoCache: true);
                if (!IsCamouflage)
                {
                    foreach (var role in Roles.Core.CustomRoleManager.AllActiveRoles.Values)
                    {
                        role.Colorchnge();
                    }
                }
            }
        }
        public static void RpcSetSkin(PlayerControl target, bool ForceRevert = false, bool RevertToDefault = false, bool kyousei = false)
        {
            if (!AmongUsClient.Instance.AmHost && !(Options.CommsCamouflage.GetBool() || kyousei)) return;
            if (GameStates.IsLobby) return;
            if (target == null) return;
            var id = target.PlayerId;

            if (IsCamouflage && !kyousei)
            {
                //コミュサボ中

                //死んでいたら処理しない
                if (PlayerState.GetByPlayerId(id).IsDead) return;
            }

            var newOutfit = CamouflageOutfit;

            if (!IsCamouflage || ForceRevert || kyousei)
            {
                //コミュサボ解除または強制解除

                if (Main.CheckShapeshift.TryGetValue(id, out var shapeshifting) && shapeshifting && !RevertToDefault)
                {
                    //シェイプシフターなら今の姿のidに変更
                    id = Main.ShapeshiftTarget[id];
                }

                newOutfit = PlayerSkins[id];
            }

            if (newOutfit.Compare(target.Data.DefaultOutfit) && !kyousei) return;

            Logger.Info($"newOutfit={newOutfit.GetString()}", "RpcSetSkin");

            var sender = CustomRpcSender.Create(name: $"Camouflage.RpcSetSkin({target.Data.PlayerName})");

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

            if (target.IsAlive())
            {
                target.SetPet(newOutfit.PetId);
                sender.AutoStartRpc(target.NetId, (byte)RpcCalls.SetPetStr)
                    .Write(newOutfit.PetId)
                    .Write(target.GetNextRpcSequenceId(RpcCalls.SetPetStr))
                    .EndRpc();
            }
            sender.SendMessage();
            if (Options.Onlyseepet.GetBool()) Main.AllPlayerControls.Do(pc => pc.OnlySeeMePet(pc.Data.DefaultOutfit.PetId));
        }
    }
}