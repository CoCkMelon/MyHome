using UnityEngine;
using UnityEngine.Audio;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;
using System.Collections.Generic;

/// <summary>
/// Singleton manager for game settings. Handles persistence via PlayerPrefs
/// and applies settings to game systems (audio, graphics, etc.)
/// </summary>
public class SettingsManager : MonoBehaviour
{
    public static SettingsManager Instance { get; private set; }

    [Header("References")]
    [SerializeField] private GameSettings defaultSettings;
    [SerializeField] private AudioMixer audioMixer;
    [SerializeField] private Volume postProcessVolume;

    [Header("Runtime")]
    public GameSettings CurrentSettings { get; private set; }

    // Cached resolution list
    private List<Resolution> availableResolutions = new List<Resolution>();
    public IReadOnlyList<Resolution> AvailableResolutions => availableResolutions;

    // Events
    public System.Action OnSettingsApplied;
    public System.Action<float> OnSensitivityChanged;

    private const string PREFS_KEY = "GameSettings";

    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }
        Instance = this;
        DontDestroyOnLoad(gameObject);

        // Create runtime settings instance
        if (defaultSettings != null)
        {
            CurrentSettings = defaultSettings.Clone();
        }
        else
        {
            CurrentSettings = ScriptableObject.CreateInstance<GameSettings>();
        }

        CacheResolutions();
        LoadSettings();
        ApplyAllSettings();
    }

    private void CacheResolutions()
    {
        availableResolutions.Clear();
        var resolutions = Screen.resolutions;
        
        // Filter to unique resolution sizes (ignore refresh rates for simplicity)
        HashSet<string> seen = new HashSet<string>();
        foreach (var res in resolutions)
        {
            string key = $"{res.width}x{res.height}";
            if (!seen.Contains(key))
            {
                seen.Add(key);
                availableResolutions.Add(res);
            }
        }
    }

    public List<string> GetResolutionStrings()
    {
        var strings = new List<string>();
        foreach (var res in availableResolutions)
        {
            strings.Add($"{res.width} x {res.height}");
        }
        return strings;
    }

    public List<string> GetDisplayStrings()
    {
        var strings = new List<string>();
        for (int i = 0; i < Display.displays.Length; i++)
        {
            strings.Add($"Display {i + 1}");
        }
        return strings;
    }

    public List<string> GetWindowModeStrings()
    {
        return new List<string>
        {
            "Fullscreen",
            "Windowed",
            "Borderless Window"
        };
    }

    public List<string> GetAntiAliasingStrings()
    {
        return new List<string>
        {
            "Off",
            "2x MSAA",
            "4x MSAA",
            "8x MSAA"
        };
    }

    public List<string> GetGraphicsPresetStrings()
    {
        return new List<string>
        {
            "Mobile",
            "PC"
        };
    }

    /// <summary>
    /// Apply all current settings to game systems
    /// </summary>
    public void ApplyAllSettings()
    {
        ApplyAudioSettings();
        ApplyDisplaySettings();
        ApplyGraphicsSettings();
        ApplyControlSettings();
        OnSettingsApplied?.Invoke();
    }

    public void ApplyAudioSettings()
    {
        if (audioMixer != null)
        {
            // AudioMixer uses logarithmic scale: -80dB to 0dB
            float masterDb = CurrentSettings.masterVolume > 0.0001f 
                ? Mathf.Log10(CurrentSettings.masterVolume) * 20f 
                : -80f;
            float musicDb = CurrentSettings.musicVolume > 0.0001f 
                ? Mathf.Log10(CurrentSettings.musicVolume) * 20f 
                : -80f;
            float soundDb = CurrentSettings.soundVolume > 0.0001f 
                ? Mathf.Log10(CurrentSettings.soundVolume) * 20f 
                : -80f;

            audioMixer.SetFloat("MasterVolume", masterDb);
            audioMixer.SetFloat("MusicVolume", musicDb);
            audioMixer.SetFloat("SFXVolume", soundDb);
        }
        else
        {
            // Fallback: use AudioListener.volume for master
            AudioListener.volume = CurrentSettings.masterVolume;
        }
    }

    public void ApplyDisplaySettings()
    {
        // Apply resolution
        if (CurrentSettings.resolutionIndex >= 0 && 
            CurrentSettings.resolutionIndex < availableResolutions.Count)
        {
            var res = availableResolutions[CurrentSettings.resolutionIndex];
            Screen.SetResolution(res.width, res.height, CurrentSettings.windowMode);
        }

        // Apply display (multi-monitor) - only works in standalone builds
#if !UNITY_WEBGL
        if (CurrentSettings.displayIndex >= 0 && 
            CurrentSettings.displayIndex < Display.displays.Length)
        {
            // Note: Changing displays at runtime has limitations
            // This primarily works when launching the application
        }
#endif
    }

    public void ApplyGraphicsSettings()
    {
        // Apply anti-aliasing
        int msaaLevel = CurrentSettings.antiAliasingLevel switch
        {
            1 => 2,
            2 => 4,
            3 => 8,
            _ => 1 // 0 = off, use 1 (no MSAA)
        };
        QualitySettings.antiAliasing = msaaLevel;

        // Apply graphics preset
        if (CurrentSettings.graphicsPreset == GameSettings.GraphicsPreset.Mobile)
        {
            QualitySettings.SetQualityLevel(0, true); // Lowest quality
        }
        else
        {
            QualitySettings.SetQualityLevel(QualitySettings.names.Length - 1, true); // Highest quality
        }

        // Apply post-processing
        if (postProcessVolume != null)
        {
            postProcessVolume.enabled = CurrentSettings.postEffectsEnabled;
        }
    }

    public void ApplyControlSettings()
    {
        // Apply mouse sensitivity to CameraRotation if it exists
        var cameraRotation = FindFirstObjectByType<CameraRotation>();
        if (cameraRotation != null)
        {
            cameraRotation.sensitivity = CurrentSettings.mouseSensitivity;
        }

        OnSensitivityChanged?.Invoke(CurrentSettings.mouseSensitivity);

        // Onscreen controls would be handled by the mobile input UI
        // This would typically enable/disable UI elements for touch controls
    }

    /// <summary>
    /// Save current settings to PlayerPrefs
    /// </summary>
    public void SaveSettings()
    {
        string json = JsonUtility.ToJson(CurrentSettings);
        PlayerPrefs.SetString(PREFS_KEY, json);
        PlayerPrefs.Save();
        Debug.Log("Settings saved");
    }

    /// <summary>
    /// Load settings from PlayerPrefs
    /// </summary>
    public void LoadSettings()
    {
        if (PlayerPrefs.HasKey(PREFS_KEY))
        {
            string json = PlayerPrefs.GetString(PREFS_KEY);
            JsonUtility.FromJsonOverwrite(json, CurrentSettings);
            Debug.Log("Settings loaded from PlayerPrefs");
        }
        else if (defaultSettings != null)
        {
            CurrentSettings.CopyFrom(defaultSettings);
            Debug.Log("Using default settings");
        }
    }

    /// <summary>
    /// Reset to default settings
    /// </summary>
    public void ResetToDefaults()
    {
        if (defaultSettings != null)
        {
            CurrentSettings.CopyFrom(defaultSettings);
        }
        else
        {
            // Create fresh default settings
            var fresh = ScriptableObject.CreateInstance<GameSettings>();
            CurrentSettings.CopyFrom(fresh);
            Destroy(fresh);
        }
        ApplyAllSettings();
    }

    /// <summary>
    /// Get the current resolution index that matches screen state
    /// </summary>
    public int GetCurrentResolutionIndex()
    {
        int width = Screen.width;
        int height = Screen.height;
        
        for (int i = 0; i < availableResolutions.Count; i++)
        {
            if (availableResolutions[i].width == width && 
                availableResolutions[i].height == height)
            {
                return i;
            }
        }
        return 0;
    }

    /// <summary>
    /// Get window mode index (0=Fullscreen, 1=Windowed, 2=Borderless)
    /// </summary>
    public int GetWindowModeIndex()
    {
        return CurrentSettings.windowMode switch
        {
            FullScreenMode.ExclusiveFullScreen => 0,
            FullScreenMode.FullScreenWindow => 2,
            FullScreenMode.Windowed => 1,
            _ => 0
        };
    }

    /// <summary>
    /// Set window mode from index
    /// </summary>
    public void SetWindowModeFromIndex(int index)
    {
        CurrentSettings.windowMode = index switch
        {
            0 => FullScreenMode.ExclusiveFullScreen,
            1 => FullScreenMode.Windowed,
            2 => FullScreenMode.FullScreenWindow,
            _ => FullScreenMode.FullScreenWindow
        };
    }
}
