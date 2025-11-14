using System;
using System.Linq;
using System.Collections;
using System.Collections.Generic;
using BepInEx.Unity.IL2CPP.Utils.Collections;
using AmongUs.GameOptions;
using AmongUs.Data;
using HarmonyLib;
using UnityEngine;
using TMPro;

using static TownOfHost.Translator;
using static TownOfHost.CustomSpawnManager;
using TownOfHost.Modules;
namespace TownOfHost;

public class CustomSpawnEditor
{
    public static bool ActiveEditMode;
    private static readonly LogHandler logger = Logger.Handler(nameof(CustomSpawnEditor));

    public class EditorAPI
    {
        public const int MaxMarker = 8;
        public static List<CustomSpawnMarker> allMarkers;
        public static List<CustomSpawnMapMarker> allMapMarkers;
        static Queue<CustomSpawnMarker> availableMarkers;
        public static bool Loaded = false;

        public static List<(StringNames, Color)> Colors = new()
        {
            (StringNames.ColorRed,Color.red),
            (StringNames.ColorYellow,Color.yellow),
            (StringNames.ColorCyan,Color.cyan),
            (StringNames.ColorLime,Color.green),
            (StringNames.ColorOrange,Palette.Orange),
            (StringNames.ColorGray,Color.gray)
        };

        public static MapNames CurrentMapId => (MapNames)AmongUsClient.Instance.TutorialMapId;
        public static CustomSpawnMap CurrentSpawnMap => Data.CurrentPreset.SpawnMaps[CurrentMapId];

        public static void Init()
        {
            allMarkers = new(MaxMarker);
            allMapMarkers = new(MaxMarker);
            availableMarkers = new(MaxMarker);

            var mapId = (MapNames)AmongUsClient.Instance.TutorialMapId;
            Data.CurrentPreset.SpawnMaps.TryAdd(mapId, new(mapId));

            var points = Data.CurrentPreset.SpawnMaps[mapId].Points;

            for (var i = 0; i < MaxMarker; ++i)
            {
                var marker = new CustomSpawnMarker();
                var mapMarker = new CustomSpawnMapMarker();
                var hasData = points.Count > i;

                if (!hasData)
                {
                    availableMarkers.Enqueue(marker);
                    continue;
                }

                marker.SetSpawn(points[i]);
                marker.Marker.gameObject.SetActive(true);
            }
        }

        public static void UpdateAllMarker()
        {
            allMarkers.Do(x => x.UpdateSpawn());
        }

        public class CustomSpawnMarker
        {
            public TextMeshPro Text;
            public SpriteRenderer Marker;
            public CustomSpawnPoint SpawnPoint;

            protected virtual Queue<CustomSpawnMarker> MarkerPool => availableMarkers;

            public CustomSpawnMarker()
            {
                //TextとMarkerを作成
                if (Text == null || Text.gameObject.IsDestroyedOrNull())
                {
                    Text = new GameObject("Text").AddComponent<TextMeshPro>();
                    SetupText();
                }
                if (Marker == null || Marker.gameObject.IsDestroyedOrNull())
                {
                    Marker = new GameObject("Marker").AddComponent<SpriteRenderer>();
                    SetupMarker();
                }

                allMarkers.Add(this);
                Marker.gameObject.SetActive(false);
            }

            public void SetupText()
            {
                if (Marker != null) Text.transform.SetParent(Marker.transform);

                var baseText = PlayerControl.LocalPlayer?.NameText() ?? Text;

                Text.fontSizeMax =
                Text.fontSizeMin =
                Text.fontSize = 1.6f;
                Text.font = baseText.font;
                Text.fontMaterial = baseText.fontMaterial;
                Text.fontStyle = baseText.fontStyle;
                Text.outlineWidth = baseText.outlineWidth;
                Text.SetOutlineColor(Color.black);

                Text.alignment = TextAlignmentOptions.Center;
                Text.transform.localPosition = new(0, 0.35f, -15);
            }

            public void SetupMarker()
            {
                if (Text != null) Text.transform.SetParent(Marker.transform);

                Marker.sprite = UtilsSprite.LoadSprite("TownOfHost.Resources.SpawnMark.png", 300f);
            }

            public void SetSpawn(CustomSpawnPoint spawnPoint)
            {
                SpawnPoint = spawnPoint;
                this.UpdateSpawn();
                Marker.gameObject.SetActive(SpawnPoint != null);
            }

            public void UpdateSpawn()
            {
                if (SpawnPoint == null) return;
                Text.text = SpawnPoint.Name;
                Text.color =
                Marker.color = SpawnPoint.Color;
                Marker.transform.position = SpawnPoint.Position;
            }

            public void Recycle()
            {
                SpawnPoint = null;
                Marker.gameObject.SetActive(false);
                if (!MarkerPool.Contains(this))
                {
                    MarkerPool.Enqueue(this);
                }
            }

            public static CustomSpawnMarker Get()
            {
                return availableMarkers.TryDequeue(out var marker) ? marker : new();
            }
        }

        public class CustomSpawnMapMarker : CustomSpawnMarker
        {
            /// <summary>recycle及びgetは使用できません</summary>
            public CustomSpawnMapMarker() : base()
            {
                Marker.gameObject.layer = LayerMask.NameToLayer("UI");
                Text.gameObject.layer = LayerMask.NameToLayer("UI");

                Marker.transform.localScale = new(0.65f, 0.65f);
                Text.transform.localScale = new(1f, 1f);

                SetupClickEvent();

                allMapMarkers.Add(this);
            }

            protected override Queue<CustomSpawnMarker> MarkerPool => new();

            public void SetupClickEvent()
            {
                var collider = Marker.gameObject.AddComponent<BoxCollider2D>();
                collider.autoTiling = true;

                var button = Marker.gameObject.AddComponent<PassiveButton>();
                button.OnMouseOut = new();
                button.OnMouseOver = new();
                button.OnClick = new();

                button.OnMouseOut.AddListener((Action)(() => Text.outlineColor = Color.black));
                button.OnMouseOver.AddListener((Action)(() => Text.outlineColor = Color.white));
                button.OnClick.AddListener((Action)(() => PlayerControl.LocalPlayer.NetTransform.SnapTo(SpawnPoint.Position)));
            }
        }
    }

    [HarmonyPatch]
    static class EditorUI
    {
        static ShapeshifterMinigame EditorMinigame;
        static int SelectedSpawnId;
        static TextBoxTMP textBox = null;
        public enum PanelTypes
        {
            Home,
            EditSpawn,
            EditColor
        }

        [HarmonyPatch(typeof(TutorialManager), nameof(TutorialManager.Awake)), HarmonyPostfix]
        public static void RunTutorialPatch(TutorialManager __instance)
        {
            __instance.StartCoroutine(RunEditor().WrapToIl2Cpp());
        }//PostFixなら多分行けると思う。

        public static IEnumerator RunEditor()
        {
            EditorAPI.Loaded = false;
            logger.Info("ロード開始!");

            while (!ShipStatus.Instance)
                yield return null;
            ShipStatus.Instance.DummyLocations = new(0);

            while (!PlayerControl.LocalPlayer)
                yield return null;
            PlayerControl.LocalPlayer.RpcSetRole(RoleTypes.Shapeshifter, false);

            EditorAPI.Init();
            logger.Info("ロード完了!");
            EditorAPI.Loaded = true;
        }

        [HarmonyPatch(typeof(ShapeshifterMinigame), nameof(ShapeshifterMinigame.Begin)), HarmonyPrefix]
        public static bool MinigamePrefixPatch(ShapeshifterMinigame __instance)
        {
            if (!CustomSpawnEditor.ActiveEditMode) return true;

            EditorMinigame = __instance;
            SelectedSpawnId = -1;

            MinigameBegin(__instance);

            __instance.potentialVictims = new();
            Il2CppSystem.Collections.Generic.List<UiElement> selectableElements = new();

            for (int index = 0; index < EditorAPI.MaxMarker + 1; index++)
            {
                int num1 = index % 3;
                int num2 = index / 3;

                ShapeshifterPanel shapeshifterPanel = GameObject.Instantiate(__instance.PanelPrefab, __instance.transform);
                shapeshifterPanel.transform.localPosition = new Vector3(__instance.XStart + num1 * __instance.XOffset, __instance.YStart + num2 * __instance.YOffset, -1f);
                SetPanel(shapeshifterPanel, index, PanelTypes.Home);
                __instance.potentialVictims.Add(shapeshifterPanel);
                selectableElements.Add(shapeshifterPanel.Button);
            }
            ControllerManager.Instance.OpenOverlayMenu(__instance.name, __instance.BackButton, __instance.DefaultButtonSelected, selectableElements);

            var wifi = __instance.transform.Find("PhoneUI/UI_Icon_Wifi");

            textBox = CreateTextBox(
                 new(0f, 0, -1f),
                 () =>
                 {
                     var points = EditorAPI.CurrentSpawnMap.Points;
                     if (SelectedSpawnId < 0)
                     {
                         Data.CurrentPreset.Name = textBox?.text ?? Data.CurrentPreset.Name;
                     }
                     else
                     {
                         var spawnPoint = points[SelectedSpawnId];
                         spawnPoint.Name = textBox?.text ?? spawnPoint.Name;
                         EditorAPI.UpdateAllMarker();
                     }
                 },
                  wifi
            );
            UpdateTextBox(textBox, SelectedSpawnId);
            return false;
        }

        public static void MinigameBegin(Minigame __instance)
        {
            Minigame.Instance = __instance;
            __instance.timeOpened = Time.realtimeSinceStartup;
            if (PlayerControl.LocalPlayer)
            {
                if (MapBehaviour.Instance) MapBehaviour.Instance.Close();
                PlayerControl.LocalPlayer.MyPhysics.SetNormalizedVelocity(Vector2.zero);
            }
            __instance.StartCoroutine(__instance.CoAnimateOpen());
        }

        public static void UpdatePanel(PanelTypes type)
        {
            var panels = EditorMinigame.potentialVictims;
            for (int i = 0; i < panels.Count; ++i)
                SetPanel(panels[i], i, type);

            UpdateTextBox(textBox, SelectedSpawnId);
        }

        public static void SetPanel(ShapeshifterPanel panel, int index, PanelTypes type)
        {
            panel.SetPlayer(index, PlayerControl.LocalPlayer.Data, null);

            panel.shapeshift = null;

            string nameText = "";
            string subText = "";
            Il2CppSystem.Action onShift = (Action)(() => { });

            panel.LevelNumberText.text = index.ToString();
            panel.PlayerIcon.gameObject.SetActive(false);

            panel.NameText.transform.SetLocalX(0f);
            panel.ColorBlindName.transform.SetLocalX(0f);

            panel.NameText.rectTransform.offsetMax += new Vector2(0.2f, 0f);
            panel.ColorBlindName.rectTransform.offsetMax += new Vector2(0.2f, 0f);

            switch (type)
            {
                case PanelTypes.Home:
                    SetupSpawnPanel(index, ref onShift, ref nameText, ref subText); break;
                case PanelTypes.EditSpawn:
                    SetupEditPanel(index, ref onShift, ref nameText); break;
                case PanelTypes.EditColor:
                    SetupColorPanel(index, ref onShift, ref nameText); break;
            }

            panel.shapeshift = onShift;
            panel.NameText.text = nameText;
            panel.ColorBlindName.text = subText;
            panel.ColorBlindName.enabled = true;
            panel.Background.color = new(1, 1, 1, nameText.IsNullOrWhiteSpace() ? 0.4f : 1f);
        }

        public static void SetupSpawnPanel(int index, ref Il2CppSystem.Action onClick, ref string text, ref string subText)
        {
            var points = EditorAPI.CurrentSpawnMap.Points;

            if (index == 0)
            {
                var canMake = points.Count < EditorAPI.MaxMarker;
                text = GetString(canMake ? "ED.Add" : "ED.NoAdd");
                subText = $"<{(canMake ? "#8cffff" : "#ff1919")}>({points.Count}/{EditorAPI.MaxMarker})";
                if (!canMake) return;
                onClick = (Action)(() =>
                {
                    var spawn = new CustomSpawnPoint($"{GetString("EDCustomSpawn")}{points.Count + 1}", PlayerControl.LocalPlayer.transform.position);
                    EditorAPI.CurrentSpawnMap.AddSpawn(spawn);
                    EditorAPI.CustomSpawnMarker.Get().SetSpawn(spawn);
                    Minigame.Instance.ForceClose();
                    RPC.PlaySound(PlayerControl.LocalPlayer.PlayerId, Sounds.TaskComplete);
                });
                return;
            }

            index--;
            if (points.Count <= index) return;

            var spawnPoint = points[index];
            text = spawnPoint.Name;
            subText = $"<#8cffff>@{GetShipRoomName(spawnPoint.Position)}";
            onClick = (Action)(() =>
            {
                SelectedSpawnId = index;
                UpdatePanel(PanelTypes.EditSpawn);
            });
        }

        public static void SetupEditPanel(int index, ref Il2CppSystem.Action onShift, ref string text)
        {
            var points = EditorAPI.CurrentSpawnMap.Points;
            var spawnPoint = points[SelectedSpawnId];

            if (spawnPoint == null) return;

            System.Action onClick = () => { };

            switch (index)
            {
                case 0:
                    text = GetString("ED.Move");
                    onClick = () =>
                    {
                        PlayerControl.LocalPlayer.NetTransform.SnapTo(spawnPoint.Position);
                        Minigame.Instance.ForceClose();
                    };
                    break;
                case 1:
                    text = GetString("ED.Movehere");
                    onClick = () =>
                    {
                        spawnPoint.Position = PlayerControl.LocalPlayer.transform.position;
                        EditorAPI.UpdateAllMarker();
                        Minigame.Instance.ForceClose();
                        RPC.PlaySound(PlayerControl.LocalPlayer.PlayerId, Sounds.TaskComplete);
                    };
                    break;
                case 2:
                    text = GetString("ED.Delete");
                    onClick = () =>
                    {
                        EditorAPI.allMarkers.DoIf(x => x.SpawnPoint == spawnPoint, x => x.Recycle());
                        points.Remove(spawnPoint);
                        Minigame.Instance.ForceClose();
                    };
                    break;
                case 3:
                    text = GetString("ED.Back");
                    onClick = () =>
                    {
                        SelectedSpawnId = -1;
                        UpdatePanel(PanelTypes.Home);
                    };
                    break;
                case 5:
                    text = GetString("ED.EditColor");
                    onClick = () => UpdatePanel(PanelTypes.EditColor);
                    break;
            }

            onShift = (Il2CppSystem.Action)onClick;
        }

        public static void SetupColorPanel(int index, ref Il2CppSystem.Action onShift, ref string text)
        {
            var points = EditorAPI.CurrentSpawnMap.Points;
            var spawnPoint = points[SelectedSpawnId];

            if (spawnPoint == null) return;

            if (index == 6)
            {
                onShift = (Action)(() => UpdatePanel(PanelTypes.EditSpawn));
                text = GetString("ED.Back");
                return;
            }

            if (index > EditorAPI.Colors.Count) return;
            var colorData = EditorAPI.Colors[index];

            onShift = (Action)(() =>
            {
                spawnPoint.SetColor(colorData.Item2);
                EditorAPI.UpdateAllMarker();
                Minigame.Instance.ForceClose();
            });
            text = GetString(colorData.Item1);
        }

        [HarmonyPatch(typeof(MapBehaviour), nameof(MapBehaviour.Show)), HarmonyPostfix]
        public static void ShowPostfix(MapBehaviour __instance)
        {
            if (!GameStates.IsFreePlay || !CustomSpawnEditor.ActiveEditMode) return;

            var spawnPoints = EditorAPI.CurrentSpawnMap.Points;
            if (spawnPoints == null) return;

            for (var i = 0; i < EditorAPI.MaxMarker; i++)
            {
                var marker = EditorAPI.allMapMarkers[i];
                var markerObj = marker.Marker.gameObject;
                var isActive = spawnPoints.Count > i;

                markerObj.SetActive(isActive);
                markerObj.transform.SetParent(__instance.HerePoint.transform.parent);

                if (!isActive) break;

                var spawnPoint = spawnPoints[i];
                marker.SetSpawn(spawnPoint);

                Vector3 vector3 = spawnPoint.Position / ShipStatus.Instance.MapScale;
                vector3.x *= Mathf.Sign(ShipStatus.Instance.transform.localScale.x);
                vector3.z = -1f;
                markerObj.transform.localPosition = vector3;
            }
            __instance.taskOverlay.gameObject.SetActive(false);
        }
    }

    public static string GetShipRoomName(Vector2 position)
    {
        if (ShipStatus.Instance is null) return "";
        var RoomName = "";

        PlainShipRoom Room = null;
        foreach (var psr in ShipStatus.Instance.AllRooms)
        {
            if (psr.roomArea == null) continue;
            if (psr.roomArea.OverlapPoint(position))
            {
                Room = psr;
            }
        }
        RoomName = Room is null ? "" : DestroyableSingleton<TranslationController>.Instance.GetString(Room.RoomId);//GetString($"{Room.RoomId}");
        if (Room?.RoomId is SystemTypes.Hallway or null)
        {
            var AllRooms = ShipStatus.Instance.AllRooms;
            Dictionary<byte, float> Distance = new();
            if (AllRooms != null)
            {
                if (EditorAPI.CurrentMapId is MapNames.Fungle)
                {
                    Distance.Add(200, Vector2.SqrMagnitude(position - new Vector2(-7.95f, -14.10f))); //西ジャングル
                    Distance.Add(201, Vector2.SqrMagnitude(position - new Vector2(1.74f, -9.76f)));//中央ジャングル
                    Distance.Add(202, Vector2.SqrMagnitude(position - new Vector2(15.81f, -8.3f)));//東ジャングル
                    Distance.Add(203, Vector2.SqrMagnitude(position - new Vector2(-8.95f, 1.79f)));//焚火
                }
                foreach (var room in AllRooms)
                {
                    if (room.RoomId == SystemTypes.Hallway) continue;
                    Distance.Add((byte)room.RoomId, Vector2.SqrMagnitude(position - (Vector2)room.transform.position));
                }
            }
            var Nearestroomid = Distance.OrderByDescending(x => x.Value).Last().Key;
            if (Room is not null)
            {
                if (Room?.RoomId is SystemTypes.Hallway && 200 > Nearestroomid && (SystemTypes)Nearestroomid is SystemTypes.VaultRoom)
                    Nearestroomid = (byte)SystemTypes.Comms;
            }
            var Nearestroom = 200 <= Nearestroomid ? GetString($"ModMapName.{Nearestroomid}") : DestroyableSingleton<TranslationController>.Instance.GetString((SystemTypes)Nearestroomid);//GetString($"{(SystemTypes)Nearestroomid}");
            RoomName = Room is null ? string.Format(GetString("Nearroom"), Nearestroom)
            : Nearestroom + RoomName;
        }
        return RoomName;
    }

    public static void FixedUpdate(PlayerControl player)
    {
        if (player.AmOwner && EditorAPI.Loaded)
        {
            string name = DataManager.player.Customization.Name;
            if (Main.nickName != "") name = Main.nickName;
            var nowCount = EditorAPI.CurrentSpawnMap.Points.Count;
            player.SetName($"<#ffffff>{name}<{(nowCount >= EditorAPI.MaxMarker ? "#ff1919" : "#8cffff")}> ({nowCount}/{EditorAPI.MaxMarker})");
        }
    }

    //過去のバージョンからひっぱってきた。いつかちゃんとしたの作る。
    private static TextBoxTMP CreateTextBox(Vector3 position, Action Event, Transform parent = null)
    {
        var collider = new GameObject("PresetText").AddComponent<BoxCollider2D>();
        var textBox = collider.gameObject.AddComponent<TextBoxTMP>();
        var button = textBox.gameObject.AddComponent<PassiveButton>();
        var text = new GameObject("Text").AddComponent<TextMeshPro>();
        string backupName = "";

        textBox.AllowEmail = false;
        textBox.AllowSymbols = true;
        textBox.AllowPaste = true;
        textBox.tempTxt = new();
        textBox.outputText = text;
        textBox.compoText = "";
        textBox.text = "";
        textBox.characterLimit = 15;
        textBox.OnChange = new();
        textBox.OnEnter = new();
        textBox.OnFocusLost = new();
        textBox.OnEnter.AddListener((Action)(() => CheckText()));
        textBox.OnFocusLost.AddListener((Action)(() => CheckText()));

        if (parent) textBox.transform.SetParent(parent);
        textBox.transform.localPosition = position;

        button.OnMouseOut = new();
        button.OnMouseOver = new();
        button.OnClick = new();
        button.OnClick.AddListener((Action)(() => textBox.GiveFocus()));
        button.OnClick.AddListener((Action)(() => backupName = textBox.text));

        collider.offset = new Vector2(1.5f, 0);
        collider.size = new Vector2(3f, 0.25f);

        text.fontSize =
        text.fontSizeMax =
        text.fontSizeMin = 2;
        text.alignment = TextAlignmentOptions.Left;
        text.transform.SetParent(textBox.transform);
        text.transform.localPosition = new(10.2f, 0.025f);

        textBox.gameObject.layer = LayerMask.NameToLayer("UI");
        text.gameObject.layer = LayerMask.NameToLayer("UI");

        return textBox;

        void CheckText()
        {
            if (textBox.text.IsNullOrWhiteSpace())
            {
                textBox.SetText(backupName);
                return;
            }
            Event.Invoke();
        }
    }

    private static void UpdateTextBox(TextBoxTMP textBox, int selectedSpawnId)
    {
        var points = EditorAPI.CurrentSpawnMap.Points;
        if (textBox?.text == null) return;
        if (selectedSpawnId < 0)
        {
            textBox.outputText.text =
            textBox.text = Data.CurrentPreset.Name;
        }
        else
        {
            var spawnPoint = points[selectedSpawnId];
            textBox.outputText.text =
            textBox.text = spawnPoint.Name;
        }
    }
}