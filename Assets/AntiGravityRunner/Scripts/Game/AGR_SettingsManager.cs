// ============================================================
// AGR_SettingsManager.cs — Stores all player settings
// ============================================================
// Static class — accessible from anywhere
// Saves settings to PlayerPrefs so they persist
// ============================================================

using UnityEngine;

public static class AGR_SettingsManager
{
    // Control Types
    public enum ControlType
    {
        Buttons,
        Swipe,
        Gyroscope
    }

    // Orientation Modes
    public enum OrientationMode
    {
        Landscape,
        Portrait
    }

    // Current settings
    public static ControlType CurrentControl = ControlType.Buttons;
    public static OrientationMode CurrentOrientation = OrientationMode.Landscape;
    public static float SwipeSensitivity = 1.0f;
    public static float GyroSensitivity = 1.0f;
    public static bool MusicOn = true;
    public static bool SFXOn = true;
    public static float MusicVolume = 0.7f;

    // Save settings
    public static void Save()
    {
        PlayerPrefs.SetInt("ControlType", (int)CurrentControl);
        PlayerPrefs.SetInt("OrientationMode", (int)CurrentOrientation);
        PlayerPrefs.SetFloat("SwipeSensitivity", SwipeSensitivity);
        PlayerPrefs.SetFloat("GyroSensitivity", GyroSensitivity);
        PlayerPrefs.SetInt("MusicOn", MusicOn ? 1 : 0);
        PlayerPrefs.SetInt("SFXOn", SFXOn ? 1 : 0);
        PlayerPrefs.SetFloat("MusicVolume", MusicVolume);
        PlayerPrefs.Save();
    }

    // Load settings
    public static void Load()
    {
        CurrentControl = (ControlType)PlayerPrefs.GetInt("ControlType", 0);
        CurrentOrientation = (OrientationMode)PlayerPrefs.GetInt("OrientationMode", 0);
        SwipeSensitivity = PlayerPrefs.GetFloat("SwipeSensitivity", 1.0f);
        GyroSensitivity = PlayerPrefs.GetFloat("GyroSensitivity", 1.0f);
        MusicOn = PlayerPrefs.GetInt("MusicOn", 1) == 1;
        SFXOn = PlayerPrefs.GetInt("SFXOn", 1) == 1;
        MusicVolume = PlayerPrefs.GetFloat("MusicVolume", 0.7f);

        // Apply orientation immediately on load
        ApplyOrientation();
    }

    /// <summary>
    /// Applies the current orientation setting to the device screen.
    /// </summary>
    public static void ApplyOrientation()
    {
        if (CurrentOrientation == OrientationMode.Portrait)
        {
            Screen.autorotateToPortrait = true;
            Screen.autorotateToPortraitUpsideDown = true;
            Screen.autorotateToLandscapeLeft = false;
            Screen.autorotateToLandscapeRight = false;
            Screen.orientation = ScreenOrientation.Portrait;
        }
        else
        {
            Screen.autorotateToPortrait = false;
            Screen.autorotateToPortraitUpsideDown = false;
            Screen.autorotateToLandscapeLeft = true;
            Screen.autorotateToLandscapeRight = true;
            Screen.orientation = ScreenOrientation.LandscapeLeft;
        }
    }
}
