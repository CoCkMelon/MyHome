using UnityEngine;
using System.Collections;

public class PressurePlate : MonoBehaviour
{
    [Header("References")]
    public SlidingDoor targetDoor;
    
    [Header("Detection")]
    public string activatorTag = "Player";
    
    [Header("Timing")]
    public float closeDelay = 3f;
    
    [Header("Plate Visual")]
    public float pressDepth = 0.05f;
    public float pressSpeed = 10f;
    
    [Header("Color")]
    public Renderer plateRenderer;
    public Color activeColor = Color.green;
    public Color inactiveColor = Color.red;
    
    private Vector3 originalPosition;
    private Vector3 pressedPosition;
    private bool isActivated = false;
    private int activatorCount = 0;
    private Coroutine closeCoroutine;

    void Start()
    {
        if (targetDoor == null)
        {
            Debug.LogError("PressurePlate: No target door assigned!");
        }
        
        if (plateRenderer == null)
        {
            plateRenderer = GetComponent<Renderer>();
        }
        
        originalPosition = transform.localPosition;
        pressedPosition = originalPosition - Vector3.up * pressDepth;
        
        UpdatePlateColor();
    }

    void Update()
    {
        Vector3 targetPos = isActivated ? pressedPosition : originalPosition;
        transform.localPosition = Vector3.Lerp(transform.localPosition, targetPos, Time.deltaTime * pressSpeed);
    }

    void OnTriggerEnter(Collider other)
    {
        if (!other.CompareTag(activatorTag)) return;
        
        activatorCount++;
        
        // Cancel any pending close
        if (closeCoroutine != null)
        {
            StopCoroutine(closeCoroutine);
            closeCoroutine = null;
        }
        
        if (!isActivated)
        {
            isActivated = true;
            UpdatePlateColor();
            
            if (targetDoor != null)
            {
                targetDoor.Open();
            }
            
            Debug.Log("Pressure plate activated");
        }
    }

    void OnTriggerExit(Collider other)
    {
        if (!other.CompareTag(activatorTag)) return;
        
        activatorCount--;
        
        if (activatorCount <= 0)
        {
            activatorCount = 0;
            closeCoroutine = StartCoroutine(CloseAfterDelay());
            
            Debug.Log("Pressure plate deactivated, closing in " + closeDelay + "s");
        }
    }
    
    private IEnumerator CloseAfterDelay()
    {
        yield return new WaitForSeconds(closeDelay);
        
        isActivated = false;
        UpdatePlateColor();
        
        if (targetDoor != null)
        {
            targetDoor.Close();
        }
        
        closeCoroutine = null;
    }
    
    private void UpdatePlateColor()
    {
        if (plateRenderer != null)
        {
            plateRenderer.material.color = isActivated ? activeColor : inactiveColor;
        }
    }

    void OnDrawGizmosSelected()
    {
        Gizmos.color = new Color(0f, 1f, 0f, 0.3f);
        
        var boxCollider = GetComponent<BoxCollider>();
        if (boxCollider != null && boxCollider.isTrigger)
        {
            Gizmos.matrix = transform.localToWorldMatrix;
            Gizmos.DrawCube(boxCollider.center, boxCollider.size);
        }
    }
}
