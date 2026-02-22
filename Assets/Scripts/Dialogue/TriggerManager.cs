using UnityEngine;
using UnityEngine.Events;
using UnityEngine.SceneManagement;
using System.Collections;

/// <summary>
/// TriggerManager handles dialogue triggers and game events
/// Guide: Add your game-specific logic here based on dialogue triggers
/// </summary>
public class TriggerManager : MonoBehaviour
{
    public static TriggerManager Instance;
    
    // GUIDE: Add references to your game systems here
    // Example:
    // [Header("System References")]
    // [SerializeField] private DialogueManager dialogueManager;
    // [SerializeField] private YourGameManager gameManager;
    // [SerializeField] private YourUIController uiController;
    
    // GUIDE: Add game state variables here
    // Example:
    // [Header("Game State")]
    // [SerializeField] private bool playerHasKey = false;
    // [SerializeField] private bool questCompleted = false;
    // [SerializeField] private int playerScore = 0;
    
    // GUIDE: Add Unity Events for your game systems
    // Example:
    // [Header("Events")]
    // public UnityEvent OnQuestStart;
    // public UnityEvent OnPlayerDeath;
    // public UnityEvent OnLevelComplete;
    
    // GUIDE: Add private variables for tracking game progression
    // Example:
    // private int currentLevel = 1;
    // private bool isInCombat = false;

    public GameObject KitchenT1;
    public GameObject ComputerT1;
    public GameObject BedT1;
    public GameObject ClosetT1;
    public GameObject GarbageT1;
    public GameObject WallHoleT1;
    public GameObject UndergroundT1;
    public GameObject FiltersT1;
    public GameObject ComputerT2;
    public GameObject ExitT;


    public Material newSkyboxMaterial;
    public GameObject Terrain2;
    public GameObject oldobj;

    private void Awake() 
    {
        if (Instance == null)
        {
            Instance = this;
            // Only apply DontDestroyOnLoad if this is a root GameObject
            if (transform.parent == null)
            {
                DontDestroyOnLoad(gameObject);
            }
        }
        else
        {
            Destroy(gameObject);
        }
    }
    
    void Start()
    {
        // GUIDE: Initialize your game systems here
        // Example:
        // if (dialogueManager == null)
        //     dialogueManager = FindFirstObjectByType<DialogueManager>();
        // if (gameManager == null)
        //     gameManager = FindFirstObjectByType<YourGameManager>();
        //     
        // SetupGameEvents();
    }
    
    private void SetupGameEvents()
    {
        // GUIDE: Setup event listeners for your game systems here
        // Example:
        // if (yourPuzzle != null)
        //     yourPuzzle.OnPuzzleComplete.AddListener(OnYourPuzzleComplete);
        // if (yourEnemy != null)
        //     yourEnemy.OnEnemyDefeated.AddListener(OnEnemyDefeated);
    }

    public void Trigger(string triggerName, DialogueLine line)
    {
        Debug.Log($"Trigger activated: {triggerName}");
        
        // GUIDE: Add your trigger handling logic here
        // This method is called when dialogue encounters a trigger
        // Example:
        switch (triggerName)
        {
            // GUIDE: Add cases for your specific triggers
            // case "start_quest":
            //     StartQuest();
            //     break;
            // case "give_item":
            //     GiveItemToPlayer(line.item_id);
            //     break;
            // case "change_scene":
            //     LoadScene(line.next_scene);
            //     break;
            // case "unlock_ability":
            //     UnlockPlayerAbility(line.ability_name);
            //     break;
            case "bathroom1":
                ClosetT1.SetActive(true);
                break;
            case "dressup1":
                KitchenT1.SetActive(true);
                break;
            case "kitchen1":
                ComputerT1.SetActive(true);
                break;
            case "bed1":
                BedT1.SetActive(true);
                break;
            case "cave1":
                SceneManager.LoadScene("Cave");
                break;
            case "cave_end1":
                SceneManager.LoadScene("Hole 1");
                break;
            case "garbage":
                WallHoleT1.SetActive(true);
                break;
            case "wallhole":
                UndergroundT1.SetActive(true);
                break;
            case "underground":
                FiltersT1.SetActive(true);
                break;
            case "filters":
                ComputerT2.SetActive(true);
                break;
            case "cave2":
                SceneManager.LoadScene("Cave 2");
                break;
            case "sky":
                Skybox camSkybox = Camera.main.GetComponent<Skybox>();

                if (camSkybox == null)
                {
                    camSkybox = Camera.main.gameObject.AddComponent<Skybox>();
                }

                camSkybox.material = newSkyboxMaterial;
                Camera.main.clearFlags = CameraClearFlags.Skybox;
                Terrain2.SetActive(true);
                oldobj.SetActive(false);
                break;
            case "cave_end2":
                SceneManager.LoadScene("Hole 2");
                break;
            case "exit":
                ExitT.SetActive(true);
                break;
            case "remove thing":
                Destroy(oldobj);
                break;
            case "loose_end":
                SceneManager.LoadScene(0);
                break;
            case "win_end":
                Application.Quit();
                break;
                
            default:
                Debug.LogWarning($"Unknown trigger: {triggerName}");
                break;
        }
    }
    
    #region Your Game Logic Methods
    
    // GUIDE: Add your triggered methods here
    // Example:
    // private void StartQuest()
    // {
    //     // Your quest starting logic
    // }
    // 
    // private void GiveItemToPlayer(string itemId)
    // {
    //     // Your item giving logic
    // }
    // 
    // private void LoadScene(string sceneName)
    // {
    //     // Your scene loading logic
    // }
    
    #endregion
}

