using UnityEngine;
using UnityEngine.InputSystem; // Required for New Input System

public class PlayerInteractor : MonoBehaviour
{
    [Header("Interaction Settings")]
    public float interactRange = 3.0f;
    public LayerMask interactableLayers; // Assign "Door" or "Default" here in Inspector
    public Camera playerCamera;

    [Header("Input Reference")]
    // Drag your InputActionAsset here, or reference the action directly
    public InputActionReference interactActionReference; 

    private InputAction interactAction;

    void Awake()
    {
        // Initialize Camera if not set
        if (playerCamera == null)
            playerCamera = Camera.main;

        // Setup Input Action
        if (interactActionReference != null && interactActionReference.action != null)
        {
            interactAction = interactActionReference.action;
        }
        else
        {
            Debug.LogWarning("PlayerInteractor: No Interact Action assigned! Please assign the InputActionReference in the Inspector.");
        }
    }

    void OnEnable()
    {
        if (interactAction != null)
            interactAction.Enable();
    }

    void OnDisable()
    {
        if (interactAction != null)
            interactAction.Disable();
    }

    void Update()
    {
        // Check if the interact button was pressed this frame
        if (interactAction != null && interactAction.WasPressedThisFrame())
        {
            PerformInteraction();
        }
    }

    void PerformInteraction()
    {
        if (playerCamera == null) return;
        
        // Raycast from center of screen
        Ray ray = new Ray(playerCamera.transform.position, playerCamera.transform.forward);
        RaycastHit hit;

        if (Physics.Raycast(ray, out hit, interactRange, interactableLayers))
        {
            // Try to get the SlidingDoor component from the hit object
            SlidingDoor door = hit.collider.GetComponent<SlidingDoor>();
            
            // If the hit object doesn't have it, check parents (in case you hit a handle child object)
            if (door == null)
            {
                door = hit.collider.GetComponentInParent<SlidingDoor>();
            }

            if (door != null)
            {
                door.Toggle();
                // Optional: Add interaction feedback here (e.g., UI prompt hide)
            }
        }
    }
    
    // Visualize ray in editor for debugging
    void OnDrawGizmos()
    {
        if (playerCamera == null) return;
        
        Gizmos.color = Color.green;
        Vector3 start = playerCamera.transform.position;
        Vector3 end = start + (playerCamera.transform.forward * interactRange);
        Gizmos.DrawLine(start, end);
    }
}