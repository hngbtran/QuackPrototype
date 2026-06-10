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

    [Header("Ceiling")]

    public Transform ceilingCheck;
    public float ceilingCheckRadius = 0.1f;
    private bool isCeilingHit;


    [Header("Swim")]
    public float swimUpForce = 6f;
    public float swimSpeed = 5f;

    [Header("Fly")]
    public float flapForce = 5f;
    public float flyGravityScale = 0.05f;
    public float flyMaxUpSpeed = 8f;

    // ── Internal ──────────────────────────────────────────────
    private Rigidbody2D rb;
    private MoveState currentState = MoveState.GROUND;
    private bool isGrounded;
    private bool isInWater;
    private float waterSurfaceY;
    private float halfHeight;
    private float defaultGravityScale;


    private Animator animator;

    // ── Lifecycle ──────────────────────────────────────────────
    void Awake()
    {
        rb = GetComponent<Rigidbody2D>();
        defaultGravityScale = rb.gravityScale;
        var col = GetComponent<Collider2D>();
        halfHeight = col != null ? col.bounds.extents.y : 0.4f;

        animator = GetComponent<Animator>();
    }

    void Update()
    {
        isGrounded = Physics2D.OverlapCircle(groundCheck.position, groundCheckRadius, groundLayer);

        isCeilingHit = Physics2D.OverlapCircle(ceilingCheck.position, ceilingCheckRadius, groundLayer);

        AutoTransitionState();
        HandleCeilingHit();

        HandleInput();
        ApplyAutoRun();

        // Debug.Log($"isGrounded: {isGrounded} | state: {currentState}");


        if (animator != null)
            animator.SetInteger("State", (int)currentState);

    }

    void FixedUpdate()
    {
        if (currentState == MoveState.SWIM)
            ApplyBuoyancy();
    }

    // ── State Transitions ──────────────────────────────────────
    void AutoTransitionState()
    {
        if (isGrounded && currentState == MoveState.FLY)
            EnterGround();
    }

    void EnterGround()
    {
        currentState = MoveState.GROUND;
        rb.gravityScale = defaultGravityScale;
        rb.linearDamping = 0f;
        Debug.Log("[Quack] → GROUND");
    }

    void EnterSwim(float surfaceY)
    {
        currentState = MoveState.SWIM;
        waterSurfaceY = surfaceY;
        rb.gravityScale = 0f;
        rb.linearDamping = 5f;
        Debug.Log("[Quack] → SWIM, surfaceY = " + surfaceY);
    }

    void EnterFly()
    {
        currentState = MoveState.FLY;
        rb.gravityScale = flyGravityScale;
        rb.linearDamping = 0.5f;
        rb.linearVelocity = new Vector2(rb.linearVelocity.x, 0f);
        Debug.Log("[Quack] → FLY");
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
                if (isGrounded) rb.linearVelocity = new Vector2(rb.linearVelocity.x, jumpForce);
                else EnterFly();
                break;
            case MoveState.SWIM:
                rb.linearVelocity = new Vector2(rb.linearVelocity.x, swimUpForce);
                break;
            case MoveState.FLY:
                float newVY = Mathf.Min(rb.linearVelocity.y + flapForce, flyMaxUpSpeed);
                rb.linearVelocity = new Vector2(rb.linearVelocity.x, newVY);
                break;
        }
    }

    // ── Auto-Run ───────────────────────────────────────────────
    void ApplyAutoRun()
    {
        float speed = (currentState == MoveState.SWIM) ? swimSpeed : runSpeed;
        rb.linearVelocity = new Vector2(speed, rb.linearVelocity.y);
    }

    // ── Buoyancy ───────────────────────────────────────────────
    void ApplyBuoyancy()
    {
        float targetY = waterSurfaceY - halfHeight * 0.3f;
        float diff = targetY - transform.position.y;
        float force = diff * 8f - rb.linearVelocity.y * 6f;
        rb.AddForce(new Vector2(0, force), ForceMode2D.Force);
        rb.linearVelocity = new Vector2(
            rb.linearVelocity.x,
            Mathf.Clamp(rb.linearVelocity.y, -4f, swimUpForce)
        );
    }

    // ── Water Detection ────────────────────────────────────────
    void OnTriggerEnter2D(Collider2D other)
    {
        Debug.Log($"[Trigger] Hit '{other.name}' layer: {LayerMask.LayerToName(other.gameObject.layer)}");
        if (other.CompareTag("Water"))
        {
            isInWater = true;
            EnterSwim(other.bounds.max.y);
        }
    }

    void OnTriggerExit2D(Collider2D other)
    {
        if (other.CompareTag("Water"))
        {
            // Chỉ thoát SWIM khi bay lên khỏi mặt nước
            if (transform.position.y > other.bounds.max.y - 0.1f)
            {
                isInWater = false;
                EnterGround();
            }
        }
    }

    // ── Ceiling Detection ────────────────────────────────────────
    void HandleCeilingHit()
    {
        if (isCeilingHit && currentState == MoveState.FLY)
        {
            // Đập đầu vào trần → dội xuống
            rb.linearVelocity = new Vector2(rb.linearVelocity.x, -3f);
        }
    }

    // ── Gizmos ─────────────────────────────────────────────────
    void OnDrawGizmosSelected()
    {
        if (groundCheck == null) return;
        Gizmos.color = Color.green;
        Gizmos.DrawWireSphere(groundCheck.position, groundCheckRadius);
    }
}