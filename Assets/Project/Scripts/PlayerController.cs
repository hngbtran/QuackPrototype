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

    [Header("Double Tap")]
    public float doubleTapWindow = 0.3f;  // thời gian tính là double tap (giây)

    // ── Internal ──────────────────────────────────────────────
    private Rigidbody2D rb;
    private MoveState currentState = MoveState.GROUND;
    private bool isGrounded;
    private bool isInWater;

    private float lastTapTime = -999f;
    private float defaultGravityScale;

    // ── Unity Lifecycle ────────────────────────────────────────
    void Awake()
    {
        rb = GetComponent<Rigidbody2D>();
        defaultGravityScale = rb.gravityScale;
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
        isInWater = Physics2D.OverlapCircle(transform.position, 0.3f, waterLayer);
    }

    // ── Auto State Transition ──────────────────────────────────
    void AutoTransitionState()
    {
        // Vào nước → chuyển SWIM (override cả FLY)
        if (isInWater && currentState != MoveState.SWIM)
        {
            EnterSwim();
            return;
        }

        // Ra khỏi nước, đang SWIM → chuyển GROUND
        if (!isInWater && currentState == MoveState.SWIM)
        {
            EnterGround();
        }

        // Chạm đất khi đang FLY → chuyển GROUND
        if (isGrounded && currentState == MoveState.FLY)
        {
            EnterGround();
        }
    }

    // ── Input ──────────────────────────────────────────────────
    void HandleInput()
    {
        // Nhận tap: mouse click hoặc touch
        bool tapped = Input.GetMouseButtonDown(0);
#if UNITY_IOS || UNITY_ANDROID
        if (Input.touchCount > 0 && Input.GetTouch(0).phase == TouchPhase.Began)
            tapped = true;
#endif

        if (!tapped) return;

        // Kiểm tra double tap
        bool isDoubleTap = (Time.time - lastTapTime) <= doubleTapWindow;
        lastTapTime = Time.time;

        if (isDoubleTap && !isInWater)
        {
            ToggleFly();
            return;
        }

        // Single tap theo state
        switch (currentState)
        {
            case MoveState.GROUND:
                if (isGrounded) Jump();
                break;

            case MoveState.SWIM:
                SwimUp();
                break;

            case MoveState.FLY:
                Flap();
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
    void EnterSwim()
    {
        currentState = MoveState.SWIM;
        rb.gravityScale = defaultGravityScale * 0.3f; // chìm chậm
        rb.linearDamping = waterDrag;
        Debug.Log("[Quack] State → SWIM");
    }

    void SwimUp()
    {
        rb.linearVelocity = new Vector2(rb.linearVelocity.x, swimUpForce);
    }

    // ── State: FLY ─────────────────────────────────────────────
    void EnterFly()
    {
        currentState = MoveState.FLY;
        rb.gravityScale = flyGravityScale;
        rb.linearDamping = 0.5f;
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