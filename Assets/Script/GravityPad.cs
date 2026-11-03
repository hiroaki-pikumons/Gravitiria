using UnityEngine;

public class GravityPad : MonoBehaviour
{
    [Header("吹き飛ばす力")]
    public float launchForce = 12f; // 上にドーンと弾き飛ばす強さ

    private void OnTriggerEnter(Collider other)
    {
        if (!PlayerUtil.IsPlayer(other)) return;

        Rigidbody rb = other.attachedRigidbody;
        if (rb == null) return;
        GravityController controller = rb.GetComponent<GravityController>();
        if (controller == null) return;

        // 1. 重力方向をパネルの「上面の向く方向の逆」に変える
        controller.ChangeGravityDirectly(-transform.up);

        // 2. 今までの地面に向かう移動速度を一旦リセットする
        rb.linearVelocity = Vector3.zero;

        // 3. パネルの上方向に向けて一気に弾き飛ばす
        rb.AddForce(transform.up * launchForce, ForceMode.VelocityChange);
        SfxPlayer.Play("sfx_jump", 0.8f);
    }
}
