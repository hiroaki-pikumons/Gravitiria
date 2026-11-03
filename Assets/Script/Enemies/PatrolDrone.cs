using UnityEngine;

/// <summary>決まった軸に沿って往復するドローン。壁にぶつかっても折り返す。ボタンの上に誘導して押させる用途にも使う。</summary>
public class PatrolDrone : Enemy
{
    [Header("巡回")]
    public float speed = 3f;
    public float patrolDistance = 6f;          // 初期位置から片側へ進む距離
    public Vector3 localAxis = Vector3.right;  // 往復する軸（ローカル）

    private Vector3 origin;
    private Vector3 axisWorld;
    private int direction = 1;

    protected override void Start()
    {
        base.Start();
        origin = transform.position;
        axisWorld = transform.TransformDirection(localAxis).normalized;
    }

    protected override void Move()
    {
        Vector3 axis = Planar(axisWorld);
        if (axis.sqrMagnitude < 0.001f) { SetPlanarVelocity(Vector3.zero); return; }
        axis.Normalize();

        float along = Vector3.Dot(transform.position - origin, axis);
        if (along > patrolDistance) direction = -1;
        else if (along < -patrolDistance) direction = 1;

        SetPlanarVelocity(axis * (speed * speedMultiplier * direction));
    }

    protected override void OnHitGeometry(Collision collision)
    {
        Vector3 normal = collision.GetContact(0).normal;
        Vector3 moving = Planar(axisWorld).normalized * direction;
        if (Vector3.Dot(normal, moving) < -0.5f) direction = -direction;
    }
}
