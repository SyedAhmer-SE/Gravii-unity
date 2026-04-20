// ============================================================
// AGR_UIManager.cs — HUD + Game Over with "Watch Ad" button!
// ============================================================
// Fixed score overlap, added "Watch Ad to Continue" button
// ATTACH THIS TO: UIManager object
// ============================================================

using UnityEngine;
using UnityEngine.UI;

public class AGR_UIManager : MonoBehaviour
{
    [Header("References")]
    [SerializeField] private AGR_GameManager gameManager;

    [Header("Gameplay UI")]
    [SerializeField] private Text scoreText;
    [SerializeField] private Text tapToStartText;

    [Header("Game Over UI")]
    [SerializeField] private GameObject gameOverPanel;
    [SerializeField] private Text finalScoreText;
    [SerializeField] private Text highScoreText;
    [SerializeField] private Button restartButton;

    // Dynamically created "Watch Ad" button
    private Button watchAdButton;
    private GameObject watchAdObj;
    
    // Dynamically created Music Credit text
    private GameObject musicCreditObj;

    // Fullscreen invisible button to fix tap-to-start getting stuck
    private GameObject invisibleTapButtonObj;
    
    // Track if we already logged to avoid spam
    private bool hasLoggedAdWarning = false;

    void Start()
    {
        // Auto-find GameManager if not assigned in Inspector
        if (gameManager == null)
            gameManager = FindObjectOfType<AGR_GameManager>();

        // AGGRESSIVELY reset ALL UI to clean start state
        if (tapToStartText != null) tapToStartText.gameObject.SetActive(true);
        
        // Hide EVERY game-over element individually (they may not be children of gameOverPanel!)
        if (gameOverPanel != null) gameOverPanel.SetActive(false);
        if (finalScoreText != null) finalScoreText.gameObject.SetActive(false);
        if (highScoreText != null) highScoreText.gameObject.SetActive(false);
        if (restartButton != null) restartButton.gameObject.SetActive(false);

        // Hide score text at the start (shows only during gameplay)
        if (scoreText != null) 
        {
            scoreText.text = "0";
            scoreText.gameObject.SetActive(false);
        }

        if (restartButton != null)
        {
            restartButton.onClick.RemoveAllListeners();
            restartButton.onClick.AddListener(OnRestartClicked);
        }

        // Fix positions so texts don't overlap
        FixUIPositions();

        // Create "Watch Ad to Continue" button on the game over panel
        CreateWatchAdButton();
        
        // Create the music credit text for the main menu
        CreateMusicCredit();
        
        // Setup initial menu text
        if (tapToStartText != null)
        {
            tapToStartText.text = "TAP TO START";
        }

        // Fix for "Tap to Start Stuck" — create a full-screen invisible button!
        CreateTapToStartButton();
        
        Debug.Log("AGR_UIManager: Start() complete. gameOverPanel=" + (gameOverPanel != null) + 
                  " finalScore=" + (finalScoreText != null) + " highScore=" + (highScoreText != null) +
                  " restartBtn=" + (restartButton != null));
    }

    private void FixUIPositions()
    {
        if (finalScoreText != null)
        {
            RectTransform rt = finalScoreText.GetComponent<RectTransform>();
            rt.anchoredPosition = new Vector2(0, 100);
            rt.sizeDelta = new Vector2(400, 120); // slightly taller for extra text
            finalScoreText.alignment = TextAnchor.MiddleCenter;
        }

        if (highScoreText != null)
        {
            RectTransform rt = highScoreText.GetComponent<RectTransform>();
            rt.anchoredPosition = new Vector2(0, 30);
            rt.sizeDelta = new Vector2(400, 60);
            highScoreText.alignment = TextAnchor.MiddleCenter;
        }

        if (restartButton != null)
        {
            RectTransform rt = restartButton.GetComponent<RectTransform>();
            rt.anchoredPosition = new Vector2(0, -100);
            rt.sizeDelta = new Vector2(250, 50); // Made slightly wider for MAIN MENU text
            
            Text btnText = restartButton.GetComponentInChildren<Text>();
            if (btnText != null)
            {
                btnText.text = "MAIN MENU";
            }
        }
    }

    private void CreateWatchAdButton()
    {
        if (gameOverPanel == null) return;

        // Create the "Watch Ad" button
        watchAdObj = new GameObject("WatchAdButton");
        watchAdObj.transform.SetParent(gameOverPanel.transform, false);

        RectTransform rt = watchAdObj.AddComponent<RectTransform>();
        rt.anchoredPosition = new Vector2(0, -40);
        rt.sizeDelta = new Vector2(280, 50);

        Image img = watchAdObj.AddComponent<Image>();
        img.color = new Color(0f, 0.6f, 0.3f); // Green button

        watchAdButton = watchAdObj.AddComponent<Button>();
        watchAdButton.onClick.AddListener(OnWatchAdClicked);

        // Button text
        GameObject txtObj = new GameObject("Label");
        txtObj.transform.SetParent(watchAdObj.transform, false);
        RectTransform trt = txtObj.AddComponent<RectTransform>();
        trt.anchorMin = Vector2.zero;
        trt.anchorMax = Vector2.one;
        trt.offsetMin = Vector2.zero;
        trt.offsetMax = Vector2.zero;
        Text txt = txtObj.AddComponent<Text>();
        txt.text = "▶ WATCH AD TO CONTINUE";
        txt.fontSize = 18;
        txt.color = Color.white;
        txt.alignment = TextAnchor.MiddleCenter;
        txt.font = Font.CreateDynamicFontFromOSFont("Arial", 18);

        watchAdObj.SetActive(false); // Hidden until game over
    }

    private void CreateMusicCredit()
    {
        // Add credit text to the main canvas (parent of tapToStartText)
        if (tapToStartText == null) return;
        Transform canvas = tapToStartText.transform.parent;

        musicCreditObj = new GameObject("MusicCredit");
        musicCreditObj.transform.SetParent(canvas, false);

        RectTransform rt = musicCreditObj.AddComponent<RectTransform>();
        rt.anchorMin = new Vector2(0.5f, 0); // Bottom center
        rt.anchorMax = new Vector2(0.5f, 0);
        rt.pivot = new Vector2(0.5f, 0);
        rt.anchoredPosition = new Vector2(0, 10); // 10px from bottom edge
        rt.sizeDelta = new Vector2(600, 40);

        Text txt = musicCreditObj.AddComponent<Text>();
        txt.text = "Music from Uppbeat (free for Creators!)\nhttps://uppbeat.io/t/d0d/voltage | License code: KY3GTGMX2NGHEO6M";
        txt.fontSize = 12;
        txt.color = new Color(1f, 1f, 1f, 0.5f); // Semi-transparent white
        txt.alignment = TextAnchor.MiddleCenter;
        txt.font = Font.CreateDynamicFontFromOSFont("Arial", 12);
    }

    private void CreateTapToStartButton()
    {
        if (tapToStartText == null || gameManager == null) return;
        
        invisibleTapButtonObj = new GameObject("InvisibleTapButton");
        invisibleTapButtonObj.transform.SetParent(tapToStartText.transform.parent, false);
        invisibleTapButtonObj.transform.SetAsLastSibling(); // Ensure it on top of other UI
        
        RectTransform rt = invisibleTapButtonObj.AddComponent<RectTransform>();
        rt.anchorMin = Vector2.zero;
        rt.anchorMax = Vector2.one;
        rt.offsetMin = Vector2.zero;
        rt.offsetMax = Vector2.zero;
        
        Image img = invisibleTapButtonObj.AddComponent<Image>();
        img.color = new Color(0, 0, 0, 0); // Fully transparent
        
        Button btn = invisibleTapButtonObj.AddComponent<Button>();
        btn.onClick.AddListener(() => {
            if (gameManager != null && !gameManager.IsGameStarted)
            {
                gameManager.ForceStartGame();
            }
        });
        
        invisibleTapButtonObj.SetActive(false); // Managed in Update
    }

    void Update()
    {
        if (gameManager == null)
        {
            gameManager = FindObjectOfType<AGR_GameManager>();
            if (gameManager == null) return;
        }

        // Manage invisible button state: active ONLY if menu is closed, game not started, and not dead!
        if (invisibleTapButtonObj != null)
        {
            bool shouldBeActive = !gameManager.IsGameStarted && !gameManager.menuOpen && !gameManager.IsGameOver;
            if (invisibleTapButtonObj.activeSelf != shouldBeActive)
            {
                invisibleTapButtonObj.SetActive(shouldBeActive);
            }
        }

        // === BEFORE GAME STARTS: Show "TAP TO START", KILL everything else ===
        if (!gameManager.IsGameStarted && !gameManager.IsGameOver)
        {
            // ONLY show tap to start and music credit if the Main Menu is actually closed!
            if (!gameManager.menuOpen)
            {
                if (tapToStartText != null && !tapToStartText.gameObject.activeSelf)
                    tapToStartText.gameObject.SetActive(true);
                if (tapToStartText != null)
                    tapToStartText.text = "TAP TO START";
                if (musicCreditObj != null) musicCreditObj.SetActive(true);
            }
            else
            {
                if (tapToStartText != null && tapToStartText.gameObject.activeSelf)
                    tapToStartText.gameObject.SetActive(false);
                if (musicCreditObj != null) musicCreditObj.SetActive(false);
            }
            
            // FORCE HIDE every game-over element every frame (they are NOT children of gameOverPanel!)
            if (scoreText != null) scoreText.gameObject.SetActive(false);
            if (gameOverPanel != null) gameOverPanel.SetActive(false);
            if (finalScoreText != null) finalScoreText.gameObject.SetActive(false);
            if (highScoreText != null) highScoreText.gameObject.SetActive(false);
            if (restartButton != null) restartButton.gameObject.SetActive(false);
            if (watchAdObj != null) watchAdObj.SetActive(false);
        }

        // === DURING GAMEPLAY: Show score, hide everything else ===
        if (gameManager.IsGameStarted && !gameManager.IsGameOver)
        {
            if (tapToStartText != null && tapToStartText.gameObject.activeSelf)
                tapToStartText.gameObject.SetActive(false);
            
            if (musicCreditObj != null && musicCreditObj.activeSelf)
                musicCreditObj.SetActive(false);

            if (scoreText != null)
            {
                if (!scoreText.gameObject.activeSelf)
                    scoreText.gameObject.SetActive(true);
                scoreText.text = gameManager.Score.ToString() + "\nCoins: " + gameManager.CurrentCoins;
            }
            
            // FORCE HIDE game over elements during gameplay too
            if (gameOverPanel != null) gameOverPanel.SetActive(false);
            if (finalScoreText != null) finalScoreText.gameObject.SetActive(false);
            if (highScoreText != null) highScoreText.gameObject.SetActive(false);
            if (restartButton != null) restartButton.gameObject.SetActive(false);
            if (watchAdObj != null) watchAdObj.SetActive(false);
        }

        if (gameManager.IsGameOver)
        {
            // HIDE live score and tap text
            if (scoreText != null && scoreText.gameObject.activeSelf)
                scoreText.gameObject.SetActive(false);
            if (tapToStartText != null && tapToStartText.gameObject.activeSelf)
                tapToStartText.gameObject.SetActive(false);

            // SHOW each game-over element individually
            if (gameOverPanel != null && !gameOverPanel.activeSelf)
                gameOverPanel.SetActive(true);

            if (finalScoreText != null && !finalScoreText.gameObject.activeSelf)
            {
                finalScoreText.gameObject.SetActive(true);
                finalScoreText.text = "Score: " + gameManager.Score + "\nCoins: " + gameManager.CurrentCoins;
            }

            if (highScoreText != null && !highScoreText.gameObject.activeSelf)
            {
                highScoreText.gameObject.SetActive(true);
                highScoreText.text = "Best: " + gameManager.HighScore;
            }

            if (restartButton != null && !restartButton.gameObject.activeSelf)
                restartButton.gameObject.SetActive(true);

            // Show "Watch Ad" button only if continue hasn't been used yet
            if (watchAdObj != null)
            {
                if (AGR_AdsManager.Instance == null)
                {
                    if (!hasLoggedAdWarning)
                    {
                        Debug.LogWarning("AGR_UIManager: AdsManager is Missing! Not showing 'Watch Ad' button. Provide an AGR_AdsManager in the scene.");
                        hasLoggedAdWarning = true;
                    }
                    watchAdObj.SetActive(false);
                }
                else if (!AGR_AdsManager.Instance.IsRewardedAdReady())
                {
                    if (!hasLoggedAdWarning)
                    {
                        Debug.LogWarning("AGR_UIManager: AdsManager found, but Ads aren't ready yet! (Requires Internet and Unity Ads properly set up).");
                        hasLoggedAdWarning = true;
                    }
                    watchAdObj.SetActive(false);
                }
                else
                {
                    bool canContinue = !gameManager.HasUsedContinue;
                    watchAdObj.SetActive(canContinue);
                    // Reset flag so next time it can log if it fails
                    hasLoggedAdWarning = false; 
                }
            }
        }
    }

    private void OnRestartClicked()
    {
        // User requested: Death -> Main Menu -> Tap To Start
        // So we reset the flag, meaning the next scene load WILL show the Main Menu!
        AGR_GameManager.hasPlayedOnce = false;
        gameManager.RestartGame();
    }

    private void OnWatchAdClicked()
    {
        if (AGR_AdsManager.Instance != null)
        {
            AGR_AdsManager.Instance.ShowRewardedAd(() =>
            {
                // Ad completed — continue the game!
                if (gameManager != null)
                {
                    gameManager.ContinueAfterAd();

                    // Hide game over panel
                    if (gameOverPanel != null)
                        gameOverPanel.SetActive(false);

                    // Show score again
                    if (scoreText != null)
                        scoreText.gameObject.SetActive(true);
                }
            });
        }
    }
}
