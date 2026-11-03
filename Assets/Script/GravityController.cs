using UnityEngine;
using UnityEngine.UI;

[RequireComponent(typeof(Rigidbody))]
public class GravityController : MonoBehaviour
{
    public float gravityForce = 9.81f;
    public float mouseSensitivity = 2f;

    [Header("移動・ダッシュ設定")]
    public float walkSpeed = 5f;        // 通常の歩くスピード
    public float dashSpeed = 9f;        // Shiftを押した時のダッシュスピード

    [Header("エイム演出設定")]
    public float normalFOV = 60f;       // 通常時のカメラの広さ
    public float aimFOV = 35f;          // エイム時（右クリック）のズーム
    public float dashFOV = 50f;         // ダッシュ時（Shift）のちょいズーム
    public float zoomSpeed = 10f;       // ズームや暗くなるスピード
    public Image vignetteImage;         // 画面を暗くする用のUI

    [Range(0f, 1f)]
    public float maxDarkness = 0.6f;    // エイム時の暗さ

    [Header("エイム中のスロー倍率")]
    [Range(0.01f, 1f)]
    public float aimTimeScale = 0.1f;

    [Header("落下リセット（原点からこの距離を超えたらやり直し）")]
    public float outOfBoundsDistance = 150f;

    [Header("ダッシュ（解放後）")]
    public float dashBurstSpeed = 22f;
    public float dashDuration = 0.18f;
    public float dashInvincibleTime = 0.35f;
    public float dashCooldown = 1.2f;

    // StasisZone などが外から書き換える移動速度倍率
    [HideInInspector] public float speedMultiplier = 1f;

    private float dashTimeLeft;
    private float dashCooldownLeft;
    private float invincibleLeft;
    private Vector3 dashDirection;

    /// <summary>ダッシュ中の無敵。レーザー・敵・弾はこれが true の間は無視する。</summary>
    public bool IsInvincible => invincibleLeft > 0f;
    public float DashCooldownRatio => dashCooldown <= 0f ? 0f : Mathf.Clamp01(dashCooldownLeft / dashCooldown);

    // 自動テスト（AutoPilot）用：true の間はマウス／キー入力の代わりに外部指示で動く
    [HideInInspector] public bool externalControl = false;
    private Vector2 externalMove = Vector2.zero;

    private Vector3 gravityDirection = Vector3.down;
    private Rigidbody rb;
    private Camera playerCamera;
    private float verticalRotation = 0f;
    private float targetFOV;
    private float targetAlpha = 0f;
    private bool isResetting = false;

    public Vector3 GravityDirection => gravityDirection;

    /// <summary>現在のシーンのプレイヤー（タッチ UI などが参照する）。</summary>
    public static GravityController Current { get; private set; }

    // タッチ操作中はマウスカーソルのロックを要求しない（モバイルにはカーソルが無く、PC のデバッグではマウスを指として使う）
    private static bool NeedsCursorLock => !TouchControls.Active;

    void Awake()
    {
        Current = this;
        rb = GetComponent<Rigidbody>();
        rb.useGravity = false;
        rb.freezeRotation = true;
        playerCamera = GetComponentInChildren<Camera>();
        if (playerCamera == null) playerCamera = Camera.main;
    }

    void Start()
    {
        targetFOV = normalFOV;
        if (NeedsCursorLock) LockCursor(true);

        if (vignetteImage == null)
        {
            GameObject vig = GameObject.Find("Vignette");
            if (vig != null) vignetteImage = vig.GetComponent<Image>();
        }

        if (vignetteImage != null)
        {
            vignetteImage.sprite = null;
            vignetteImage.raycastTarget = false;
            Color c = Color.black;
            c.a = 0f;
            vignetteImage.color = c;
            vignetteImage.transform.localScale = Vector3.one;
        }
    }

    void Update()
    {
        if (GameSession.IsPaused) return;

        if (externalControl)
        {
            SetTimeScale(1f);
            playerCamera.transform.localRotation = Quaternion.Euler(verticalRotation, 0f, 0f);
            playerCamera.fieldOfView = Mathf.Lerp(playerCamera.fieldOfView, normalFOV, Time.unscaledDeltaTime * zoomSpeed);
            return;
        }

        if (NeedsCursorLock)
        {
            HandleCursor();
            if (Cursor.lockState != CursorLockMode.Locked) return;
        }

        // 1. 視点移動
        Vector2 look = GameInput.Look;
        float mouseX = look.x * mouseSensitivity;
        float mouseY = look.y * mouseSensitivity;

        transform.Rotate(Vector3.up * mouseX);
        verticalRotation -= mouseY;
        verticalRotation = Mathf.Clamp(verticalRotation, -85f, 85f);
        playerCamera.transform.localRotation = Quaternion.Euler(verticalRotation, 0f, 0f);

        // 2. カメラの目標視野角（FOV）と画面の暗さを決める
        if (GameInput.AimHeld)
        {
            // 右クリック長押し（エイム中）：スロー＆エイムズーム＆画面暗化
            SetTimeScale(aimTimeScale);
            targetFOV = aimFOV;
            targetAlpha = maxDarkness;
        }
        else if (GameInput.DashHeld)
        {
            // Shiftキー押しっぱなし（ダッシュ中）：ちょいズーム、画面は暗くしない
            SetTimeScale(1f);
            targetFOV = dashFOV;
            targetAlpha = 0f;
        }
        else
        {
            SetTimeScale(1f);
            targetFOV = normalFOV;
            targetAlpha = 0f;
        }

        // 右クリックを「離した瞬間」に、照準の先の面へ重力を切り替える
        if (GameInput.AimReleased)
        {
            FlipGravityToAim();
        }

        if (GameInput.DashPressed) TryDash();
        Vector2 quick = GameInput.QuickGravityPressed;
        if (quick != Vector2.zero) TryQuickGravity(quick);

        playerCamera.fieldOfView = Mathf.Lerp(playerCamera.fieldOfView, targetFOV, Time.unscaledDeltaTime * zoomSpeed);

        // レーザー警告中はLaserVignetteSyncが画面を点滅させるので、ここでは触らない
        if (vignetteImage != null && !LaserVignetteSync.PlayerInDanger)
        {
            Color c = vignetteImage.color;
            c.a = Mathf.Lerp(c.a, targetAlpha, Time.unscaledDeltaTime * zoomSpeed);
            vignetteImage.color = c;
        }
    }

    void FixedUpdate()
    {
        if (isResetting || GameSession.IsPaused) return;

        // 3. WASD移動 ＆ スピードの切り替え
        Vector2 move = GameInput.Move;
        float moveH = move.x;
        float moveV = move.y;
        if (externalControl) { moveH = externalMove.x; moveV = externalMove.y; }
        else if (NeedsCursorLock && Cursor.lockState != CursorLockMode.Locked) { moveH = 0f; moveV = 0f; }

        float currentSpeed = walkSpeed;
        if (GameInput.DashHeld)
        {
            currentSpeed = dashSpeed;
        }
        currentSpeed *= speedMultiplier;

        // 姿勢が重力方向へ追従している最中に移動入力の縦成分が蓄積して打ち上がらないよう、重力に垂直な面へ投影する
        Vector3 moveDir = Vector3.ProjectOnPlane(transform.forward * moveV + transform.right * moveH, gravityDirection).normalized;
        Vector3 targetVelocity = moveDir * currentSpeed;

        dashCooldownLeft = Mathf.Max(0f, dashCooldownLeft - Time.fixedDeltaTime);
        invincibleLeft = Mathf.Max(0f, invincibleLeft - Time.fixedDeltaTime);
        if (dashTimeLeft > 0f)
        {
            dashTimeLeft -= Time.fixedDeltaTime;
            targetVelocity = Vector3.ProjectOnPlane(dashDirection, gravityDirection).normalized * dashBurstSpeed;
        }
        Vector3 currentGravityVelocity = Vector3.Project(rb.linearVelocity, gravityDirection);
        rb.linearVelocity = targetVelocity + currentGravityVelocity;

        // 4. 重力
        GravityField.Set(gravityDirection);
        rb.AddForce(gravityDirection.normalized * gravityForce, ForceMode.Acceleration);

        // 5. 体の回転（足元が重力方向を向くように）
        Quaternion targetRotation = Quaternion.FromToRotation(-transform.up, gravityDirection) * transform.rotation;
        transform.rotation = Quaternion.Slerp(transform.rotation, targetRotation, Time.fixedDeltaTime * 8f);

        // 6. ステージ外へ落ちたらやり直し
        if (transform.position.sqrMagnitude > outOfBoundsDistance * outOfBoundsDistance)
        {
            ResetStage();
        }
    }

    public void ChangeGravityDirectly(Vector3 newGravityDirection)
    {
        gravityDirection = newGravityDirection.normalized;
    }

    /// <summary>ダッシュ：短い突進＋無敵。解放前・クールダウン中は何もしない。</summary>
    public bool TryDash()
    {
        if (!Progress.DashUnlocked || dashCooldownLeft > 0f || dashTimeLeft > 0f || isResetting) return false;
        Vector2 move = GameInput.Move;
        Vector3 dir = move.sqrMagnitude > 0.01f ? (transform.forward * move.y + transform.right * move.x) : transform.forward;
        dashDirection = Vector3.ProjectOnPlane(dir, gravityDirection).normalized;
        dashTimeLeft = dashDuration;
        invincibleLeft = dashInvincibleTime;
        dashCooldownLeft = dashCooldown;
        playerCamera.fieldOfView = Mathf.Max(playerCamera.fieldOfView - 8f, 30f);
        SfxPlayer.Play("sfx_jump", 0.7f, 1.4f);
        return true;
    }

    /// <summary>クイック重力：照準なしで視点基準の左/右（x）・上/下（y）へ重力を一発で切り替える。解放後のみ。</summary>
    public bool TryQuickGravity(Vector2 input)
    {
        if (!Progress.QuickGravityUnlocked || isResetting) return false;
        Vector3 dir;
        if (Mathf.Abs(input.x) >= Mathf.Abs(input.y)) dir = playerCamera.transform.right * Mathf.Sign(input.x);
        else dir = transform.up * Mathf.Sign(input.y);
        dir = SnapToAxis(dir);
        if (Vector3.Angle(dir, gravityDirection) < 1f) return false;
        gravityDirection = dir;
        SfxPlayer.Play("sfx_flip", 0.7f);
        return true;
    }

    private static Vector3 SnapToAxis(Vector3 v)
    {
        Vector3 a = new Vector3(Mathf.Abs(v.x), Mathf.Abs(v.y), Mathf.Abs(v.z));
        if (a.x >= a.y && a.x >= a.z) return new Vector3(Mathf.Sign(v.x), 0f, 0f);
        if (a.y >= a.z) return new Vector3(0f, Mathf.Sign(v.y), 0f);
        return new Vector3(0f, 0f, Mathf.Sign(v.z));
    }

    /// <summary>照準（画面中央）の先にある面へ重力を切り替える。トリガーは無視。</summary>
    public bool FlipGravityToAim()
    {
        Ray ray = playerCamera.ViewportPointToRay(new Vector3(0.5f, 0.5f, 0));
        if (Physics.Raycast(ray, out RaycastHit hit, 100f, Physics.DefaultRaycastLayers, QueryTriggerInteraction.Ignore))
        {
            Vector3 newDir = -hit.normal;
            if (Vector3.Angle(newDir, gravityDirection) > 1f) SfxPlayer.Play("sfx_flip", 0.7f);
            gravityDirection = newDir;
            return true;
        }
        return false;
    }

    public void SetExternalMove(Vector2 move)
    {
        externalMove = Vector2.ClampMagnitude(move, 1f);
    }

    /// <summary>外部操作用：体の向きとカメラの仰角を、指定したワールド座標を向くように設定する。</summary>
    public void LookAtWorldPoint(Vector3 worldPoint)
    {
        Vector3 up = transform.up;
        Vector3 dir = (worldPoint - playerCamera.transform.position).normalized;
        Vector3 planar = Vector3.ProjectOnPlane(dir, up);
        if (planar.sqrMagnitude < 1e-6f) planar = transform.forward;
        transform.rotation = Quaternion.LookRotation(planar.normalized, up);
        verticalRotation = Mathf.Clamp(-Mathf.Asin(Mathf.Clamp(Vector3.Dot(dir, up), -1f, 1f)) * Mathf.Rad2Deg, -85f, 85f);
        playerCamera.transform.localRotation = Quaternion.Euler(verticalRotation, 0f, 0f);
    }

    public void ResetStage()
    {
        if (isResetting) return;
        isResetting = true;
        if (GameSession.Instance != null) GameSession.Instance.RegisterDeath();

        SceneFader fader = FindAnyObjectByType<SceneFader>();
        if (fader != null)
        {
            fader.FadeAndReload();
        }
        else
        {
            SetTimeScale(1f);
            UnityEngine.SceneManagement.SceneManager.LoadScene(
                UnityEngine.SceneManagement.SceneManager.GetActiveScene().name);
        }
    }

    private void HandleCursor()
    {
        // Esc はポーズ（GameSession）が担当。フォーカス喪失などで外れたらクリックで再ロック
        if (Cursor.lockState != CursorLockMode.Locked && Input.GetMouseButtonDown(0)) // マウスのクリックでロック復帰（入力抽象化の対象外）
        {
            LockCursor(true);
        }
    }

    private static void LockCursor(bool locked)
    {
        Cursor.lockState = locked ? CursorLockMode.Locked : CursorLockMode.None;
        Cursor.visible = !locked;
    }

    private static void SetTimeScale(float scale)
    {
        Time.timeScale = scale;
        Time.fixedDeltaTime = 0.02f * scale;
    }

    void OnDisable()
    {
        SetTimeScale(1f);
    }

    void OnDestroy()
    {
        if (Current == this) Current = null;
    }
}
