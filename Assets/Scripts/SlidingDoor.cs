using UnityEngine;

public class SlidingDoor : MonoBehaviour
{
    [Header("Movement Settings")]
    public Vector3 openDirection = Vector3.right; // Local space direction
    public float openDistance = 2.0f;
    public float openSpeed = 2.0f;

    [Header("Audio")]
    public AudioClip openSound;
    public AudioClip closeSound;
    private AudioSource audioSource;

    [Header("State")]
    public bool isOpen = false;
    public bool isMoving = false;

    private Vector3 closedPosition;
    private Vector3 openPosition;

    void Start()
    {
        // Cache positions
        closedPosition = transform.localPosition;
        openPosition = closedPosition + (openDirection.normalized * openDistance);

        // Setup Audio
        if (openSound != null || closeSound != null)
        {
            audioSource = gameObject.AddComponent<AudioSource>();
            audioSource.playOnAwake = false;
            audioSource.spatialBlend = 1.0f;
        }
    }

    void Update()
    {
        if (isMoving)
        {
            Vector3 targetPos = isOpen ? openPosition : closedPosition;

            transform.localPosition = Vector3.MoveTowards(
                transform.localPosition,
                targetPos,
                openSpeed * Time.deltaTime
            );

            if (Vector3.Distance(transform.localPosition, targetPos) < 0.01f)
            {
                transform.localPosition = targetPos;
                isMoving = false;
            }
        }
    }

    /// <summary>
    /// Call this from the PlayerInteractor script to toggle the door.
    /// </summary>
    public void Toggle()
    {
        isOpen = !isOpen;
        isMoving = true;
        PlaySound(isOpen ? openSound : closeSound);
    }

    // Optional: Force open/close specifically
    public void Open() { if (!isOpen) Toggle(); }
    public void Close() { if (isOpen) Toggle(); }

    void PlaySound(AudioClip clip)
    {
        if (audioSource != null && clip != null)
        {
            audioSource.PlayOneShot(clip);
        }
    }

    // Visual helper in Editor
    void OnDrawGizmosSelected()
    {
        Vector3 start = transform.position;
        Vector3 end = start + (transform.TransformDirection(openDirection.normalized) * openDistance);

        Gizmos.color = Color.cyan;
        Gizmos.DrawLine(start, end);
        Gizmos.DrawWireSphere(end, 0.1f);
    }
}
