
using System.Collections;
using BepInEx.Unity.IL2CPP.Utils.Collections;
using Rewired.Utils;
using TownOfHost.Templates;
using UnityEngine;

namespace TownOfHost;

class ToolTip
{
    public static SimpleButton button = null;
    public static MonoBehaviour obj;
    public static Coroutine coTimer;
    public static float delay = 1.0f;

    public static void Setup()
    {
        if (button?.Button?.IsDestroyedOrNull() == false) return;
        button = new SimpleButton(null, "ToolTip", Vector3.zero, new(0, 0, 0, 200), Color.black, () => { }, string.Empty, true);

        button.Button.gameObject.layer = LayerMask.NameToLayer("UI");
        button.NormalSprite.gameObject.layer = LayerMask.NameToLayer("UI");

        button.Button.ClickSound = null;
        button.Button.transform.localScale = new Vector3(0.5f, 0.5f, 1);
        button.Label.alignment = TMPro.TextAlignmentOptions.Left;
        button.NormalSprite.sprite = button.HoverSprite.sprite;
        GameObject.Destroy(button.HoverSprite.gameObject);

        foreach (var collider in button.Button.Colliders)
            GameObject.Destroy(collider);
        GameObject.DontDestroyOnLoad(button.Button);
        button.Button.gameObject.SetActive(false);
    }

    /// <summary>
    /// ツールチップを表示します
    /// </summary>
    /// <param name="mono">ツールチップを呼び出したオブジェクト</param>
    /// <param name="text">表示させたいテキスト</param>
    /// <param name="pos">表示させたい場所 / nullでマウスの場所をベースに<c>mono</c>の少し下に召喚されます </param>
    public static void Show(MonoBehaviour mono, string text, Vector3? pos)
    {
        Setup();
        Hide();

        obj = mono;
        coTimer = obj.StartCoroutine(CoShow(text, pos).WrapToIl2Cpp());
    }

    private static IEnumerator CoShow(string text, Vector3? pos)
    {
        yield return new WaitForSeconds(delay);

        button.Label.text = text;
        button.Label.ForceMeshUpdate(true);
        var textBounds = button.Label.GetRenderedValues(true);

        pos ??= GetMoucePos(new Vector3(0, -(textBounds.y / 2), obj.transform.position.z - 5f));
        button.Button.transform.position = pos.Value;
        button.NormalSprite.size = textBounds + new Vector2(0.1f, 0.1f);
        button.Label.rectTransform.pivot = new((textBounds.x - 1.1f) / 2, 0.5f);

        button.Button.gameObject.SetActive(true);
        button.Button.StartCoroutine(CoCheckObject().WrapToIl2Cpp());
        coTimer = null;
    }

    private static IEnumerator CoCheckObject()
    {
        while (true)
        {
            if (obj.IsDestroyedOrNull() || !obj.gameObject.activeInHierarchy)
            {
                coTimer = null;
                Hide();
                yield break;
            }
            yield return null;
        }
    }

    /// <summary>Showを呼び出した後は必ず呼ぶように / monoが非表示orDestorydになった場合はダイジョブ</summary>
    public static void Hide()
    {
        button?.Button?.StopAllCoroutines();
        button?.Button?.gameObject?.SetActive(false);

        if (coTimer != null) obj?.StopCoroutine(coTimer);
        coTimer = null;
        button?.Button?.gameObject?.SetActive(false); //念のため
    }

    public static Vector3 GetMoucePos(Vector3 offset = default)
    {
        Vector3 mousePos = Input.mousePosition;
        var pos = Camera.main.ScreenToWorldPoint(mousePos);
        pos.z = 0;
        pos += offset;
        return pos;
    }
}