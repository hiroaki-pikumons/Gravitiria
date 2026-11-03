using System;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

/// <summary>
/// 画面上のタッチ操作 UI（仮想スティック、重力 / ダッシュ / ポーズのボタン、解放後のクイック重力、ポーズメニュー）。
/// 実機（Application.isMobilePlatform）では自動で有効。PC では非表示。
/// デバッグ: 起動引数 -touchui、または Development Build / エディタで F10 を押すと切り替わる。
/// すべての要素は Screen.safeArea の内側に置く（ノッチ・丸角を避ける）。
/// </summary>
public class TouchControls : MonoBehaviour
{
    public static TouchControls Instance { get; private set; }
    public static TouchInputSource Source { get; private set; }

    // 起動引数 -touchui で強制有効（静的初期化なので、どの Bootstrap より先に決まる）
    private static bool forced = HasArg("-touchui");
    /// <summary>タッチ UI が有効か。GravityController はこれが true の間カーソルロックを要求しない。</summary>
    public static bool Active => Application.isMobilePlatform || forced;

    public static void SetForced(bool value)
    {
        forced = value;
        if (Instance != null) Instance.Refresh();
        Debug.Log($"[TouchControls] forced={value} active={Active}");
    }

    private Canvas canvas;
    private RectTransform safeRoot;
    private Rect lastSafeArea;
    private Vector2Int lastScreen;

    private Image stickBase, stickKnob;
    private Image gravityButton, dashButton, dashCooldown, pauseButton;
    private readonly Dictionary<TouchInputSource.Button, RectTransform> hitRects = new Dictionary<TouchInputSource.Button, RectTransform>();
    private readonly List<RectTransform> quickButtons = new List<RectTransform>();
    private RectTransform pauseMenu;
    private RectTransform languageButton;
    private TextMeshProUGUI gravityLabel;
    private static Sprite circle;

    private static readonly Color Idle = new Color(1f, 1f, 1f, 0.28f);
    private static readonly Color Pressed = new Color(0.45f, 0.75f, 1f, 0.6f);

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
    private static void Bootstrap()
    {
        if (Instance != null) return;

        Source = new TouchInputSource();
        GameInput.AddSource(Source);

        var go = new GameObject("TouchControls");
        DontDestroyOnLoad(go);
        Instance = go.AddComponent<TouchControls>();

        if (Application.isMobilePlatform)
        {
            // Android の既定は 30fps。ゲームは軽いので 60 を要求し、プレイ中は画面を消さない
            Application.targetFrameRate = 60;
            Screen.sleepTimeout = SleepTimeout.NeverSleep;
        }
    }

    private void Awake()
    {
        BuildUi();
        SceneManager.sceneLoaded += OnSceneLoaded;
        Refresh();
    }

    private void OnDestroy()
    {
        SceneManager.sceneLoaded -= OnSceneLoaded;
        if (Instance == this) Instance = null;
    }

    private void OnSceneLoaded(Scene scene, LoadSceneMode mode) => Refresh();

    /// <summary>ボタンの中心（スクリーン座標）。AutoPilot の擬似タッチ用。</summary>
    public Vector2 ButtonCenter(TouchInputSource.Button b)
    {
        if (!hitRects.TryGetValue(b, out var rt)) return new Vector2(-1f, -1f);
        return RectTransformUtility.WorldToScreenPoint(null, rt.position);
    }

    // ------------------------------------------------------------ UI
    private void BuildUi()
    {
        canvas = UiKit.CreateCanvas("TouchUI", 60, persistent: true);
        canvas.transform.SetParent(transform, false);
        var group = canvas.gameObject.AddComponent<CanvasGroup>();
        group.blocksRaycasts = false;
        group.interactable = false;

        safeRoot = UiKit.Rect(canvas.transform, "SafeArea", Vector2.zero, Vector2.one, Vector2.zero, Vector2.zero);

        // 仮想スティック（指を置いた所に出る）
        stickBase = Circle(safeRoot, "StickBase", 230f, new Color(1f, 1f, 1f, 0.16f));
        stickKnob = Circle(safeRoot, "StickKnob", 100f, Idle);
        stickBase.gameObject.SetActive(false);
        stickKnob.gameObject.SetActive(false);

        // 右下: 重力（大）、その左にダッシュ
        gravityButton = Circle(safeRoot, "Gravity", 210f, Idle, new Vector2(1f, 0f), new Vector2(-150f, 150f));
        gravityLabel = Label(gravityButton.rectTransform, Loc.T("重力", "FLIP"), 40f);
        Register(TouchInputSource.Button.Gravity, gravityButton.rectTransform);

        dashButton = Circle(safeRoot, "Dash", 140f, Idle, new Vector2(1f, 0f), new Vector2(-360f, 110f));
        dashCooldown = Circle(dashButton.rectTransform, "Cooldown", 140f, new Color(0.1f, 0.9f, 1f, 0.45f));
        dashCooldown.type = Image.Type.Filled;
        dashCooldown.fillMethod = Image.FillMethod.Radial360;
        dashCooldown.fillOrigin = (int)Image.Origin360.Top;
        dashCooldown.fillClockwise = false;
        dashCooldown.fillAmount = 0f;
        Label(dashButton.rectTransform, "DASH", 30f);
        Register(TouchInputSource.Button.Dash, dashButton.rectTransform);

        // クイック重力（解放後のみ）: 重力ボタンの上に十字
        AddQuick(TouchInputSource.Button.QuickUp, "▲", new Vector2(-150f, 345f));
        AddQuick(TouchInputSource.Button.QuickDown, "▼", new Vector2(-150f, 275f));
        AddQuick(TouchInputSource.Button.QuickLeft, "◀", new Vector2(-220f, 310f));
        AddQuick(TouchInputSource.Button.QuickRight, "▶", new Vector2(-80f, 310f));

        // 右上: ポーズ
        pauseButton = Circle(safeRoot, "Pause", 90f, Idle, new Vector2(1f, 1f), new Vector2(-75f, -75f));
        Label(pauseButton.rectTransform, "II", 36f, FontStyles.Bold);
        Register(TouchInputSource.Button.Pause, pauseButton.rectTransform);

        // ポーズメニュー（ポーズ中のみ）
        pauseMenu = UiKit.Rect(safeRoot, "PauseMenu", new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), new Vector2(0f, -150f), new Vector2(1400f, 120f));
        AddMenuButton(TouchInputSource.Button.Resume, Loc.T("再開", "RESUME"), new Vector2(-460f, 0f));
        AddMenuButton(TouchInputSource.Button.Restart, Loc.T("やり直す", "RESTART"), new Vector2(0f, 0f));
        AddMenuButton(TouchInputSource.Button.Title, Loc.T("タイトルへ", "TITLE"), new Vector2(460f, 0f));
        pauseMenu.gameObject.SetActive(false);

        // タイトル画面: 言語切替
        languageButton = Pill(safeRoot, "Language", Loc.T("English", "日本語"), new Vector2(1f, 1f), new Vector2(-150f, -70f), new Vector2(240f, 80f));
        Register(TouchInputSource.Button.Language, languageButton);
        languageButton.gameObject.SetActive(false);

        Source.HitTest = HitTest;
    }

    private void AddQuick(TouchInputSource.Button b, string glyph, Vector2 pos)
    {
        var img = Circle(safeRoot, b.ToString(), 64f, Idle, new Vector2(1f, 0f), pos);
        Label(img.rectTransform, glyph, 28f);
        Register(b, img.rectTransform);
        quickButtons.Add(img.rectTransform);
    }

    private void AddMenuButton(TouchInputSource.Button b, string text, Vector2 pos)
    {
        var rt = Pill(pauseMenu, b.ToString(), text, new Vector2(0.5f, 0.5f), pos, new Vector2(400f, 110f));
        Register(b, rt);
    }

    private RectTransform Pill(Transform parent, string name, string text, Vector2 anchor, Vector2 pos, Vector2 size)
    {
        // 外側が薄い縁、内側が暗い面
        var img = UiKit.Panel(parent, name, new Color(1f, 1f, 1f, 0.35f), anchor, anchor, pos, size);
        UiKit.Panel(img.transform, "Face", new Color(0f, 0f, 0f, 0.6f), Vector2.zero, Vector2.one, Vector2.zero, new Vector2(-6f, -6f));
        Label(img.rectTransform, text, 36f);
        return img.rectTransform;
    }

    private void Register(TouchInputSource.Button b, RectTransform rt) => hitRects[b] = rt;

    private TouchInputSource.Button HitTest(Vector2 screenPos)
    {
        foreach (var kv in hitRects)
        {
            if (!kv.Value.gameObject.activeInHierarchy) continue;
            if (RectTransformUtility.RectangleContainsScreenPoint(kv.Value, screenPos, null)) return kv.Key;
        }
        return TouchInputSource.Button.None;
    }

    private static Image Circle(Transform parent, string name, float diameter, Color color, Vector2? anchor = null, Vector2? pos = null)
    {
        Vector2 a = anchor ?? new Vector2(0.5f, 0.5f);
        var img = UiKit.Panel(parent, name, color, a, a, pos ?? Vector2.zero, new Vector2(diameter, diameter));
        img.sprite = CircleSprite();
        img.type = Image.Type.Simple;
        img.preserveAspect = true;
        return img;
    }

    private static TextMeshProUGUI Label(RectTransform parent, string text, float size, FontStyles style = FontStyles.Bold)
    {
        return UiKit.Text(parent, "Label", text, size, Color.white, Vector2.zero, Vector2.one, Vector2.zero, Vector2.zero, TextAlignmentOptions.Center, style);
    }

    /// <summary>アンチエイリアスの効いた円のスプライトを実行時に作る（画像アセット不要）。</summary>
    private static Sprite CircleSprite()
    {
        if (circle != null) return circle;
        const int n = 128;
        var tex = new Texture2D(n, n, TextureFormat.RGBA32, false) { filterMode = FilterMode.Bilinear };
        var px = new Color32[n * n];
        float r = n * 0.5f - 1f, c = n * 0.5f - 0.5f;
        for (int y = 0; y < n; y++)
            for (int x = 0; x < n; x++)
            {
                float d = Mathf.Sqrt((x - c) * (x - c) + (y - c) * (y - c));
                float a = Mathf.Clamp01(r - d + 0.5f);
                px[y * n + x] = new Color32(255, 255, 255, (byte)(a * 255f));
            }
        tex.SetPixels32(px);
        tex.Apply();
        circle = Sprite.Create(tex, new Rect(0, 0, n, n), new Vector2(0.5f, 0.5f), 100f);
        return circle;
    }

    // ------------------------------------------------------------ update
    private void Refresh()
    {
        bool active = Active;
        canvas.gameObject.SetActive(active);
        if (!active) return;
        bool inStage = GameSession.IsStage(SceneManager.GetActiveScene().name);
        bool title = SceneManager.GetActiveScene().name == "Title";
        gravityButton.gameObject.SetActive(inStage);
        dashButton.gameObject.SetActive(inStage && Progress.DashUnlocked);
        foreach (var q in quickButtons) q.gameObject.SetActive(inStage && Progress.QuickGravityUnlocked);
        pauseButton.gameObject.SetActive(inStage);
        languageButton.gameObject.SetActive(title);
        Source.tapAnywhereIsSubmit = !inStage;
        gravityLabel.text = Loc.T("重力", "FLIP");
        ApplySafeArea(force: true);
    }

    private void ApplySafeArea(bool force = false)
    {
        Rect sa = Screen.safeArea;
        var screen = new Vector2Int(Screen.width, Screen.height);
        if (!force && sa == lastSafeArea && screen == lastScreen) return;
        lastSafeArea = sa; lastScreen = screen;
        if (Screen.width <= 0 || Screen.height <= 0) return;
        safeRoot.anchorMin = new Vector2(sa.xMin / Screen.width, sa.yMin / Screen.height);
        safeRoot.anchorMax = new Vector2(sa.xMax / Screen.width, sa.yMax / Screen.height);
        safeRoot.offsetMin = Vector2.zero;
        safeRoot.offsetMax = Vector2.zero;
    }

    private void Update()
    {
        // デバッグ切替（開発ビルドとエディタのみ）
        if ((Debug.isDebugBuild || Application.isEditor) && Input.GetKeyDown(KeyCode.F10)) SetForced(!forced);
        if (!Active) return;
        ApplySafeArea();

        bool inStage = GameSession.IsStage(SceneManager.GetActiveScene().name);
        if (inStage)
        {
            bool dashNow = Progress.DashUnlocked, quickNow = Progress.QuickGravityUnlocked;
            if (dashButton.gameObject.activeSelf != dashNow) dashButton.gameObject.SetActive(dashNow);
            if (quickButtons[0].gameObject.activeSelf != quickNow) foreach (var q in quickButtons) q.gameObject.SetActive(quickNow);
            if (pauseMenu.gameObject.activeSelf != GameSession.IsPaused) pauseMenu.gameObject.SetActive(GameSession.IsPaused);
        }

        // スティックの見た目
        if (Source.StickActive)
        {
            if (!stickBase.gameObject.activeSelf) { stickBase.gameObject.SetActive(true); stickKnob.gameObject.SetActive(true); }
            stickBase.rectTransform.position = ScreenToCanvas(Source.StickOrigin);
            stickKnob.rectTransform.position = ScreenToCanvas(Source.StickPosition);
        }
        else if (stickBase.gameObject.activeSelf) { stickBase.gameObject.SetActive(false); stickKnob.gameObject.SetActive(false); }

        gravityButton.color = Source.HeldButton == TouchInputSource.Button.Gravity ? Pressed : Idle;
        dashButton.color = Source.DashButtonHeld ? Pressed : Idle;
        var player = GravityController.Current;
        dashCooldown.fillAmount = player != null ? player.DashCooldownRatio : 0f;
    }

    private Vector3 ScreenToCanvas(Vector2 screenPos)
    {
        // Overlay キャンバスではスクリーン座標 = ワールド座標
        return new Vector3(screenPos.x, screenPos.y, 0f);
    }

    private static bool HasArg(string name)
    {
        foreach (var a in Environment.GetCommandLineArgs()) if (string.Equals(a, name, StringComparison.OrdinalIgnoreCase)) return true;
        return false;
    }
}
