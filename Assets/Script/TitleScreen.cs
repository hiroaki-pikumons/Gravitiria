using System.Collections;
using System.Text;
using TMPro;
using UnityEngine;
using UnityEngine.SceneManagement;

public class TitleScreen : MonoBehaviour
{
    public string firstScene = "Tutorial";

    private CanvasGroup fade;
    private TextMeshProUGUI prompt, records, resetHint;
    private bool starting;
    private float deleteHeld;

    private string ResetHintText => TouchControls.Active
        ? Loc.T("右上のボタンで言語切替", "Language: button at the top right")
        : Loc.T("Delete 長押しで記録をリセット    L 言語切替 (English)    Esc で終了",
                "Hold Delete to reset records    L: language (日本語)    Esc: quit");

    private void Start()
    {
        Cursor.lockState = CursorLockMode.None;
        Cursor.visible = true;
        Time.timeScale = 1f;

        var canvas = UiKit.CreateCanvas("TitleUI", 10, persistent: false);
        var root = canvas.transform;

        UiKit.Panel(root, "TopShade", new Color(0f, 0f, 0f, 0.35f), new Vector2(0f, 0.55f), new Vector2(1f, 1f), Vector2.zero, Vector2.zero);
        UiKit.Text(root, "Logo", "GRAVITIRIA", 150f, Color.white,
            new Vector2(0f, 0.5f), new Vector2(1f, 0.5f), new Vector2(0f, 270f), new Vector2(0f, 200f), TextAlignmentOptions.Center, FontStyles.Bold);
        UiKit.Text(root, "Tagline", Loc.T("重力を操り、裏側へ。", "Bend gravity. Reach the other side."), 44f, UiKit.Accent,
            new Vector2(0f, 0.5f), new Vector2(1f, 0.5f), new Vector2(0f, 150f), new Vector2(0f, 60f));

        bool touch = TouchControls.Active;
        prompt = UiKit.Text(root, "Prompt",
            touch ? Loc.T("画面をタップしてスタート", "Tap to start") : Loc.T("ENTER / クリック でスタート", "Press ENTER / Click to start"),
            40f, Color.white, new Vector2(0f, 0.5f), new Vector2(1f, 0.5f), new Vector2(0f, -40f), new Vector2(0f, 60f));

        UiKit.Text(root, "Controls",
            touch
            ? Loc.T("左半分で移動    右半分をスワイプで視点    重力ボタン長押し スロー＆照準 → 離して反転    右側をタップで字幕を閉じる",
                    "Left half: move    Swipe right half: look    Hold FLIP to slow time & aim → release to flip    Tap right side to close messages")
            : Loc.T("WASD 移動    Shift ダッシュ    右クリック長押し スロー＆照準 → 離して重力反転    Space 字幕を閉じる    Esc ポーズ",
                    "WASD move    Shift dash    Hold right-click to slow time & aim → release to flip gravity    Space closes messages    Esc pause"),
            26f, UiKit.Dim, new Vector2(0f, 0.5f), new Vector2(1f, 0.5f), new Vector2(0f, -110f), new Vector2(-200f, 50f));

        UiKit.Panel(root, "RecordShade", new Color(0f, 0f, 0f, 0.5f), new Vector2(0.5f, 0f), new Vector2(0.5f, 0f), new Vector2(0f, 170f), new Vector2(900f, 190f));
        records = UiKit.Text(root, "Records", "", 28f, Color.white,
            new Vector2(0.5f, 0f), new Vector2(0.5f, 0f), new Vector2(0f, 170f), new Vector2(860f, 170f), TextAlignmentOptions.Center);
        resetHint = UiKit.Text(root, "ResetHint", ResetHintText, 22f, UiKit.Dim,
            new Vector2(0.5f, 0f), new Vector2(0.5f, 0f), new Vector2(0f, 40f), new Vector2(1100f, 40f));

        var credit = UiKit.Text(root, "Credit", "Original game by Hiroaki  /  BGM & SFX: procedural (in-house)", 20f, UiKit.Dim,
            new Vector2(1f, 0f), new Vector2(1f, 0f), new Vector2(-20f, 14f), new Vector2(900f, 30f), TextAlignmentOptions.Right);
        credit.rectTransform.pivot = new Vector2(1f, 0f);

        var fadeImg = UiKit.Panel(root, "Fade", Color.black, Vector2.zero, Vector2.one, Vector2.zero, Vector2.zero);
        fade = fadeImg.gameObject.AddComponent<CanvasGroup>();
        fade.alpha = 1f;
        fade.blocksRaycasts = false;
        StartCoroutine(FadeTo(0f, 0.8f));

        RefreshRecords();
    }

    private void RefreshRecords()
    {
        int bestScore = PlayerPrefs.GetInt(GameSession.KeyBestScore, 0);
        float bestTime = PlayerPrefs.GetFloat(GameSession.KeyBestTime, 0f);
        int runs = PlayerPrefs.GetInt(GameSession.KeyRuns, 0);

        var sb = new StringBuilder();
        if (runs == 0)
        {
            sb.Append(Loc.T("<color=#A0C8FF>記録なし</color>\n全ステージをクリアするとスコアとタイムが記録される",
                            "<color=#A0C8FF>No records yet</color>\nClear every stage to record your score and time"));
        }
        else
        {
            sb.Append($"<color=#A0C8FF>BEST SCORE</color>  <b>{bestScore:N0}</b>      ");
            sb.Append($"<color=#A0C8FF>BEST TIME</color>  <b>{UiKit.FormatTime(bestTime)}</b>      ");
            sb.Append($"<color=#A0C8FF>CLEARS</color>  <b>{runs}</b>\n");
            bool first = true;
            foreach (var scene in GameSession.StageOrder)
            {
                string key = GameSession.KeyStageTime(scene);
                if (!PlayerPrefs.HasKey(key)) continue;
                var info = GameSession.Stages[scene];
                sb.Append(first ? "" : "   ").Append($"<size=24>{info.Title} {UiKit.FormatTime(PlayerPrefs.GetFloat(key))}</size>");
                first = false;
            }
        }
        records.text = sb.ToString();
    }

    private void Update()
    {
        if (starting) return;

        prompt.alpha = 0.6f + 0.4f * Mathf.Sin(Time.unscaledTime * 3f);

        if (GameInput.SubmitPressed)
        {
            StartGame();
        }
        else if (GameInput.CancelPressed)
        {
            Application.Quit();
        }
        else if (GameInput.ToggleLanguagePressed)
        {
            // 言語を切り替えて画面を作り直す
            Loc.Toggle();
            SfxPlayer.Play("sfx_select", 0.7f);
            SceneManager.LoadScene(SceneManager.GetActiveScene().name);
            return;
        }

        if (GameInput.ResetRecordsHeld)
        {
            deleteHeld += Time.unscaledDeltaTime;
            resetHint.text = Loc.T($"記録をリセット中… {Mathf.Clamp01(deleteHeld / 1.5f) * 100f:0}%",
                                   $"Resetting records… {Mathf.Clamp01(deleteHeld / 1.5f) * 100f:0}%");
            if (deleteHeld >= 1.5f)
            {
                GameSession.ResetRecords();
                RefreshRecords();
                deleteHeld = -999f;
                resetHint.text = Loc.T("記録をリセットした", "Records reset");
                SfxPlayer.Play("sfx_select");
            }
        }
        else if (deleteHeld > 0f)
        {
            deleteHeld = 0f;
            resetHint.text = ResetHintText;
        }
    }

    public void StartGame()
    {
        if (starting) return;
        starting = true;
        SfxPlayer.Play("sfx_select");
        StartCoroutine(StartRoutine());
    }

    private IEnumerator StartRoutine()
    {
        yield return FadeTo(1f, 0.6f);
        SceneManager.LoadScene(firstScene);
    }

    private IEnumerator FadeTo(float target, float duration)
    {
        float from = fade.alpha, t = 0f;
        while (t < duration)
        {
            t += Time.unscaledDeltaTime;
            fade.alpha = Mathf.Lerp(from, target, t / duration);
            yield return null;
        }
        fade.alpha = target;
    }
}
