using System.Linq;
using UnityEngine;
using Il2CppInterop.Runtime;
using Il2CppInterop.Runtime.InteropTypes;

namespace TownOfHost;

public static class ObjectHelper
{
    /// <summary>
    /// オブジェクトの<see cref="TextTranslatorTMP"/>コンポーネントを破棄します
    /// </summary>
    public static void DestroyTranslator(this GameObject obj)
    {
        var translator = obj.GetComponent<TextTranslatorTMP>();
        if (translator != null)
        {
            Object.Destroy(translator);
        }
    }
    /// <summary>
    /// オブジェクトの<see cref="TextTranslatorTMP"/>コンポーネントを破棄します
    /// </summary>
    public static void DestroyTranslator(this MonoBehaviour obj) => obj.gameObject.DestroyTranslator();

    /// <summary>
    /// オブジェクトを取得します
    /// </summary>
    public static T FindObjectsOfTypeAll<T>() where T : Il2CppObjectBase
    {
        var objs = Resources.FindObjectsOfTypeAll(Il2CppType.Of<T>());
        if (objs == null || objs.Length == 0) return null;
        return objs.First().TryCast<T>();
    }
}
