using UnityEngine;

public class DialogueTrigger : MonoBehaviour
{
    public string dialogueScenePath;
    public string triggerId;

    public bool destroyOnTrigger = false;

    private void OnTriggerEnter(Collider other)
    {
        if (!other.CompareTag("Player")) return;

        TryLoadDialogue();
    }
    void TryLoadDialogue()
    {
        bool shouldDestroy = false;

        if (!string.IsNullOrEmpty(dialogueScenePath))
        {
            if (DialogueManager.Instance != null)
            {
                DialogueManager.Instance.LoadAndStartScene(dialogueScenePath);
                shouldDestroy = true;
            }
            else
            {
                Debug.LogError("DialogueTrigger: DialogueManager.Instance is null!");
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
                Debug.LogError("DialogueTrigger: TriggerManager.Instance is null!");
            }
        }
        if (shouldDestroy && destroyOnTrigger)
        {
            Destroy(gameObject);
        }
    }
}

