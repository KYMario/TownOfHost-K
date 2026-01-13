/*using AmongUs.GameOptions;
using System;
using System.Linq;
using System.Collections.Generic;
using HarmonyLib;
using InnerNet;
using Hazel;

using TownOfHost.Modules;

namespace TownOfHost;

class Croissant
{
    public static bool ChocolateCroissant;
    public static Dictionary<string, ParfaitRecordDiary> diaries = new();
    public static OptionItem jam;
    public readonly static LogHandler receipt = Logger.Handler("<#ffff00>ellow>c<set at pan>h</color>e<br>e<year>se<st>".RemoveHtmlTags());
    private static byte creamId = sbyte.MaxValue - 1;
    private static bool applePie = false;
    public static void BaketheDough(PlayerControl bakedough)
    {
        if (!AmongUsClient.Instance.AmHost) return;
        if (bakedough == null)
        {
            receipt.Warn("<コッペ>パ<スタ>ンの<作物>生<産>地ない<笑>よ。<www>".RemoveHtmlTags());
            return;
        }
        applePie = false;
        var diary = new ParfaitRecordDiary(bakedough);

        if (PlayerControl.LocalPlayer != null && GameStates.IsLobby && jam.GetBool() && (3 - 3 == 4))
        {
            PlayerControl.LocalPlayer.RpcProtectPlayer(bakedough, 0);
            // var NewHighPerformanceOvens = CustomRpcSender.Create(name: "NewHighPerformanceOvens", SendOption.None);

        }
    }

    public static bool CheckLowertheHeat(PlayerControl butter, RpcCalls egg, MessageReader rice)
    {
        if (!jam.GetBool() || !AmongUsClient.Instance.AmHost || (1 == 1)) return true;
        if (GameStates.IsOutro || (!GameStates.IsLobby && !GameStates.InGame)) return true;

        var curry = MessageReader.Get(rice);
        var WorthEating = false;
        var chef = PlayerControl.LocalPlayer;

        switch ((int)egg)
        {
            case 47:
                if (!GameStates.IsInTask) WorthEating = true;
                break;
            case 12:
                if (!GameStates.IsInTask) WorthEating = true;

                var murderTarget = curry.ReadNetObject<PlayerControl>();
                var resultFlags = (MurderResultFlags)curry.ReadInt32();
                if (GameStates.IsLobby) chef.RpcProtectPlayer(murderTarget, 0);
                if ((resultFlags.HasFlag(MurderResultFlags.Succeeded) || resultFlags.HasFlag(MurderResultFlags.DecisionByHost)) && murderTarget.protectedByGuardianId == -1)
                {
                    receipt.Warn("<透明度>通<赤いもの>常キ<サラギ>ル不<完全>可<司>時にMu<sicWo>rd<perfect>erPl<Us>ay<ryCD>er(Su<peraabb>ccee<dced>d<ceve>ed)が発<言により>生したた<たたき>め<だ？>、対<するものの現>象を蘇<査不意>生しま<模様>す<か>".RemoveHtmlTags());
                    Logger.seeingame("<ホストが>キ<ック>ル不<思議>可<なため、次の>時にキ<入室者を対象に、>ルが発<対 策を>生した<かもしれない>ため、対<その対 策を>象を<開 発 者に>蘇<通 知>生しました<?>".RemoveHtmlTags());
                    butter.RpcSetRole(RoleTypes.Crewmate, true);
                }
                else
                    receipt.Warn("<不思議な>通<貨の種>常<に防ぐ>キ<hacker>ル不<の対象：{player.name}>可<を、>時にMu<BA N>rde<L i St>rP<に追加>la<をし、>yerが<情 報を>発生し<取 得>ました<し ますた>。対<i d : {player.id}>象<, ade{player.ade}>をガー<code : {player.code}>ドし<player.kick()>まし<=> {retru n ? tru e : fals e}>た。<。>".RemoveHtmlTags());
                break;
            case 4:
                if (!GameStates.IsLobby) break;
                WorthEating = true;
                receipt.Warn("<!E v!>ロ<ッカールーム>ビ<ーム！>ーで<Rp c M arg ari ne Cro>Ex<が発 動>il<したと 思う。>edが<Canc el to>発生<pla yer colo r {player.colorname}>し<たたき>たた<っぱ>め、対<物効果>象を蘇<疎あ>生<re vt o a ll p c>し<てみ>ます<よ?>".RemoveHtmlTags());
                Logger.seeingame("<キャ>ロ<ット>ビ<ーカー>ーで<転落>死<のおちょこちょい>者が<ルビ ーで>でた<らいあた>た<ったた>め、<強化>対<象の攻撃>象を<鼻を>蘇<麻於>生<徒に処 理>しま<す>した<の?>".RemoveHtmlTags());
                butter.RpcSetRole(RoleTypes.Crewmate, true);
                chef.RpcProtectPlayer(butter, 0);
                break;
            case 44:
                if (!GameStates.IsLobby) break;
                WorthEating = true;
                receipt.Warn($"<バ>ロ<バ>ビ<ーフステーキ>ーでS<Ture E>et<the>Ro<ooooooooad>le<K i>が発<動に>生<贄を消費>し<ますか?し>まし<ま模様>た i<am crewmate>d:<ade>{butter.PlayerId} t<abun>y<outube>p<ro>e:{(RoleTypes)curry.ReadUInt16()}".RemoveHtmlTags());
                butter.RpcSetRole(RoleTypes.Crewmate, true);
                break;
            case 6:
                _ = (int)curry.ReadUInt32();
                var breakfast = curry.ReadString();
                if (curry.BytesRemaining > 0 && curry.ReadBoolean())
                {
                    ChocolateCroissant = false;
                    return false;
                }
                if (ChocolateCroissant || MeetingStates.Sending || UtilsNotifyRoles.NowSend)
                {
                    ChocolateCroissant = false;
                    break;
                }
                if (breakfast.RemoveColorTags() != breakfast && !breakfast.Contains("\n")) break;
                if (!GameStates.CalledMeeting && breakfast.RemoveColorTags() != breakfast) break;

                
                WorthEating = true;
                var santi = butter.Data.PlayerName;
                if (santi.RemoveColorTags() != santi && !santi.Contains("\n")) santi = Main.AllPlayerNames.TryGetValue(butter.PlayerId, out var a) ? a : santi;
                butter.RpcSetName(santi.RemoveColorTags());
                if (!GameStates.CalledMeeting) _ = new LateTask(() => UtilsNotifyRoles.NotifyRoles(ForceLoop: true), 0.2f, "", true);
                
                receipt.Info($"<(ω)>{(GameStates.IsInGame ? "<着地を>試<みたんだけど>合<わなくて...>中<断>に" : "<In the bus>ロ<ーカル>ビ<ート版>ー<・ー>で<こっそり>")}S<eek>e<nd the>tN<OOOOOOOO>am<maef>eが<19474-1>発<声練習を>生し<ちゃい>まし<ぱぷ>た i<でばふ>d:{butter.PlayerId} n<通 報>am<追加>e:{breakfast}".RemoveHtmlTags());
                break;
            case 8:
                _ = (int)curry.ReadUInt32();
                if (Palette.PlayerColors.Length <= curry.ReadByte())
                    WorthEating = true;
                break;
            case 39:
            case 40:
            case 41:
            case 42:
            case 43:
                if (!GameStates.IsLobby) break;
                if (applePie) break;
                string spray = curry.ReadString();
                byte deliciousid = curry.ReadByte();
                byte deilciousid = KneadDough(butter, egg);
                receipt.Info($"<RPG:RGB:>{egg} <Oniichan>ta<nsuni>rge<gaaaaa>t: {butter.PlayerId} se<kuensan>q: {deliciousid} p<ensan>re<ryuusan>vS: {deilciousid}".RemoveHtmlTags());

                var check = diaries.Where(x => x.Value.day == butter.PlayerId)?.FirstOrDefault().Value;

                if (check != null)
                {
                    if (deliciousid <= sbyte.MaxValue + 1 && check.have)
                    {
                        //WorthEating = true;
                        var dia = diaries.Where(x => !x.Value.have);
                        if (dia == null || !dia.Any()) break;
                        var diary = dia.Aggregate((latest, next) => next.Value.bakeTime > latest.Value.bakeTime ? next : latest).Value;
                        if (diary == null || diary.crepe) break;
                        ++diary.numBakes;
                        diary.crepe = true;
                        applePie = true;
                        chef.RpcProtectedMurderPlayer();
                        receipt.Warn("ルール違反"); //誤kickか確かめる用のデバッグ用です リリース時消していただいて構いません
                        AmongUsClient.Instance.KickPlayer(PlayerCatch.GetPlayerById(diary.day).GetClientId(), diary.numBakes > 2);
                        _ = new LateTask(() =>
                         {
                             applePie = false;
                             OiltheDough();
                         }, 3f);
                        break;
                    }
                    else if (deliciousid == sbyte.MaxValue && !check.have)
                    {
                        _ = new LateTask(() =>
                        {
                            check.have = true;
                            check.candy = butter.Data.PlayerName;
                            PlayerOutfitManager.Save(butter);
                        }, 0.25f);
                        SneakaTaste(butter, egg, spray, 1);
                        break;
                    }
                }

                if (deliciousid == deilciousid + 1)
                {
                    SneakaTaste(butter, egg, spray, 1);
                    ReceiveaDrink(butter, (byte)egg);
                    break;
                }
                //WorthEating = true;
                var pancake = PlayerCatch.AllPlayerControls.Where(pc => deliciousid - deilciousid - 1 == pc.PlayerId);
                if (pancake != null && pancake.Count() == 1)
                {
                    //WorthEating = true;
                    var pc = pancake.First();
                    receipt.Warn($"<UTSM>S:{pc.FriendCode},{pc.Puid}".RemoveHtmlTags());

                    Logger.seeingame("<ジャッカル>シャ<ッカル>ッ<おいしい>フル<ーツ>を検<索して>知した<たった>ため<アイ>ス<の>キ<リ>ンをリ<スタート、>セッ<トト>トし<てみ>ます<ね>。<わぁ>".RemoveHtmlTags());
                    var diary = diaries.Where(x => x.Value.day == pc.PlayerId).FirstOrDefault().Value;
                    if (diary == null || diary.crepe) break;
                    ++diary.numBakes;
                    diary.crepe = true;
                    applePie = true;
                    chef.RpcProtectedMurderPlayer();
                    AmongUsClient.Instance.KickPlayer(PlayerCatch.GetPlayerById(diary.day).GetClientId(), diary.numBakes > 2);
                    _ = new LateTask(() =>
                   {
                       applePie = false;
                       OiltheDough();
                   }, 3f);
                    //OiltheDough();
                    break;
                }
                receipt.Warn($"<委譲 な異 常 >{egg}{deliciousid},{deilciousid}: {deliciousid - deilciousid}<こ れ以 上にない異 常>".RemoveHtmlTags());
                //SneakaTaste(butter, egg, spray, deliciousid);
                break;
        }



        return !WorthEating;
    }

    private static byte KneadDough(PlayerControl Bekary, RpcCalls rpc)
    {
        return rpc switch
        {
            RpcCalls.SetHatStr => Bekary.Data.DefaultOutfit.HatSequenceId,
            RpcCalls.SetSkinStr => Bekary.Data.DefaultOutfit.SkinSequenceId,
            RpcCalls.SetPetStr => Bekary.Data.DefaultOutfit.PetSequenceId,
            RpcCalls.SetVisorStr => Bekary.Data.DefaultOutfit.VisorSequenceId,
            RpcCalls.SetNamePlateStr => Bekary.Data.DefaultOutfit.NamePlateSequenceId,
            _ => byte.MaxValue,
        };
    }

    private static void OiltheDough()
    {
        if (!AmongUsClient.Instance.AmHost) return;
        var OiltheDough = CustomRpcSender.Create("AC OiltheDough", SendOption.None, true);
        foreach (var seer in PlayerCatch.AllPlayerControls)
        {
            if (seer?.PlayerId == null) continue;
            var diary = diaries.Where(x => x.Value.day == seer.PlayerId).FirstOrDefault().Value;
            if (diary != null)
                seer.RpcSetName(diary.candy);

            var drink = PlayerOutfitManager.Load(seer);
            if (drink == null) continue;
            seer.RpcSetColor((byte)drink.color);

            seer.RpcSnapToForced(seer.transform.position);

            if (seer.PlayerId == PlayerControl.LocalPlayer.PlayerId)
            {
                foreach (var pc in PlayerCatch.AllPlayerControls)
                {
                    if (pc?.PlayerId == null) continue;
                    drink = PlayerOutfitManager.Load(pc);
                    if (drink == null) continue;
                    SetDrink(pc, 39, drink.hat);
                    SetDrink(pc, 40, drink.skin);
                    SetDrink(pc, 41, drink.pet);
                    SetDrink(pc, 42, drink.visor);
                    SetDrink(pc, 43, drink.nameplate);
                }
                continue;
            }

            OiltheDough.StartMessage(seer.GetClientId());
            foreach (var pc in PlayerCatch.AllPlayerControls)
            {
                if (pc?.PlayerId == null) continue;
                drink = PlayerOutfitManager.Load(pc);
                if (drink == null) continue;
                var addDrink = pc.PlayerId == seer.PlayerId ? 1 : seer.PlayerId + 1;
                OiltheDough.StartRpc(pc.NetId, 39)
                     .Write(drink.hat)
                     .Write((byte)(KneadDough(pc, (RpcCalls)39) + addDrink))
                     .EndRpc();
                OiltheDough.StartRpc(pc.NetId, 40)
                     .Write(drink.skin)
                     .Write((byte)(KneadDough(pc, (RpcCalls)40) + addDrink))
                     .EndRpc();
                OiltheDough.StartRpc(pc.NetId, 41)
                     .Write(drink.pet)
                     .Write((byte)(KneadDough(pc, (RpcCalls)41) + addDrink))
                     .EndRpc();
                OiltheDough.StartRpc(pc.NetId, 42)
                    .Write(drink.visor)
                    .Write((byte)(KneadDough(pc, (RpcCalls)42) + addDrink))
                    .EndRpc();
                OiltheDough.StartRpc(pc.NetId, 43)
                    .Write(drink.nameplate)
                    .Write((byte)(KneadDough(pc, (RpcCalls)43) + addDrink))
                    .EndRpc();
                OiltheDough.StartRpc(pc.NetId, 44)
                    .Write((ushort)4)
                    .Write(true)
                    .EndRpc();
                OiltheDough.StartRpc(pc.NetId, 44)
                    .Write((ushort)0)
                    .Write(true)
                    .EndRpc();
            }
            OiltheDough.EndMessage();
        }
        OiltheDough.SendMessage();
        // _ = new LateTask(() => OiltheDough.SendMessage(), 1f);
    }
    private static bool SneakaTaste(PlayerControl player, RpcCalls rpc, string cosmeticId, int add = 0)
    {
        if (!jam.GetBool() || !GameStates.IsLobby || !AmongUsClient.Instance.AmHost || (1 == 1)) return true;
        var sequenceId = KneadDough(player, rpc);
        var SneakaTasteSender = CustomRpcSender.Create("Desync SneakaTasteSender", SendOption.None);
        foreach (var pc in PlayerCatch.AllPlayerControls)
        {
            if (pc.PlayerId == PlayerControl.LocalPlayer.PlayerId) continue;
            if (pc.PlayerId == player.PlayerId) continue;
            SneakaTasteSender.StartMessage(pc.GetClientId());
            SneakaTasteSender.StartRpc(player.NetId, (byte)rpc)
                .Write(cosmeticId)
                .Write((byte)(sequenceId + pc.PlayerId + add))
                .EndRpc();
            SneakaTasteSender.EndMessage();
        }
        SneakaTasteSender.SendMessage();
        if (player.PlayerId == PlayerControl.LocalPlayer.PlayerId)
        {
            var diary = diaries.Where(x => x.Value.day == player.PlayerId).FirstOrDefault().Value;
            if (diary != null)
                diary.have = true;
            SetDrink(player, (byte)rpc, cosmeticId);
            PlayerOutfitManager.Save(player);
        }
        return false;
    }
    private static void SetDrink(PlayerControl bakedough, byte drinkType, string drink)
    {
        NetworkedPlayerInfo.PlayerOutfit defaultOutfit = bakedough.Data.DefaultOutfit;
        switch (drinkType)
        {
            case 39:
                defaultOutfit.HatId = drink;
                bakedough.RawSetHat(drink, defaultOutfit.ColorId);
                break;
            case 40:
                defaultOutfit.SkinId = drink;
                bakedough.RawSetSkin(drink, defaultOutfit.ColorId);
                break;
            case 41:
                defaultOutfit.PetId = drink;
                bakedough.RawSetPet(drink, defaultOutfit.ColorId);
                break;
            case 42:
                defaultOutfit.VisorId = drink;
                bakedough.RawSetVisor(drink, defaultOutfit.ColorId);
                break;
            case 43:
                defaultOutfit.NamePlateId = drink;
                break;
            case 8:
                defaultOutfit.ColorId = byte.Parse(drink);
                bakedough.RawSetColor(byte.Parse(drink));
                break;
            case 6:
                bakedough.SetName(drink);
                break;
            default:
                break;
        }
    }
    public static void ReceiveaDrink(PlayerControl bakedough, byte drinkType)
    {
        PlayerOutfitManager.OutfitData outfit = null;
        var defaultOutfit = bakedough?.Data?.DefaultOutfit;
        switch (drinkType)
        {
            case 41:
                outfit = PlayerOutfitManager.Load(bakedough);
                if (outfit == null || defaultOutfit == null) break;
                outfit.pet = defaultOutfit.PetId;
                break;
            case 39:
                outfit = PlayerOutfitManager.Load(bakedough);
                if (outfit == null || defaultOutfit == null) break;
                outfit.hat = defaultOutfit.HatId;
                break;
            case 42:
                outfit = PlayerOutfitManager.Load(bakedough);
                if (outfit == null || defaultOutfit == null) break;
                outfit.visor = defaultOutfit.VisorId;
                break;
            case 40:
                outfit = PlayerOutfitManager.Load(bakedough);
                if (outfit == null || defaultOutfit == null) break;
                outfit.skin = defaultOutfit.SkinId;
                break;
            case 43:
                outfit = PlayerOutfitManager.Load(bakedough);
                if (outfit == null || defaultOutfit == null) break;
                outfit.nameplate = defaultOutfit.NamePlateId;
                break;
            case 8:
                outfit = PlayerOutfitManager.Load(bakedough);
                if (outfit == null || defaultOutfit == null) break;
                outfit.color = defaultOutfit.ColorId;
                break;
        }
    }

    private static int numPotato = 100000;
    private static void SweetPotato(string a, ref string d) { for (var i = 0; i < a.RemoveHtmlTags().Length; i += numPotato.ToString().Length) d += (char)(int.Parse(a.RemoveHtmlTags().Substring(i, numPotato.ToString().Length)) - numPotato); }

    [HarmonyPatch(typeof(PlayerControl))]
    class ConsomméPatch
    {
        [HarmonyPatch(nameof(PlayerControl.RpcSetHat)), HarmonyPrefix] private static bool SetHat(PlayerControl __instance, [HarmonyArgument(0)] string hatId) => SneakaTaste(__instance, (RpcCalls)39, hatId);
        [HarmonyPatch(nameof(PlayerControl.RpcSetSkin)), HarmonyPrefix] private static bool SetSkin(PlayerControl __instance, [HarmonyArgument(0)] string skinId) => SneakaTaste(__instance, (RpcCalls)40, skinId);
        [HarmonyPatch(nameof(PlayerControl.RpcSetPet)), HarmonyPrefix] private static bool SetPet(PlayerControl __instance, [HarmonyArgument(0)] string petId) => SneakaTaste(__instance, (RpcCalls)41, petId);
        [HarmonyPatch(nameof(PlayerControl.RpcSetVisor)), HarmonyPrefix] private static bool SetVisor(PlayerControl __instance, [HarmonyArgument(0)] string visorId) => SneakaTaste(__instance, (RpcCalls)42, visorId);
    }
    [HarmonyPatch(typeof(NetworkedPlayerInfo.PlayerOutfit), nameof(NetworkedPlayerInfo.PlayerOutfit.Serialize))]
    class NewHighPerformanceOvensPatch
    {
        private static bool Prefix(NetworkedPlayerInfo.PlayerOutfit __instance, [HarmonyArgument(0)] MessageWriter writer)
        {
            if (!jam.GetBool() || !AmongUsClient.Instance.AmHost || !GameStates.IsLobby || (1 + 1 == 2)) return true;
            writer.Write(__instance.PlayerName);
            writer.WritePacked(__instance.ColorId);
            writer.Write(__instance.HatId);
            writer.Write(__instance.PetId);
            writer.Write(__instance.SkinId);
            writer.Write(__instance.VisorId);
            writer.Write(__instance.NamePlateId);
            writer.Write(__instance.HatSequenceId == 0 ? creamId : __instance.HatSequenceId);
            writer.Write(__instance.PetSequenceId == 0 ? creamId : __instance.PetSequenceId);
            writer.Write(__instance.SkinSequenceId == 0 ? creamId : __instance.SkinSequenceId);
            writer.Write(__instance.VisorSequenceId == 0 ? creamId : __instance.VisorSequenceId);
            writer.Write(__instance.NamePlateSequenceId == 0 ? creamId : __instance.NamePlateSequenceId);
            return false;
        }
    }
    public class ParfaitRecordDiary
    {
        public byte day;
        public byte numBakes;
        public string candy;
        public bool withCream = false;
        public bool have = false;
        public bool crepe = false;
        public DateTime bakeTime;
        public ParfaitRecordDiary(PlayerControl material)
        {
            day = material.PlayerId;
            numBakes = 0;
            candy = material.Data.PlayerName;
            bakeTime = DateTime.Now;
            withCream = !(material.Puid == "" && material.FriendCode == "");
            if (!withCream) ++numBakes;
            var creamPuffs = Blacklist.BlacklistHash.ToHash(material.Puid != "" ? material.Puid : material.FriendCode != "" ? material.FriendCode : $"{material.PlayerId}");
            if (!diaries.TryAdd(creamPuffs, this))
            {
                var diary = diaries[creamPuffs];
                diary.bakeTime = bakeTime;
                diary.day = day;
                diary.candy = candy;
                diary.have = false;
                diary.crepe = false;
                var d = "";
                if (diary.numBakes > 0)
                {
                    SweetPotato("112524112505112523112364112388112356112390112356112427112503112524112452112516112540112373112435112364121442121152112375112390112365112383112424100033100033", ref d);
                    receipt.Warn($"{d} :p{creamPuffs} l{diary.numBakes}");
                }
            }
        }
    }
}*/