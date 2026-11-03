using UnityEngine;

/// <summary>
/// プレイヤーが触れると、登録されたレーザーを全て非アクティブにするスイッチ。
/// シーン再読み込み（死亡・やり直し）で自動的に元に戻る。
/// </summary>
public class LaserSwitch : MonoBehaviour
{
    [Header("解除するレーザー（SecurityLaser プレハブのインスタンス）")]
    public GameObject[] lasers;

    [Header("解除時の字幕（空なら表示しない）")]
    [TextArea(1, 3)]
    public string message = "天井のレーザーを解除した。";
    [TextArea(1, 3)]
    public string messageEn = "Ceiling lasers disabled.";

    public Color activatedColor = new Color(0.15f, 0.2f, 0.25f, 1f);

    [Header("色（対応するレーザーと同じ色）")]
    public Color groupColor = new Color(1f, 0.12f, 0.05f);

    private bool used = false;

    private void Start()
    {
        var renderer = GetComponent<Renderer>();
        if (renderer == null) return;
        var block = new MaterialPropertyBlock();
        renderer.GetPropertyBlock(block);
        block.SetColor("_Color", groupColor * 0.5f);
        block.SetColor("_EmissionColor", groupColor * 3f);
        renderer.SetPropertyBlock(block);
    }

    private void OnTriggerEnter(Collider other)
    {
        if (used) return;
        if (!PlayerUtil.IsPlayer(other) && other.GetComponentInParent<Enemy>() == null) return;
        used = true;

        foreach (var laser in lasers)
        {
            if (laser != null) laser.SetActive(false);
        }

        var renderer = GetComponent<Renderer>();
        if (renderer != null)
        {
            var block = new MaterialPropertyBlock();
            renderer.GetPropertyBlock(block);
            block.SetColor("_Color", activatedColor);
            block.SetColor("_EmissionColor", Color.black);
            renderer.SetPropertyBlock(block);
        }

        SfxPlayer.Play("sfx_select");
        if (!string.IsNullOrEmpty(message) && SubtitleManager.Instance != null)
        {
            SubtitleManager.Instance.ShowSubtitle(Loc.T(message, messageEn));
        }
    }
}
