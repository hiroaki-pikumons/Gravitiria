using UnityEngine;

/// <summary>
/// レーザーの演出：コア／グローの強さをゆっくり脈動させ、微かに揺らがせ、光のパケットをビームに沿って走らせる。
/// 値は MaterialPropertyBlock で個体ごとに渡すのでマテリアル資産は共有のまま。
/// </summary>
public class LaserBeamVisual : MonoBehaviour
{
    private static readonly int IntensityId = Shader.PropertyToID("_Intensity");
    private static readonly int PulsePosId = Shader.PropertyToID("_PulsePos");
    private static readonly int ColorId = Shader.PropertyToID("_Color");
    private static readonly int EmissionId = Shader.PropertyToID("_EmissionColor");

    [Header("色（対応するスイッチと同じ色にする）")]
    public Color groupColor = new Color(1f, 0.12f, 0.05f);
    private Renderer[] rings;

    public Renderer core;
    public Renderer glow;
    public Renderer halo;

    public float coreIntensity = 4.5f;
    public float glowIntensity = 1.4f;
    public float haloIntensity = 0.35f;

    public float pulseSpeed = 2.0f;
    public float pulseAmount = 0.12f;
    public float packetSpeed = 0.9f;      // 1 秒あたりにビームを進む割合
    public float packetInterval = 2.2f;   // パケットの周期（秒）

    private MaterialPropertyBlock block;
    private float seed;

    private void Awake()
    {
        block = new MaterialPropertyBlock();
        seed = Random.Range(0f, 100f);
        var list = new System.Collections.Generic.List<Renderer>();
        foreach (var r in GetComponentsInChildren<Renderer>(true)) if (r.name.StartsWith("Ring")) list.Add(r);
        rings = list.ToArray();
    }

    private void Update()
    {
        float t = Time.unscaledTime + seed;
        float pulse = 1f + pulseAmount * Mathf.Sin(t * pulseSpeed);
        float flicker = 1f + 0.06f * (Mathf.PerlinNoise(t * 13f, seed) - 0.5f);

        // パケット：周期ごとに 0→1 を走り、残りの時間は画面外（-1）
        float cycle = Mathf.Repeat(t, packetInterval);
        float packetPos = cycle * packetSpeed;
        if (packetPos > 1.15f) packetPos = -1f;

        Apply(core, coreIntensity * flicker, packetPos, Color.Lerp(groupColor, Color.white, 0.8f));
        Apply(glow, glowIntensity * pulse, packetPos, groupColor);
        Apply(halo, haloIntensity * (2f - pulse), -1f, groupColor);
        foreach (var ring in rings)
        {
            ring.GetPropertyBlock(block);
            block.SetColor(EmissionId, groupColor * 6f);
            block.SetColor(ColorId, groupColor * 0.3f);
            ring.SetPropertyBlock(block);
        }
    }

    private void Apply(Renderer r, float intensity, float packetPos, Color color)
    {
        if (r == null) return;
        r.GetPropertyBlock(block);
        block.SetColor(ColorId, color);
        block.SetFloat(IntensityId, intensity);
        block.SetFloat(PulsePosId, packetPos);
        r.SetPropertyBlock(block);
    }
}
