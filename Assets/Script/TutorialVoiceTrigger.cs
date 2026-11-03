using UnityEngine;

public class TutorialVoiceTrigger : MonoBehaviour
{
    [TextArea(2, 5)]
    [SerializeField] private string message = "その先は足場が崩れている。気をつけて進め。";

    [TextArea(2, 5)]
    [SerializeField] private string messageEn = "The footing ahead has collapsed. Watch your step.";

    [SerializeField] private bool playOnlyOnce = true;
    [SerializeField] private string playerTag = "Player";

    private bool hasPlayed = false;

    private void OnTriggerEnter(Collider other)
    {
        if (!IsPlayer(other)) return;
        if (playOnlyOnce && hasPlayed) return;

        SubtitleManager manager = SubtitleManager.Instance;
        if (manager == null) return;

        manager.ShowSubtitle(Loc.T(message, messageEn));
        hasPlayed = true;
    }

    private bool IsPlayer(Collider other)
    {
        if (PlayerUtil.IsPlayer(other)) return true;
        return !string.IsNullOrEmpty(playerTag) && other.CompareTag(playerTag);
    }
}
