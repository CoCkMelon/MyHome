using UnityEngine;
using UnityEngine.InputSystem;

[RequireComponent(typeof(Camera))]
public class SmoothThirdPersonCamera : MonoBehaviour
{
    [Header("Target Settings")]
    public Transform target;                // The player object to follow
    public Vector3 offset = new Vector3(0, 2, -5);

    [Header("Camera Settings")]
    public float distance = 5.0f;
    public float minDistance = 1.5f;
    public float height = 2.0f;
    public float smoothSpeed = 10.0f;

    [Header("Collision")]
    public LayerMask collisionLayers;
    public float radius = 0.2f;

    [Header("Mouse Look")]
    public float mouseSensitivity = 2.0f;
    public float pitchMin = -45f;
    public float pitchMax = 85f;

    [Header("New Input System")]
    public InputActionReference lookAction; // Drag "Look" action here

    private float currentYaw = 0f;
    private float currentPitch = 0f;
    private Vector3 currentVelocity = Vector3.zero;

    void OnEnable()
    {
        if(lookAction != null && lookAction.action != null)
            lookAction.action.Enable();
    }

    void OnDisable()
    {
        if(lookAction != null && lookAction.action != null)
            lookAction.action.Disable();
    }

    void Start()
    {
        if (target == null)
        {
            Debug.LogError("ThirdPersonCamera: No Target assigned!");
            return;
        }

        // Initialize angles based on current position
        Vector3 currentOffset = transform.position - target.position;
        currentYaw = Mathf.Atan2(currentOffset.x, currentOffset.z) * Mathf.Rad2Deg;
        currentPitch = Mathf.Asin(currentOffset.y / currentOffset.magnitude) * Mathf.Rad2Deg;
    }

    void LateUpdate()
    {
        if (target == null) return;

        // 1. Handle Mouse Input for Rotation (New Input System)
        Vector2 lookInput = Vector2.zero;
        if (lookAction != null && lookAction.action != null)
        {
            lookInput = lookAction.action.ReadValue<Vector2>();
        }

        currentYaw += lookInput.x * mouseSensitivity;
        currentPitch -= lookInput.y * mouseSensitivity;
        currentPitch = Mathf.Clamp(currentPitch, pitchMin, pitchMax);

        // 2. Calculate Desired Position
        Quaternion rotation = Quaternion.Euler(currentPitch, currentYaw, 0);
        Vector3 desiredPosition = target.position + rotation * new Vector3(0, 0, -distance) + Vector3.up * height;

        // 3. Collision Detection (Raycast/SphereCast)
        Vector3 direction = (desiredPosition - target.position).normalized;
        float castDistance = Vector3.Distance(target.position, desiredPosition);

        if (Physics.SphereCast(target.position, radius, direction, out RaycastHit hit, castDistance, collisionLayers))
        {
            desiredPosition = target.position + direction * (hit.distance - 0.1f);

            if (Vector3.Distance(target.position, desiredPosition) < minDistance)
            {
                desiredPosition = target.position + direction * minDistance;
            }
        }

        // 4. Smoothly Move Camera
        transform.position = Vector3.SmoothDamp(transform.position, desiredPosition, ref currentVelocity, 1f / smoothSpeed);

        // 5. Always Look at Target
        transform.LookAt(target.position + Vector3.up * height);
    }

    public void SetTarget(Transform newTarget)
    {
        target = newTarget;
    }
}
