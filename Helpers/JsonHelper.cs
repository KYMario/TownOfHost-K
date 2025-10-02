using System;
using System.Text.Json;
using System.Text.Json.Serialization;
using UnityEngine;

namespace TownOfHost;

public static class JsonHelper
{
    public class Vector2Converter : JsonConverter<Vector2>
    {
        public override Vector2 Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
        {
            using JsonDocument doc = JsonDocument.ParseValue(ref reader);
            float x = doc.RootElement.GetProperty("x").GetSingle();
            float y = doc.RootElement.GetProperty("y").GetSingle();
            return new Vector2(x, y);
        }

        public override void Write(Utf8JsonWriter writer, Vector2 value, JsonSerializerOptions options)
        {
            writer.WriteStartObject();
            writer.WriteNumber("x", value.x);
            writer.WriteNumber("y", value.y);
            writer.WriteEndObject();
        }
    }
    public class ColorConverter : JsonConverter<Color32>
    {
        public override Color32 Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
        {
            var colorString = reader.GetString();
            return StringHelper.CodeColor(colorString);
        }

        public override void Write(Utf8JsonWriter writer, Color32 value, JsonSerializerOptions options)
        {
            writer.WriteStringValue(StringHelper.ColorCode(value));
        }
    }
}