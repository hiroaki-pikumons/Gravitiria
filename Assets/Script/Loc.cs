using System;
using UnityEngine;

/// <summary>
/// 表示言語。既定は OS の言語（日本語なら日本語、それ以外は英語）。
/// タイトル画面の L キーで切り替えられ、選択は PlayerPrefs に保存される。
/// 起動引数 -lang ja|en でも指定できる（検証用）。
/// </summary>
public static class Loc
{
    private const string PrefKey = "lang";

    public static bool IsJapanese { get; private set; }

    static Loc()
    {
        Refresh();
    }

    public static void Refresh()
    {
        string forced = GetArg("-lang");
        string pref = string.IsNullOrEmpty(forced) ? PlayerPrefs.GetString(PrefKey, "") : forced;
        if (pref == "ja") IsJapanese = true;
        else if (pref == "en") IsJapanese = false;
        else IsJapanese = Application.systemLanguage == SystemLanguage.Japanese;
    }

    public static void Toggle()
    {
        PlayerPrefs.SetString(PrefKey, IsJapanese ? "en" : "ja");
        PlayerPrefs.Save();
        Refresh();
    }

    public static string T(string ja, string en)
    {
        if (IsJapanese) return ja;
        return string.IsNullOrEmpty(en) ? ja : en;
    }

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
