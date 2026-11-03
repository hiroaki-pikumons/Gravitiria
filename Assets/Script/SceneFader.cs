using System.Collections;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.SceneManagement;

public class SceneFader : MonoBehaviour
{
    public Image fadeImage;
    public float fadeDuration = 0.5f; // 暗くなるまでの時間（秒）

    private bool isFading = false;

    void Start()
    {
        if (fadeImage != null)
        {
            fadeImage.raycastTarget = false;
            StartCoroutine(Fade(1f, 0f));
        }
    }

    public void FadeAndReload()
    {
        FadeAndLoad(SceneManager.GetActiveScene().name);
    }

    public void FadeAndLoad(string sceneName)
    {
        if (isFading) return;
        if (!Application.CanStreamedLevelBeLoaded(sceneName))
        {
            Debug.LogError($"シーン '{sceneName}' が Build Settings に登録されていません。");
            return;
        }
        StartCoroutine(FadeOutAndLoad(sceneName));
    }

    private IEnumerator FadeOutAndLoad(string sceneName)
    {
        isFading = true;
        yield return Fade(0f, 1f);

        // スロー中に遷移しても次のシーンが止まらないように戻す
        Time.timeScale = 1f;
        Time.fixedDeltaTime = 0.02f;
        SceneManager.LoadScene(sceneName);
    }

    private IEnumerator Fade(float from, float to)
    {
        if (fadeImage == null) yield break;

        float timer = 0f;
        Color c = fadeImage.color;
        c.a = from;
        fadeImage.color = c;

        while (timer < fadeDuration)
        {
            timer += Time.unscaledDeltaTime;
            c.a = Mathf.Lerp(from, to, Mathf.Clamp01(timer / fadeDuration));
            fadeImage.color = c;
            yield return null;
        }
        c.a = to;
        fadeImage.color = c;
    }
}
