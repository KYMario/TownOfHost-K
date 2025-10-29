using System;
using System.IO;
using System.Text.Json;
using System.Collections.Generic;
using UnityEngine;
using TownOfHost.Modules;
using TownOfHost.Attributes;
using UnityEngine.UIElements;

namespace TownOfHost;

public class CustomSpawnManager
{
    private static readonly DirectoryInfo SaveDataDirectoryInfo = new(Main.BaseDirectory);
    private static readonly FileInfo SaveDataFileInfo = new($"{SaveDataDirectoryInfo.FullName}/CustomSpawns.txt");
    private static readonly LogHandler logger = Logger.Handler(nameof(CustomSpawnManager));

    public static CustomSpawnData Data = new();
    public const int Version = 1;

    public static void CreateIfNotExists()
    {
        if (!SaveDataDirectoryInfo.Exists)
        {
            SaveDataDirectoryInfo.Create();
        }
        if (!SaveDataFileInfo.Exists)
        {
            SaveDataFileInfo.Create().Dispose();
            Save();
        }
    }

    [PluginModuleInitializer(InitializePriority.VeryLow)]
    public static void Load()
    {
        try
        {
            if (Main.IsAndroid()) return;
            CreateIfNotExists();
            var jsonString = File.ReadAllText(SaveDataFileInfo.FullName);

            if (jsonString.Length <= 0)
            {
                logger.Info("オプションデータが空のためデフォルト値を保存");
                Save();
                return;
            }

            Data = CustomSpawnDeserializer.Deserialize(jsonString, out bool updated);
            if (updated) Save();
        }
        catch (Exception ex)
        {
            logger.Exception(ex);
        }
    }

    public static void Save()
    {
        var options = new JsonSerializerOptions();
        options.Converters.Add(new JsonHelper.Vector2Converter());
        options.Converters.Add(new JsonHelper.ColorConverter());

        var jsonString = JsonSerializer.Serialize(Data, options);
        File.WriteAllText(SaveDataFileInfo.FullName, jsonString);
    }

    public class CustomSpawnPreset(string name)
    {
        public string Name { get; set; } = name;
        public Color32 Color { get; set; } = UnityEngine.Color.white;
        public Dictionary<MapNames, CustomSpawnMap> SpawnMaps { get; set; } = new();

        public void SetColor(Color32 color)
            => this.Color = color;
    }

    public class CustomSpawnMap
    {
        public MapNames MapId { get; set; }
        public string MapName { get; set; }

        public CustomSpawnMap() { }
        public CustomSpawnMap(MapNames map)
        {
            MapId = map;
            MapName = map.ToString();
        }

        public List<CustomSpawnPoint> Points { get; set; } = new();

        public bool AddSpawn(CustomSpawnPoint spawnPoint)
        {
            if (this.Points.Contains(spawnPoint)) return false;
            this.Points.Add(spawnPoint);
            return true;
        }
    }

    public class CustomSpawnPoint(string name, Vector2 position)
    {
        public string Name { get; set; } = name;
        public Color32 Color { get; set; } = UnityEngine.Color.white;
        public Vector2 Position { get; set; } = position;

        public void SetPreset(CustomSpawnPreset preset, MapNames map)
        {
            if (!preset.SpawnMaps.TryGetValue(map, out var data))
            {
                data = preset.SpawnMaps[map] = new(map);
            }
            data.AddSpawn(this);
        }

        public void SetColor(Color color)
            => this.Color = color;
    }

    public class CustomSpawnData
    {
        public int CurrentPresetId { get; set; } = 0;
        public List<CustomSpawnPreset> Presets { get; set; } = new() { new("プリセット1") };
        public int Version { get; set; } = CustomSpawnManager.Version;

        public CustomSpawnPreset CurrentPreset => Presets[CurrentPresetId];
    }

    public static List<CustomSpawnPoint> GetPoints(MapNames mapId)
    {
        if (Data == null || Data.CurrentPreset == null) return null;
        if (!Data.CurrentPreset.SpawnMaps.TryGetValue(mapId, out var spawnMap)) return null;
        return spawnMap.Points;
    }

    public static bool CheckActiveSpawns(int index)
    {
        var mapId = (MapNames)Main.NormalOptions.MapId;
        var spawnPoints = GetPoints(mapId);
        if (spawnPoints == null) return false;
        return spawnPoints.Count > index;
    }

    public static void UpdateOptionName()
    {
        var mapId = (MapNames)(Main.NormalOptions?.MapId ?? 0);
        var spawnPoints = GetPoints(mapId);

        SetSpawnName(Options.RandomSpawnCustom1, 0);
        SetSpawnName(Options.RandomSpawnCustom2, 1);
        SetSpawnName(Options.RandomSpawnCustom3, 2);
        SetSpawnName(Options.RandomSpawnCustom4, 3);
        SetSpawnName(Options.RandomSpawnCustom5, 4);
        SetSpawnName(Options.RandomSpawnCustom6, 5);
        SetSpawnName(Options.RandomSpawnCustom7, 6);
        SetSpawnName(Options.RandomSpawnCustom8, 7);

        void SetSpawnName(OptionItem option, int index)
        {
            if (!AmongUsClient.Instance.AmHost || spawnPoints == null || spawnPoints.Count <= index)
            {
                ResetSpawnName(option, index);
                return;
            }

            var point = spawnPoints[index];
            Dictionary<string, string> dict = new()
            {
                {"*",""},
                {"CustomSpawn",point.Name}
            };

            option.ReplacementDictionary = dict;
            option.SetColor(point.Color);
        }

        void ResetSpawnName(OptionItem option, int index)
        {
            Dictionary<string, string> dict = new()
            {
                {"*",""},
                {"CustomSpawn",$"{Translator.GetString("EDCustomSpawn")}{index + 1}"}
            };

            option.ReplacementDictionary = dict;
            option.SetColor(Color.white);
        }
    }
}