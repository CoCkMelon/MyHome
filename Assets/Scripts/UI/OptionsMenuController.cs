using UnityEngine;
using UnityEngine.UIElements;
using UnityEngine.InputSystem;
using System.Collections;

/// <summary>
/// Controller for the options/settings menu UI.
/// Handles all settings controls and resolution confirmation with 15-second timeout.
/// Attach to a GameObject with UIDocument component referencing OptionsMenu.uxml
/// </summary>
[RequireComponent(typeof(UIDocument))]
public class OptionsMenuController : MonoBehaviour
{
    private UIDocument uiDocument;
    private VisualElement root;
    private VisualElement optionsMenuRoot;
    
    // Audio controls
    private Slider masterVolumeSlider;
    private Label masterVolumeValue;
    private Slider musicVolumeSlider;
    private Label musicVolumeValue;
    private Slider soundVolumeSlider;
    private Label soundVolumeValue;
    
    // Display controls
    private DropdownField resolutionDropdown;
    private DropdownField windowModeDropdown;
    private DropdownField displayDropdown;
    private DropdownField antiAliasingDropdown;
    private Toggle postEffectsToggle;
    private DropdownField graphicsPresetDropdown;
    
    // Controls
    private Slider sensitivitySlider;
    private Label sensitivityValue;
    private Toggle onscreenJoystickToggle;
    private Toggle onscreenDPadToggle;
    
    // Buttons
    private Button saveButton;
    private Button backButton;
    
    // Confirmation dialog
    private VisualElement confirmationOverlay;
    private Label confirmationTimer;
    private Button confirmKeepButton;
    private Button confirmRevertButton;
    
    // State
    private System.Action onCloseCallback;
    private bool hasUnsavedDisplayChanges = false;
    private int previousResolutionIndex;
    private int previousWindowModeIndex;
    private Coroutine confirmationCoroutine;
    
    private const float CONFIRMATION_TIMEOUT = 15f;

    private void Awake()
    {
        uiDocument = GetComponent<UIDocument>();
    }

    private void OnEnable()
    {
        root = uiDocument.rootVisualElement;
        
        // Query all UI elements
        QueryUIElements();
        
        // Register callbacks
        RegisterCallbacks();
        
        // Ensure menu is hidden initially
        if (optionsMenuRoot != null)
        {
            optionsMenuRoot.style.display = DisplayStyle.None;
        }
    }

    private void OnDisable()
    {
        UnregisterCallbacks();
    }

    private void Update()
    {
        // Handle ESC to close options (back without saving)
        var keyboard = Keyboard.current;
        if (keyboard != null && keyboard.escapeKey.wasPressedThisFrame)
        {
            if (optionsMenuRoot != null && optionsMenuRoot.style.display == DisplayStyle.Flex)
            {
                // If confirmation is showing, treat ESC as revert
                if (confirmationOverlay != null && confirmationOverlay.style.display == DisplayStyle.Flex)
                {
                    RevertDisplayChanges();
                }
                else
                {
                    OnBackClicked();
                }
            }
        }
    }

    private void QueryUIElements()
    {
        optionsMenuRoot = root.Q<VisualElement>("options-menu-root");
        
        // Audio
        masterVolumeSlider = root.Q<Slider>("master-volume-slider");
        masterVolumeValue = root.Q<Label>("master-volume-value");
        musicVolumeSlider = root.Q<Slider>("music-volume-slider");
        musicVolumeValue = root.Q<Label>("music-volume-value");
        soundVolumeSlider = root.Q<Slider>("sound-volume-slider");
        soundVolumeValue = root.Q<Label>("sound-volume-value");
        
        // Display
        resolutionDropdown = root.Q<DropdownField>("resolution-dropdown");
        windowModeDropdown = root.Q<DropdownField>("window-mode-dropdown");
        displayDropdown = root.Q<DropdownField>("display-dropdown");
        antiAliasingDropdown = root.Q<DropdownField>("antialiasing-dropdown");
        postEffectsToggle = root.Q<Toggle>("post-effects-toggle");
        graphicsPresetDropdown = root.Q<DropdownField>("graphics-preset-dropdown");
        
        // Controls
        sensitivitySlider = root.Q<Slider>("sensitivity-slider");
        sensitivityValue = root.Q<Label>("sensitivity-value");
        onscreenJoystickToggle = root.Q<Toggle>("onscreen-joystick-toggle");
        onscreenDPadToggle = root.Q<Toggle>("onscreen-dpad-toggle");
        
        // Buttons
        saveButton = root.Q<Button>("save-button");
        backButton = root.Q<Button>("back-button");
        
        // Confirmation
        confirmationOverlay = root.Q<VisualElement>("confirmation-overlay");
        confirmationTimer = root.Q<Label>("confirmation-timer");
        confirmKeepButton = root.Q<Button>("confirm-keep-button");
        confirmRevertButton = root.Q<Button>("confirm-revert-button");
    }

    private void RegisterCallbacks()
    {
        // Audio sliders
        if (masterVolumeSlider != null)
        {
            masterVolumeSlider.RegisterValueChangedCallback(evt => {
                if (masterVolumeValue != null) masterVolumeValue.text = $"{Mathf.RoundToInt(evt.newValue)}%";
            });
        }
        if (musicVolumeSlider != null)
        {
            musicVolumeSlider.RegisterValueChangedCallback(evt => {
                if (musicVolumeValue != null) musicVolumeValue.text = $"{Mathf.RoundToInt(evt.newValue)}%";
            });
        }
        if (soundVolumeSlider != null)
        {
            soundVolumeSlider.RegisterValueChangedCallback(evt => {
                if (soundVolumeValue != null) soundVolumeValue.text = $"{Mathf.RoundToInt(evt.newValue)}%";
            });
        }
        
        // Sensitivity slider
        if (sensitivitySlider != null)
        {
            sensitivitySlider.RegisterValueChangedCallback(evt => {
                if (sensitivityValue != null) sensitivityValue.text = evt.newValue.ToString("F1");
            });
        }
        
        // Display change tracking
        if (resolutionDropdown != null)
        {
            resolutionDropdown.RegisterValueChangedCallback(evt => hasUnsavedDisplayChanges = true);
        }
        if (windowModeDropdown != null)
        {
            windowModeDropdown.RegisterValueChangedCallback(evt => hasUnsavedDisplayChanges = true);
        }
        
        // Buttons
        if (saveButton != null) saveButton.clicked += OnSaveClicked;
        if (backButton != null) backButton.clicked += OnBackClicked;
        if (confirmKeepButton != null) confirmKeepButton.clicked += OnConfirmKeepClicked;
        if (confirmRevertButton != null) confirmRevertButton.clicked += OnConfirmRevertClicked;
    }

    private void UnregisterCallbacks()
    {
        if (saveButton != null) saveButton.clicked -= OnSaveClicked;
        if (backButton != null) backButton.clicked -= OnBackClicked;
        if (confirmKeepButton != null) confirmKeepButton.clicked -= OnConfirmKeepClicked;
        if (confirmRevertButton != null) confirmRevertButton.clicked -= OnConfirmRevertClicked;
    }

    /// <summary>
    /// Show the options menu
    /// </summary>
    /// <param name="onClose">Callback when menu is closed</param>
    public void Show(System.Action onClose = null)
    {
        onCloseCallback = onClose;
        
        // Populate dropdowns with current values
        PopulateControls();
        
        // Show menu
        if (optionsMenuRoot != null)
        {
            optionsMenuRoot.style.display = DisplayStyle.Flex;
        }
        
        // Hide confirmation overlay
        if (confirmationOverlay != null)
        {
            confirmationOverlay.style.display = DisplayStyle.None;
        }
        
        hasUnsavedDisplayChanges = false;
    }

    /// <summary>
    /// Hide the options menu
    /// </summary>
    public void Hide()
    {
        if (confirmationCoroutine != null)
        {
            StopCoroutine(confirmationCoroutine);
            confirmationCoroutine = null;
        }
        
        if (optionsMenuRoot != null)
        {
            optionsMenuRoot.style.display = DisplayStyle.None;
        }
        
        onCloseCallback?.Invoke();
        onCloseCallback = null;
    }

    private void PopulateControls()
    {
        var settings = SettingsManager.Instance;
        if (settings == null)
        {
            Debug.LogWarning("SettingsManager not found! Using default values.");
            return;
        }
        
        var current = settings.CurrentSettings;
        
        // Audio
        if (masterVolumeSlider != null)
        {
            masterVolumeSlider.value = current.masterVolume * 100f;
            if (masterVolumeValue != null) masterVolumeValue.text = $"{Mathf.RoundToInt(current.masterVolume * 100f)}%";
        }
        if (musicVolumeSlider != null)
        {
            musicVolumeSlider.value = current.musicVolume * 100f;
            if (musicVolumeValue != null) musicVolumeValue.text = $"{Mathf.RoundToInt(current.musicVolume * 100f)}%";
        }
        if (soundVolumeSlider != null)
        {
            soundVolumeSlider.value = current.soundVolume * 100f;
            if (soundVolumeValue != null) soundVolumeValue.text = $"{Mathf.RoundToInt(current.soundVolume * 100f)}%";
        }
        
        // Display dropdowns
        if (resolutionDropdown != null)
        {
            resolutionDropdown.choices = settings.GetResolutionStrings();
            resolutionDropdown.index = current.resolutionIndex;
            previousResolutionIndex = current.resolutionIndex;
        }
        if (windowModeDropdown != null)
        {
            windowModeDropdown.choices = settings.GetWindowModeStrings();
            windowModeDropdown.index = settings.GetWindowModeIndex();
            previousWindowModeIndex = settings.GetWindowModeIndex();
        }
        if (displayDropdown != null)
        {
            displayDropdown.choices = settings.GetDisplayStrings();
            displayDropdown.index = current.displayIndex;
        }
        if (antiAliasingDropdown != null)
        {
            antiAliasingDropdown.choices = settings.GetAntiAliasingStrings();
            antiAliasingDropdown.index = current.antiAliasingLevel;
        }
        if (postEffectsToggle != null)
        {
            postEffectsToggle.value = current.postEffectsEnabled;
        }
        if (graphicsPresetDropdown != null)
        {
            graphicsPresetDropdown.choices = settings.GetGraphicsPresetStrings();
            graphicsPresetDropdown.index = (int)current.graphicsPreset;
        }
        
        // Controls
        if (sensitivitySlider != null)
        {
            sensitivitySlider.value = current.mouseSensitivity;
            if (sensitivityValue != null) sensitivityValue.text = current.mouseSensitivity.ToString("F1");
        }
        if (onscreenJoystickToggle != null)
        {
            onscreenJoystickToggle.value = current.onscreenJoystick;
        }
        if (onscreenDPadToggle != null)
        {
            onscreenDPadToggle.value = current.onscreenDPad;
        }
    }

    private void ApplySettingsFromUI()
    {
        var settings = SettingsManager.Instance;
        if (settings == null) return;
        
        var current = settings.CurrentSettings;
        
        // Audio
        if (masterVolumeSlider != null) current.masterVolume = masterVolumeSlider.value / 100f;
        if (musicVolumeSlider != null) current.musicVolume = musicVolumeSlider.value / 100f;
        if (soundVolumeSlider != null) current.soundVolume = soundVolumeSlider.value / 100f;
        
        // Display
        if (resolutionDropdown != null) current.resolutionIndex = resolutionDropdown.index;
        if (windowModeDropdown != null) settings.SetWindowModeFromIndex(windowModeDropdown.index);
        if (displayDropdown != null) current.displayIndex = displayDropdown.index;
        if (antiAliasingDropdown != null) current.antiAliasingLevel = antiAliasingDropdown.index;
        if (postEffectsToggle != null) current.postEffectsEnabled = postEffectsToggle.value;
        if (graphicsPresetDropdown != null) current.graphicsPreset = (GameSettings.GraphicsPreset)graphicsPresetDropdown.index;
        
        // Controls
        if (sensitivitySlider != null) current.mouseSensitivity = sensitivitySlider.value;
        if (onscreenJoystickToggle != null) current.onscreenJoystick = onscreenJoystickToggle.value;
        if (onscreenDPadToggle != null) current.onscreenDPad = onscreenDPadToggle.value;
    }

    private void OnSaveClicked()
    {
        Debug.Log("Save clicked");
        
        ApplySettingsFromUI();
        
        var settings = SettingsManager.Instance;
        if (settings == null) return;
        
        // If display settings changed, show confirmation dialog
        if (hasUnsavedDisplayChanges)
        {
            // Apply display settings first
            settings.ApplyDisplaySettings();
            
            // Show confirmation with timer
            ShowConfirmationDialog();
        }
        else
        {
            // No display changes, just save and apply everything
            settings.ApplyAllSettings();
            settings.SaveSettings();
            Hide();
        }
    }

    private void OnBackClicked()
    {
        Debug.Log("Back clicked (without saving)");
        
        // Reload settings from saved state to discard changes
        var settings = SettingsManager.Instance;
        if (settings != null)
        {
            settings.LoadSettings();
            settings.ApplyAllSettings();
        }
        
        Hide();
    }

    private void ShowConfirmationDialog()
    {
        if (confirmationOverlay == null) return;
        
        confirmationOverlay.style.display = DisplayStyle.Flex;
        
        // Start countdown coroutine
        if (confirmationCoroutine != null)
        {
            StopCoroutine(confirmationCoroutine);
        }
        confirmationCoroutine = StartCoroutine(ConfirmationCountdown());
    }

    private IEnumerator ConfirmationCountdown()
    {
        float timeRemaining = CONFIRMATION_TIMEOUT;
        
        while (timeRemaining > 0)
        {
            if (confirmationTimer != null)
            {
                confirmationTimer.text = Mathf.CeilToInt(timeRemaining).ToString();
            }
            
            // Use unscaled time since game might be paused
            yield return new WaitForSecondsRealtime(1f);
            timeRemaining -= 1f;
        }
        
        // Time ran out - revert changes
        RevertDisplayChanges();
    }

    private void OnConfirmKeepClicked()
    {
        Debug.Log("Keeping display settings");
        
        // Stop countdown
        if (confirmationCoroutine != null)
        {
            StopCoroutine(confirmationCoroutine);
            confirmationCoroutine = null;
        }
        
        // Hide confirmation
        if (confirmationOverlay != null)
        {
            confirmationOverlay.style.display = DisplayStyle.None;
        }
        
        // Save and apply all settings
        var settings = SettingsManager.Instance;
        if (settings != null)
        {
            settings.ApplyAllSettings();
            settings.SaveSettings();
        }
        
        hasUnsavedDisplayChanges = false;
        Hide();
    }

    private void OnConfirmRevertClicked()
    {
        RevertDisplayChanges();
    }

    private void RevertDisplayChanges()
    {
        Debug.Log("Reverting display settings");
        
        // Stop countdown
        if (confirmationCoroutine != null)
        {
            StopCoroutine(confirmationCoroutine);
            confirmationCoroutine = null;
        }
        
        // Hide confirmation
        if (confirmationOverlay != null)
        {
            confirmationOverlay.style.display = DisplayStyle.None;
        }
        
        // Revert display dropdowns to previous values
        if (resolutionDropdown != null)
        {
            resolutionDropdown.index = previousResolutionIndex;
        }
        if (windowModeDropdown != null)
        {
            windowModeDropdown.index = previousWindowModeIndex;
        }
        
        // Reload and apply saved settings (reverts display changes)
        var settings = SettingsManager.Instance;
        if (settings != null)
        {
            settings.LoadSettings();
            settings.ApplyAllSettings();
        }
        
        hasUnsavedDisplayChanges = false;
    }
}
