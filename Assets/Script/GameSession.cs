using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.SceneManagement;

/// <summary>
/// 1 周（Tutorial → Level5）のタイム・死亡回数・スコアを追跡し、HUD とポーズメニューを提供する。
/// 自己ベストは PlayerPrefs に保存される。
/// </summary>
public class GameSession : MonoBehaviour
{
    public struct StageInfo
    {
        public int Order;
        public string Title;
        public string SubtitleJa;
        public string SubtitleEn;
        public string Subtitle => Loc.T(SubtitleJa, SubtitleEn);
        public StageInfo(int order, string title, string subtitleJa, string subtitleEn)
        {
            Order = order; Title = title; SubtitleJa = subtitleJa; SubtitleEn = subtitleEn;
        }
    }

    public struct StageResult
    {
        public float Time;
        public int Deaths;
        public int Score => Mathf.Max(500, 10000 - Mathf.RoundToInt(Time) * 20 - Deaths * 750);
    }

    public struct RunSummary
    {
        public int Score;
        public float TotalTime;
        public int TotalDeaths;
        public bool NewBestScore;
        public bool NewBestTime;
        public int BestScore;
        public float BestTime;
    }

    public static readonly Dictionary<string, StageInfo> Stages = new Dictionary<string, StageInfo>
    {
        { "Tutorial",     new StageInfo(0, "TUTORIAL",   "基本操作",      "Basics") },
        { "Level1",       new StageInfo(1, "STAGE 1",    "裏側へ",        "To the Underside") },
        { "Level2",       new StageInfo(2, "STAGE 2",    "壁を歩け",      "Walk the Walls") },
        { "Level3",       new StageInfo(3, "STAGE 3",    "時間の番人",    "Keepers of Time") },
        { "Level4",       new StageInfo(4, "STAGE 4",    "塔",            "The Tower") },
        { "Level5",       new StageInfo(5, "STAGE 5",    "最終試験",      "Final Exam") },
        { "R1",           new StageInfo(6, "REMIX 1",    "反転と鍵",      "Flip & Key") },
        { "R2",           new StageInfo(7, "REMIX 2",    "裏口",          "Back Door") },
        { "R3",           new StageInfo(8, "REMIX 3",    "誘導",          "Lure") },
        { "R4",           new StageInfo(9, "REMIX 4",    "点滅",          "Blink") },
        { "R5",           new StageInfo(10, "REMIX 5",   "総合試験",      "Final Remix") },
    };
    public static readonly string[] StageOrder = { "Tutorial", "Level1", "Level2", "Level3", "Level4", "Level5", "R1", "R2", "R3", "R4", "R5" };

    // ステージクリアで解放する能力（ピックアップを取り逃した場合の保険）
    public static readonly Dictionary<string, Progress.Ability> UnlockOnClear = new Dictionary<string, Progress.Ability>
    {
        { "Level5", Progress.Ability.Dash },
        { "R1", Progress.Ability.QuickGravity },
    };

    public const string KeyBestScore = "best_score";
    public const string KeyBestTime = "best_time";
    public const string KeyRuns = "runs_completed";
    public static string KeyStageTime(string scene) => "best_stage_time_" + scene;

    public static GameSession Instance { get; private set; }
    public static bool IsPaused => Instance != null && Instance.paused;
    public static bool IsStage(string scene) => Stages.ContainsKey(scene);

    public readonly Dictionary<string, StageResult> Results = new Dictionary<string, StageResult>();
    public RunSummary LastSummary { get; private set; }

    private string currentScene = "";
    private string lastScene = "";
    private float stageStart;
    private int stageDeaths;
    private int runDeaths;
    private bool stageCleared;
    private bool paused;
    private float pauseBegan;

    /// <summary>この周回での累計死亡数。死亡やステージ再読み込みをまたいで保持され、最初からやり直す時だけ 0 に戻る。</summary>
    public int RunDeaths => runDeaths;

    private Canvas hud;
    private CanvasGroup hudGroup;
    private TextMeshProUGUI stageLabel, timerLabel, deathsLabel, bannerTitle, bannerSub, pauseText, abilityLabel;
    private CanvasGroup bannerGroup, pauseGroup;
    private Coroutine bannerRoutine;

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
    private static void Bootstrap()
    {
        if (Instance != null) return;
        var go = new GameObject("GameSession");
        DontDestroyOnLoad(go);
        Instance = go.AddComponent<GameSession>();
        Instance.BuildHud();
        SceneManager.sceneLoaded += Instance.OnSceneLoaded;
        Instance.OnSceneLoaded(SceneManager.GetActiveScene(), LoadSceneMode.Single);
    }

    private void OnDestroy()
    {
        SceneManager.sceneLoaded -= OnSceneLoaded;
        if (Instance == this) Instance = null;
    }

    // ------------------------------------------------------------ run tracking
    private void OnSceneLoaded(Scene scene, LoadSceneMode mode)
    {
        SetPaused(false, silent: true);
        lastScene = currentScene;
        currentScene = scene.name;
        bool isStage = IsStage(currentScene);
        hudGroup.alpha = isStage ? 1f : 0f;

        if (!isStage) return;

        // 同じステージの再読み込み（死亡・やり直し）ではタイマーと死亡数を引き継ぐ
        bool isReload = currentScene == lastScene && !stageCleared;
        // タイトルなどステージ外から Tutorial に入った時だけ新しい周回
        bool isNewRun = currentScene == "Tutorial" && !IsStage(lastScene);

        if (isNewRun)
        {
            Results.Clear();
            runDeaths = 0;
        }
        if (!isReload)
        {
            stageStart = Time.unscaledTime;
            stageDeaths = 0;
            stageCleared = false;
        }

        var info = Stages[currentScene];
        stageLabel.text = $"{info.Title}  <color=#A0C8FF>{info.Subtitle}</color>";
        if (bannerRoutine != null) StopCoroutine(bannerRoutine);
        bannerRoutine = StartCoroutine(ShowBanner(info));
        Debug.Log($"[GameSession] scene={currentScene} reload={isReload} newRun={isNewRun} runTime={RunTime:F2} stageDeaths={stageDeaths} runDeaths={runDeaths}");
    }

    public float CurrentStageTime => stageCleared ? Results[currentScene].Time : Time.unscaledTime - stageStart;

    /// <summary>周回の累計タイム（クリア済みステージの合計＋現在ステージの経過）。死亡では戻らない。</summary>
    public float RunTime
    {
        get
        {
            float total = 0f;
            foreach (var kv in Results) total += kv.Value.Time;
            if (IsStage(currentScene) && !stageCleared) total += Time.unscaledTime - stageStart;
            return total;
        }
    }

    public void RegisterDeath()
    {
        if (!IsStage(currentScene) || stageCleared) return;
        stageDeaths++;
        runDeaths++;
        SfxPlayer.Play("sfx_death", 0.9f);
    }

    public void StageCleared()
    {
        if (!IsStage(currentScene) || stageCleared) return;
        stageCleared = true;
        var result = new StageResult { Time = Time.unscaledTime - stageStart, Deaths = stageDeaths };
        Results[currentScene] = result;

        string key = KeyStageTime(currentScene);
        if (!PlayerPrefs.HasKey(key) || result.Time < PlayerPrefs.GetFloat(key)) PlayerPrefs.SetFloat(key, result.Time);
        PlayerPrefs.Save();
        SfxPlayer.Play("sfx_goal");
        if (UnlockOnClear.TryGetValue(currentScene, out var ability)) Progress.Unlock(ability);
    }

    /// <summary>エンディングで呼ぶ。周回の合計を確定し自己ベストを更新する。</summary>
    public RunSummary FinishRun()
    {
        var summary = new RunSummary();
        foreach (var kv in Results)
        {
            summary.Score += kv.Value.Score;
            summary.TotalTime += kv.Value.Time;
            summary.TotalDeaths += kv.Value.Deaths;
        }

        int bestScore = PlayerPrefs.GetInt(KeyBestScore, 0);
        float bestTime = PlayerPrefs.GetFloat(KeyBestTime, 0f);
        summary.NewBestScore = summary.Score > bestScore;
        summary.NewBestTime = Results.Count > 0 && (bestTime <= 0f || summary.TotalTime < bestTime);
        if (summary.NewBestScore) PlayerPrefs.SetInt(KeyBestScore, summary.Score);
        if (summary.NewBestTime) PlayerPrefs.SetFloat(KeyBestTime, summary.TotalTime);
        PlayerPrefs.SetInt(KeyRuns, PlayerPrefs.GetInt(KeyRuns, 0) + 1);
        PlayerPrefs.Save();

        summary.BestScore = PlayerPrefs.GetInt(KeyBestScore, 0);
        summary.BestTime = PlayerPrefs.GetFloat(KeyBestTime, 0f);
        LastSummary = summary;
        return summary;
    }

    public static void ResetRecords()
    {
        PlayerPrefs.DeleteKey(KeyBestScore);
        PlayerPrefs.DeleteKey(KeyBestTime);
        PlayerPrefs.DeleteKey(KeyRuns);
        foreach (var s in StageOrder) PlayerPrefs.DeleteKey(KeyStageTime(s));
        Progress.ResetAll();
        PlayerPrefs.Save();
    }

    // ------------------------------------------------------------ HUD
    private void BuildHud()
    {
        hud = UiKit.CreateCanvas("HUD", 50, persistent: true);
        hud.transform.SetParent(transform, false);
        hudGroup = hud.gameObject.AddComponent<CanvasGroup>();
        hudGroup.blocksRaycasts = false;
        hudGroup.interactable = false;

        // 小さい画面（タッチ）では HUD の文字を大きくし、ノッチを避けて少し内側に置く
        float hs = UiKit.HudScale;
        float inset = TouchControls.Active ? 60f : 32f;
        float insetRight = TouchControls.Active ? 170f : 32f;   // 右上はタッチのポーズボタンを避ける
        stageLabel = UiKit.Text(hud.transform, "StageLabel", "", 30f * hs, Color.white,
            new Vector2(0f, 1f), new Vector2(0f, 1f), new Vector2(inset, -28f), new Vector2(700f, 44f * hs), TextAlignmentOptions.Left);
        stageLabel.rectTransform.pivot = new Vector2(0f, 1f);

        timerLabel = UiKit.Text(hud.transform, "Timer", "", 30f * hs, Color.white,
            new Vector2(1f, 1f), new Vector2(1f, 1f), new Vector2(-insetRight, -28f), new Vector2(600f, 44f * hs), TextAlignmentOptions.Right);
        timerLabel.rectTransform.pivot = new Vector2(1f, 1f);

        deathsLabel = UiKit.Text(hud.transform, "Deaths", "", 30f * hs, Color.white,
            new Vector2(1f, 1f), new Vector2(1f, 1f), new Vector2(-insetRight, -28f - 42f * hs), new Vector2(600f, 44f * hs), TextAlignmentOptions.Right);
        deathsLabel.rectTransform.pivot = new Vector2(1f, 1f);

        abilityLabel = UiKit.Text(hud.transform, "Abilities", "", 22f * hs, UiKit.Dim,
            new Vector2(0f, 1f), new Vector2(0f, 1f), new Vector2(inset, -28f - 42f * hs), new Vector2(700f, 32f * hs), TextAlignmentOptions.Left);
        abilityLabel.rectTransform.pivot = new Vector2(0f, 1f);

        var banner = UiKit.Rect(hud.transform, "Banner", new Vector2(0f, 0.5f), new Vector2(1f, 0.5f), new Vector2(0f, 120f), new Vector2(0f, 220f));
        bannerGroup = banner.gameObject.AddComponent<CanvasGroup>();
        bannerGroup.alpha = 0f;
        UiKit.Panel(banner, "Shade", new Color(0f, 0f, 0f, 0.45f), Vector2.zero, Vector2.one, Vector2.zero, Vector2.zero);
        bannerTitle = UiKit.Text(banner, "Title", "", 88f, Color.white,
            new Vector2(0f, 0.5f), new Vector2(1f, 0.5f), new Vector2(0f, 30f), new Vector2(0f, 110f), TextAlignmentOptions.Center, FontStyles.Bold);
        bannerSub = UiKit.Text(banner, "Sub", "", 40f, UiKit.Accent,
            new Vector2(0f, 0.5f), new Vector2(1f, 0.5f), new Vector2(0f, -50f), new Vector2(0f, 60f));

        var pause = UiKit.Rect(hud.transform, "Pause", Vector2.zero, Vector2.one, Vector2.zero, Vector2.zero);
        pauseGroup = pause.gameObject.AddComponent<CanvasGroup>();
        pauseGroup.alpha = 0f;
        UiKit.Panel(pause, "Shade", new Color(0f, 0f, 0f, 0.7f), Vector2.zero, Vector2.one, Vector2.zero, Vector2.zero);
        UiKit.Text(pause, "Title", Loc.T("PAUSED", "PAUSED"), 96f, Color.white,
            new Vector2(0f, 0.5f), new Vector2(1f, 0.5f), new Vector2(0f, 90f), new Vector2(0f, 120f), TextAlignmentOptions.Center, FontStyles.Bold);
        // タッチ操作中はポーズメニューのボタンを TouchControls が出すので、キー表記の代わりに操作説明だけ出す
        bool touch = TouchControls.Active;
        pauseText = UiKit.Text(pause, "Help",
            touch ? "" :
            Loc.T("<color=#A0C8FF>[Esc]</color> 再開     <color=#A0C8FF>[R]</color> ステージをやり直す     <color=#A0C8FF>[T]</color> タイトルへ",
                  "<color=#A0C8FF>[Esc]</color> Resume     <color=#A0C8FF>[R]</color> Restart stage     <color=#A0C8FF>[T]</color> Title"),
            34f, Color.white, new Vector2(0f, 0.5f), new Vector2(1f, 0.5f), new Vector2(0f, -40f), new Vector2(0f, 60f));
        UiKit.Text(pause, "Controls",
            touch
            ? Loc.T("左半分: 指を置いて動かすと移動   右半分: スワイプで視点   重力ボタン長押し: スロー＆照準（ドラッグで狙う）→ 離して反転   右側をタップ: 字幕を閉じる",
                    "Left half: touch & drag to move   Right half: swipe to look   Hold FLIP: slow-mo & aim (drag to aim) → release to flip   Tap right side: close message")
            : Loc.T("WASD 移動   Shift ダッシュ   右クリック長押し スロー＆照準   離して重力反転   Space 字幕を閉じる",
                    "WASD move   Shift dash   Hold right-click: slow-mo & aim   Release: flip gravity   Space: close message"),
            touch ? 30f : 26f, UiKit.Dim, new Vector2(0f, 0.5f), new Vector2(1f, 0.5f), new Vector2(0f, touch ? 260f : -110f), new Vector2(-200f, 90f));
    }

    private IEnumerator ShowBanner(StageInfo info)
    {
        bannerTitle.text = info.Title;
        bannerSub.text = info.Subtitle;
        float t = 0f;
        while (t < 0.4f) { t += Time.unscaledDeltaTime; bannerGroup.alpha = t / 0.4f; yield return null; }
        bannerGroup.alpha = 1f;
        yield return new WaitForSecondsRealtime(2.0f);
        t = 0f;
        while (t < 0.6f) { t += Time.unscaledDeltaTime; bannerGroup.alpha = 1f - t / 0.6f; yield return null; }
        bannerGroup.alpha = 0f;
        bannerRoutine = null;
    }

    private void Update()
    {
        if (!IsStage(currentScene)) return;

        timerLabel.text = $"TIME <color=#A0C8FF>{UiKit.FormatTime(RunTime)}</color>";
        deathsLabel.text = $"Deaths: <color=#FF7A5C>{runDeaths}</color>";
        abilityLabel.text = (Progress.DashUnlocked ? "<color=#19E6FF>DASH</color>  " : "") + (Progress.QuickGravityUnlocked ? "<color=#FF33E6>4-WAY</color>" : "");

        if (GameInput.PausePressed) SetPaused(!paused);
        if (!paused) return;

        if (GameInput.RestartPressed)
        {
            SetPaused(false);
            var fader = FindAnyObjectByType<SceneFader>();
            if (fader != null) fader.FadeAndReload();
            else SceneManager.LoadScene(currentScene);
        }
        else if (GameInput.ToTitlePressed)
        {
            SetPaused(false);
            SceneManager.LoadScene("Title");
        }
    }

    private void SetPaused(bool value, bool silent = false)
    {
        if (paused == value) return;
        paused = value;
        if (pauseGroup != null) pauseGroup.alpha = value ? 1f : 0f;
        if (value)
        {
            pauseBegan = Time.unscaledTime;
            Time.timeScale = 0f;
            Cursor.lockState = CursorLockMode.None;
            Cursor.visible = true;
            if (!silent) SfxPlayer.Play("sfx_select", 0.6f);
        }
        else
        {
            // ポーズ中の時間はタイムに含めない
            if (!silent) stageStart += Time.unscaledTime - pauseBegan;
            Time.timeScale = 1f;
            Time.fixedDeltaTime = 0.02f;
            if (IsStage(currentScene) && !TouchControls.Active)
            {
                Cursor.lockState = CursorLockMode.Locked;
                Cursor.visible = false;
            }
        }
    }
}
