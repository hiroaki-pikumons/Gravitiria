using TMPro;
using UnityEngine;
using UnityEngine.UI;

/// <summary>コードから UI を組み立てるための最小ヘルパー。</summary>
public static class UiKit
{
    private const string FontPath = "Fonts/NotoSansJP-Regular SDF";
    private static TMP_FontAsset font;

    public static TMP_FontAsset Font
    {
        get
        {
            if (font == null) font = Resources.Load<TMP_FontAsset>(FontPath);
            return font;
        }
    }

    public static readonly Color Accent = new Color(0.45f, 0.75f, 1f);
    public static readonly Color Warn = new Color(1f, 0.45f, 0.35f);
    public static readonly Color Dim = new Color(1f, 1f, 1f, 0.55f);

    /// <summary>HUD・字幕の文字倍率。タッチ操作（小さい画面）では大きくする。</summary>
    public static float HudScale => TouchControls.Active ? 1.3f : 1f;

    public static Canvas CreateCanvas(string name, int sortingOrder, bool persistent)
    {
        var go = new GameObject(name, typeof(RectTransform));
        go.layer = LayerMask.NameToLayer("UI");
        if (persistent) Object.DontDestroyOnLoad(go);
        var canvas = go.AddComponent<Canvas>();
        canvas.renderMode = RenderMode.ScreenSpaceOverlay;
        canvas.sortingOrder = sortingOrder;
        var scaler = go.AddComponent<CanvasScaler>();
        scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
        scaler.referenceResolution = new Vector2(1920f, 1080f);
        scaler.matchWidthOrHeight = 0.5f;
        return canvas;
    }

    public static RectTransform Rect(Transform parent, string name, Vector2 anchorMin, Vector2 anchorMax, Vector2 anchoredPos, Vector2 size)
    {
        var go = new GameObject(name, typeof(RectTransform));
        go.layer = LayerMask.NameToLayer("UI");
        var rt = go.GetComponent<RectTransform>();
        rt.SetParent(parent, false);
        rt.anchorMin = anchorMin;
        rt.anchorMax = anchorMax;
        rt.anchoredPosition = anchoredPos;
        rt.sizeDelta = size;
        return rt;
    }

    public static Image Panel(Transform parent, string name, Color color, Vector2 anchorMin, Vector2 anchorMax, Vector2 anchoredPos, Vector2 size)
    {
        var rt = Rect(parent, name, anchorMin, anchorMax, anchoredPos, size);
        var img = rt.gameObject.AddComponent<Image>();
        img.color = color;
        img.raycastTarget = false;
        return img;
    }

    public static TextMeshProUGUI Text(Transform parent, string name, string text, float size, Color color,
        Vector2 anchorMin, Vector2 anchorMax, Vector2 anchoredPos, Vector2 boxSize,
        TextAlignmentOptions align = TextAlignmentOptions.Center, FontStyles style = FontStyles.Normal)
    {
        var rt = Rect(parent, name, anchorMin, anchorMax, anchoredPos, boxSize);
        var tmp = rt.gameObject.AddComponent<TextMeshProUGUI>();
        tmp.font = Font;
        tmp.text = text;
        tmp.fontSize = size;
        tmp.color = color;
        tmp.alignment = align;
        tmp.fontStyle = style;
        tmp.raycastTarget = false;
        tmp.enableWordWrapping = true;
        tmp.richText = true;
        return tmp;
    }

    public static string FormatTime(float seconds)
    {
        if (seconds < 0f) seconds = 0f;
        int m = (int)(seconds / 60f);
        float s = seconds - m * 60f;
        return $"{m:00}:{s:00.00}";
    }
}
