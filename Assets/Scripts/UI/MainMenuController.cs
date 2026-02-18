using UnityEngine;
using UnityEngine.UIElements;
using UnityEngine.SceneManagement;

/// <summary>
/// Controller for the main menu UI.
/// Attach to a GameObject with UIDocument component referencing MainMenu.uxml
/// </summary>
[RequireComponent(typeof(UIDocument))]
public class MainMenuController : MonoBehaviour
{
    [Header("Scene Settings")]
    [SerializeField] private string gameSceneName = "Hole";
    
    [Header("References")]
    [SerializeField] private UIDocument optionsDocument;
    
    private UIDocument uiDocument;
    private VisualElement root;
    private Button startButton;
    private Button optionsButton;
    private Button quitButton;

    private void Awake()
    {
        uiDocument = GetComponent<UIDocument>();
    }

    private void OnEnable()
    {
        root = uiDocument.rootVisualElement;
        
        // Query UI elements
        startButton = root.Q<Button>("start-button");
        optionsButton = root.Q<Button>("options-button");
        quitButton = root.Q<Button>("quit-button");
        
        // Register callbacks
        if (startButton != null) startButton.clicked += OnStartClicked;
        if (optionsButton != null) optionsButton.clicked += OnOptionsClicked;
        if (quitButton != null) quitButton.clicked += OnQuitClicked;
    }

    private void OnDisable()
    {
        // Unregister callbacks
        if (startButton != null) startButton.clicked -= OnStartClicked;
        if (optionsButton != null) optionsButton.clicked -= OnOptionsClicked;
        if (quitButton != null) quitButton.clicked -= OnQuitClicked;
    }

    private void Start()
    {
        // Ensure cursor is visible and unlocked in main menu
        UnityEngine.Cursor.lockState = CursorLockMode.None;
        UnityEngine.Cursor.visible = true;
        
        // Pause time in menu (optional, depends on your game)
        Time.timeScale = 1f;
    }

    private void OnStartClicked()
    {
        Debug.Log("Starting game...");
        
        // Load the game scene
        if (!string.IsNullOrEmpty(gameSceneName))
        {
            SceneManager.LoadScene(gameSceneName);
        }
        else
        {
            Debug.LogWarning("Game scene name not set in MainMenuController!");
        }
    }

    private void OnOptionsClicked()
    {
        Debug.Log("Opening options...");
        
        // Show options menu
        if (optionsDocument != null)
        {
            var optionsController = optionsDocument.GetComponent<OptionsMenuController>();
            if (optionsController != null)
            {
                optionsController.Show(() => {
                    // Callback when options closes - show main menu again
                    Show();
                });
                Hide();
            }
        }
        else
        {
            Debug.LogWarning("Options document not assigned in MainMenuController!");
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

    /// <summary>
    /// Show the main menu
    /// </summary>
    public void Show()
    {
        var menuRoot = root.Q<VisualElement>("main-menu-root");
        if (menuRoot != null)
        {
            menuRoot.style.display = DisplayStyle.Flex;
        }
        
        UnityEngine.Cursor.lockState = CursorLockMode.None;
        UnityEngine.Cursor.visible = true;
    }

    /// <summary>
    /// Hide the main menu
    /// </summary>
    public void Hide()
    {
        var menuRoot = root.Q<VisualElement>("main-menu-root");
        if (menuRoot != null)
        {
            menuRoot.style.display = DisplayStyle.None;
        }
    }
}
