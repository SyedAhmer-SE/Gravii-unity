// ============================================================
// AGR_GhostParticles.cs — Adds spooky particle effects
// ============================================================
// Creates swirling dark particles around the ghost
// ATTACH THIS TO: The Ghost GameObject
// ============================================================

using UnityEngine;

public class AGR_GhostParticles : MonoBehaviour
{
    [Header("Skin")]
    [Tooltip("Change this color to skin the Ghost! It will automatically update all aura and trail colors.")]
    public Color ghostSkinColor = new Color(0.6f, 0f, 1f, 1f); // Default Purple

    void Start()
    {
        // === GHOST AURA (Custom Color) ===
        GameObject auraObj = new GameObject("GhostAura");
        auraObj.transform.SetParent(transform);
        auraObj.transform.localPosition = Vector3.zero;

        ParticleSystem aura = auraObj.AddComponent<ParticleSystem>();
        var main = aura.main;
        main.startLifetime = 0.8f;
        main.startSpeed = 2f;
        main.startSize = 0.3f;
        main.maxParticles = 50;
        
        Color baseColorWithAlpha = new Color(ghostSkinColor.r, ghostSkinColor.g, ghostSkinColor.b, 0.5f);
        main.startColor = baseColorWithAlpha;
        main.simulationSpace = ParticleSystemSimulationSpace.World;

        var emission = aura.emission;
        emission.rateOverTime = 30f;

        var shape = aura.shape;
        shape.shapeType = ParticleSystemShapeType.Sphere;
        shape.radius = 0.5f;

        var colorOverLifetime = aura.colorOverLifetime;
        colorOverLifetime.enabled = true;
        Color darkColor = new Color(ghostSkinColor.r * 0.3f, ghostSkinColor.g * 0.3f, ghostSkinColor.b * 0.3f);
        
        Gradient grad = new Gradient();
        grad.SetKeys(
            new GradientColorKey[] {
                new GradientColorKey(ghostSkinColor, 0f),
                new GradientColorKey(darkColor, 1f)
            },
            new GradientAlphaKey[] {
                new GradientAlphaKey(0.5f, 0f),
                new GradientAlphaKey(0f, 1f)
            }
        );
        colorOverLifetime.color = grad;

        var sizeOverLifetime = aura.sizeOverLifetime;
        sizeOverLifetime.enabled = true;
        sizeOverLifetime.size = new ParticleSystem.MinMaxCurve(1f, 0f);

        // Use default particle material
        ParticleSystemRenderer renderer = auraObj.GetComponent<ParticleSystemRenderer>();
        renderer.material = new Material(Shader.Find("Particles/Standard Unlit"));
        renderer.material.color = baseColorWithAlpha;

        // === GHOST TRAIL (dark smoke behind) ===
        TrailRenderer trail = gameObject.AddComponent<TrailRenderer>();
        trail.time = 1.0f;
        trail.startWidth = 0.8f;
        trail.endWidth = 0f;
        Material trailMat = new Material(Shader.Find("Particles/Standard Unlit"));
        trailMat.color = new Color(ghostSkinColor.r, ghostSkinColor.g, ghostSkinColor.b, 0.3f);
        trail.material = trailMat;
        trail.startColor = new Color(ghostSkinColor.r, ghostSkinColor.g, ghostSkinColor.b, 0.6f);
        trail.endColor = new Color(darkColor.r, darkColor.g, darkColor.b, 0f);
        
        // Prevent the "lightning across the map" instantly upon spawning warp
        // Use WaitForSecondsRealtime because Invoke doesn't work at timeScale=0!
        trail.emitting = false;
        trail.Clear();
        StartCoroutine(EnableTrailDelayed(trail, 0.5f));
    }

    private System.Collections.IEnumerator EnableTrailDelayed(TrailRenderer trail, float delay)
    {
        yield return new WaitForSecondsRealtime(delay);
        if (trail != null)
        {
            trail.Clear();
            trail.emitting = true;
        }
    }
}
