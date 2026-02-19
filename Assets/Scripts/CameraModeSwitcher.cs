using UnityEngine;
using UnityEngine.InputSystem;

public class CameraModeSwitcher : MonoBehaviour
{
    [Header("Camera Objects")]
    public GameObject fpvCameraObject;
    public GameObject tpvCameraObject;

    [Header("Control Scripts")]
    // Drag the specific look/control scripts attached to the cameras here
    // This ensures only one script processes input at a time
    public Behaviour fpvControlScript;
    public Behaviour tpvControlScript;

    [Header("New Input System")]
    public InputActionReference toggleCameraAction; // Drag "ToggleCamera" action here

    [Header("Settings")]
    public bool lockCursorInFPV = true;
    public bool lockCursorInTPV = false;

    private bool isTPVActive = false;

    void OnEnable()
    {
        if (toggleCameraAction != null && toggleCameraAction.action != null)
        {
            toggleCameraAction.action.Enable();
            toggleCameraAction.action.performed += OnToggleCamera;
        }

        // Initialize state based on what is active in inspector
        isTPVActive = tpvCameraObject.activeSelf;
        UpdateCameraState();
    }

    void OnDisable()
    {
        if (toggleCameraAction != null && toggleCameraAction.action != null)
        {
            toggleCameraAction.action.performed -= OnToggleCamera;
            toggleCameraAction.action.Disable();
        }
    }

    void OnToggleCamera(InputAction.CallbackContext context)
    {
        ToggleCamera();
    }

    void ToggleCamera()
    {
        isTPVActive = !isTPVActive;
        UpdateCameraState();
    }

    void UpdateCameraState()
    {
        if (isTPVActive)
        {
            // Switch to Third Person
            SetCameraActive(fpvCameraObject, fpvControlScript, false);
            SetCameraActive(tpvCameraObject, tpvControlScript, true);

            SetCursorState(lockCursorInTPV);
            // Debug.Log("Switched to Third Person");
        }
        else
        {
            // Switch to First Person
            SetCameraActive(fpvCameraObject, fpvControlScript, true);
            SetCameraActive(tpvCameraObject, tpvControlScript, false);

            SetCursorState(lockCursorInFPV);
            // Debug.Log("Switched to First Person");
        }
    }

    void SetCameraActive(GameObject camObj, Behaviour controlScript, bool isActive)
    {
        camObj.SetActive(isActive);
        if (controlScript != null)
        {
            controlScript.enabled = isActive;
        }

        // Handle AudioListener to prevent double audio
        AudioListener listener = camObj.GetComponent<AudioListener>();
        if (listener != null)
        {
            listener.enabled = isActive;
        }
        // Also check children for AudioListener (common in prefabs)
        if(isActive && listener == null)
        {
            listener = camObj.GetComponentInChildren<AudioListener>();
            if(listener != null) listener.enabled = true;
        }
        else if (!isActive && listener == null)
        {
            listener = camObj.GetComponentInChildren<AudioListener>();
            if(listener != null) listener.enabled = false;
        }
    }

    void SetCursorState(bool shouldLock)
    {
        if (shouldLock)
        {
            Cursor.lockState = CursorLockMode.Locked;
            Cursor.visible = false;
        }
        else
        {
            Cursor.lockState = CursorLockMode.None;
            Cursor.visible = true;
        }
    }
}
