// ============================================================
// AGR_SFXManager.cs — Procedural 8-bit Sound Effects!
// ============================================================
// Generates audio clips at runtime using pure math,
// so you don't even need to download .wav or .mp3 files!
// ATTACH THIS TO: GameManager or an empty GameObject
// ============================================================

using UnityEngine;

public class AGR_SFXManager : MonoBehaviour
{
    public static AGR_SFXManager Instance;

    // This automatically creates the SFXManager when the game starts, 
    // without needing to drag it into the Inspector!
    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSceneLoad)]
    private static void AutoInitialize()
    {
        if (Instance == null)
        {
            GameObject obj = new GameObject("AGR_SFXManager");
            Instance = obj.AddComponent<AGR_SFXManager>();
        }
    }

    private AudioSource audioSource;
    private AudioClip jumpClip;
    private AudioClip coinClip;
    private AudioClip crashClip;
    private AudioClip surgeClip;

    private int sampleRate = 44100;

    void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }
        Instance = this;
        DontDestroyOnLoad(gameObject);

        audioSource = gameObject.AddComponent<AudioSource>();
        audioSource.playOnAwake = false;

        GenerateSFX();
    }

    private void GenerateSFX()
    {
        // 1. JUMP: Quick frequency drop (pew!)
        jumpClip = GenerateClip(0.3f, (time, length) =>
        {
            float freq = Mathf.Lerp(800f, 300f, time / length); // Pitch drop
            float envelope = Mathf.Exp(-time * 15f); // Quick fade out
            return Mathf.Sin(2f * Mathf.PI * freq * time) * envelope * 0.5f; // Sine wave
        });

        // 2. COIN: Fast, high-pitched sweep up (bliiing!)
        coinClip = GenerateClip(0.4f, (time, length) =>
        {
            float freq = Mathf.Lerp(1200f, 2000f, time / length); // Pitch goes UP
            float envelope = Mathf.Exp(-time * 10f);
            
            // Add a little tremolo (rapid volume pulsing)
            float tremolo = Mathf.Sin(2f * Mathf.PI * 30f * time);
            
            return Mathf.Sin(2f * Mathf.PI * freq * time) * (0.8f + 0.2f * tremolo) * envelope * 0.4f;
        });

        // 3. CRASH/STRIKE: Burst of noise that decays quickly (pssshhh)
        crashClip = GenerateClip(0.8f, (time, length) =>
        {
            float noise = Random.Range(-1f, 1f); // White noise
            float baseFreq = Mathf.Lerp(150f, 50f, time / length); // Underlying low rumble
            float rumble = Mathf.Sin(2f * Mathf.PI * baseFreq * time);
            
            float envelope = Mathf.Exp(-time * 8f);
            
            return (noise * 0.6f + rumble * 0.4f) * envelope * 0.8f;
        });

        // 4. GHOST SURGE: Terrifying low sweep
        surgeClip = GenerateClip(1.0f, (time, length) =>
        {
            float freq = Mathf.Lerp(200f, 40f, Mathf.Pow(time / length, 2f)); // Parabolic drop
            float envelope = Mathf.Sin(Mathf.PI * (time / length)); // Swell in and out
            float buzz = Mathf.Sign(Mathf.Sin(2f * Mathf.PI * freq * time)); // Square wave for harshness
            
            return buzz * envelope * 0.6f;
        });
    }

    /// <summary>
    /// Helper to mathematically generate an AudioClip
    /// </summary>
    private AudioClip GenerateClip(float duration, System.Func<float, float, float> synthesisFunc)
    {
        int sampleCount = Mathf.CeilToInt(duration * sampleRate);
        float[] samples = new float[sampleCount];

        for (int i = 0; i < sampleCount; i++)
        {
            float time = i / (float)sampleRate;
            samples[i] = synthesisFunc(time, duration);
        }

        AudioClip clip = AudioClip.Create("ProceduralSFX", sampleCount, 1, sampleRate, false);
        clip.SetData(samples, 0);
        return clip;
    }

    // --- PUBLIC PLAY METHODS ---

    public void PlayJump()
    {
        if (AGR_SettingsManager.SFXOn && audioSource != null && jumpClip != null)
            audioSource.PlayOneShot(jumpClip, 0.6f);
    }

    public void PlayCoin()
    {
        if (AGR_SettingsManager.SFXOn && audioSource != null && coinClip != null)
            audioSource.PlayOneShot(coinClip, 0.7f);
    }

    public void PlayCrash()
    {
        if (AGR_SettingsManager.SFXOn && audioSource != null && crashClip != null)
            audioSource.PlayOneShot(crashClip, 0.9f);
    }

    public void PlayGhostSurge()
    {
        if (AGR_SettingsManager.SFXOn && audioSource != null && surgeClip != null)
            audioSource.PlayOneShot(surgeClip, 0.8f);
    }
}
