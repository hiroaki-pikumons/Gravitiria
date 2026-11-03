using UnityEngine;

/// <summary>触れると能力を解放するピックアップ。説明文は出さず、音と消滅だけで伝える。</summary>
public class UnlockPickup : MonoBehaviour
{
    public Progress.Ability ability = Progress.Ability.Dash;

    private void OnTriggerEnter(Collider other)
    {
        if (!PlayerUtil.IsPlayer(other)) return;
        Progress.Unlock(ability);
        SfxPlayer.Play("sfx_goal", 0.6f, 1.3f);
        gameObject.SetActive(false);
    }
}
