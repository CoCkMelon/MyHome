// HovercraftController_InputActions.cs
// Uses the *generated C# wrapper* for your Input Actions asset named: HovercraftInput
// Action map: "Player"
// Actions: "Move" (Vector2), "Jump" (Button), "StickToWalls" (Button)
//
// IMPORTANT:
// 1) Put your JSON into an Input Actions asset named "HovercraftInput".
// 2) In that asset's Inspector, enable: "Generate C# Class"
// 3) Set the generated class name to: HovercraftInput   (matches below)
// 4) Save/Apply so Unity generates HovercraftInput.cs
//
// This controller defaults to terrain/ramps hover.
// If you hold StickToWalls, it will attempt wall-sticking (optional, can be ignored).

using UnityEngine;
using UnityEngine.InputSystem;

[DisallowMultipleComponent]
[RequireComponent(typeof(Rigidbody))]
public class HovercraftController : MonoBehaviour
{
    [Header("Hover Points (4 corners recommended)")]
    [SerializeField] private Transform[] hoverPoints;

    [Header("Hover (Multi-ray spring)")]
    [Min(0.05f)] [SerializeField] private float hoverHeight = 1.5f;
    [SerializeField] private float rayLengthMultiplier = 1.5f;
    [SerializeField] private float springStrength = 120f;
    [SerializeField] private float springDamping = 18f;
    [SerializeField] private LayerMask groundMask = ~0;

    [Header("Movement")]
    [SerializeField] private float thrustAccel = 25f;
    [SerializeField] private float turnTorqueAccel = 12f;
    [SerializeField] private float maxSpeed = 35f;

    [Header("Orientation (ramps/terrain)")]
    [SerializeField] private float alignToGroundSpeed = 6f;

    [Header("Handling")]
    [SerializeField] private float lateralFriction = 6f;
    [SerializeField] private float angularDamping = 2f;

    [Header("Jump")]
    [SerializeField] private float jumpImpulse = 7f;
    [SerializeField] private float jumpCooldown = 0.25f;

    [Header("Optional Wall Stick (hold action)")]
    [Tooltip("If enabled, holding StickToWalls will align 'up' to the surface normal even when steep.\n" +
             "If disabled, the craft only aligns on typical terrain/ramp angles.")]
    [SerializeField] private bool enableWallStickOption = false;

    [Tooltip("Max slope angle (degrees) where we still align in normal terrain mode.")]
    [Range(0f, 89f)]
    [SerializeField] private float maxAlignSlopeAngle = 70f;

    [Tooltip("How quickly we rotate in wall-stick mode.")]
    [SerializeField] private float wallStickAlignSpeed = 10f;

    // --- NEW FAN SECTION ---
    [Header("Fans (Visual Only)")]
    [Tooltip("Enable or disable visual fan animations.")]
    [SerializeField] private bool enableFans = false; // Default to false, let user enable

    [Tooltip("The GameObject representing the vertical lift fan (e.g., central fan).")]
    [SerializeField] private GameObject verticalFanModel;

    [Tooltip("The GameObject representing the left forward/thrust fan.")]
    [SerializeField] private GameObject leftForwardFanModel;

    [Tooltip("The GameObject representing the right forward/thrust fan.")]
    [SerializeField] private GameObject rightForwardFanModel;

    [Tooltip("Max spin speed for the vertical fan in degrees per second.")]
    [SerializeField] private float verticalFanMaxSpinSpeed = 1000f;

    [Tooltip("Max spin speed for forward/backward thrust component of the forward fans in degrees per second.")]
    [SerializeField] private float forwardFanMaxThrustSpeed = 1500f;

    [Tooltip("Max spin speed for turning component of the forward fans in degrees per second.")]
    [SerializeField] private float turnFanMaxThrustSpeed = 800f;

    [Tooltip("Additional spin boost for the vertical fan when jumping, in degrees per second.")]
    [SerializeField] private float jumpSpinBoost = 500f;

    [Tooltip("How long the jump spin boost lasts (seconds).")]
    [SerializeField] private float jumpSpinBoostDuration = 0.2f;

    private Rigidbody rb;

    // Input
    private HovercraftInput input;                  // generated wrapper
    private Vector2 move;
    private bool jumpQueued;
    private bool stickHeld;

    // Ground sampling
    private int groundedPointCount;
    private Vector3 accumulatedGroundNormal;
    private float lastJumpTime = -999f;

    // --- NEW FAN STATE ---
    private float jumpSpinBoostTimer = 0f; // Timer for jump visual boost

    private void Awake()
    {
        rb = GetComponent<Rigidbody>();
        rb.useGravity = true; // normal gravity for terrain/ramps mode

        input = new HovercraftInput();
    }

    private void OnEnable()
    {
        input.Enable();

        input.Player.Move.performed += OnMove;
        input.Player.Move.canceled += OnMove;

        input.Player.Jump.performed += OnJump;

        input.Player.StickToWalls.performed += OnStick;
        input.Player.StickToWalls.canceled += OnStick;
    }

    private void OnDisable()
    {
        // Unsubscribe first (good practice, prevents duplicate subscriptions in domain reload edge cases)
        input.Player.Move.performed -= OnMove;
        input.Player.Move.canceled -= OnMove;

        input.Player.Jump.performed -= OnJump;

        input.Player.StickToWalls.performed -= OnStick;
        input.Player.StickToWalls.canceled -= OnStick;

        input.Disable();
    }

    // Update for visual elements like fan rotations
    private void Update()
    {
        if (enableFans)
        {
            UpdateFanRotations();
        }
    }

    private void FixedUpdate()
    {
        if (hoverPoints == null || hoverPoints.Length == 0)
            return;

        ApplyHoverForces();
        AlignToGround();

        ApplyDrive();
        ApplySideFriction();
        ClampMaxSpeed();
        ApplyAngularDamping();

        if (jumpQueued)
            TryJump();
    }

    // ---------------- Input callbacks ----------------

    private void OnMove(InputAction.CallbackContext ctx)
    {
        move = ctx.ReadValue<Vector2>();
    }

    private void OnJump(InputAction.CallbackContext ctx)
    {
        // Queue for FixedUpdate so physics stays deterministic.
        jumpQueued = true;
    }

    private void OnStick(InputAction.CallbackContext ctx)
    {
        stickHeld = ctx.ReadValueAsButton();
    }

    // ---------------- Hover physics ----------------

    private void ApplyHoverForces()
    {
        groundedPointCount = 0;
        accumulatedGroundNormal = Vector3.zero;

        Vector3 downAxis = -transform.up;
        Vector3 upAxis = transform.up;

        float rayLength = hoverHeight * Mathf.Max(1f, rayLengthMultiplier);

        for (int i = 0; i < hoverPoints.Length; i++)
        {
            Transform p = hoverPoints[i];
            if (p == null) continue;

            Vector3 origin = p.position;

            if (!Physics.Raycast(origin, downAxis, out RaycastHit hit, rayLength, groundMask, QueryTriggerInteraction.Ignore))
                continue;

            // Only push when within hover height. Beyond that, no "spring".
            if (hit.distance > hoverHeight)
                continue;

            float compression = hoverHeight - hit.distance;

            // Damping using point velocity along the spring axis
            Vector3 pointVel = rb.GetPointVelocity(origin);
            float velAlongUp = Vector3.Dot(pointVel, upAxis);

            float springForce = compression * springStrength;
            float dampingForce = -velAlongUp * springDamping;

            float total = springForce + dampingForce;

            rb.AddForceAtPosition(upAxis * total, origin, ForceMode.Acceleration);

            accumulatedGroundNormal += hit.normal;
            groundedPointCount++;
        }
    }

    private void AlignToGround()
    {
        if (groundedPointCount <= 0)
            return;

        Vector3 avgNormal = (accumulatedGroundNormal / groundedPointCount).normalized;

        // Determine if we should align, depending on wall-stick option and slope angle.
        float slopeAngle = Vector3.Angle(avgNormal, Vector3.up);

        bool wantWallStick = enableWallStickOption && stickHeld;
        bool canAlignInTerrainMode = slopeAngle <= maxAlignSlopeAngle;

        if (!wantWallStick && !canAlignInTerrainMode)
            return;

        float speed = wantWallStick ? wallStickAlignSpeed : alignToGroundSpeed;

        Quaternion target = Quaternion.FromToRotation(transform.up, avgNormal) * rb.rotation;
        rb.MoveRotation(Quaternion.Slerp(rb.rotation, target, Time.fixedDeltaTime * speed));
    }

    // ---------------- Driving / handling ----------------

    private void ApplyDrive()
    {
        float throttle = Mathf.Clamp(move.y, -1f, 1f);
        float steer = Mathf.Clamp(move.x, -1f, 1f);

        rb.AddForce(transform.forward * (throttle * thrustAccel), ForceMode.Acceleration);
        rb.AddTorque(transform.up * (steer * turnTorqueAccel), ForceMode.Acceleration);
    }

    private void ApplySideFriction()
    {
        // Reduce sideways slip in local space, keep forward speed more intact.
        // Exponential decay gives stable behavior across different FixedDeltaTime.
        Vector3 localVel = transform.InverseTransformDirection(rb.linearVelocity);

        float t = 1f - Mathf.Exp(-lateralFriction * Time.fixedDeltaTime);
        localVel.x = Mathf.Lerp(localVel.x, 0f, t);

        rb.linearVelocity = transform.TransformDirection(localVel);
    }

    private void ClampMaxSpeed()
    {
        Vector3 v = rb.linearVelocity;
        float speed = v.magnitude;
        if (speed > maxSpeed)
            rb.linearVelocity = v * (maxSpeed / speed);
    }

    private void ApplyAngularDamping()
    {
        float t = 1f - Mathf.Exp(-angularDamping * Time.fixedDeltaTime);
        rb.angularVelocity = Vector3.Lerp(rb.angularVelocity, Vector3.zero, t);
    }

    // ---------------- Jump ----------------

    private void TryJump()
    {
        jumpQueued = false;

        if (Time.time < lastJumpTime + jumpCooldown)
            return;

        if (groundedPointCount <= 0)
            return;

        lastJumpTime = Time.time;
        jumpSpinBoostTimer = jumpSpinBoostDuration; // Start visual jump fan boost

        Vector3 avgNormal = (accumulatedGroundNormal / groundedPointCount).normalized;
        rb.AddForce(avgNormal * jumpImpulse, ForceMode.Impulse);
    }

    // ---------------- Fan Rotations (Visual Only) ----------------
    private void UpdateFanRotations()
    {
        // --- Vertical Fan ---
        if (verticalFanModel != null)
        {
            float currentVerticalSpeed = 0f;

            // Vertical fan spins constantly when the hovercraft is active (hovering or moving)
            bool isHoveringActive = groundedPointCount > 0 || rb.linearVelocity.sqrMagnitude > 0.1f || move.magnitude > 0.05f;
            if (isHoveringActive)
            {
                 currentVerticalSpeed = verticalFanMaxSpinSpeed;
            }

            // Apply jump boost if active
            if (jumpSpinBoostTimer > 0)
            {
                currentVerticalSpeed += jumpSpinBoost;
                jumpSpinBoostTimer -= Time.deltaTime;
            }
            else
            {
                jumpSpinBoostTimer = 0; // Ensure timer doesn't go negative
            }

            // Rotate the vertical fan model around its local up axis (Y-axis for many propeller-like models)
            // Adjust `verticalFanModel.transform.up` to `verticalFanModel.transform.forward` or `verticalFanModel.transform.right`
            // if your model's rotational axis is different.
            verticalFanModel.transform.Rotate(Vector3.up, currentVerticalSpeed * Time.deltaTime, Space.Self);
        }

        // --- Forward Fans ---
        if (leftForwardFanModel != null && rightForwardFanModel != null)
        {
            float throttleInput = move.y; // -1 (backward) to 1 (forward)
            float steerInput = move.x;   // -1 (left) to 1 (right)

            // If there's no significant input for forward/backward or turning, fans should not spin.
            // Using a small epsilon (0.05f) for float comparison to account for minor stick drift.
            if (Mathf.Abs(throttleInput) < 0.05f && Mathf.Abs(steerInput) < 0.05f)
            {
                // For immediate stop, just return. For more realism, you could gradually decelerate here.
                return;
            }

            // Calculate base spin for both fans from forward/backward thrust
            // Positive throttleInput means forward, negative means backward (reversed spin)
            float throttleSpin = throttleInput * forwardFanMaxThrustSpeed;

            // Calculate turning component for each fan based on steer input
            // As per requirements:
            // "Right fan should turn clockwise to turn right. Left counterclockwise."
            // "Turn left opposite."
            // We assume that a positive spin value results in clockwise rotation when looking at the fan's 'front'.
            // When turning right (steerInput > 0):
            //   - Right fan needs a positive turning component (to spin CW).
            //   - Left fan needs a negative turning component (to spin CCW).
            // This is achieved by adding `steerInput` for the right fan and subtracting it for the left fan.
            float rightFanTurnSpin = steerInput * turnFanMaxThrustSpeed;
            float leftFanTurnSpin = -steerInput * turnFanMaxThrustSpeed; // Invert direction for left fan

            // Total speed for each fan is a combination of throttle and turn components
            float leftFanTotalSpeed = throttleSpin + leftFanTurnSpin;
            float rightFanTotalSpeed = throttleSpin + rightFanTurnSpin;

            // Rotate the forward fan models around their local forward axis (Z-axis for many propeller-like models).
            // This assumes the fan model's local Z-axis is the axis of rotation for generating forward thrust.
            // Adjust `transform.forward` to `transform.up` or `transform.right` if your models are oriented differently.
            leftForwardFanModel.transform.Rotate(Vector3.up, leftFanTotalSpeed * Time.deltaTime, Space.Self);
            rightForwardFanModel.transform.Rotate(Vector3.up, rightFanTotalSpeed * Time.deltaTime, Space.Self);
        }
    }


    // ---------------- Debug ----------------

    private void OnDrawGizmosSelected()
    {
        if (hoverPoints == null) return;

        Gizmos.color = Color.yellow;
        float rayLength = hoverHeight * Mathf.Max(1f, rayLengthMultiplier);

        foreach (var p in hoverPoints)
        {
            if (p == null) continue;
            Gizmos.DrawLine(p.position, p.position - transform.up * rayLength);
        }
    }
}
