using UnityEngine;
using UnityEngine.InputSystem;

public class PlayerMovement : MonoBehaviour
{
    [Header("Movement")]
    public float moveSpeed = 5f;
    public float acceleration = 50f;
    public float deceleration = 30f;
    public float maxSpeed = 8f;

    [Header("Jump")]
    public float jumpForce = 8f;
    public float groundCheckDistance = 0.1f;
    [Tooltip("Set this to layers that count as ground (NOT the Player layer!)")]
    public LayerMask groundMask;

    [Header("Physics")]
    public float airControl = 0.3f;
    public float gravity = 20f;

    [Header("References")]
    public Transform cameraTransform;

    private Rigidbody rb;
    private CapsuleCollider capsuleCollider;
    private Vector2 moveInput;
    private bool jumpPressed;
    private bool isGrounded;
    private float capsuleRadius;
    private float capsuleHeight;

    void Start()
    {
        rb = GetComponent<Rigidbody>();
        capsuleCollider = GetComponent<CapsuleCollider>();

        if (capsuleCollider != null)
        {
            capsuleRadius = capsuleCollider.radius;
            capsuleHeight = capsuleCollider.height;
        }

        if (cameraTransform == null)
        {
            Transform cam = transform.Find("Main Camera");
            if (cam != null)
            {
                cameraTransform = cam;
            }
        }
    }

    void Update()
    {
        ReadInput();
        CheckGrounded();
    }

    void FixedUpdate()
    {
        ApplyMovement();
        ApplyJump();
    }

    private void ReadInput()
    {
        var keyboard = Keyboard.current;
        if (keyboard == null) return;

        float horizontal = 0f;
        float vertical = 0f;

        if (keyboard.wKey.isPressed) vertical += 1f;
        if (keyboard.sKey.isPressed) vertical -= 1f;
        if (keyboard.aKey.isPressed) horizontal -= 1f;
        if (keyboard.dKey.isPressed) horizontal += 1f;

        moveInput = new Vector2(horizontal, vertical).normalized;

        if (keyboard.spaceKey.wasPressedThisFrame)
        {
            jumpPressed = true;
        }
    }

    private void CheckGrounded()
    {
        if (capsuleCollider == null)
        {
            // Fallback raycast if no capsule collider
            isGrounded = Physics.Raycast(transform.position, Vector3.down, groundCheckDistance + 0.1f, groundMask);
            return;
        }

        // Cast a small sphere downward from bottom of capsule
        Vector3 origin = transform.position + Vector3.down * (capsuleHeight * 0.5f - capsuleRadius - 0.01f);
        
        // Use SphereCast going down - won't detect player's own collider
        isGrounded = Physics.SphereCast(
            origin,
            capsuleRadius * 0.9f,  // Slightly smaller to avoid edge cases
            Vector3.down,
            out RaycastHit hit,
            groundCheckDistance,
            groundMask,
            QueryTriggerInteraction.Ignore
        );
    }

    private void ApplyMovement()
    {
        if (cameraTransform == null) return;

        Vector3 forward = cameraTransform.forward;
        Vector3 right = cameraTransform.right;
        forward.y = 0f;
        right.y = 0f;
        forward.Normalize();
        right.Normalize();

        Vector3 targetDirection = (forward * moveInput.y + right * moveInput.x).normalized;
        Vector3 currentHorizontalVelocity = new Vector3(rb.linearVelocity.x, 0f, rb.linearVelocity.z);

        float currentControl = isGrounded ? 1f : airControl;

        if (targetDirection.magnitude > 0.1f)
        {
            Vector3 targetVelocity = targetDirection * moveSpeed;
            Vector3 velocityChange = (targetVelocity - currentHorizontalVelocity) * currentControl;
            velocityChange = Vector3.ClampMagnitude(velocityChange, acceleration * Time.fixedDeltaTime);

            rb.AddForce(velocityChange, ForceMode.VelocityChange);
        }
        else if (isGrounded)
        {
            Vector3 decel = -currentHorizontalVelocity.normalized * deceleration * Time.fixedDeltaTime;
            if (decel.magnitude > currentHorizontalVelocity.magnitude)
            {
                decel = -currentHorizontalVelocity;
            }
            rb.AddForce(decel, ForceMode.VelocityChange);
        }

        Vector3 clampedHorizontal = new Vector3(rb.linearVelocity.x, 0f, rb.linearVelocity.z);
        if (clampedHorizontal.magnitude > maxSpeed)
        {
            clampedHorizontal = clampedHorizontal.normalized * maxSpeed;
            rb.linearVelocity = new Vector3(clampedHorizontal.x, rb.linearVelocity.y, clampedHorizontal.z);
        }
    }

    private void ApplyJump()
    {
        if (jumpPressed && isGrounded)
        {
            rb.linearVelocity = new Vector3(rb.linearVelocity.x, 0f, rb.linearVelocity.z);
            rb.AddForce(Vector3.up * jumpForce, ForceMode.VelocityChange);
        }

        jumpPressed = false;
    }

    void OnDrawGizmos()
    {
        CapsuleCollider col = capsuleCollider;
        if (col == null) col = GetComponent<CapsuleCollider>();
        if (col == null) return;

        float height = col.height;
        float radius = col.radius;
        
        Vector3 origin = transform.position + Vector3.down * (height * 0.5f - radius - 0.01f);
        Vector3 end = origin + Vector3.down * groundCheckDistance;
        
        Gizmos.color = isGrounded ? Color.green : Color.red;
        Gizmos.DrawWireSphere(origin, radius * 0.9f);
        Gizmos.DrawWireSphere(end, radius * 0.9f);
        Gizmos.DrawLine(origin, end);
    }
}
