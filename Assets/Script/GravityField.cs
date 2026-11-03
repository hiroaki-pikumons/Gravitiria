using UnityEngine;

/// <summary>現在の重力方向（プレイヤーの重力）。敵や物体はこれを読んで落ちる。</summary>
public static class GravityField
{
    public static Vector3 Direction { get; private set; } = Vector3.down;

    public static void Set(Vector3 direction)
    {
        if (direction.sqrMagnitude > 0.0001f) Direction = direction.normalized;
    }
}
