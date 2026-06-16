using UnityEngine;
using UnityEngine.InputSystem;

public class PlayerMovement_UnityEvents : MonoBehaviour
{
    [Header("Movement")]
    public float speed = 5f;

    [Header("If you have Rigidbody2D, drag it here (optional)")]
    public Rigidbody2D rb;

    private Vector2 moveInput;

    // Gắn vào PlayerInput -> Events -> Player -> Move
    public void OnMove(InputAction.CallbackContext context)
    {
        moveInput = context.ReadValue<Vector2>();
    }

    void Reset()
    {
        rb = GetComponent<Rigidbody2D>();
    }

    void Update()
    {
        // Nếu không dùng Rigidbody2D thì di chuyển transform
        if (rb == null)
        {
            Vector3 delta = new Vector3(moveInput.x, moveInput.y, 0f) * speed * Time.deltaTime;
            transform.Translate(delta);
        }
    }

    void FixedUpdate()
    {
        // Nếu có Rigidbody2D thì nên dùng physics movement cho mượt
        if (rb != null)
        {
            rb.linearVelocity = moveInput * speed;
        }
    }
}