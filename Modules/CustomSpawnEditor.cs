using System;
using System.IO;
using System.Linq;
using System.Text.Json;
using System.Collections.Generic;
using AmongUs.Data;
using AmongUs.GameOptions;
using UnityEngine;

using TownOfHost.Attributes;
using TownOfHost.Modules;

using Object = UnityEngine.Object;

namespace TownOfHost
{
    public class CustomSpawnEditor
    {
        public static Dictionary<int, List<Vector2>> CustomSpawnPosition = new();
        private static readonly string FILE_PATH = "./TOHK_DATA/CustomSpawns.txt";
        private readonly static LogHandler logger = Logger.Handler("CustomSpawnEditor");

        [PluginModuleInitializer]
        public static void Init()
        {
            CreateIfNotExists();
            Load();
        }

        public static void Setup()
        {
            CustomSpawnPosition.TryAdd(AmongUsClient.Instance.TutorialMapId, new List<Vector2>());

            PlayerControl.LocalPlayer.StartCoroutine(PlayerControl.LocalPlayer.CoSetRole(RoleTypes.Shapeshifter, false));
            if (PlayerControl.AllPlayerControls.Count < 10)
            {
                //SNR参考 https://github.com/SuperNewRoles/SuperNewRoles/blob/master/SuperNewRoles/Modules/BotManager.cs
                byte id = 0;
                foreach (var p in PlayerControl.AllPlayerControls)
                    id++;
                for (var i = 0; PlayerControl.AllPlayerControls.Count < 10; i++) //足りない分を召喚
                {
                    var dummy = Object.Instantiate(AmongUsClient.Instance.PlayerPrefab);
                    dummy.isDummy = true;
                    dummy.PlayerId = id;
                    AmongUsClient.Instance.Spawn(GameData.Instance.AddDummy(dummy));
                    AmongUsClient.Instance.Spawn(dummy);
                    dummy.NetTransform.enabled = true;
                    dummy.SetColor(6);
                    id++;
                }
            }
            //Mark
            var mark = UtilsSprite.LoadSprite("TownOfHost.Resources.SpawnMark.png", 300f);
            foreach (var p in PlayerControl.AllPlayerControls.ToArray().Where(p => p.PlayerId > 1))
            {
                _ = new LateTask(() =>
                {
                    var nametext = p.transform.Find("Names/NameText_TMP");
                    nametext.transform.position -= new Vector3(0, nametext.gameObject.activeSelf ? 0.3f : -0.3f);
                    nametext.gameObject.SetActive(true);
                }, 0.5f);
                Object.Destroy(p.transform.Find("Names/ColorblindName_TMP").gameObject);
                p.transform.Find("BodyForms").gameObject.active = false;
                var hand = p.transform.Find("BodyForms/Seeker/SeekerHand");
                var Mark = Object.Instantiate(hand, hand.transform.parent.parent.parent);
                Mark.transform.localPosition = new Vector2(0, 0);
                Mark.GetComponent<SpriteRenderer>().sprite = mark;
                Object.Destroy(Mark.GetComponent<PowerTools.SpriteAnimNodeSync>());
                Mark.name = "Mark";
                Mark.gameObject.SetActive(true);
            }

        }

        public static void FixedUpdate(PlayerControl player)
        {
            var mapid = AmongUsClient.Instance.TutorialMapId;
            if (player.PlayerId is 1)
            {
                player.SetName(CustomSpawnPosition[mapid].Count > 7 ? Translator.GetString("ED.noadd") : Translator.GetString("ED.add"));
                player.NetTransform.SnapTo(new Vector2(9999f, 9999f));
            }
            else if (player.PlayerId is not 0)
            {
                var check = CustomSpawnPosition[mapid].Count >= player.PlayerId - 1;
                player.SetName(check ? $"{Translator.GetString("EDCustomSpawn")}{player.PlayerId - 1}<size=60%><#8cffff>\n@{player.GetShipRoomName()}" : "<size=0>");
                player.NetTransform.SnapTo(check ? CustomSpawnPosition[mapid][player.PlayerId - 2] : new Vector2(9999f, 9999f));
            }

            if (Minigame.Instance.IsDestroyedOrNull() && Main.page is not 0)
                Main.page = 0;

            if (player.AmOwner)
            {
                string name = DataManager.player.Customization.Name;
                if (Main.nickName != "") name = Main.nickName;
                var nowcount = CustomSpawnEditor.CustomSpawnPosition[Main.NormalOptions.MapId].Count;
                player.SetName($"<#ffffff>{name}" + $"<{(nowcount is 8 ? "#ff1919" : "#8cffff")}> ({nowcount}/8)");
            }
        }

        public static void CheckShapeshift(PlayerControl __instance, PlayerControl target)
        {
            var mapid = AmongUsClient.Instance.TutorialMapId;
            if (target.PlayerId is 1)
            {
                if (Main.page is 0)
                {
                    if (CustomSpawnPosition[mapid].Count < 8)
                    {
                        CustomSpawnPosition[mapid].Add(__instance.transform.position);
                        RPC.PlaySound(0, Sounds.TaskComplete);
                    }
                }
                else
                    __instance.NetTransform.SnapTo(CustomSpawnPosition[mapid][Main.page - 2]);
            }
            else if (Main.page is not 0)
            {
                if (target.PlayerId is 2)
                {
                    CustomSpawnPosition[mapid][Main.page - 2] = __instance.transform.position;
                    RPC.PlaySound(0, Sounds.TaskComplete);
                }
                if (target.PlayerId is 3)
                {
                    CustomSpawnPosition[mapid].Remove(CustomSpawnPosition[mapid][Main.page - 2]);
                }
                if (target.PlayerId is 4)
                {
                    Minigame.Instance.ForceClose();
                    Main.page = 0;
                    _ = new LateTask(() => DestroyableSingleton<HudManager>.Instance.AbilityButton.DoClick(), 0.03f, "Open Menu");
                }
            }
            else if (target.PlayerId <= CustomSpawnPosition[mapid].Count + 1)
            {
                Minigame.Instance.ForceClose();
                _ = new LateTask(() =>
                {
                    PlayerControl.AllPlayerControls[1].SetName(Translator.GetString("ED.Move"));
                    PlayerControl.AllPlayerControls[2].SetName(Translator.GetString("ED.Movehere"));
                    PlayerControl.AllPlayerControls[3].SetName(Translator.GetString("ED.delete"));
                    PlayerControl.AllPlayerControls[4].SetName(Translator.GetString("ED.back"));
                    foreach (var pc in PlayerControl.AllPlayerControls)
                        if (pc.PlayerId > 4) pc.SetName("<size=0>");
                    DestroyableSingleton<HudManager>.Instance.AbilityButton.DoClick();
                    Main.page = target.PlayerId;
                }, 0.03f, "Open Menu");
            }
            __instance.RpcRejectShapeshift();
        }

        public static void CreateIfNotExists()
        {
            if (Main.IsAndroid()) return;
            if (!File.Exists(FILE_PATH))
            {
                try
                {
                    if (!Directory.Exists(@"TOHK_DATA")) Directory.CreateDirectory(@"TOHK_DATA");
                    if (File.Exists(@"./CustomSpawns.txt"))
                    {
                        File.Move(@"./CustomSpawns.txt", FILE_PATH);
                    }
                    else
                    {
                        logger.Info("Among Us.exeと同じフォルダにCustomSpawns.txtが見つかりませんでした。新規作成します。");
                        File.WriteAllText(FILE_PATH, "{}");
                    }
                }
                catch (Exception ex)
                {
                    logger.Exception(ex);
                }
            }
        }

        //ゴミコードでごめんな！自分こんなコードしか書けないんだわ！((
        public static void Save()
        {
            CreateIfNotExists();

            string hozon = "{";

            foreach (var Data in CustomSpawnPosition)
            {
                hozon += $"\"{Data.Key}\" :[";
                foreach (var vec2 in Data.Value)
                {
                    hozon += $"{{\"x\":{vec2.x},\"y\":{vec2.y}}},";
                }
                hozon = hozon.Remove(hozon.Length - 1);
                hozon += "],";
            }
            hozon = hozon.Remove(hozon.Length - 1);
            hozon += "}";

            File.WriteAllText(FILE_PATH, hozon);
        }

        //ChatGPTさんが手伝ってくれました 感謝です
        public static void Load()
        {
            CreateIfNotExists();
            string data = File.ReadAllText(FILE_PATH);

            Dictionary<int, List<Vector2>> customSpawnPosition = new();

            try
            {
                using JsonDocument document = JsonDocument.Parse(data);
                JsonElement root = document.RootElement;
                foreach (JsonProperty property in root.EnumerateObject())
                {
                    int mapId = int.Parse(property.Name);
                    JsonElement positionArray = property.Value;
                    List<Vector2> positions = new();
                    foreach (JsonElement positionElement in positionArray.EnumerateArray())
                    {
                        float x = positionElement.GetProperty("x").GetSingle();
                        float y = positionElement.GetProperty("y").GetSingle();
                        Vector2 position = new(x, y);
                        positions.Add(position);
                    }
                    customSpawnPosition.Add(mapId, positions);
                }
            }
            catch (JsonException ex)
            {
                logger.Error($"{ex.Message}");
            }

            CustomSpawnPosition = new(customSpawnPosition);
        }
    }
}