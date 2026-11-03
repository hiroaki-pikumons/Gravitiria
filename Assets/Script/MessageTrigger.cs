using UnityEngine;
using TMPro;

public class MessageTrigger : MonoBehaviour
{
    public GameObject textObject;
    public float fadeSpeed = 5f;

    private bool isPlayerInside = false;
    private CanvasGroup canvasGroup;
    private TMP_Text tmpText;

    void Start()
    {
        if (textObject != null)
        {
            tmpText = textObject.GetComponent<TMP_Text>();

            // UI用（Canvas内）だった場合のためにCanvasGroupもケアしておく
            canvasGroup = textObject.GetComponent<CanvasGroup>();
            if (canvasGroup == null && textObject.GetComponent<RectTransform>() != null)
            {
                canvasGroup = textObject.AddComponent<CanvasGroup>();
            }
        }
        else
        {
            Debug.LogWarning($"MessageTrigger '{name}': textObject が未設定のため何も表示されません。", this);
        }

        SetAlpha(0f);
    }

    void Update()
    {
        if (canvasGroup == null && tmpText == null) return;

        float targetAlpha = isPlayerInside ? 1f : 0f;
        float newAlpha = Mathf.Lerp(GetAlpha(), targetAlpha, Time.unscaledDeltaTime * fadeSpeed);
        SetAlpha(newAlpha);
    }

    float GetAlpha()
    {
        if (canvasGroup != null) return canvasGroup.alpha;
        if (tmpText != null) return tmpText.color.a;
        return 0f;
    }

    void SetAlpha(float alpha)
    {
        if (canvasGroup != null)
        {
            canvasGroup.alpha = alpha;
        }
        else if (tmpText != null)
        {
            Color c = tmpText.color;
            c.a = alpha;
            tmpText.color = c;
        }
    }

    private void OnTriggerEnter(Collider other)
    {
        if (PlayerUtil.IsPlayer(other)) isPlayerInside = true;
    }

    private void OnTriggerExit(Collider other)
    {
        if (PlayerUtil.IsPlayer(other)) isPlayerInside = false;
    }
}
