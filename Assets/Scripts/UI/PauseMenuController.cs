using UnityEngine;
using UnityEngine.UIElements;
using UnityEngine.InputSystem;
using UnityEngine.SceneManagement;

/// <summary>
/// Controller for the pause menu UI.
/// Handles ESC key to toggle pause state.
/// Attach to a GameObject with UIDocument component referencing PauseMenu.uxml
/// </summary>
[RequireComponent(typeof(UIDocument))]
public class PauseMenuController : MonoBehaviour
{
    public static PauseMenuController Instance { get; private set; }
    
    [Header("Scene Settings")]
    [SerializeField] private string mainMenuSceneName = "MainMenu";
    
    [Header("References")]
    [SerializeField] private UIDocument optionsDocument;
    
    private UIDocument uiDocument;
    private VisualElement root;
    private VisualElement pauseMenuRoot;
    private Button resumeButton;
    private Button optionsButton;
    private Button mainMenuButton;
    private Button quitButton;
    
    private bool isPaused = false;
    private bool optionsOpen = false;

    public bool IsPaused => isPaused;

    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }
        Instance = this;
        
        uiDocument = GetComponent<UIDocument>();
    }

    private void OnEnable()
    {
        root = uiDocument.rootVisualElement;
        
        // Query UI elements
        pauseMenuRoot = root.Q<VisualElement>("pause-menu-root");
        resumeButton = root.Q<Button>("resume-button");
        optionsButton = root.Q<Button>("options-button");
        mainMenuButton = root.Q<Button>("main-menu-button");
        quitButton = root.Q<Button>("quit-button");
        
        // Register callbacks
        if (resumeButton != null) resumeButton.clicked += OnResumeClicked;
        if (optionsButton != null) optionsButton.clicked += OnOptionsClicked;
        if (mainMenuButton != null) mainMenuButton.clicked += OnMainMenuClicked;
        if (quitButton != null) quitButton.clicked += OnQuitClicked;
        
        // Ensure menu is hidden initially
        if (pauseMenuRoot != null)
        {
            pauseMenuRoot.style.display = DisplayStyle.None;
        }
    }

    private void OnDisable()
    {
        // Unregister callbacks
        if (resumeButton != null) resumeButton.clicked -= OnResumeClicked;
        if (optionsButton != null) optionsButton.clicked -= OnOptionsClicked;
        if (mainMenuButton != null) mainMenuButton.clicked -= OnMainMenuClicked;
        if (quitButton != null) quitButton.clicked -= OnQuitClicked;
    }

    private void Update()
    {
        // Handle ESC key to toggle pause
        var keyboard = Keyboard.current;
        if (keyboard != null && keyboard.escapeKey.wasPressedThisFrame)
        {
            // Don't toggle if options are open - let options handle ESC
            if (optionsOpen) return;
            
            if (isPaused)
            {
                Resume();
            }
            else
            {
                Pause();
            }
        }
    }

    /// <summary>
    /// Pause the game and show the pause menu
    /// </summary>
    public void Pause()
    {
        if (isPaused) return;
        
        isPaused = true;
        Time.timeScale = 0f;
        
        if (pauseMenuRoot != null)
        {
            pauseMenuRoot.style.display = DisplayStyle.Flex;
        }
        
        // Show cursor
        UnityEngine.Cursor.lockState = CursorLockMode.None;
        UnityEngine.Cursor.visible = true;
        
        Debug.Log("Game paused");
    }

    /// <summary>
    /// Resume the game and hide the pause menu
    /// </summary>
    public void Resume()
    {
        if (!isPaused) return;
        
        isPaused = false;
        Time.timeScale = 1f;
        
        if (pauseMenuRoot != null)
        {
            pauseMenuRoot.style.display = DisplayStyle.None;
        }
        
        // Lock cursor for gameplay
        UnityEngine.Cursor.lockState = CursorLockMode.Locked;
        UnityEngine.Cursor.visible = false;
        
        Debug.Log("Game resumed");
    }

    private void OnResumeClicked()
    {
        Resume();
    }

    private void OnOptionsClicked()
    {
        Debug.Log("Opening options from pause menu...");
        
        if (optionsDocument != null)
        {
            var optionsController = optionsDocument.GetComponent<OptionsMenuController>();
            if (optionsController != null)
            {
                optionsOpen = true;
                optionsController.Show(() => {
                    // Callback when options closes - show pause menu again
                    optionsOpen = false;
                    if (pauseMenuRoot != null)
                    {
                        pauseMenuRoot.style.display = DisplayStyle.Flex;
                    }
                });
                
                // Hide pause menu while options are shown
                if (pauseMenuRoot != null)
                {
                    pauseMenuRoot.style.display = DisplayStyle.None;
                }
            }
        }
        else
        {
            Debug.LogWarning("Options document not assigned in PauseMenuController!");
        }
    }

    private void OnMainMenuClicked()
    {
        Debug.Log("Returning to main menu...");
        
        // Reset time scale before loading new scene
        Time.timeScale = 1f;
        isPaused = false;
        
        // Load main menu scene
        if (!string.IsNullOrEmpty(mainMenuSceneName))
        {
            SceneManager.LoadScene(mainMenuSceneName);
        }
        else
        {
            Debug.LogWarning("Main menu scene name not set in PauseMenuController!");
        }
    }

    private void OnQuitClicked()
    {
        Debug.Log("Quitting game...");
        
#if UNITY_EDITOR
        UnityEditor.EditorApplication.isPlaying = false;
#else
        Application.Quit();
#endif
    }

    private void OnDestroy()
    {
        // Ensure time scale is reset if this object is destroyed
        if (isPaused)
        {
            Time.timeScale = 1f;
        }
        
        if (Instance == this)
        {
            Instance = null;
        }
    }
}
