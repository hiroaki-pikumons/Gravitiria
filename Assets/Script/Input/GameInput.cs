using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// ゲーム内で参照する唯一の入力窓口。複数の IInputSource（キーボード＋マウス、ゲームパッド、将来はタッチ UI）を束ね、
/// どれかが入力していればそれを採用する。スクリプトは UnityEngine.Input を直接触らず、ここだけを見る。
/// </summary>
public static class GameInput
{
    private static readonly List<IInputSource> sources = new List<IInputSource>
    {
        new KeyboardMouseInputSource(),
        new GamepadInputSource(),
    };

    private static int polledFrame = -1;

    /// <summary>別の入力ソース（タッチ UI など）を追加する。同じ型は重複登録しない。</summary>
    public static void AddSource(IInputSource source)
    {
        foreach (var s in sources) if (s.GetType() == source.GetType()) return;
        sources.Add(source);
    }

    public static void RemoveSource(IInputSource source) => sources.Remove(source);

    private static void PollIfNeeded()
    {
        if (polledFrame == Time.frameCount) return;
        polledFrame = Time.frameCount;
        foreach (var s in sources) if (s.IsAvailable) s.Poll();
    }

    private static Vector2 LargestVector(System.Func<IInputSource, Vector2> get)
    {
        PollIfNeeded();
        Vector2 best = Vector2.zero;
        foreach (var s in sources)
        {
            if (!s.IsAvailable) continue;
            var v = get(s);
            if (v.sqrMagnitude > best.sqrMagnitude) best = v;
        }
        return best;
    }

    private static bool Any(System.Func<IInputSource, bool> get)
    {
        PollIfNeeded();
        foreach (var s in sources) if (s.IsAvailable && get(s)) return true;
        return false;
    }

    public static Vector2 Move => LargestVector(s => s.Move);
    public static Vector2 Look => LargestVector(s => s.Look);
    public static bool AimHeld => Any(s => s.AimHeld);
    public static bool AimReleased => Any(s => s.AimReleased);
    public static bool DashPressed => Any(s => s.DashPressed);
    public static bool DashHeld => Any(s => s.DashHeld);
    public static bool PausePressed => Any(s => s.PausePressed);
    public static bool SubmitPressed => Any(s => s.SubmitPressed);
    public static bool CloseMessagePressed => Any(s => s.CloseMessagePressed);
    public static bool CancelPressed => Any(s => s.CancelPressed);
    public static bool RestartPressed => Any(s => s.RestartPressed);
    public static bool ToTitlePressed => Any(s => s.ToTitlePressed);
    public static Vector2 QuickGravityPressed => LargestVector(s => s.QuickGravityPressed);
    public static bool ToggleLanguagePressed => Any(s => s.ToggleLanguagePressed);
    public static bool ResetRecordsHeld => Any(s => s.ResetRecordsHeld);
}
