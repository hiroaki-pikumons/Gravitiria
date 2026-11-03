using UnityEngine;

/// <summary>プレイヤーが近づくと面に沿って寄ってくる。レーザーの前に誘い込んで消す、が想定解。</summary>
public class Magnet : Enemy
{
    [Header("追尾")]
    public float speed = 2.5f;
    public float range = 14f;
    public float stopDistance = 0.9f;

    protected override void Move()
    {
        if (player == null) { SetPlanarVelocity(Vector3.zero); return; }

        Vector3 to = Planar(player.position - transform.position);
        float distance = to.magnitude;
        if (distance > range || distance < stopDistance)
        {
            SetPlanarVelocity(Vector3.zero);
            return;
        }
        SetPlanarVelocity(to.normalized * (speed * speedMultiplier));
    }
}
