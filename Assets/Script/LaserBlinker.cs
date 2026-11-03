using UnityEngine;

/// <summary>レーザーを周期的に点滅させる。消えている間は当たり判定も見た目も消える。スロー中は点滅も遅くなる。</summary>
public class LaserBlinker : MonoBehaviour
{
    public float period = 2f;
    [Range(0.05f, 0.95f)] public float onFraction = 0.6f;
    [Range(0f, 1f)] public float phase = 0f;

    private Collider[] colliders;
    private Renderer[] renderers;
    private bool isOn = true;

    private void Awake()
    {
        colliders = GetComponentsInChildren<Collider>(true);
        renderers = GetComponentsInChildren<Renderer>(true);
    }

    private void Update()
    {
        bool shouldBeOn = Mathf.Repeat(Time.time / Mathf.Max(period, 0.1f) + phase, 1f) < onFraction;
        if (shouldBeOn == isOn) return;
        isOn = shouldBeOn;
        foreach (var c in colliders) c.enabled = isOn;
        foreach (var r in renderers) r.enabled = isOn;
    }
}
