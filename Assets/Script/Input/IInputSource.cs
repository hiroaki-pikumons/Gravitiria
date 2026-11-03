using UnityEngine;

/// <summary>
/// 入力ソースの差し替え口。キーボード＋マウス、ゲームパッド、（将来）Android のタッチ UI などが実装する。
/// 毎フレーム GameInput から Poll() が呼ばれ、その後各プロパティが読まれる。
/// </summary>
public interface IInputSource
{
    /// <summary>このソースが現在有効か（接続されていない／使わない場合は false）。</summary>
    bool IsAvailable { get; }

    void Poll();

    /// <summary>移動（x: 右、y: 前）。-1..1。</summary>
    Vector2 Move { get; }

    /// <summary>視点（x: ヨー、y: ピッチ）。1 フレームあたりの回転量に相当する生値。</summary>
    Vector2 Look { get; }

    /// <summary>照準（スロー）を押している間 true。離した瞬間に重力が反転する。</summary>
    bool AimHeld { get; }
    bool AimReleased { get; }

    bool DashPressed { get; }
    bool DashHeld { get; }

    bool PausePressed { get; }

    /// <summary>決定（タイトル開始、エンディングで戻る）。</summary>
    bool SubmitPressed { get; }

    /// <summary>表示中の無線メッセージを閉じる（Space / East）。</summary>
    bool CloseMessagePressed { get; }

    /// <summary>戻る／終了。</summary>
    bool CancelPressed { get; }

    /// <summary>ポーズ中：やり直し / タイトルへ。</summary>
    bool RestartPressed { get; }
    bool ToTitlePressed { get; }

    /// <summary>4 方向重力（解放後）。視点基準で 左/右/上/下 を一発で指定する。無ければ Vector2.zero。</summary>
    Vector2 QuickGravityPressed { get; }

    /// <summary>言語切替（タイトル）。</summary>
    bool ToggleLanguagePressed { get; }

    /// <summary>記録リセット長押し（タイトル）。</summary>
    bool ResetRecordsHeld { get; }
}
