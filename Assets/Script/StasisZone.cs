using System.Collections.Generic;
using UnityEngine;

public class StasisZone : MonoBehaviour
{
    [Header("減速倍率（0.1 = 10%の速度, 0 = 完全停止）")]
    [Range(0f, 1f)]
    public float slowFactor = 0.2f;

    [Header("プレイヤーも減速させるか")]
    public bool affectsPlayer = true;

    private readonly Dictionary<Rigidbody, float> originalDamping = new Dictionary<Rigidbody, float>();

    private void OnTriggerEnter(Collider other)
    {
        Rigidbody body = other.attachedRigidbody;
        if (body == null || originalDamping.ContainsKey(body)) return;

        originalDamping[body] = body.linearDamping;

        if (body.TryGetComponent(out TimeDefyingEnemy enemy))
        {
            enemy.speedMultiplier = slowFactor;
            return;
        }

        if (body.TryGetComponent(out Enemy newEnemy))
        {
            newEnemy.speedMultiplier = slowFactor;
            return;
        }

        if (body.TryGetComponent(out GravityController player))
        {
            if (affectsPlayer) player.speedMultiplier = slowFactor;
            return;
        }

        // 速度を自前で管理していない物体は、速度を落とした上で強い抵抗を掛けて鈍らせる
        body.linearVelocity *= slowFactor;
        body.linearDamping = slowFactor <= 0f ? 100f : (1f - slowFactor) * 10f;
    }

    private void OnTriggerExit(Collider other)
    {
        Rigidbody body = other.attachedRigidbody;
        if (body == null) return;
        Release(body);
    }

    private void OnDisable()
    {
        foreach (var body in new List<Rigidbody>(originalDamping.Keys))
        {
            Release(body);
        }
    }

    private void Release(Rigidbody body)
    {
        if (!originalDamping.TryGetValue(body, out float damping)) return;
        originalDamping.Remove(body);
        if (body == null) return;

        if (body.TryGetComponent(out TimeDefyingEnemy enemy)) enemy.speedMultiplier = 1f;
        else if (body.TryGetComponent(out Enemy newEnemy)) newEnemy.speedMultiplier = 1f;
        else if (body.TryGetComponent(out GravityController player)) player.speedMultiplier = 1f;
        else body.linearDamping = damping;
    }
}
