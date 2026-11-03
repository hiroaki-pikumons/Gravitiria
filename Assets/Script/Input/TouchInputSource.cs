using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// タッチ入力（Android 向け）。画面を 2 つの領域と数個のボタンに分けて IInputSource に変換する。
///   左半分: 指を置いた場所を中心にした仮想スティック（移動）
///   右半分: スワイプで視点
///   重力ボタン: 押している間は照準（スロー＋ズーム）。押したままドラッグすると視点が動く。離した瞬間に反転
///   ダッシュ / ポーズ / クイック重力 / ポーズメニュー / 言語 のボタン: 押した瞬間のイベント
///   短いタップ（右半分、ボタン外）: 決定 / 字幕を閉じる
/// ボタンの当たり判定は TouchControls（UI 側）が HitTest で提供する。UI が無いときはボタンなしで動く。
/// 実機のタッチが無い環境（エディタ、PC）では、強制有効時にマウス左ボタンを 1 本指として扱う。
/// AutoPilot からは Press/Move/Release で擬似タッチを流し込める。
/// </summary>
public class TouchInputSource : IInputSource
{
    public enum Button { None, Gravity, Dash, Pause, QuickLeft, QuickRight, QuickDown, QuickUp, Resume, Restart, Title, Language }

    public struct TouchPoint
    {
        public int id;
        public Vector2 position;
        public TouchPhase phase;
    }

    public bool IsAvailable => TouchControls.Active;

    public Vector2 Move { get; private set; }
    public Vector2 Look { get; private set; }
    public bool AimHeld { get; private set; }
    public bool AimReleased { get; private set; }
    public bool DashPressed { get; private set; }
    public bool DashHeld { get; private set; }
    public bool PausePressed { get; private set; }
    public bool SubmitPressed { get; private set; }
    public bool CloseMessagePressed { get; private set; }
    public bool CancelPressed { get; private set; }
    public bool RestartPressed { get; private set; }
    public bool ToTitlePressed { get; private set; }
    public Vector2 QuickGravityPressed { get; private set; }
    public bool ToggleLanguagePressed { get; private set; }
    public bool ResetRecordsHeld { get; private set; }

    /// <summary>画面の高さいっぱいスワイプしたときの「マウス単位」。GravityController 側で mouseSensitivity(2) が掛かる。</summary>
    public float lookUnitsPerScreenHeight = 90f;
    /// <summary>スティックの半径（画面の高さに対する比率）。</summary>
    public float stickRadiusFraction = 0.11f;
    /// <summary>これより短く・小さく触れたらタップ扱い。</summary>
    public float tapMaxDuration = 0.3f;
    public float tapMaxMovePixels = 24f;

    /// <summary>ボタンの当たり判定（UI 側が設定）。null ならボタン無し。</summary>
    public System.Func<Vector2, Button> HitTest;
    /// <summary>プレイヤーの居ない画面（タイトル・エンディング）では、どこをタップしても決定にする。</summary>
    public bool tapAnywhereIsSubmit;

    // UI が見た目を描くための状態
    public bool StickActive { get; private set; }
    public Vector2 StickOrigin { get; private set; }
    public Vector2 StickPosition { get; private set; }
    public Button HeldButton => gravityFinger >= 0 ? Button.Gravity : Button.None;
    public bool DashButtonHeld => dashFinger >= 0;

    private class Finger
    {
        public int id;
        public Vector2 start, last;
        public float startTime;
        public bool moved;
        public Button button;      // 押したときに乗っていたボタン
        public bool isStick, isLook;
    }

    private readonly Dictionary<int, Finger> fingers = new Dictionary<int, Finger>();
    private int stickFinger = -1, lookFinger = -1, gravityFinger = -1, dashFinger = -1;
    private readonly List<TouchPoint> frame = new List<TouchPoint>();

    // ---- 擬似タッチ（AutoPilot / テスト用） ----
    private readonly Dictionary<int, Vector2> scripted = new Dictionary<int, Vector2>();
    private readonly HashSet<int> scriptedNew = new HashSet<int>();
    private readonly HashSet<int> scriptedReleased = new HashSet<int>();
    public bool ScriptedMode { get; private set; }

    public void Press(int id, Vector2 screenPos) { ScriptedMode = true; scripted[id] = screenPos; scriptedNew.Add(id); }
    public void MoveTo(int id, Vector2 screenPos) { if (scripted.ContainsKey(id)) scripted[id] = screenPos; }
    public void Release(int id) { if (scripted.ContainsKey(id)) scriptedReleased.Add(id); }

    private bool mouseWasDown;

    public void Poll()
    {
        CollectTouches();

        Look = Vector2.zero;
        AimReleased = false;
        DashPressed = false;
        PausePressed = SubmitPressed = CloseMessagePressed = CancelPressed = RestartPressed = ToTitlePressed = ToggleLanguagePressed = false;
        QuickGravityPressed = Vector2.zero;
        ResetRecordsHeld = false;

        float halfWidth = Screen.width * 0.5f;
        float stickRadius = Mathf.Max(1f, Screen.height * stickRadiusFraction);

        foreach (var tp in frame)
        {
            switch (tp.phase)
            {
                case TouchPhase.Began:
                {
                    var f = new Finger { id = tp.id, start = tp.position, last = tp.position, startTime = Time.unscaledTime };
                    f.button = HitTest != null ? HitTest(tp.position) : Button.None;
                    fingers[tp.id] = f;
                    switch (f.button)
                    {
                        case Button.Gravity:
                            if (gravityFinger < 0) { gravityFinger = tp.id; AimHeld = true; }
                            break;
                        case Button.Dash:
                            if (dashFinger < 0) { dashFinger = tp.id; DashPressed = true; DashHeld = true; }
                            break;
                        case Button.Pause: PausePressed = true; break;
                        case Button.Resume: PausePressed = true; break;
                        case Button.Restart: RestartPressed = true; break;
                        case Button.Title: ToTitlePressed = true; break;
                        case Button.Language: ToggleLanguagePressed = true; break;
                        case Button.QuickLeft: QuickGravityPressed = new Vector2(-1f, 0f); break;
                        case Button.QuickRight: QuickGravityPressed = new Vector2(1f, 0f); break;
                        case Button.QuickDown: QuickGravityPressed = new Vector2(0f, -1f); break;
                        case Button.QuickUp: QuickGravityPressed = new Vector2(0f, 1f); break;
                        case Button.None:
                            if (tp.position.x < halfWidth && stickFinger < 0)
                            {
                                stickFinger = tp.id; f.isStick = true;
                                StickActive = true; StickOrigin = tp.position; StickPosition = tp.position; Move = Vector2.zero;
                            }
                            else if (lookFinger < 0)
                            {
                                lookFinger = tp.id; f.isLook = true;
                            }
                            break;
                    }
                    break;
                }
                case TouchPhase.Moved:
                case TouchPhase.Stationary:
                {
                    if (!fingers.TryGetValue(tp.id, out var f)) break;
                    Vector2 delta = tp.position - f.last;
                    if ((tp.position - f.start).magnitude > tapMaxMovePixels) f.moved = true;
                    if (f.isStick)
                    {
                        Vector2 off = tp.position - StickOrigin;
                        Move = Vector2.ClampMagnitude(off / stickRadius, 1f);
                        StickPosition = StickOrigin + Vector2.ClampMagnitude(off, stickRadius);
                    }
                    else if (f.isLook || tp.id == gravityFinger)
                    {
                        Look += delta / Screen.height * lookUnitsPerScreenHeight;
                    }
                    f.last = tp.position;
                    break;
                }
                case TouchPhase.Ended:
                case TouchPhase.Canceled:
                {
                    if (!fingers.TryGetValue(tp.id, out var f)) break;
                    bool isTap = !f.moved && Time.unscaledTime - f.startTime <= tapMaxDuration && tp.phase == TouchPhase.Ended;
                    if (tp.id == stickFinger) { stickFinger = -1; StickActive = false; Move = Vector2.zero; }
                    if (tp.id == lookFinger)
                    {
                        lookFinger = -1;
                        if (isTap) { SubmitPressed = true; CloseMessagePressed = true; }
                    }
                    if (tp.id == gravityFinger) { gravityFinger = -1; AimHeld = false; AimReleased = tp.phase == TouchPhase.Ended; }
                    if (tp.id == dashFinger) { dashFinger = -1; DashHeld = false; }
                    if (f.isStick && isTap && tapAnywhereIsSubmit) SubmitPressed = true;
                    fingers.Remove(tp.id);
                    break;
                }
            }
        }

        if (fingers.Count == 0) { StickActive = false; Move = Vector2.zero; AimHeld = false; DashHeld = false; }
    }

    /// <summary>今フレームのタッチを集める。擬似タッチ → 実タッチ → マウス代用の順。</summary>
    private void CollectTouches()
    {
        frame.Clear();
        if (ScriptedMode)
        {
            // 同じフレームに押して離した指は、今フレーム Began、次フレーム Ended にする
            var endedNow = new List<int>();
            foreach (var kv in scripted)
            {
                var tp = new TouchPoint { id = kv.Key, position = kv.Value };
                if (scriptedNew.Contains(kv.Key)) tp.phase = TouchPhase.Began;
                else if (scriptedReleased.Contains(kv.Key)) { tp.phase = TouchPhase.Ended; endedNow.Add(kv.Key); }
                else tp.phase = TouchPhase.Moved;
                frame.Add(tp);
            }
            scriptedNew.Clear();
            foreach (var id in endedNow) { scripted.Remove(id); scriptedReleased.Remove(id); }
            if (scripted.Count == 0 && frame.Count == 0) ScriptedMode = false;
            return;
        }

        if (Input.touchCount > 0)
        {
            for (int i = 0; i < Input.touchCount; i++)
            {
                var t = Input.GetTouch(i);
                frame.Add(new TouchPoint { id = t.fingerId, position = t.position, phase = t.phase });
            }
            return;
        }

        // マウスを 1 本指として扱う（エディタ・PC でのデバッグ用）
        if (Input.touchSupported && Application.isMobilePlatform) return;
        bool down = Input.GetMouseButton(0);
        Vector2 pos = Input.mousePosition;
        if (down && !mouseWasDown) frame.Add(new TouchPoint { id = 999, position = pos, phase = TouchPhase.Began });
        else if (down) frame.Add(new TouchPoint { id = 999, position = pos, phase = TouchPhase.Moved });
        else if (mouseWasDown) frame.Add(new TouchPoint { id = 999, position = pos, phase = TouchPhase.Ended });
        mouseWasDown = down;
    }
}
