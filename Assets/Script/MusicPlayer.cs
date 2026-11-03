using UnityEngine;
using UnityEngine.SceneManagement;

/// <summary>
/// 起動時に自動生成され、シーンをまたいで BGM をループ再生する。
/// タイトル／エンディングでは落ち着いた曲、ステージでは疾走感のある曲。
/// スロー（右クリック長押し）中はピッチを落として緊張感を出す。
/// </summary>
public class MusicPlayer : MonoBehaviour
{
    private const string StageClip = "Audio/bgm_stage";
    private const string MenuClip = "Audio/bgm_title";

    [Range(0f, 1f)] public float volume = 0.42f;
    public float slowMotionPitch = 0.55f;
    public float pitchLerpSpeed = 6f;

    private static MusicPlayer instance;
    private AudioSource source;
    private AudioClip stage, menu;
    private float targetVolume;

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
    private static void Bootstrap()
    {
        if (instance != null) return;
        var go = new GameObject("MusicPlayer");
        DontDestroyOnLoad(go);
        instance = go.AddComponent<MusicPlayer>();
        instance.stage = Resources.Load<AudioClip>(StageClip);
        instance.menu = Resources.Load<AudioClip>(MenuClip);
        if (instance.stage == null && instance.menu == null)
        {
            Debug.LogWarning("MusicPlayer: Resources/Audio に BGM がありません。");
        }
        instance.source = go.AddComponent<AudioSource>();
        instance.source.loop = true;
        instance.source.playOnAwake = false;
        instance.source.spatialBlend = 0f;
        instance.source.ignoreListenerPause = true;
        SceneManager.sceneLoaded += instance.OnSceneLoaded;
        instance.OnSceneLoaded(SceneManager.GetActiveScene(), LoadSceneMode.Single);
    }

    private void OnSceneLoaded(Scene scene, LoadSceneMode mode)
    {
        bool isMenu = scene.name == "Title" || scene.name == "Ending";
        var clip = isMenu ? menu : stage;
        targetVolume = isMenu ? volume * 0.8f : volume;
        if (clip == null || source.clip == clip) return;

        source.clip = clip;
        source.volume = 0f;
        source.pitch = 1f;
        source.Play();
    }

    void Update()
    {
        if (source == null) return;
        float targetPitch = (Time.timeScale > 0f && Time.timeScale < 0.99f) ? slowMotionPitch : 1f;
        source.pitch = Mathf.Lerp(source.pitch, targetPitch, Time.unscaledDeltaTime * pitchLerpSpeed);
        source.volume = Mathf.MoveTowards(source.volume, GameSession.IsPaused ? targetVolume * 0.35f : targetVolume, Time.unscaledDeltaTime * 0.6f);
    }
}
