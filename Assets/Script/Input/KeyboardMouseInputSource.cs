using UnityEngine;

/// <summary>キーボード（WASD / 矢印）＋マウスの入力。</summary>
public class KeyboardMouseInputSource : IInputSource
{
    public bool IsAvailable => true;

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

    public void Poll()
    {
        float h = Axis(KeyCode.D, KeyCode.A) + Axis(KeyCode.RightArrow, KeyCode.LeftArrow);
        float v = Axis(KeyCode.W, KeyCode.S) + Axis(KeyCode.UpArrow, KeyCode.DownArrow);
        Move = Vector2.ClampMagnitude(new Vector2(h, v), 1f);

        Look = new Vector2(Input.GetAxis("Mouse X"), Input.GetAxis("Mouse Y"));

        AimHeld = Input.GetMouseButton(1);
        AimReleased = Input.GetMouseButtonUp(1);

        DashHeld = Input.GetKey(KeyCode.LeftShift) || Input.GetKey(KeyCode.RightShift);
        DashPressed = Input.GetKeyDown(KeyCode.LeftShift) || Input.GetKeyDown(KeyCode.RightShift);

        PausePressed = Input.GetKeyDown(KeyCode.Escape);
        SubmitPressed = Input.GetKeyDown(KeyCode.Return) || Input.GetKeyDown(KeyCode.KeypadEnter) || Input.GetKeyDown(KeyCode.Space) || Input.GetMouseButtonDown(0);
        CloseMessagePressed = Input.GetKeyDown(KeyCode.Space);
        CancelPressed = Input.GetKeyDown(KeyCode.Escape);
        RestartPressed = Input.GetKeyDown(KeyCode.R);
        ToTitlePressed = Input.GetKeyDown(KeyCode.T);

        // Q / E で左右、Z / C で下 / 上（視点基準）
        float gx = (Input.GetKeyDown(KeyCode.E) ? 1f : 0f) - (Input.GetKeyDown(KeyCode.Q) ? 1f : 0f);
        float gy = (Input.GetKeyDown(KeyCode.C) ? 1f : 0f) - (Input.GetKeyDown(KeyCode.Z) ? 1f : 0f);
        QuickGravityPressed = new Vector2(gx, gy);

        ToggleLanguagePressed = Input.GetKeyDown(KeyCode.L);
        ResetRecordsHeld = Input.GetKey(KeyCode.Delete);
    }

    private static float Axis(KeyCode positive, KeyCode negative)
    {
        return (Input.GetKey(positive) ? 1f : 0f) - (Input.GetKey(negative) ? 1f : 0f);
    }
}
