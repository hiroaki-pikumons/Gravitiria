using UnityEngine;

/// <summary>一定間隔でレーザー弾を撃つ固定砲台。弾はプレイヤーを殺し、壁や他の敵に当たると消える。</summary>
public class LaserTurret : Enemy
{
    [Header("射撃")]
    public float interval = 2f;
    public float firstDelay = 1f;
    public float boltSpeed = 14f;
    public float boltLifetime = 4f;
    public bool aimAtPlayer = false;
    public float aimRange = 30f;
    public Transform muzzle;
    public GameObject boltPrefab;

    private float timer;

    protected override void Awake()
    {
        base.Awake();
        rb.isKinematic = true;
        affectedByGravity = false;
        timer = interval - firstDelay;
    }

    protected override void Move()
    {
        timer += Time.fixedDeltaTime * speedMultiplier;
        if (timer < interval) return;
        timer = 0f;
        Fire();
    }

    private void Fire()
    {
        Transform from = muzzle != null ? muzzle : transform;
        Vector3 dir = from.forward;
        if (aimAtPlayer && player != null && (player.position - from.position).magnitude <= aimRange)
        {
            dir = (player.position - from.position).normalized;
        }

        GameObject bolt;
        if (boltPrefab != null)
        {
            bolt = Instantiate(boltPrefab, from.position + dir * 0.6f, Quaternion.LookRotation(dir));
        }
        else
        {
            bolt = LaserBolt.CreateFallback(from.position + dir * 0.6f, Quaternion.LookRotation(dir));
        }
        bolt.GetComponent<LaserBolt>().Launch(dir * boltSpeed, boltLifetime, this);
        SfxPlayer.Play("sfx_select", 0.35f, 0.6f);
    }
}
