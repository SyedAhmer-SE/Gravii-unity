// ============================================================
// AGR_GameManager.cs — Controls the entire game flow
// ============================================================
// Handles: starting, game over, restarting, scoring
// Now integrates with ads and music intensity
// ATTACH THIS TO: GameManager object
// ============================================================

using UnityEngine;
using UnityEngine.SceneManagement;

public class AGR_GameManager : MonoBehaviour
{
    [Header("References")]
    [SerializeField] private AGR_PlayerController player;

    // Game state
    private bool gameStarted = false;
    private bool gameOver = false;
    public bool menuOpen = true; // Controlled dynamically on Start
    public static bool hasPlayedOnce = false; // Remembers across scene unloads!
    private bool hasUsedContinue = false; // Only 1 free continue per run

    // Score
    private float score = 0f;
    private int highScore = 0;
    private string deathMessage = "";
    
    // Coins
    private int currentCoins = 0;
    private int totalLifetimeCoins = 0;

    // Public properties
    public bool IsGameStarted => gameStarted;
    public bool IsGameOver => gameOver;
    public int Score => Mathf.FloorToInt(score);
    public int HighScore => highScore;
    public string DeathMessage => deathMessage;
    public bool HasUsedContinue => hasUsedContinue;
    public int CurrentCoins => currentCoins;
    public int TotalLifetimeCoins => totalLifetimeCoins;

    void Awake()
    {
        // Force 60 FPS on mobile devices to prevent lag
        Application.targetFrameRate = 60;

        // Auto-find player if not assigned in Inspector
        if (player == null)
            player = FindObjectOfType<AGR_PlayerController>();
    }

    void Start()
    {
        highScore = PlayerPrefs.GetInt("HighScore", 0);
        totalLifetimeCoins = PlayerPrefs.GetInt("TotalCoins", 0);
        
        // If we've already opened the app and hit PLAY once, we NEVER want the main menu to reappear on Restart.
        if (hasPlayedOnce)
        {
            menuOpen = false;
        }

        // Only stop time if the menu is actually open
        if (menuOpen)
        {
            Time.timeScale = 0f;
            
            // Ensure music is STOPPED while on the main menu, and set it to calm
            AGR_MusicManager mm = FindObjectOfType<AGR_MusicManager>();
            if (mm != null) mm.StopMusic();
            AGR_MusicManager.SetIntensity(0.3f);
        }
        else
        {
            // Bypassing menu directly to "Tap To Start" screen
            Time.timeScale = 1f; 
        }

        // FAILSAFE: If there is no Main Menu in the scene, ensure game doesn't get stuck
        AGR_MainMenuUI mainMenu = FindObjectOfType<AGR_MainMenuUI>();
        if (mainMenu == null || !mainMenu.isActiveAndEnabled)
            menuOpen = false;

        Debug.Log("AGR_GameManager: Start() complete. menuOpen=" + menuOpen + 
                   " gameStarted=" + gameStarted + " gameOver=" + gameOver +
                   " timeScale=" + Time.timeScale);
    }

    void Update()
    {
        // DON'T process game input if menu is still open!
        if (menuOpen) return;

        // BEFORE game starts: wait for first tap
        if (!gameStarted && !gameOver)
        {
            // Check for ANY input at all
            bool tapped = false;

            // Mouse click (works in Editor)
            if (Input.GetMouseButtonDown(0)) tapped = true;
            // Keyboard
            if (Input.GetKeyDown(KeyCode.Space)) tapped = true;
            // Mobile buttons
            if (AGR_MobileButtons.jumpPressed) tapped = true;
            // Touch (works on device)
            if (Input.touchCount > 0 && Input.GetTouch(0).phase == TouchPhase.Began) tapped = true;

            if (tapped)
            {
                Debug.Log("AGR_GameManager: Tap detected! Starting game...");
                ForceStartGame();
            }
            return;
        }

        // DURING game: count score & adjust music intensity
        if (gameStarted && !gameOver)
        {
            score += Time.deltaTime * 10f;

            // Music gets more intense as score rises
            float musicIntensity = Mathf.Clamp01(0.5f + (score / 500f));
            AGR_MusicManager.SetIntensity(musicIntensity);
        }

        // GAME OVER: check for restart via keyboard
        if (gameOver)
        {
            if (Input.GetKeyDown(KeyCode.R))
            {
                RestartGame();
            }
        }
    }

    public void ForceStartGame()
    {
        if (gameStarted)
        {
            Debug.Log("AGR_GameManager: ForceStartGame called but game already started!");
            return;
        }

        gameStarted = true;
        menuOpen = false;
        Time.timeScale = 1f; // FORCIBLY RESUME TIME!

        // Auto-find player if reference was lost during scene reload
        if (player == null)
        {
            player = FindObjectOfType<AGR_PlayerController>();
        }

        if (player != null)
        {
            player.GameStarted = true;
        }
        else
        {
            Debug.LogError("AGR_GameManager: Player reference is NULL! Cannot start game properly.");
        }
        
        // START the music when the game actually begins!
        AGR_MusicManager mm = FindObjectOfType<AGR_MusicManager>();
        if (mm != null && AGR_SettingsManager.MusicOn) mm.StartMusic();
        
        AGR_MusicManager.SetIntensity(0.6f);
        Debug.Log("AGR_GameManager: Game Started! player=" + (player != null));
    }

    public void GameOver()
    {
        GameOver("GAME OVER");
    }

    public void GameOver(string message)
    {
        if (gameOver) return;

        gameOver = true;
        deathMessage = message;
        
        if (player != null) player.Die();

        int finalScore = Mathf.FloorToInt(score);
        if (finalScore > highScore)
        {
            highScore = finalScore;
            PlayerPrefs.SetInt("HighScore", highScore);
            PlayerPrefs.Save();
        }

        // Trigger ad on death (counts up to 3)
        if (AGR_AdsManager.Instance != null)
        {
            AGR_AdsManager.Instance.OnPlayerDeath();
        }

        // Save Coins!
        totalLifetimeCoins += currentCoins;
        PlayerPrefs.SetInt("TotalCoins", totalLifetimeCoins);
        PlayerPrefs.Save();

        // Music stops on death
        AGR_MusicManager mm = FindObjectOfType<AGR_MusicManager>();
        if (mm != null) mm.StopMusic();

        Debug.Log(message + " | Score: " + finalScore + " | Coins Earned: " + currentCoins);
    }
    
    public void AddCoins(int amount)
    {
        if (gameOver) return;
        currentCoins += amount;
    }

    /// <summary>
    /// Called after watching a rewarded ad — continue the run!
    /// </summary>
    public void ContinueAfterAd()
    {
        if (!gameOver || hasUsedContinue) return;

        hasUsedContinue = true;
        gameOver = false;
        deathMessage = "";

        // Revive the player
        if (player != null)
        {
            // Reset player death state but keep position/score
            player.GameStarted = true;
            Vector3 currentPos = player.transform.position;
            player.ResetPlayer(currentPos);
            player.GameStarted = true;
        }

        // Resume music if continued
        AGR_MusicManager mm = FindObjectOfType<AGR_MusicManager>();
        if (mm != null && AGR_SettingsManager.MusicOn) mm.StartMusic();

        Debug.Log("Continued after ad! Score preserved: " + Score);
    }

    public void RestartGame()
    {
        Debug.Log("AGR_GameManager: RestartGame called. Reloading scene...");
        // Reset time scale before reloading so the new scene isn't frozen
        Time.timeScale = 1f;
        
        // CRITICAL FIX: Because GameManager is sharing a GameObject with DontDestroyOnLoad scripts 
        // (like AdsManager), it survives scene reloads and its Start() never runs again!
        // We MUST manually clear the game state here so the next run starts fresh.
        gameOver = false;
        gameStarted = false;
        hasUsedContinue = false;
        score = 0f;
        currentCoins = 0;
        menuOpen = false; // Bypass the "Main Menu" on restart, go straight to tap-to-start
        deathMessage = "";

        SceneManager.LoadScene(SceneManager.GetActiveScene().buildIndex);
    }
}
