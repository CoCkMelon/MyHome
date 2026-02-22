using UnityEngine;
using UnityEngine.InputSystem;

public class PlayerMovement : MonoBehaviour
{
    [Header("Movement")]
    public float moveSpeed = 5f;
    public float crouchSpeed = 2.5f;
    public float acceleration = 50f;
    public float deceleration = 30f;
    public float maxSpeed = 8f;

    [Header("Jump")]
    public float jumpForce = 8f;
    public float groundCheckDistance = 0.1f;
    public LayerMask groundMask;

    [Header("Physics")]
    public float airControl = 0.3f;
    public float gravity = 20f;

    [Header("Crouch Settings")]
    public float crouchHeight = 1.0f;
    public float standingHeight = 1.8f; // Original standing height
    public float ceilingCheckDistance = 0.1f; // Distance to check for ceiling
    public LayerMask ceilingMask; // Layers that count as ceilings

    [Header("Animation")]
    public Animator animator;

    [Header("References")]
    public Transform cameraTransform;

    private Rigidbody rb;
    private CapsuleCollider capsuleCollider;
    private Vector2 moveInput;
    private bool jumpPressed;
    private bool isGrounded;
    private bool isCrouching = false;
    private bool wantsToCrouch = false;

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
            originalCapsuleHeight = capsuleCollider.height;
            originalCapsuleCenter = capsuleCollider.center;
            standingHeight = originalCapsuleHeight; // Set standing height
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
        HandleCrouchInput();
        UpdateAnimator();
    }

    void FixedUpdate()
    {
        ApplyMovement();
        ApplyJump();
        HandleCrouchState();
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

        // Store crouch input separately
        wantsToCrouch = keyboard.ctrlKey.isPressed || keyboard.cKey.isPressed;
    }

    private void HandleCrouchInput()
    {
        // Just store the input - actual crouch state is handled in FixedUpdate
    }

    private void HandleCrouchState()
    {
        // If we want to crouch but aren't already
        if (wantsToCrouch && !isCrouching)
        {
            isCrouching = true;
            capsuleCollider.height = crouchHeight;
            capsuleCollider.center = new Vector3(0, crouchHeight / 2, 0);
        }
        // If we don't want to crouch but are crouching
        else if (!wantsToCrouch && isCrouching)
        {
            // Check for ceiling before standing up
            if (!CheckForCeiling())
            {
                isCrouching = false;
                capsuleCollider.height = standingHeight;
                capsuleCollider.center = originalCapsuleCenter;
            }
        }
    }

    private bool CheckForCeiling()
    {
        // Calculate the position where the top of the capsule would be when standing
        float standingTop = transform.position.y + standingHeight - capsuleRadius;
        Vector3 ceilingCheckPos = new Vector3(
            transform.position.x,
            standingTop + ceilingCheckDistance,
            transform.position.z
        );

        // Check if there's anything above us that would prevent standing
        return Physics.CheckSphere(
            ceilingCheckPos,
            capsuleRadius * 0.9f,
            ceilingMask,
            QueryTriggerInteraction.Ignore
        );
    }

    private void UpdateAnimator()
    {
        if (animator == null) return;

        float speed = new Vector3(rb.linearVelocity.x, 0f, rb.linearVelocity.z).magnitude;
        animator.SetFloat("Speed", speed, 0.1f, Time.deltaTime);
        animator.SetBool("IsGrounded", isGrounded);
        animator.SetBool("IsCrouching", isCrouching);
    }

    private void CheckGrounded()
    {
        if (capsuleCollider == null)
        {
            isGrounded = Physics.Raycast(transform.position, Vector3.down, groundCheckDistance + 0.1f, groundMask);
            return;
        }

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
        Vector3 forward = transform.forward;
        Vector3 right = transform.right;

        forward.y = 0f;
        right.y = 0f;
        forward.Normalize();
        right.Normalize();

        Vector3 targetDirection = (forward * moveInput.y + right * moveInput.x).normalized;
        float currentSpeedLimit = isCrouching ? crouchSpeed : moveSpeed;

        Vector3 currentHorizontalVelocity = new Vector3(rb.linearVelocity.x, 0f, rb.linearVelocity.z);
        float currentControl = isGrounded ? 1f : airControl;

        if (targetDirection.magnitude > 0.1f)
        {
            Vector3 targetVelocity = targetDirection * currentSpeedLimit;
            Vector3 velocityChange = (targetVelocity - currentHorizontalVelocity) * currentControl;

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
        if (jumpPressed && isGrounded && !isCrouching)
        {
            rb.linearVelocity = new Vector3(rb.linearVelocity.x, 0f, rb.linearVelocity.z);
            rb.AddForce(Vector3.up * jumpForce, ForceMode.VelocityChange);
        }

        jumpPressed = false;
    }

    void OnDrawGizmos()
    {
        if (capsuleCollider == null) return;

        // Draw ground check
        float height = capsuleCollider.height;
        float radius = capsuleCollider.radius;
        Vector3 origin = transform.position + Vector3.down * (height * 0.5f - radius - 0.01f);
        Vector3 end = origin + Vector3.down * groundCheckDistance;

        Gizmos.color = isGrounded ? Color.green : Color.red;
        Gizmos.DrawWireSphere(origin, radius * 0.9f);
        Gizmos.DrawWireSphere(end, radius * 0.9f);
        Gizmos.DrawLine(origin, end);

        // Draw ceiling check when crouching
        if (isCrouching)
        {
            float standingTop = transform.position.y + standingHeight - radius;
            Vector3 ceilingCheckPos = new Vector3(
                transform.position.x,
                standingTop + ceilingCheckDistance,
                transform.position.z
            );

            Gizmos.color = Color.blue;
            Gizmos.DrawWireSphere(ceilingCheckPos, radius * 0.9f);
        }
    }
}
