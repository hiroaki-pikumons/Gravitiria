using System.Collections;
using UnityEngine;
using UnityEngine.SceneManagement;

public class GoalDoor : MonoBehaviour
{
    [Header("遷移先のシーン名 (Build Settings に登録済みのもの)")]
    public string nextSceneName = "Level1";

    [Header("暗転フェードを使用するか")]
    public bool useFade = true;

    [Header("遷移前に表示する字幕（空なら表示しない）")]
    [TextArea(1, 3)]
    public string clearMessage = "";
    [TextArea(1, 3)]
    public string clearMessageEn = "";
    public float clearMessageHold = 2f;

    private bool isGoalReached = false;

    private void OnTriggerEnter(Collider other)
    {
        if (isGoalReached || !PlayerUtil.IsPlayer(other)) return;

        if (!Application.CanStreamedLevelBeLoaded(nextSceneName))
        {
            Debug.LogError($"GoalDoor: シーン '{nextSceneName}' が Build Settings に登録されていません。", this);
            return;
        }

        isGoalReached = true;
        if (GameSession.Instance != null) GameSession.Instance.StageCleared();
        else SfxPlayer.Play("sfx_goal");
        StartCoroutine(GoalRoutine());
    }

    private IEnumerator GoalRoutine()
    {
        if (!string.IsNullOrEmpty(clearMessage) && SubtitleManager.Instance != null)
        {
            SubtitleManager.Instance.ShowSubtitle(Loc.T(clearMessage, clearMessageEn));
            yield return new WaitForSecondsRealtime(clearMessageHold);
        }

        SceneFader fader = useFade ? FindAnyObjectByType<SceneFader>() : null;
        if (fader != null)
        {
            fader.FadeAndLoad(nextSceneName);
        }
        else
        {
            Time.timeScale = 1f;
            Time.fixedDeltaTime = 0.02f;
            SceneManager.LoadScene(nextSceneName);
        }
    }
}
