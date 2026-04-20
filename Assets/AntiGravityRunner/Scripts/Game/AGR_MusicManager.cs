// ============================================================
// AGR_MusicManager.cs — Procedural Synthwave Music!
// ============================================================
// Generates dark electronic music at RUNTIME — no audio files!
// Creates a bass line + pad + hi-hat rhythm
// Matches the neon cyberpunk aesthetic of GRAVI
// Music gets more intense during gameplay
// ATTACH THIS TO: GameManager or "MusicManager" empty object
// ============================================================

using UnityEngine;

public class AGR_MusicManager : MonoBehaviour
{
    [Header("Music Settings")]
    [Tooltip("Leave empty to use procedural music. Assign a track to override.")]
    [SerializeField] private AudioClip customMusicClip;
    
    [Header("Procedural Settings")]
    [SerializeField] private float masterVolume = 0.4f;
    [SerializeField] private float bassVolume = 0.5f;
    [SerializeField] private float padVolume = 0.25f;
    [SerializeField] private float hihatVolume = 0.15f;

    // Audio
    private AudioSource audioSource;
    private float sampleRate;
    private float phase = 0f;
    private float bassPhase = 0f;
    private float padPhase1 = 0f;
    private float padPhase2 = 0f;
    private float padPhase3 = 0f;
    private float hihatPhase = 0f;
    private float globalTime = 0f;

    // Music state
    private bool isPlaying = false;
    private float intensity = 0.5f; // 0 = calm menu, 1 = intense gameplay
    private float targetIntensity = 0.5f;

    // Bass pattern (dark minor key — A minor pentatonic)
    // Note frequencies for a dark synthwave feel
    private float[] bassNotes = new float[]
    {
        55.0f,   // A1
        65.41f,  // C2
        73.42f,  // D2
        82.41f,  // E2
        98.0f,   // G2
        55.0f,   // A1 (repeat)
        73.42f,  // D2
        82.41f,  // E2
    };

    // Pad chord frequencies (Am7 → Dm7 → Em7 → Am7)
    private float[][] padChords = new float[][]
    {
        new float[] { 220f, 261.6f, 329.6f },  // Am (A3, C4, E4)
        new float[] { 146.8f, 174.6f, 220f },   // Dm (D3, F3, A3)
        new float[] { 164.8f, 196f, 246.9f },   // Em (E3, G3, B3)
        new float[] { 220f, 261.6f, 329.6f },   // Am (repeat)
    };

    private int currentBassNote = 0;
    private int currentChord = 0;
    private float beatTimer = 0f;
    private float chordTimer = 0f;
    private float bpm = 120f;
    private float beatDuration;
    private float chordDuration;

    // Singleton
    private static AGR_MusicManager instance;

    void Awake()
    {
        // Singleton pattern — survive scene reloads
        if (instance != null && instance != this)
        {
            Destroy(gameObject);
            return;
        }
        instance = this;
        DontDestroyOnLoad(gameObject);

        sampleRate = AudioSettings.outputSampleRate;
        beatDuration = 60f / bpm;
        chordDuration = beatDuration * 4f; // Change chord every 4 beats

        AGR_SettingsManager.Load();
    }

    void Start()
    {
        // Create AudioSource for audio
        audioSource = gameObject.AddComponent<AudioSource>();
        audioSource.playOnAwake = false;
        audioSource.loop = true;
        audioSource.volume = masterVolume;
        
        // Setup Custom Music if provided
        if (customMusicClip != null)
        {
            audioSource.clip = customMusicClip;
            Debug.Log("AGR_MusicManager: Using custom music track.");
        }
        else
        {
             Debug.Log("AGR_MusicManager: Using procedural music generator.");
        }
    }

    void Update()
    {
        if (audioSource == null) return;

        // Respond to music setting changes natively
        if (AGR_SettingsManager.MusicOn)
        {
            audioSource.volume = masterVolume;
        }
        else
        {
            if (isPlaying) StopMusic();
        }

        // Smoothly transition intensity
        intensity = Mathf.MoveTowards(intensity, targetIntensity, Time.deltaTime * 0.5f);

        // Advance beat and chord timers
        beatTimer += Time.deltaTime;
        chordTimer += Time.deltaTime;

        if (beatTimer >= beatDuration)
        {
            beatTimer -= beatDuration;
            currentBassNote = (currentBassNote + 1) % bassNotes.Length;
        }

        if (chordTimer >= chordDuration)
        {
            chordTimer -= chordDuration;
            currentChord = (currentChord + 1) % padChords.Length;
        }
    }

    public void StartMusic()
    {
        isPlaying = true;
        if (customMusicClip != null && audioSource != null && !audioSource.isPlaying)
        {
            audioSource.Play();
        }
    }

    public void StopMusic()
    {
        isPlaying = false;
        if (customMusicClip != null && audioSource != null && audioSource.isPlaying)
        {
            audioSource.Stop();
        }
    }

    /// <summary>
    /// Call this to make music more/less intense.
    /// 0.3 = calm menu, 0.7 = normal gameplay, 1.0 = high speed / danger
    /// </summary>
    public static void SetIntensity(float value)
    {
        if (instance != null)
        {
            instance.targetIntensity = Mathf.Clamp01(value);
        }
    }

    // ============================================================
    // PROCEDURAL AUDIO GENERATION
    // ============================================================
    // This is called by Unity's audio thread — generates samples!
    // ============================================================
    void OnAudioFilterRead(float[] data, int channels)
    {
        // If we are using a custom clip, bypass the procedural generation entirely
        if (customMusicClip != null) return;
        
        if (!isPlaying) return;

        float dt = 1f / sampleRate;

        for (int i = 0; i < data.Length; i += channels)
        {
            globalTime += dt;
            float sample = 0f;

            // === BASS (Saw wave with low-pass feel) ===
            float bassFreq = bassNotes[currentBassNote];
            bassPhase += bassFreq * dt;
            if (bassPhase > 1f) bassPhase -= 1f;

            // Soft saw wave for bass (triangle-ish for warmth)
            float bassSample = (bassPhase * 2f - 1f); // Saw
            bassSample = bassSample * 0.7f + Mathf.Sin(bassPhase * Mathf.PI * 2f) * 0.3f; // Mix with sine
            
            // Simple envelope per beat for rhythmic pulse
            float beatPos = beatTimer / beatDuration;
            float bassEnv = Mathf.Max(0f, 1f - beatPos * 2f); // Quick decay
            bassEnv = Mathf.Pow(bassEnv, 0.5f); // Shape

            sample += bassSample * bassEnv * bassVolume;

            // === PAD (3 detuned sine waves = lush chord) ===
            float[] chord = padChords[currentChord];

            padPhase1 += chord[0] * dt;
            padPhase2 += chord[1] * dt;
            padPhase3 += chord[2] * dt;

            if (padPhase1 > 1f) padPhase1 -= 1f;
            if (padPhase2 > 1f) padPhase2 -= 1f;
            if (padPhase3 > 1f) padPhase3 -= 1f;

            float padSample = Mathf.Sin(padPhase1 * Mathf.PI * 2f) * 0.33f
                            + Mathf.Sin(padPhase2 * Mathf.PI * 2f) * 0.33f
                            + Mathf.Sin(padPhase3 * Mathf.PI * 2f) * 0.33f;

            // Slow LFO for movement
            float lfo = Mathf.Sin(globalTime * 0.5f * Mathf.PI * 2f) * 0.3f + 0.7f;
            padSample *= lfo;

            sample += padSample * padVolume;

            // === HI-HAT (Noise burst on beats) ===
            float hihatBeatPos = beatTimer / beatDuration;
            float hihatPattern = 0f;

            // 8th note pattern (hit every half-beat)
            float eighthPos = (hihatBeatPos * 2f) % 1f;
            if (eighthPos < 0.1f)
            {
                // Short noise burst
                hihatPattern = Random.Range(-1f, 1f);
                float hihatEnv = 1f - (eighthPos / 0.1f);
                hihatPattern *= hihatEnv;
            }

            // Scale hi-hat with intensity (more hi-hat during gameplay)
            sample += hihatPattern * hihatVolume * intensity;

            // === KICK DRUM (Low sine burst on beat 1 and 3) ===
            if (intensity > 0.4f)
            {
                float kickEnv = Mathf.Max(0f, 1f - beatPos * 6f);
                kickEnv = Mathf.Pow(kickEnv, 2f);
                float kickFreq = 60f * (1f + kickEnv * 2f); // Pitch drop
                float kickSample = Mathf.Sin(globalTime * kickFreq * Mathf.PI * 2f) * kickEnv;
                sample += kickSample * 0.3f * intensity;
            }

            // === MASTER PROCESSING ===
            // Soft clip to prevent harsh distortion
            sample = Mathf.Clamp(sample, -1f, 1f);
            sample *= masterVolume;

            // Scale with overall intensity
            sample *= Mathf.Lerp(0.6f, 1f, intensity);

            // Write to all channels
            for (int ch = 0; ch < channels; ch++)
            {
                data[i + ch] = sample;
            }
        }
    }

    void OnDestroy()
    {
        if (instance == this)
        {
            instance = null;
        }
    }
}
