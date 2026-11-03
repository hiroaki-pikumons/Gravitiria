using System.Collections;
using System.Text;
using TMPro;
using UnityEngine;
using UnityEngine.SceneManagement;

public class EndingScreen : MonoBehaviour
{
    private CanvasGroup fade;
    private CanvasGroup content;
    private TextMeshProUGUI prompt;
    private bool leaving;
    private bool ready;

    private void Start()
    {
        Cursor.lockState = CursorLockMode.None;
        Cursor.visible = true;
        Time.timeScale = 1f;

        GameSession.RunSummary summary = GameSession.Instance != null
            ? GameSession.Instance.FinishRun()
            : new GameSession.RunSummary();

        var canvas = UiKit.CreateCanvas("EndingUI", 10, persistent: false);
        var root = canvas.transform;
        UiKit.Panel(root, "Shade", new Color(0f, 0f, 0f, 0.55f), Vector2.zero, Vector2.one, Vector2.zero, Vector2.zero);

        var contentRt = UiKit.Rect(root, "Content", Vector2.zero, Vector2.one, Vector2.zero, Vector2.zero);
        content = contentRt.gameObject.AddComponent<CanvasGroup>();
        content.alpha = 0f;

        UiKit.Text(contentRt, "Title", "ALL STAGES CLEAR", 110f, Color.white,
            new Vector2(0f, 0.5f), new Vector2(1f, 0.5f), new Vector2(0f, 340f), new Vector2(0f, 140f), TextAlignmentOptions.Center, FontStyles.Bold);
        UiKit.Text(contentRt, "Sub", Loc.T("重力はもう、君の味方だ。", "Gravity is on your side now."), 40f, UiKit.Accent,
            new Vector2(0f, 0.5f), new Vector2(1f, 0.5f), new Vector2(0f, 250f), new Vector2(0f, 60f));

        var sb = new StringBuilder();
        if (GameSession.Instance != null)
        {
            foreach (var scene in GameSession.StageOrder)
            {
                if (!GameSession.Instance.Results.TryGetValue(scene, out var r)) continue;
                var info = GameSession.Stages[scene];
                sb.Append($"<align=left>{info.Title,-11} <color=#A0C8FF>{info.Subtitle}</color></align>");
                sb.Append($"<pos=52%>{UiKit.FormatTime(r.Time)}<pos=68%>DEATHS {r.Deaths}<pos=84%>{r.Score:N0}\n");
            }
        }
        UiKit.Text(contentRt, "Table", sb.ToString(), 30f, Color.white,
            new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), new Vector2(0f, 40f), new Vector2(1100f, 330f), TextAlignmentOptions.TopLeft);

        string totals = $"<color=#A0C8FF>TOTAL</color>  {UiKit.FormatTime(summary.TotalTime)}     " +
                        $"<color=#A0C8FF>DEATHS</color>  {summary.TotalDeaths}     " +
                        $"<color=#A0C8FF>SCORE</color>  <b>{summary.Score:N0}</b>";
        if (summary.NewBestScore) totals += "   <color=#FFD766>NEW BEST SCORE!</color>";
        else if (summary.NewBestTime) totals += "   <color=#FFD766>NEW BEST TIME!</color>";
        UiKit.Text(contentRt, "Totals", totals, 36f, Color.white,
            new Vector2(0f, 0.5f), new Vector2(1f, 0.5f), new Vector2(0f, -170f), new Vector2(0f, 60f));
        UiKit.Text(contentRt, "Best", $"BEST SCORE {summary.BestScore:N0}    BEST TIME {UiKit.FormatTime(summary.BestTime)}", 26f, UiKit.Dim,
            new Vector2(0f, 0.5f), new Vector2(1f, 0.5f), new Vector2(0f, -220f), new Vector2(0f, 40f));

        prompt = UiKit.Text(contentRt, "Prompt",
            TouchControls.Active ? Loc.T("タップでタイトルへ", "Tap to return to the title") : Loc.T("ENTER でタイトルへ", "Press ENTER to return to the title"),
            36f, Color.white,
            new Vector2(0f, 0f), new Vector2(1f, 0f), new Vector2(0f, 70f), new Vector2(0f, 60f));

        var credit = UiKit.Text(root, "Credit", "Original game by Hiroaki  /  Thanks for playing!", 20f, UiKit.Dim,
            new Vector2(1f, 0f), new Vector2(1f, 0f), new Vector2(-20f, 14f), new Vector2(900f, 30f), TextAlignmentOptions.Right);
        credit.rectTransform.pivot = new Vector2(1f, 0f);

        var fadeImg = UiKit.Panel(root, "Fade", Color.black, Vector2.zero, Vector2.one, Vector2.zero, Vector2.zero);
        fade = fadeImg.gameObject.AddComponent<CanvasGroup>();
        fade.alpha = 1f;
        fade.blocksRaycasts = false;

        StartCoroutine(Intro());
    }

    private IEnumerator Intro()
    {
        yield return Fade(fade, 1f, 0f, 1.0f);
        SfxPlayer.Play("sfx_fanfare");
        yield return Fade(content, 0f, 1f, 1.2f);
        ready = true;
    }

    private void Update()
    {
        if (!ready || leaving) return;
        prompt.alpha = 0.6f + 0.4f * Mathf.Sin(Time.unscaledTime * 3f);
        if (GameInput.SubmitPressed || GameInput.CancelPressed)
        {
            Continue();
        }
    }

    public void Continue()
    {
        if (leaving) return;
        leaving = true;
        SfxPlayer.Play("sfx_select");
        StartCoroutine(Leave());
    }

    private IEnumerator Leave()
    {
        yield return Fade(fade, 0f, 1f, 0.6f);
        SceneManager.LoadScene("Title");
    }

    private static IEnumerator Fade(CanvasGroup g, float from, float to, float duration)
    {
        float t = 0f;
        while (t < duration)
        {
            t += Time.unscaledDeltaTime;
            g.alpha = Mathf.Lerp(from, to, t / duration);
            yield return null;
        }
        g.alpha = to;
    }
}
