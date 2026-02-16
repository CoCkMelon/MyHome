using UnityEngine;
using UnityEngine.InputSystem;

public class CameraRotation : MonoBehaviour
{
    [Header("Look Settings")]
    [Tooltip("Mouse sensitivity multiplier")]
    public float sensitivity = 20f;

    [Tooltip("Minimum vertical angle (looking down) in degrees")]
    public float minPitch = -80f;

    [Tooltip("Maximum vertical angle (looking up) in degrees")]
    public float maxPitch = 80f;

    [Tooltip("Invert Y axis")]
    public bool invertY = false;

    // internal state
    private float yaw;   // horizontal rotation
    private float pitch; // vertical rotation

    void Start()
    {
        Cursor.lockState = CursorLockMode.Locked;
        Cursor.visible = false;
    }

    void Update()
    {
        LookAround();
    }

    private void LookAround()
    {
        Vector2 mouseDelta = Mouse.current.delta.ReadValue();

        float deltaX = mouseDelta.x * sensitivity * Time.deltaTime;
        float deltaY = mouseDelta.y * sensitivity * Time.deltaTime * (invertY ? 1f : -1f);

        yaw += deltaX;
        pitch += deltaY;

        // Clamp pitch to avoid flipping
        pitch = Mathf.Clamp(pitch, minPitch, maxPitch);

        // Optionally wrap yaw to avoid large numbers (not strictly necessary)
        yaw = Mathf.Repeat(yaw, 360f);

        // Apply rotation: pitch affects X (look up/down), yaw affects Y (turn left/right)
        transform.localRotation = Quaternion.Euler(pitch, yaw, 0f);
    }
}
