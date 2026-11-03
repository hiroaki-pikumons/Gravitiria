using UnityEngine;

[RequireComponent(typeof(Rigidbody))]
public class TimeDefyingEnemy : MonoBehaviour
{
    public float normalSpeed = 3f;    // 通常時のスピード
    public float fastSpeed = 20f;     // スローモーション時のスピード（爆速！）

    // StasisZone などが外から書き換える速度倍率
    [HideInInspector] public float speedMultiplier = 1f;

    private Rigidbody rb;
    private Vector3 moveDirection = Vector3.right; // 最初に進む方向（右）

    void Awake()
    {
        rb = GetComponent<Rigidbody>();
        rb.useGravity = false;
        rb.freezeRotation = true;
    }

    void FixedUpdate()
    {
        bool isWorldSlowed = Time.timeScale < 1.0f;
        float currentSpeed = (isWorldSlowed ? fastSpeed : normalSpeed) * speedMultiplier;

        // 世界がスロー（0.1倍）の時は、見た目の速度を保つために timeScale の逆数を掛ける
        if (isWorldSlowed)
        {
            currentSpeed /= Mathf.Max(Time.timeScale, 0.01f);
        }

        rb.linearVelocity = moveDirection * currentSpeed;
    }

    private void OnCollisionEnter(Collision collision)
    {
        if (collision.contactCount == 0) return;

        // ぶつかった壁の向き（法線）を使って、進行方向を反転（跳ね返り）させる
        Vector3 reflected = Vector3.Reflect(moveDirection, collision.GetContact(0).normal);

        // 真上や真下にずれないように、高さを水平（Y軸＝0）にリセットする
        reflected.y = 0f;
        if (reflected.sqrMagnitude < 0.0001f) reflected = -moveDirection;
        moveDirection = reflected.normalized;
    }
}
