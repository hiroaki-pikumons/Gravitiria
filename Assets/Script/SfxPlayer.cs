using System.Collections.Generic;
using UnityEngine;

/// <summary>Resources/Audio 配下の効果音を名前で鳴らす（2D・スロー中も実時間）。</summary>
public static class SfxPlayer
{
    private static AudioSource source;
    private static readonly Dictionary<string, AudioClip> cache = new Dictionary<string, AudioClip>();

    public static float Volume = 0.8f;

    public static void Play(string name, float volume = 1f, float pitch = 1f)
    {
        var clip = Load(name);
        if (clip == null) return;
        EnsureSource();
        source.pitch = pitch;
        source.PlayOneShot(clip, volume * Volume);
    }

    private static AudioClip Load(string name)
    {
        if (!cache.TryGetValue(name, out var clip))
        {
            clip = Resources.Load<AudioClip>("Audio/" + name);
            if (clip == null) Debug.LogWarning($"SfxPlayer: Audio/{name} が見つかりません");
            cache[name] = clip;
        }
        return clip;
    }

    private static void EnsureSource()
    {
        if (source != null) return;
        var go = new GameObject("SfxPlayer");
        Object.DontDestroyOnLoad(go);
        source = go.AddComponent<AudioSource>();
        source.spatialBlend = 0f;
        source.ignoreListenerPause = true;
        source.playOnAwake = false;
    }
}
