using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// Measures race time from scene start to when the player crosses the finish line.
/// Displays the result in the UI.
/// </summary>
public class RaceTimer : MonoBehaviour
{
    [SerializeField] private Text timeDisplay;  // UI Text to show the time
    private float startTime;                     // Time when the race starts (scene load)
    private float finishTime;                    // Time when the player crosses the finish line
    private bool hasFinished = false;            // Prevent multiple time recordings

    /// <summary>
    /// Called when the script is initialized (scene starts).
    /// </summary>
    private void Start()
    {
        // Record the start time
        startTime = Time.time;

        // Initialize the display to "0.000"
        UpdateTimeDisplay("0.000");
    }

    /// <summary>
    /// Called when the player enters the trigger zone.
    /// </summary>
    private void OnTriggerEnter(Collider other)
    {
        // Only trigger if the object has the "Player" tag and hasn't already finished
        if (other.CompareTag("Player") && !hasFinished)
        {
            // Record the finish time
            finishTime = Time.time;
            hasFinished = true;

            // Calculate elapsed time
            float elapsedTime = finishTime - startTime;

            // Format and display the time (e.g., "1.234")
            UpdateTimeDisplay(elapsedTime.ToString("F3"));
        }
    }

    /// <summary>
    /// Updates the UI Text with the given time string.
    /// </summary>
    private void UpdateTimeDisplay(string displayText)
    {
        if (timeDisplay != null)
        {
            timeDisplay.text = displayText;
        }
        else
        {
            Debug.LogWarning("Time display UI is not assigned!");
        }
    }
}