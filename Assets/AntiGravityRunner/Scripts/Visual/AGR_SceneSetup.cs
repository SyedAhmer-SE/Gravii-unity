// ============================================================
// AGR_SceneSetup.cs — Sets up the dark neon environment
// ============================================================
// Applies dark lighting, fog, and camera settings
// Works WITH the new EndlessGround (doesn't conflict)
// ATTACH THIS TO: An empty GameObject called "SceneSetup"
// ============================================================

using UnityEngine;

public class AGR_SceneSetup : MonoBehaviour
{
    void Start()
    {
        // === DARK BACKGROUND ===
        if (Camera.main != null)
        {
            Camera.main.clearFlags = CameraClearFlags.SolidColor;
            Camera.main.backgroundColor = new Color(0.01f, 0.01f, 0.03f);
            Camera.main.farClipPlane = 200f; // Don't render too far
        }

        // === DIRECTIONAL LIGHT — Dim it for mood ===
        Light[] lights = FindObjectsOfType<Light>();
        foreach (Light light in lights)
        {
            if (light.type == LightType.Directional)
            {
                light.intensity = 0.2f;
                light.color = new Color(0.3f, 0.3f, 0.8f); // Blue-ish
                light.shadowStrength = 0.5f;
            }
        }

        // NOTE: Ground and walls are now handled by AGR_EndlessGround.cs
        // We don't touch "Ground" or "Ceiling" objects here anymore
    }
}
