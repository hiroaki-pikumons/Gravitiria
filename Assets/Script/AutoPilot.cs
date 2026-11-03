using System;
using System.Collections;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using UnityEngine;
using UnityEngine.SceneManagement;

/// <summary>
/// 自動プレイによる動作検証。起動引数 -autopilot <台本ファイル> で有効になる。
/// 台本は [シーン名] セクションごとに1行1コマンド：
///   lookat x y z / walk 秒 [横 前] / flip / wait 秒 / log 文
/// シーンが切り替わるたびに該当セクションを実行し、ゴール到達（シーン遷移）を Player.log で確認する。
/// 起動引数 -startScene <シーン名> で開始シーンを指定できる。
/// </summary>
public class AutoPilot : MonoBehaviour
{
    private static readonly Dictionary<string, List<string>> sections = new Dictionary<string, List<string>>();
    private static AutoPilot instance;
    private static readonly List<string> visited = new List<string>();

    private GravityController player;
    private Coroutine running;

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
    private static void Bootstrap()
    {
        string script = GetArg("-autopilot");
        string startScene = GetArg("-startScene");

        if (!string.IsNullOrEmpty(script) && instance == null)
        {
            if (!File.Exists(script))
            {
                Debug.LogError($"[AutoPilot] 台本が見つかりません: {script}");
            }
            else
            {
                Parse(File.ReadAllLines(script));
                var go = new GameObject("AutoPilot");
                DontDestroyOnLoad(go);
                instance = go.AddComponent<AutoPilot>();
                SceneManager.sceneLoaded += instance.OnSceneLoaded;
                Debug.Log($"[AutoPilot] 台本読込: {sections.Count} セクション");
            }
        }

        if (!string.IsNullOrEmpty(startScene) && SceneManager.GetActiveScene().name != startScene)
        {
            Debug.Log($"[AutoPilot] 開始シーンへ移動: {startScene}");
            SceneManager.LoadScene(startScene);
            return;
        }

        if (instance != null) instance.OnSceneLoaded(SceneManager.GetActiveScene(), LoadSceneMode.Single);
    }

    private static void Parse(string[] lines)
    {
        sections.Clear();
        List<string> current = null;
        foreach (var raw in lines)
        {
            string line = raw.Trim();
            if (line.Length == 0 || line.StartsWith("#")) continue;
            if (line.StartsWith("[") && line.EndsWith("]"))
            {
                current = new List<string>();
                sections[line.Substring(1, line.Length - 2)] = current;
                continue;
            }
            current?.Add(line);
        }
    }

    private void OnSceneLoaded(Scene scene, LoadSceneMode mode)
    {
        visited.Add(scene.name);
        Debug.Log($"[AutoPilot] scene={scene.name} (visited: {string.Join(" > ", visited)})");

        if (running != null) StopCoroutine(running);
        running = null;

        string captureDir = GetArg("-capture");
        if (!string.IsNullOrEmpty(captureDir)) StartCoroutine(Capture(scene.name, captureDir));

        if (scene.name == "Title" && visited.Contains("Ending"))
        {
            Debug.Log("[AutoPilot] CHAIN COMPLETE: Ending を経てタイトルに戻った");
            StartCoroutine(QuitAfter(4f));
            return;
        }

        if (!sections.TryGetValue(scene.name, out var lines))
        {
            Debug.Log($"[AutoPilot] セクション無し: {scene.name}");
            return;
        }
        running = StartCoroutine(Run(scene.name, lines));
    }

    private IEnumerator QuitAfter(float seconds)
    {
        yield return new WaitForSecondsRealtime(seconds);
        Application.Quit();
    }

    private IEnumerator Capture(string sceneName, string dir)
    {
        yield return new WaitForSecondsRealtime(2.6f);
        Directory.CreateDirectory(dir);
        string path = Path.Combine(dir, sceneName + ".png");
        ScreenCapture.CaptureScreenshot(path);
        Debug.Log($"[AutoPilot] capture {path}");
    }

    private IEnumerator Run(string sceneName, List<string> lines)
    {
        yield return new WaitForSecondsRealtime(1.0f);
        player = FindAnyObjectByType<GravityController>();
        if (player != null) player.externalControl = true;

        foreach (var line in lines)
        {
            var t = line.Split(new[] { ' ' }, StringSplitOptions.RemoveEmptyEntries);
            string cmd = t[0].ToLowerInvariant();
            if (player == null && cmd != "wait" && cmd != "log" && cmd != "startgame" && cmd != "continue" && cmd != "space"
                && !cmd.StartsWith("t") && cmd != "manual")
            {
                Debug.LogError($"[AutoPilot] プレイヤーが居ないシーンで '{cmd}' は使えません");
                yield break;
            }
            switch (cmd)
            {
                case "startgame":
                    FindAnyObjectByType<TitleScreen>()?.StartGame();
                    break;
                case "continue":
                    FindAnyObjectByType<EndingScreen>()?.Continue();
                    break;
                case "space":
                    if (SubtitleManager.Instance != null) SubtitleManager.Instance.Dismiss();
                    break;

                // ---- タッチ操作の検証（擬似タッチ。座標は画面に対する 0〜1） ----
                case "touchui":          // touchui on|off
                    TouchControls.SetForced(t[1] == "on");
                    break;
                case "manual":           // 外部操作をやめ、GameInput（タッチ含む）でプレイヤーを動かす
                    if (player != null) player.externalControl = false;
                    break;
                case "tdown":            // tdown id x y
                    TouchControls.Source.Press(int.Parse(t[1]), Norm(F(t[2]), F(t[3])));
                    break;
                case "tmove":            // tmove id x y
                    TouchControls.Source.MoveTo(int.Parse(t[1]), Norm(F(t[2]), F(t[3])));
                    break;
                case "tup":              // tup id
                    TouchControls.Source.Release(int.Parse(t[1]));
                    break;
                case "tbutton":          // tbutton id Gravity|Dash|Pause|... （ボタンの中心を押す。離すのは tup）
                {
                    var b = (TouchInputSource.Button)Enum.Parse(typeof(TouchInputSource.Button), t[2], true);
                    Vector2 c = TouchControls.Instance.ButtonCenter(b);
                    Debug.Log($"[AutoPilot] tbutton {b} center={c}");
                    TouchControls.Source.Press(int.Parse(t[1]), c);
                    break;
                }
                case "tlog":             // 入力の状態と重力・時間倍率を記録
                    Debug.Log($"[AutoPilot] tlog move={GameInput.Move} look={GameInput.Look} aim={GameInput.AimHeld} dashHeld={GameInput.DashHeld} " +
                              $"timeScale={Time.timeScale:F2} gravity={(player ? player.GravityDirection.ToString() : "-")} pos={(player ? player.transform.position.ToString() : "-")} rot={(player ? player.transform.eulerAngles.ToString() : "-")} " +
                              $"paused={GameSession.IsPaused} touchActive={TouchControls.Active}");
                    break;
                case "dash":
                    Debug.Log($"[AutoPilot] dash -> {player.TryDash()} at {player.transform.position}");
                    break;
                case "quick":
                    Debug.Log($"[AutoPilot] quick -> {player.TryQuickGravity(new Vector2(F(t[1]), F(t[2])))} -> {player.GravityDirection}");
                    break;
                case "lookat":
                    player.LookAtWorldPoint(new Vector3(F(t[1]), F(t[2]), F(t[3])));
                    yield return new WaitForFixedUpdate();
                    break;
                case "walk":
                {
                    float dur = F(t[1]);
                    Vector2 mv = t.Length >= 4 ? new Vector2(F(t[2]), F(t[3])) : new Vector2(0f, 1f);
                    player.SetExternalMove(mv);
                    yield return SimulatedSeconds(dur);
                    player.SetExternalMove(Vector2.zero);
                    break;
                }
                case "flip":
                {
                    bool ok = player.FlipGravityToAim();
                    Debug.Log($"[AutoPilot] flip -> {(ok ? player.GravityDirection.ToString() : "MISS")} at {player.transform.position}");
                    break;
                }
                case "wait":
                    yield return SimulatedSeconds(F(t[1]));
                    break;
                case "log":
                    Debug.Log($"[AutoPilot] {line.Substring(3).Trim()} pos={(player ? player.transform.position.ToString() : "-")}");
                    break;
                default:
                    Debug.LogWarning($"[AutoPilot] 不明なコマンド: {line}");
                    break;
            }
            Debug.Log($"[AutoPilot] {sceneName}: {line}" + (player ? $" -> {player.transform.position}" : ""));
        }

        Debug.LogWarning($"[AutoPilot] SECTION END WITHOUT SCENE CHANGE: {sceneName} pos={(player ? player.transform.position.ToString() : "-")}");
    }

    /// <summary>
    /// 物理時間で待つ。実時間（WaitForSecondsRealtime）だと低 fps の環境で 1 フレーム分だけ長く歩いて位置がずれるため、
    /// FixedUpdate の回数で数えて、機材が違っても同じ距離・同じタイミングになるようにする。
    /// </summary>
    private static IEnumerator SimulatedSeconds(float seconds)
    {
        float t = 0f;
        while (t < seconds)
        {
            yield return new WaitForFixedUpdate();
            t += Time.fixedDeltaTime;
        }
    }

    private static float F(string s) => float.Parse(s, CultureInfo.InvariantCulture);
    private static Vector2 Norm(float x, float y) => new Vector2(x * Screen.width, y * Screen.height);

    private static string GetArg(string name)
    {
        string[] args = Environment.GetCommandLineArgs();
        for (int i = 0; i < args.Length - 1; i++)
        {
            if (string.Equals(args[i], name, StringComparison.OrdinalIgnoreCase)) return args[i + 1];
        }
        return null;
    }
}
