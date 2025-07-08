using System.Collections.Generic;

namespace TownOfHost;

public static class PlayerOutfitManager
{
    private static Dictionary<byte, OutfitData> PlayerOutfits = new();
    /// <summary>
    /// プレイヤーのスキンをセーブ
    /// </summary>
    public static void Save(PlayerControl player)
        => PlayerOutfits[player.PlayerId] = new OutfitData(player);

    /// <summary>
    /// プレイヤーのスキンをロード(n c h s v np lev pet)
    /// </summary>
    public static OutfitData Load(PlayerControl player)
        => PlayerOutfits.GetValueOrDefault(player.PlayerId);

    /// <summary>
    /// 保存されたプレイヤーのデータを削除
    /// </summary>
    public static bool Remove(PlayerControl player)
        => PlayerOutfits.Remove(player.PlayerId);

    /// <summary>
    /// 保存されたデータを全て削除
    /// </summary>
    [Attributes.GameModuleInitializer]
    public static void RemoveAll()
        => PlayerOutfits.Clear();

    public class OutfitData
    {
        public string name;
        public uint level;
        public byte playerId;

        public int color;
        public string hat;
        public string visor;
        public string skin;
        public string pet;
        public string nameplate;

        public OutfitData(PlayerControl player)
        {
            var playerOutfit = player.CurrentOutfit;

            this.playerId = player.PlayerId;
            this.level = player.Data.PlayerLevel;

            this.name = playerOutfit.PlayerName;
            this.color = playerOutfit.ColorId;
            this.hat = playerOutfit.HatId;
            this.visor = playerOutfit.VisorId;
            this.skin = playerOutfit.SkinId;
            this.pet = playerOutfit.PetId;
            this.nameplate = playerOutfit.NamePlateId;
        }
    }

}