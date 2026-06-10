using UnityEngine;

public enum MoveState { GROUND, SWIM, FLY }

[RequireComponent(typeof(Rigidbody2D))]
public class PlayerController : MonoBehaviour
{
    [Header("Auto-Run")]
    public float runSpeed = 5f;

    [Header("Ground")]
    public float jumpForce = 10f;
    public LayerMask groundLayer;
    public Transform groundCheck;
    public float groundCheckRadius = 0.1f;

    [Header("Swim")]
    public float swimSpeed = 3f;          // tốc độ di chuyển dọc trong nước
    public float swimUpForce = 6f;        // lực đẩy lên khi tap trong nước
    public float waterDrag = 3f;          // giảm tốc độ rơi trong nước
    public LayerMask waterLayer;

    [Header("Fly")]
    public float flapForce = 7f;          // lực đập cánh
    public float flyGravityScale = 0.4f;  // trọng lực nhẹ khi bay
    public float flyMaxUpSpeed = 6f;


    // ── Internal ──────────────────────────────────────────────
    private Rigidbody2D rb;
    private MoveState currentState = MoveState.GROUND;
    private bool isGrounded;
    private bool isInWater;

    private float lastTapTime = -999f;
    private float defaultGravityScale;

    private float waterSurfaceY;      // Y của mặt nước
    private float halfHeight;         // nửa chiều cao nhân vật
    private Collider2D waterZone;     // zone nước đang đứng trong


    // ── Unity Lifecycle ────────────────────────────────────────
    void Awake()
    {
        rb = GetComponent<Rigidbody2D>();
        defaultGravityScale = rb.gravityScale;

        var col = GetComponent<Collider2D>();
        halfHeight = col != null ? col.bounds.extents.y : 0.4f;

    }

    void Update()
    {
        CheckEnvironment();
        AutoTransitionState();
        HandleInput();
        ApplyAutoRun();
    }

    // ── Environment Detection ──────────────────────────────────
    void CheckEnvironment()
    {
        isGrounded = Physics2D.OverlapCircle(groundCheck.position, groundCheckRadius, groundLayer);
    }

    void OnTriggerEnter2D(Collider2D other)
    {
            Debug.Log($"Trigger hit: '{other.gameObject.name}' | layer index: {other.gameObject.layer} | layer name: {LayerMask.LayerToName(other.gameObject.layer)}");

        if (other.gameObject.layer == LayerMask.NameToLayer("Water"))
        {
            Debug.Log("[Quack] Entered water!");
            isInWater = true;
            EnterSwim(other);
        }
    }

    void OnTriggerExit2D(Collider2D other)
    {
        if (other.gameObject.layer == LayerMask.NameToLayer("Water"))
        {
            Debug.Log("[Quack] Exited water!");
            isInWater = false;
            waterZone = null;
            EnterGround();
        }
    }


    // ── Auto State Transition ──────────────────────────────────
    void AutoTransitionState()
    {
        // Chạm đất khi FLY → GROUND
        if (isGrounded && currentState == MoveState.FLY)
        {
            EnterGround();
        }
    }


    // ── Input ──────────────────────────────────────────────────
    void HandleInput()
    {
        bool tapped = Input.GetMouseButtonDown(0);
#if UNITY_IOS || UNITY_ANDROID
    if (Input.touchCount > 0 && Input.GetTouch(0).phase == TouchPhase.Began)
        tapped = true;
#endif

        if (!tapped) return;

        switch (currentState)
        {
            case MoveState.GROUND:
                if (isGrounded)
                {
                    Jump(); // tap khi đứng → nhảy, vẫn ở GROUND state
                }
                else
                {
                    EnterFly(); // tap khi đang trên không → vào FLY
                }
                break;

            case MoveState.SWIM:
                SwimUp();
                break;

            case MoveState.FLY:
                Flap(); // tap liên tục như Flappy Bird
                break;
        }
    }

    // ── State: GROUND ──────────────────────────────────────────
    void EnterGround()
    {
        currentState = MoveState.GROUND;
        rb.gravityScale = defaultGravityScale;
        rb.linearDamping = 0f;
        Debug.Log("[Quack] State → GROUND");
    }

    void Jump()
    {
        rb.linearVelocity = new Vector2(rb.linearVelocity.x, jumpForce);
    }

    // ── State: SWIM ────────────────────────────────────────────
    void EnterSwim(Collider2D water)
    {
        currentState = MoveState.SWIM;
        waterZone = water;
        // Mặt nước = cạnh trên của water collider
        waterSurfaceY = water.bounds.max.y;
        rb.gravityScale = 0f;          // tắt gravity, dùng buoyancy thay
        rb.linearDamping = 5f;
        Debug.Log("[Quack] State → SWIM");
    }


    void SwimUp()
    {
        rb.linearVelocity = new Vector2(rb.linearVelocity.x, swimUpForce);
    }

    void ApplyBuoyancy()
    {
        // Điểm nhân vật muốn nổi: mặt nước - 30% chiều cao (hơi lún nhẹ)
        float targetY = waterSurfaceY - halfHeight * 0.3f;
        float diff = targetY - transform.position.y;

        // Lực kéo về mặt nước (proportional)
        float buoyancyForce = diff * 12f;
        // Damping theo chiều dọc để không dao động
        float dampingForce = -rb.linearVelocity.y * 4f;

        rb.AddForce(new Vector2(0, buoyancyForce + dampingForce), ForceMode2D.Force);

        // Giới hạn tốc độ dọc
        rb.linearVelocity = new Vector2(
            rb.linearVelocity.x,
            Mathf.Clamp(rb.linearVelocity.y, -4f, swimUpForce)
        );
    }


    void FixedUpdate()
    {
        if (currentState == MoveState.SWIM)
            ApplyBuoyancy();
    }


    // ── State: FLY ─────────────────────────────────────────────
    void EnterFly()
    {
        currentState = MoveState.FLY;
        rb.gravityScale = flyGravityScale;
        rb.linearDamping = 0.5f;
        // Reset velocity Y về 0 để cảm giác "chuyển mode" rõ ràng
        rb.linearVelocity = new Vector2(rb.linearVelocity.x, 0f);
        Debug.Log("[Quack] State → FLY");
    }

    void ExitFly()
    {
        rb.gravityScale = defaultGravityScale;
        rb.linearDamping = 0f;
    }

    void ToggleFly()
    {
        if (currentState == MoveState.FLY)
        {
            EnterGround(); // thả → rơi xuống
        }
        else
        {
            EnterFly();
        }
    }

    void Flap()
    {
        // Cap tốc độ bay lên để không bay mãi
        float newVY = Mathf.Min(rb.linearVelocity.y + flapForce, flyMaxUpSpeed);
        rb.linearVelocity = new Vector2(rb.linearVelocity.x, newVY);
    }

    // ── Auto-Run ───────────────────────────────────────────────
    void ApplyAutoRun()
    {
        float speed = (currentState == MoveState.SWIM) ? swimSpeed : runSpeed;
        rb.linearVelocity = new Vector2(speed, rb.linearVelocity.y);
    }

    // ── Gizmos (debug) ─────────────────────────────────────────
    void OnDrawGizmosSelected()
    {
        if (groundCheck == null) return;
        Gizmos.color = Color.green;
        Gizmos.DrawWireSphere(groundCheck.position, groundCheckRadius);
        Gizmos.color = Color.cyan;
        Gizmos.DrawWireSphere(transform.position, 0.3f); // water check
    }
}