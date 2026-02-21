using UnityEngine;
using UnityEngine.InputSystem;

public class PlayerMovement : MonoBehaviour
{
    [Header("Movement")]
    public float moveSpeed = 5f;
    public float crouchSpeed = 2.5f; // Speed while crouching
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

    [Header("Crouch Settings")]
    public float crouchHeight = 1.0f; // Height of capsule when crouching

    [Header("Animation")]
    public Animator animator; // Drag your Animator component here

    [Header("References")]
    public Transform cameraTransform;

    private Rigidbody rb;
    private CapsuleCollider capsuleCollider;
    private Vector2 moveInput;
    private bool jumpPressed;
    private bool isGrounded;
    private bool isCrouching = false;

    private float capsuleRadius;
    private float originalCapsuleHeight;
    private Vector3 originalCapsuleCenter;

    void Start()
    {
        rb = GetComponent<Rigidbody>();
        capsuleCollider = GetComponent<CapsuleCollider>();

        if (capsuleCollider != null)
        {
            capsuleRadius = capsuleCollider.radius;
            // Store original values to restore them when uncrouching
            originalCapsuleHeight = capsuleCollider.height;
            originalCapsuleCenter = capsuleCollider.center;
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
        HandleCrouch();
        UpdateAnimator();
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

    private void HandleCrouch()
    {
        // Use Left Control (leftCtrlKey) to toggle crouch
        bool wantToCrouch = Keyboard.current.ctrlKey.isPressed
        || Keyboard.current.cKey.isPressed;

        if (wantToCrouch != isCrouching)
        {
            if (wantToCrouch)
            {
                // Enter Crouch
                isCrouching = true;
                capsuleCollider.height = crouchHeight;
                capsuleCollider.center = new Vector3(0, crouchHeight / 2, 0);
            }
            else
            {
                // Exit Crouch (Simple restore)
                // Note: In a full game, you should check for ceilings here to prevent standing up inside walls
                isCrouching = false;
                capsuleCollider.height = originalCapsuleHeight;
                capsuleCollider.center = originalCapsuleCenter;
            }
        }
    }

    private void UpdateAnimator()
    {
        if (animator == null) return;

        // Calculate horizontal speed for animation
        float speed = new Vector3(rb.linearVelocity.x, 0f, rb.linearVelocity.z).magnitude;

        // Send parameters to Animator
        // 'Speed' should be a float parameter in your Animator
        animator.SetFloat("Speed", speed, 0.1f, Time.deltaTime);

        // 'IsGrounded' should be a bool parameter
        animator.SetBool("IsGrounded", isGrounded);
    }

    private void CheckGrounded()
    {
        if (capsuleCollider == null)
        {
            isGrounded = Physics.Raycast(transform.position, Vector3.down, groundCheckDistance + 0.1f, groundMask);
            return;
        }

        // Use the CURRENT height of the collider (handles crouching dynamically)
        float currentHeight = capsuleCollider.height;
        Vector3 origin = transform.position + Vector3.down * (currentHeight * 0.5f - capsuleRadius - 0.01f);

        isGrounded = Physics.SphereCast(
            origin,
            capsuleRadius * 0.9f,
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

        // Use player's rotation instead of camera's rotation
        // This ensures movement is based on yaw only, not pitch

        Vector3 forward = transform.forward;
        Vector3 right = transform.right;

        forward.y = 0f;
        right.y = 0f;
        forward.Normalize();
        right.Normalize();

        Vector3 targetDirection = (forward * moveInput.y + right * moveInput.x).normalized;

        // Use crouch speed if currently crouching, otherwise normal speed
        float currentSpeedLimit = isCrouching ? crouchSpeed : moveSpeed;

        Vector3 currentHorizontalVelocity = new Vector3(rb.linearVelocity.x, 0f, rb.linearVelocity.z);

        float currentControl = isGrounded ? 1f : airControl;

        if (targetDirection.magnitude > 0.1f)
        {
            Vector3 targetVelocity = targetDirection * currentSpeedLimit;
            Vector3 velocityChange = (targetVelocity - currentHorizontalVelocity) * currentControl;

            // Reduce acceleration slightly when crouching
            if (isCrouching) velocityChange *= 0.5f;

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
        if (clampedHorizontal.magnitude > currentSpeedLimit)
        {
            clampedHorizontal = clampedHorizontal.normalized * currentSpeedLimit;
            rb.linearVelocity = new Vector3(clampedHorizontal.x, rb.linearVelocity.y, clampedHorizontal.z);
        }
    }

    private void ApplyJump()
    {
        // Prevent jumping if crouching (optional)
        if (jumpPressed && isGrounded && !isCrouching)
        {
            rb.linearVelocity = new Vector3(rb.linearVelocity.x, 0f, rb.linearVelocity.z);
            rb.AddForce(Vector3.up * jumpForce, ForceMode.VelocityChange);
        }

        jumpPressed = false;
    }

    void OnDrawGizmos()
    {
        CapsuleCollider col = GetComponent<CapsuleCollider>();
        if (col == null) return;

        // Use dynamic height in Gizmos to visualize crouch correctly
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
