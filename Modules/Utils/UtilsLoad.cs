using System.IO;
using System.Reflection;
using UnityEngine;

namespace TownOfHost
{
    #region Sprite
    public static class UtilsSprite
    {
        public static Sprite LoadSprite(string path, float pixelsPerUnit = 1f)
        {
            Sprite sprite = null;
            try
            {
                var stream = Assembly.GetExecutingAssembly().GetManifestResourceStream(path);
                if (stream == null) return null;
                var texture = new Texture2D(1, 1, TextureFormat.ARGB32, false);
                using MemoryStream ms = new();
                stream.CopyTo(ms);
                ImageConversion.LoadImage(texture, ms.ToArray());
                sprite = Sprite.Create(texture, new(0, 0, texture.width, texture.height), new(0.5f, 0.5f), pixelsPerUnit);
            }
            catch
            {
                //Logger.Error($"\"{path}\"の読み込みに失敗しました。", "LoadSprite");
            }
            return sprite;
        }
    }
}
#endregion