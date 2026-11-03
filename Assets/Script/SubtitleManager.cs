using System.Collections;
using UnityEngine;
using UnityEngine.SceneManagement;
using TMPro;

public class SubtitleManager : MonoBehaviour
{
    private const string ResourcePath = "SubtitleUI";

    private static SubtitleManager instance;

    /// <summary>
    /// シーンに無ければ Resources/SubtitleUI プレハブから自動生成する。
    /// </summary>
    public static SubtitleManager Instance
    {
        get
        {
            if (instance == null)
            {
                instance = FindAnyObjectByType<SubtitleManager>();
                if (instance == null)
                {
                    var prefab = Resources.Load<SubtitleManager>(ResourcePath);
                    if (prefab != null) instance = Instantiate(prefab);
                    else Debug.LogWarning("SubtitleManager: Resources/SubtitleUI が見つかりません。");
                }
            }
            return instance;
        }
    }

    [Header("UI参照")]
    [SerializeField] private CanvasGroup subtitlePanel; // SubtitlePanelのCanvasGroup
    [SerializeField] private TMP_Text subtitleText;      // SubtitleText (TextMeshProUGUI)

    public static string CloseHint => TouchControls.Active
        ? Loc.T("画面の右側をタップで閉じる / Tap the right side", "Tap the right side to close / 右側をタップ")
        : Loc.T("Space キーで閉じる / Press SPACE", "Press SPACE to close / Space キーで閉じる");

    [Header("演出設定")]
    [SerializeField] private float charsPerSecond = 30f;   // 1秒あたりに表示する文字数
    [SerializeField] private float fadeDuration = 0.3f;    // フェードイン/アウトの時間

    private static bool hintShownOnce = false;   // 最初の文章の一行目にだけ操作ヒントを付ける
    private bool dismissRequested = false;
    private TMPro.TextMeshProUGUI hintLabel;

    [Header("演出用オーディオ(任意)")]
    [SerializeField] private AudioSource voiceAudioSource; // ノイズ/無線音などを鳴らすAudioSource
    [SerializeField] private AudioClip staticNoiseClip;    // 声が流れる間に鳴らすノイズ音

    private Coroutine currentRoutine;

    private void Awake()
    {
        if (instance != null && instance != this)
        {
            Destroy(gameObject);
            return;
        }
        instance = this;
        DontDestroyOnLoad(gameObject);

        if (subtitlePanel == null || subtitleText == null)
        {
            Debug.LogError("SubtitleManager: subtitlePanel / subtitleText が未設定です。", this);
            enabled = false;
            return;
        }

        subtitlePanel.alpha = 0f;
        subtitlePanel.blocksRaycasts = false;
        subtitleText.text = string.Empty;

        hintLabel = UiKit.Text(subtitlePanel.transform, "CloseHint",
            TouchControls.Active ? Loc.T("右側をタップで閉じる", "Tap right side to close") : Loc.T("[Space] 閉じる", "[Space] close"),
            20f * UiKit.HudScale, UiKit.Dim,
            new Vector2(1f, 0f), new Vector2(1f, 0f), new Vector2(-16f, 8f), new Vector2(400f, 26f * UiKit.HudScale), TMPro.TextAlignmentOptions.Right);
        hintLabel.rectTransform.pivot = new Vector2(1f, 0f);
        hintLabel.gameObject.SetActive(false);
        // 小さい画面では字幕も大きく
        subtitleText.fontSize *= UiKit.HudScale;

        if (staticNoiseClip == null) staticNoiseClip = Resources.Load<AudioClip>("Audio/sfx_static");
        if (voiceAudioSource == null && staticNoiseClip != null)
        {
            voiceAudioSource = gameObject.AddComponent<AudioSource>();
            voiceAudioSource.spatialBlend = 0f;
            voiceAudioSource.volume = 0.35f;
            voiceAudioSource.playOnAwake = false;
            voiceAudioSource.ignoreListenerPause = true;
        }

        SceneManager.sceneLoaded += OnSceneLoaded;
    }

    private void OnDestroy()
    {
        SceneManager.sceneLoaded -= OnSceneLoaded;
        if (instance == this) instance = null;
    }

    private void OnSceneLoaded(Scene scene, LoadSceneMode mode)
    {
        Hide();
    }

    /// <summary>
    /// 字幕を表示する。トリガーからはこれを呼ぶだけでOK。
    /// </summary>
    public void ShowSubtitle(string message)
    {
        if (!enabled) return;
        if (currentRoutine != null) StopCoroutine(currentRoutine);
        currentRoutine = StartCoroutine(SubtitleRoutine(message));
    }

    public void Hide()
    {
        if (currentRoutine != null)
        {
            StopCoroutine(currentRoutine);
            currentRoutine = null;
        }
        if (voiceAudioSource != null) voiceAudioSource.Stop();
        if (subtitlePanel != null) subtitlePanel.alpha = 0f;
        if (subtitleText != null) subtitleText.text = string.Empty;
        if (hintLabel != null) hintLabel.gameObject.SetActive(false);
        dismissRequested = false;
    }

    /// <summary>表示中の文章を閉じる（Space キーと同じ）。</summary>
    public void Dismiss()
    {
        dismissRequested = true;
    }

    private void Update()
    {
        if (currentRoutine != null && GameInput.CloseMessagePressed) dismissRequested = true;
    }

    private IEnumerator SubtitleRoutine(string message)
    {
        // ノイズ音を鳴らす(通信越しの声っぽさを演出)
        if (voiceAudioSource != null && staticNoiseClip != null)
        {
            voiceAudioSource.clip = staticNoiseClip;
            voiceAudioSource.loop = true;
            voiceAudioSource.Play();
        }

        // 最初に表示する文章の一行目に閉じ方を書く
        if (!hintShownOnce)
        {
            hintShownOnce = true;
            message = CloseHint + "\n" + message;
        }
        dismissRequested = false;

        // 文字列は先に全部入れておき、表示文字数だけを増やす（折り返しがガタつかない）
        subtitleText.text = message;
        subtitleText.maxVisibleCharacters = 0;
        subtitleText.ForceMeshUpdate();
        int totalChars = subtitleText.textInfo.characterCount;

        yield return FadeCanvasGroup(subtitlePanel, subtitlePanel.alpha, 1f, fadeDuration);

        // タイプライター表示（スロー中でも実時間で進める）
        float delayPerChar = 1f / Mathf.Max(charsPerSecond, 1f);
        for (int i = 1; i <= totalChars && !dismissRequested; i++)
        {
            subtitleText.maxVisibleCharacters = i;
            yield return new WaitForSecondsRealtime(delayPerChar);
        }
        subtitleText.maxVisibleCharacters = totalChars;
        if (voiceAudioSource != null) voiceAudioSource.Stop();

        // Space が押されるまで表示したままにする（ゲームの進行は止めない）
        dismissRequested = false;
        hintLabel.gameObject.SetActive(true);
        while (!dismissRequested) yield return null;
        hintLabel.gameObject.SetActive(false);

        yield return FadeCanvasGroup(subtitlePanel, 1f, 0f, fadeDuration);
        subtitleText.text = string.Empty;

        currentRoutine = null;
    }

    private IEnumerator FadeCanvasGroup(CanvasGroup group, float from, float to, float duration)
    {
        float elapsed = 0f;
        while (elapsed < duration)
        {
            elapsed += Time.unscaledDeltaTime;
            group.alpha = Mathf.Lerp(from, to, elapsed / duration);
            yield return null;
        }
        group.alpha = to;
    }
}
