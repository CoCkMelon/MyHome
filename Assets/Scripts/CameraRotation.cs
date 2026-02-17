using UnityEngine;
using UnityEngine.InputSystem;

public class CameraRotation : MonoBehaviour
{
    [Header("Look Settings")]
    public float sensitivity = 2f;
    public float minPitch = -80f;
    public float maxPitch = 80f;
    public bool invertY = false;

    [Header("Camera Bobbing")]
    public bool enableBobbing = true;
    public float bobAmountY = 0.04f;
    public float bobAmountX = 0.02f;
    public float bobSpeed = 12f;
    public float bobSpeedThreshold = 1f;

    [Header("References")]
    public Rigidbody playerRigidbody;

    private float yaw;
    private float pitch;
    private float bobTimer;
    private Vector3 originalLocalPosition;

    void Start()
    {
        Cursor.lockState = CursorLockMode.Locked;
        Cursor.visible = false;
        
        originalLocalPosition = transform.localPosition;

        if (playerRigidbody == null && transform.parent != null)
        {
            playerRigidbody = transform.parent.GetComponent<Rigidbody>();
        }
    }

    void Update()
    {
        HandleMouseLook();
        HandleCameraBob();
    }

    private void HandleMouseLook()
    {
        var mouse = Mouse.current;
        if (mouse == null) return;

        Vector2 mouseDelta = mouse.delta.ReadValue();

        float mouseX = mouseDelta.x * sensitivity;
        float mouseY = mouseDelta.y * sensitivity * (invertY ? 1f : -1f);

        yaw += mouseX;
        pitch = Mathf.Clamp(pitch + mouseY, minPitch, maxPitch);

        transform.parent.rotation = Quaternion.Euler(0f, yaw, 0f);
        transform.localRotation = Quaternion.Euler(pitch, 0f, 0f);
    }

    private void HandleCameraBob()
    {
        if (!enableBobbing || playerRigidbody == null)
        {
            transform.localPosition = originalLocalPosition;
            return;
        }

        Vector3 horizontalVelocity = new Vector3(playerRigidbody.linearVelocity.x, 0f, playerRigidbody.linearVelocity.z);
        float speed = horizontalVelocity.magnitude;

        if (speed > bobSpeedThreshold)
        {
            bobTimer += Time.deltaTime * bobSpeed;

            float bobOffsetY = Mathf.Sin(bobTimer * 2f) * bobAmountY;
            float bobOffsetX = Mathf.Cos(bobTimer) * bobAmountX;

            Vector3 bobOffset = new Vector3(bobOffsetX, bobOffsetY, 0f);
            transform.localPosition = originalLocalPosition + bobOffset;
        }
        else
        {
            bobTimer = 0f;
            transform.localPosition = Vector3.Lerp(transform.localPosition, originalLocalPosition, Time.deltaTime * 5f);
        }
    }
}
