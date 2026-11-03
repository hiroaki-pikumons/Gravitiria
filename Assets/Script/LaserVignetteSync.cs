using UnityEngine;
using UnityEngine.UI;

public class LaserVignetteSync : MonoBehaviour
{
    public Image vignetteImage;
    public float flickerSpeed = 10f;
    public Color dangerColor = new Color(0.6f, 0f, 0f, 1f);

    private static int dangerZoneCount = 0;

    // GravityController はこれが true の間、ビネットの透明度を触らない
    public static bool PlayerInDanger => dangerZoneCount > 0;

    private Color originalColor;
    private bool isPlayerInside = false;

    void Start()
    {
        if (vignetteImage == null)
        {
            GameObject vig = GameObject.Find("Vignette");
            if (vig != null) vignetteImage = vig.GetComponent<Image>();
        }

        if (vignetteImage != null)
        {
            originalColor = vignetteImage.color;
        }
    }

    private void OnTriggerEnter(Collider other)
    {
        if (isPlayerInside || !PlayerUtil.IsPlayer(other)) return;
        isPlayerInside = true;
        dangerZoneCount++;
    }

    private void OnTriggerExit(Collider other)
    {
        if (!isPlayerInside || !PlayerUtil.IsPlayer(other)) return;
        Leave();
    }

    private void OnDisable()
    {
        if (isPlayerInside) Leave();
    }

    private void Leave()
    {
        isPlayerInside = false;
        dangerZoneCount = Mathf.Max(0, dangerZoneCount - 1);
        if (vignetteImage != null && !PlayerInDanger)
        {
            Color c = originalColor;
            c.a = 0f;
            vignetteImage.color = c;
        }
    }

    void Update()
    {
        if (!isPlayerInside || vignetteImage == null) return;

        Color c = dangerColor;
        c.a = 0.25f + Mathf.PingPong(Time.unscaledTime * flickerSpeed, 0.35f);
        vignetteImage.color = c;
    }
}
