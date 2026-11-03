using UnityEngine;

/// <summary>解放式の能力。PlayerPrefs に保存され、周回をまたいで残る。</summary>
public static class Progress
{
    public enum Ability { Dash, QuickGravity, Echo }

    private static string Key(Ability a) => "unlock_" + a.ToString().ToLowerInvariant();

    public static bool IsUnlocked(Ability a) => PlayerPrefs.GetInt(Key(a), 0) == 1;

    public static bool DashUnlocked => IsUnlocked(Ability.Dash);
    public static bool QuickGravityUnlocked => IsUnlocked(Ability.QuickGravity);
    public static bool EchoUnlocked => IsUnlocked(Ability.Echo);

    /// <summary>新規に解放された時だけ true を返す。</summary>
    public static bool Unlock(Ability a)
    {
        if (IsUnlocked(a)) return false;
        PlayerPrefs.SetInt(Key(a), 1);
        PlayerPrefs.Save();
        return true;
    }

    public static void ResetAll()
    {
        foreach (Ability a in System.Enum.GetValues(typeof(Ability))) PlayerPrefs.DeleteKey(Key(a));
        PlayerPrefs.Save();
    }
}
