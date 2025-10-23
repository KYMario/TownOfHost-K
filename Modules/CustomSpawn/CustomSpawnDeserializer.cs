using System;
using System.IO;
using System.Text.Json;
using System.Collections.Generic;
using UnityEngine;

using static TownOfHost.CustomSpawnManager;
using TownOfHost.Modules;

namespace TownOfHost;

public class CustomSpawnDeserializer
{
    private static readonly LogHandler logger = Logger.Handler(nameof(CustomSpawnDeserializer));

    public static CustomSpawnData Deserialize(string json, out bool updated)
    {
        using var doc = JsonDocument.Parse(json);
        var root = doc.RootElement;
        updated = false;

        if (!root.TryGetProperty("Version", out var property) || !property.TryGetInt32(out int version))
        {
            version = 0;
            logger.Warn($"バージョンタグの取得に失敗");
        }

        try
        {
            for (var ver = version; ver < CustomSpawnManager.Version; ver++)
            {
                logger.Info($"変換中../バージョン:{ver}");
                json = ver switch
                {
                    0 => V0.Upgrade(json),
                    _ => json
                };
                updated = true;
            }
        }
        catch (Exception ex)
        {
            logger.Exception(ex);
            updated = false;
        }

        Logger.Info($"スポーンのロード開始", nameof(CustomSpawnDeserializer));
        return V1.Deserialize(json);
    }

    class V0
    {
        public static void GetConverters(JsonSerializerOptions opt)
        {
            opt.Converters.Add(new JsonHelper.Vector2Converter());
        }

        public static Dictionary<int, List<Vector2>> Deserialize(string json)
        {
            var options = new JsonSerializerOptions();
            GetConverters(options);

            return JsonSerializer.Deserialize<Dictionary<int, List<Vector2>>>(json, options);
        }

        public static string Upgrade(string json)
        {
            var data = Deserialize(json);
            var newData = new CustomSpawnData() { Version = 1 };
            var spawnMaps = newData.Presets[0].SpawnMaps;
            foreach (var (mapId, positions) in data)
            {
                var map = (MapNames)mapId;
                var spawnData = spawnMaps[map] = new(map);

                for (var i = 0; i < positions.Count; ++i)
                {
                    spawnData.AddSpawn(new($"{Translator.GetString("EDCustomSpawn")}{i + 1}", positions[i]));
                }
            }

            var options = new JsonSerializerOptions();
            V1.GetConverters(options);
            return JsonSerializer.Serialize(newData, options);
        }
    }
    class V1
    {
        public static void GetConverters(JsonSerializerOptions opt)
        {
            opt.Converters.Add(new JsonHelper.Vector2Converter());
            opt.Converters.Add(new JsonHelper.ColorConverter());
        }

        public static CustomSpawnData Deserialize(string json)
        {
            var options = new JsonSerializerOptions();
            GetConverters(options);

            return JsonSerializer.Deserialize<CustomSpawnData>(json, options);
        }
    }
}