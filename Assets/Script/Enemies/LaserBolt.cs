using UnityEngine;

/// <summary>砲台が撃つレーザー弾。直進し、プレイヤーに当たれば殺し、壁や敵に当たれば消える。</summary>
[RequireComponent(typeof(Rigidbody))]
public class LaserBolt : MonoBehaviour
{
    private Rigidbody rb;
    private LaserTurret owner;
    private float life;

    private void Awake()
    {
        rb = GetComponent<Rigidbody>();
        rb.useGravity = false;
        rb.isKinematic = false;
        rb.collisionDetectionMode = CollisionDetectionMode.ContinuousDynamic;
        foreach (var c in GetComponentsInChildren<Collider>()) c.isTrigger = true;
    }

    public void Launch(Vector3 velocity, float lifetime, LaserTurret from)
    {
        owner = from;
        life = lifetime;
        rb.linearVelocity = velocity;
    }

    private void Update()
    {
        life -= Time.deltaTime;
        if (life <= 0f) Destroy(gameObject);
    }

    private void OnTriggerEnter(Collider other)
    {
        if (other.isTrigger && other.GetComponentInParent<SecurityLaser>() == null && other.GetComponent<LaserBolt>() == null)
        {
            return; // メッセージ領域などの触れないトリガーは素通り
        }
        if (owner != null && other.transform.IsChildOf(owner.transform)) return;

        if (PlayerUtil.IsPlayer(other))
        {
            var body = other.attachedRigidbody;
            var controller = body != null ? body.GetComponent<GravityController>() : null;
            if (controller != null && controller.IsInvincible) return;
            if (controller != null) controller.ResetStage();
        }
        else
        {
            var enemy = other.GetComponentInParent<Enemy>();
            if (enemy != null && !(enemy is LaserTurret)) enemy.Die();
        }
        Destroy(gameObject);
    }

    /// <summary>プレハブが無い場合の簡易弾（コアのレーザー素材を使う）。</summary>
    public static GameObject CreateFallback(Vector3 position, Quaternion rotation)
    {
        var go = GameObject.CreatePrimitive(PrimitiveType.Capsule);
        go.name = "LaserBolt";
        go.transform.SetPositionAndRotation(position, rotation * Quaternion.Euler(90f, 0f, 0f));
        go.transform.localScale = new Vector3(0.25f, 0.45f, 0.25f);
        var mat = Resources.Load<Material>("Enemies/LaserBolt_Mat");
        if (mat != null) go.GetComponent<Renderer>().sharedMaterial = mat;
        go.AddComponent<Rigidbody>();
        go.AddComponent<LaserBolt>();
        return go;
    }
}
