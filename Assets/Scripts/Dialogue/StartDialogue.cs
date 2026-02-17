using UnityEngine;

public class StartDialogue : MonoBehaviour
{
    public string dialogueScenePath;
    public string triggerId;

    public bool destroyOnTrigger = true;

    private void Start()
    {
        Invoke(nameof(BeginDialogue), 1f);
    }

    private void BeginDialogue()
    {
        if (!string.IsNullOrEmpty(dialogueScenePath))
        {
            if (DialogueManager.Instance != null)
            {
                DialogueManager.Instance.LoadAndStartScene(dialogueScenePath);
            }
            else
            {
                Debug.LogError("StartDialogue: DialogueManager.Instance is null!");
            }
        }
        if (!string.IsNullOrEmpty(triggerId))
        {
            if (TriggerManager.Instance != null)
            {
                TriggerManager.Instance.Trigger(triggerId, null);
            }
            else
            {
                Debug.LogError("StartDialogue: TriggerManager.Instance is null!");
            }
        }
        if (destroyOnTrigger)
        {
            Destroy(gameObject);
        }
    }
}

