using UnityEngine;

/// <summary>
/// ScriptableObject that stores all game settings.
/// Create an instance via Assets > Create > MyHome > Game Settings
/// </summary>
[CreateAssetMenu(fileName = "GameSettings", menuName = "MyHome/Game Settings")]
public class GameSettings : ScriptableObject
{
    [Header("Audio")]
    [Range(0f, 1f)] public float masterVolume = 1f;
    [Range(0f, 1f)] public float musicVolume = 1f;
    [Range(0f, 1f)] public float soundVolume = 1f;

    [Header("Display")]
    public int resolutionIndex = 0;
    public FullScreenMode windowMode = FullScreenMode.FullScreenWindow;
    public int displayIndex = 0;
    public int antiAliasingLevel = 0; // 0=Off, 1=2x, 2=4x, 3=8x
    public bool postEffectsEnabled = true;
    public GraphicsPreset graphicsPreset = GraphicsPreset.PC;

    [Header("Controls")]
    [Range(0.1f, 5f)] public float mouseSensitivity = 1f;
    public bool onscreenJoystick = false;
    public bool onscreenDPad = false;

    public enum GraphicsPreset
    {
        Mobile,
        PC
    }

    /// <summary>
    /// Creates a copy of current settings for temporary editing
    /// </summary>
    public GameSettings Clone()
    {
        var clone = CreateInstance<GameSettings>();
        clone.masterVolume = masterVolume;
        clone.musicVolume = musicVolume;
        clone.soundVolume = soundVolume;
        clone.resolutionIndex = resolutionIndex;
        clone.windowMode = windowMode;
        clone.displayIndex = displayIndex;
        clone.antiAliasingLevel = antiAliasingLevel;
        clone.postEffectsEnabled = postEffectsEnabled;
        clone.graphicsPreset = graphicsPreset;
        clone.mouseSensitivity = mouseSensitivity;
        clone.onscreenJoystick = onscreenJoystick;
        clone.onscreenDPad = onscreenDPad;
        return clone;
    }

    /// <summary>
    /// Copies values from another settings instance
    /// </summary>
    public void CopyFrom(GameSettings other)
    {
        masterVolume = other.masterVolume;
        musicVolume = other.musicVolume;
        soundVolume = other.soundVolume;
        resolutionIndex = other.resolutionIndex;
        windowMode = other.windowMode;
        displayIndex = other.displayIndex;
        antiAliasingLevel = other.antiAliasingLevel;
        postEffectsEnabled = other.postEffectsEnabled;
        graphicsPreset = other.graphicsPreset;
        mouseSensitivity = other.mouseSensitivity;
        onscreenJoystick = other.onscreenJoystick;
        onscreenDPad = other.onscreenDPad;
    }
}
