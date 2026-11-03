using UnityEngine;

/// <summary>
/// ゲームパッド（旧 Input Manager 経由、Windows の XInput 配置）。
///   左スティック / 十字キー: 移動   右スティック: 視点
///   South (A): 照準（押している間スロー、離して重力反転）   West (X): ダッシュ
///   Start: ポーズ   East (B): 決定/戻る   North (Y): やり直し（ポーズ中）
///   十字キー + 照準ボタン: 4 方向重力（解放後）
/// 軸名 "DPadX/DPadY/RightStickX/RightStickY" は ProjectSettings/InputManager.asset に定義してある。
/// </summary>
public class GamepadInputSource : IInputSource
{
    private const float Dead = 0.2f;

    public bool IsAvailable
    {
        get
        {
            var names = Input.GetJoystickNames();
            foreach (var n in names) if (!string.IsNullOrEmpty(n)) return true;
            return false;
        }
    }

    public Vector2 Move { get; private set; }
    public Vector2 Look { get; private set; }
    public bool AimHeld { get; private set; }
    public bool AimReleased { get; private set; }
    public bool DashPressed { get; private set; }
    public bool DashHeld { get; private set; }
    public bool PausePressed { get; private set; }
    public bool SubmitPressed { get; private set; }
    public bool CloseMessagePressed { get; private set; }
    public bool CancelPressed { get; private set; }
    public bool RestartPressed { get; private set; }
    public bool ToTitlePressed { get; private set; }
    public Vector2 QuickGravityPressed { get; private set; }
    public bool ToggleLanguagePressed { get; private set; }
    public bool ResetRecordsHeld { get; private set; }

    private Vector2 lastDpad;
    public float lookSensitivity = 2.5f;

    public void Poll()
    {
        Vector2 stick = new Vector2(Input.GetAxisRaw("Horizontal"), Input.GetAxisRaw("Vertical"));
        if (stick.magnitude < Dead) stick = Vector2.zero;
        Vector2 dpad = new Vector2(Input.GetAxisRaw("DPadX"), Input.GetAxisRaw("DPadY"));
        if (dpad.magnitude < Dead) dpad = Vector2.zero;

        bool aim = Input.GetKey(KeyCode.JoystickButton0);
        AimReleased = Input.GetKeyUp(KeyCode.JoystickButton0);
        AimHeld = aim;

        // 十字キーは通常は移動、照準ボタンと同時押しなら 4 方向重力
        Vector2 dpadPressed = new Vector2(
            Mathf.Abs(dpad.x) > 0.5f && Mathf.Abs(lastDpad.x) <= 0.5f ? Mathf.Sign(dpad.x) : 0f,
            Mathf.Abs(dpad.y) > 0.5f && Mathf.Abs(lastDpad.y) <= 0.5f ? Mathf.Sign(dpad.y) : 0f);
        lastDpad = dpad;
        QuickGravityPressed = aim ? dpadPressed : Vector2.zero;
        Move = Vector2.ClampMagnitude(stick + (aim ? Vector2.zero : dpad), 1f);

        Vector2 right = new Vector2(Input.GetAxisRaw("RightStickX"), Input.GetAxisRaw("RightStickY"));
        if (right.magnitude < Dead) right = Vector2.zero;
        Look = right * lookSensitivity;

        DashHeld = Input.GetKey(KeyCode.JoystickButton2);
        DashPressed = Input.GetKeyDown(KeyCode.JoystickButton2);
        PausePressed = Input.GetKeyDown(KeyCode.JoystickButton7);
        SubmitPressed = Input.GetKeyDown(KeyCode.JoystickButton0) || Input.GetKeyDown(KeyCode.JoystickButton7);
        CloseMessagePressed = Input.GetKeyDown(KeyCode.JoystickButton1);
        CancelPressed = Input.GetKeyDown(KeyCode.JoystickButton1);
        RestartPressed = Input.GetKeyDown(KeyCode.JoystickButton3);
        ToTitlePressed = Input.GetKeyDown(KeyCode.JoystickButton6);
        ToggleLanguagePressed = false;
        ResetRecordsHeld = false;
    }
}
