using UnityEngine;
using UnityEngine.InputSystem;

[RequireComponent(typeof(PlayerInput))]
public class ThirdPersonCameraNewInput : MonoBehaviour
{
    [Header("Target Settings")]
    public Transform target;
    public Vector3 pivotOffset = new Vector3(0f, 1.5f, 0f); // Camera pivot point (chest height)
    public float smoothSpeed = 0.125f;

    [Header("Camera Settings")]
    public float minDistance = 1f;
    public float maxDistance = 5f;
    public float zoomSpeed = 2f;
    public float rotationSpeed = 5f;
    public float wallCheckRadius = 0.3f;
    public LayerMask collisionMask;

    [Header("Rotation Limits")]
    [Range(-89f, 0f)]
    public float minYAngle = -80f; // Look down limit (negative = below player)
    [Range(0f, 89f)]
    public float maxYAngle = 80f;  // Look up limit

    [Header("Input Actions")]
    public InputActionReference lookAction;
    public InputActionReference zoomAction;

    private float currentDistance;
    private float currentX = 0f;
    private float currentY = 0f;

    private void Start()
    {
        currentDistance = maxDistance * 0.6f; // Start at 60% of max distance
        Cursor.lockState = CursorLockMode.Locked;
    }

    private void OnEnable()
    {
        lookAction.action.Enable();
        zoomAction.action.Enable();
    }

    private void OnDisable()
    {
        lookAction.action.Disable();
        zoomAction.action.Disable();
    }

    private void LateUpdate()
    {
        if (target == null) return;

        // Get input from the new Input System
        Vector2 lookInput = lookAction.action.ReadValue<Vector2>();
        float zoomInput = zoomAction.action.ReadValue<Vector2>().y;

        // Apply rotation
        currentX += lookInput.x * rotationSpeed;
        currentY -= lookInput.y * rotationSpeed;
        currentY = Mathf.Clamp(currentY, minYAngle, maxYAngle);

        // Apply zoom
        currentDistance -= zoomInput * zoomSpeed;
        currentDistance = Mathf.Clamp(currentDistance, minDistance, maxDistance);

        // Calculate pivot point (where camera orbits around)
        Vector3 pivotPoint = target.position + pivotOffset;

        // Calculate rotation
        Quaternion rotation = Quaternion.Euler(currentY, currentX, 0);

        // Calculate desired camera position
        Vector3 negativeDistance = new Vector3(0f, 0f, -currentDistance);
        Vector3 desiredPosition = pivotPoint + rotation * negativeDistance;

        // Wall collision check
        Vector3 direction = (desiredPosition - pivotPoint).normalized;
        float targetDistance = currentDistance;

        RaycastHit hit;
        if (Physics.SphereCast(pivotPoint, wallCheckRadius, direction, out hit,
            currentDistance, collisionMask, QueryTriggerInteraction.Ignore))
        {
            targetDistance = Mathf.Max(hit.distance - wallCheckRadius, minDistance);
        }

        // Apply final position
        Vector3 finalPosition = pivotPoint + rotation * new Vector3(0f, 0f, -targetDistance);
        transform.position = Vector3.Lerp(transform.position, finalPosition, smoothSpeed);

        // Look at pivot point
        transform.LookAt(pivotPoint);
    }

    // Optional: Draw gizmos to visualize camera behavior
    private void OnDrawGizmosSelected()
    {
        if (target == null) return;

        Vector3 pivotPoint = target.position + pivotOffset;

        // Draw pivot point
        Gizmos.color = Color.yellow;
        Gizmos.DrawWireSphere(pivotPoint, 0.2f);

        // Draw camera range
        Gizmos.color = Color.cyan;
        Gizmos.DrawWireSphere(pivotPoint, minDistance);
        Gizmos.DrawWireSphere(pivotPoint, maxDistance);

        // Draw current camera position
        if (Application.isPlaying)
        {
            Gizmos.color = Color.green;
            Gizmos.DrawLine(pivotPoint, transform.position);
            Gizmos.DrawWireSphere(transform.position, wallCheckRadius);
        }
    }
}
