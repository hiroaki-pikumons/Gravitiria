using UnityEngine;

/// <summary>
/// 敵の共通部分：現在の重力方向（GravityField）に従って落ち、レーザーに触れると消え、プレイヤーに触れると殺す。
/// 死亡によるやり直しはシーン再読み込みなので、敵も自動的に初期配置へ戻る。
/// </summary>
[RequireComponent(typeof(Rigidbody))]
public abstract class Enemy : MonoBehaviour
{
    [Header("共通")]
    public float gravityForce = 9.81f;
    public bool affectedByGravity = true;
    public bool lethalToPlayer = true;
    public bool dieOnLaser = true;

    // StasisZone などが外から書き換える速度倍率
    [HideInInspector] public float speedMultiplier = 1f;

    protected Rigidbody rb;
    protected Transform player;
    protected bool dead;

    protected virtual void Awake()
    {
        rb = GetComponent<Rigidbody>();
        rb.useGravity = false;
        rb.freezeRotation = true;
        rb.interpolation = RigidbodyInterpolation.Interpolate;
    }

    protected virtual void Start()
    {
        var controller = FindAnyObjectByType<GravityController>();
        if (controller != null) player = controller.transform;
    }

    protected virtual void FixedUpdate()
    {
        if (dead) return;
        if (affectedByGravity && !rb.isKinematic)
        {
            rb.AddForce(GravityField.Direction * gravityForce, ForceMode.Acceleration);
        }
        Move();
    }

    protected abstract void Move();

    protected Vector3 Planar(Vector3 v) => Vector3.ProjectOnPlane(v, GravityField.Direction);

    /// <summary>重力方向の速度成分は保ったまま、面に沿った速度を設定する。</summary>
    protected void SetPlanarVelocity(Vector3 planar)
    {
        Vector3 along = Vector3.Project(rb.linearVelocity, GravityField.Direction);
        rb.linearVelocity = Planar(planar) + along;
    }

    private void OnTriggerEnter(Collider other) => HandleContact(other);

    private void OnCollisionEnter(Collision collision)
    {
        HandleContact(collision.collider);
        if (!dead && collision.contactCount > 0) OnHitGeometry(collision);
    }

    protected virtual void HandleContact(Collider other)
    {
        if (dead) return;
        if (dieOnLaser && (other.GetComponentInParent<SecurityLaser>() != null || other.GetComponent<LaserBolt>() != null))
        {
            Die();
            return;
        }
        if (lethalToPlayer && PlayerUtil.IsPlayer(other))
        {
            KillPlayer(other);
        }
    }

    protected virtual void OnHitGeometry(Collision collision) { }

    protected static void KillPlayer(Collider other)
    {
        var body = other.attachedRigidbody;
        var controller = body != null ? body.GetComponent<GravityController>() : other.GetComponent<GravityController>();
        if (controller != null && !controller.IsInvincible) controller.ResetStage();
    }

    public virtual void Die()
    {
        if (dead) return;
        dead = true;
        SfxPlayer.Play("sfx_death", 0.5f, 1.5f);
        gameObject.SetActive(false);
    }
}
