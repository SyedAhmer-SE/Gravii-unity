// ============================================================
// AGR_AdsManager.cs — Unity Ads Integration
// ============================================================
// Handles: Banner, Interstitial, and Rewarded ads
// Shows interstitial every N deaths
// Rewarded ad = "Watch Ad to Continue" after game over
//
// *** SETUP INSTRUCTIONS ***
// 1. In Unity: Window → Package Manager → Unity Registry
// 2. Search "Advertisement" and install "Advertisement Legacy"
// 3. Go to https://dashboard.unity3d.com
// 4. Create project, enable Ads, get your Game IDs
// 5. Replace the placeholder IDs below
//
// ATTACH THIS TO: GameManager or a new "AdsManager" object
// ============================================================

using UnityEngine;
using UnityEngine.Advertisements;

public class AGR_AdsManager : MonoBehaviour, IUnityAdsInitializationListener, IUnityAdsLoadListener, IUnityAdsShowListener
{
    [Header("Ad Settings")]
    [Tooltip("Show interstitial ad every N deaths")]
    [SerializeField] private int deathsPerInterstitial = 10;

    [Header("Game IDs — REPLACE WITH YOUR OWN!")]
    [SerializeField] private string androidGameId = "6090767";
    [SerializeField] private string iosGameId = "YOUR_IOS_GAME_ID";
    private bool testMode = false; // HARDCODED: Always run real ads

    // Ad unit IDs (Unity default)
    private string interstitialAdUnit = "Interstitial_Android";
    private string rewardedAdUnit = "Rewarded_Android";
    private string bannerAdUnit = "Banner_Android";

    private static AGR_AdsManager instance;
    private int deathCount = 0;
    private bool rewardedAdReady = false;
    private System.Action onRewardedAdComplete;

    // Track which ad we WANT to show when it finishes loading
    private bool wantToShowInterstitial = false;
    private bool wantToShowRewarded = false;

    // Singleton
    public static AGR_AdsManager Instance => instance;

    void Awake()
    {

        if (instance != null && instance != this)
        {
            Destroy(gameObject);
            return;
        }
        instance = this;
        DontDestroyOnLoad(gameObject);

        // Set correct ad unit IDs based on platform
        #if UNITY_IOS
            interstitialAdUnit = "Interstitial_iOS";
            rewardedAdUnit = "Rewarded_iOS";
            bannerAdUnit = "Banner_iOS";
        #endif
    }

    void Start()
    {
        InitializeAds();
    }

    // ==================== INITIALIZATION ====================
    private void InitializeAds()
    {
        string gameId = "";

        #if UNITY_ANDROID
            gameId = androidGameId;
        #elif UNITY_IOS
            gameId = iosGameId;
        #else
            gameId = androidGameId; // Fallback for editor testing
        #endif

        if (gameId == "YOUR_ANDROID_GAME_ID" || gameId == "YOUR_IOS_GAME_ID")
        {
            Debug.LogWarning("AGR_AdsManager: Please set your Unity Ads Game ID! " +
                "Go to https://dashboard.unity3d.com to get one.");
            return;
        }

        Advertisement.Initialize(gameId, testMode, this);
        Debug.Log("AGR_AdsManager: Initializing ads with Game ID: " + gameId);
    }

    // ==================== INTERSTITIAL ADS ====================
    /// <summary>
    /// Call this on each death. Shows interstitial every N deaths.
    /// </summary>
    public void OnPlayerDeath()
    {
        deathCount++;
        Debug.Log("AGR_AdsManager: Death #" + deathCount + " / " + deathsPerInterstitial);

        if (deathCount >= deathsPerInterstitial)
        {
            deathCount = 0;
            ShowInterstitialAd();
        }
    }

    public void ShowInterstitialAd()
    {
        Debug.Log("AGR_AdsManager: Requesting interstitial ad...");

        if (Advertisement.isInitialized)
        {
            wantToShowInterstitial = true;
            Advertisement.Load(interstitialAdUnit, this);
        }
        else
        {
            Debug.LogWarning("AGR_AdsManager: Ads not initialized yet!");
        }
    }

    // ==================== REWARDED ADS ====================
    /// <summary>
    /// Shows a rewarded ad. Calls onComplete when user finishes watching.
    /// Use this for "Watch Ad to Continue" feature.
    /// </summary>
    public void ShowRewardedAd(System.Action onComplete)
    {
        onRewardedAdComplete = onComplete;
        Debug.Log("AGR_AdsManager: Requesting rewarded ad...");

        if (Advertisement.isInitialized)
        {
            wantToShowRewarded = true;
            Advertisement.Load(rewardedAdUnit, this);
        }

        // FOR TESTING: Simulate ad completion after 1 second
        if (testMode)
        {
            Debug.Log("AGR_AdsManager: [TEST MODE] Simulating rewarded ad completion...");
            StartCoroutine(SimulateRewardedAd());
        }
    }

    private System.Collections.IEnumerator SimulateRewardedAd()
    {
        yield return new WaitForSecondsRealtime(1f);
        Debug.Log("AGR_AdsManager: [TEST MODE] Rewarded ad completed! Granting reward.");
        onRewardedAdComplete?.Invoke();
    }

    /// <summary>
    /// Returns true if a rewarded ad is ready to show.
    /// </summary>
    public bool IsRewardedAdReady()
    {
        return Advertisement.isInitialized;
    }

    // ==================== BANNER ADS ====================
    public void ShowBannerAd()
    {
        Debug.Log("AGR_AdsManager: Showing banner ad...");

        if (Advertisement.isInitialized)
        {
            Advertisement.Banner.SetPosition(BannerPosition.BOTTOM_CENTER);
            Advertisement.Banner.Load(bannerAdUnit);
            Advertisement.Banner.Show(bannerAdUnit);
        }
    }

    public void HideBannerAd()
    {
        Advertisement.Banner.Hide();
    }

    // ==================== AD CALLBACKS ====================

    // --- Initialization ---
    public void OnInitializationComplete()
    {
        Debug.Log("Unity Ads initialized successfully!");
        // Pre-load ads so they're ready when needed
        Advertisement.Load(interstitialAdUnit, this);
        Advertisement.Load(rewardedAdUnit, this);
    }

    public void OnInitializationFailed(UnityAdsInitializationError error, string message)
    {
        Debug.LogError("Unity Ads init failed: " + error + " - " + message);
    }

    // --- Loading ---
    public void OnUnityAdsAdLoaded(string adUnitId)
    {
        Debug.Log("AGR_AdsManager: Ad loaded: " + adUnitId);

        if (adUnitId == rewardedAdUnit)
        {
            rewardedAdReady = true;
        }

        // Show the ad ONLY if we explicitly requested it
        if (adUnitId == interstitialAdUnit && wantToShowInterstitial)
        {
            wantToShowInterstitial = false;
            Debug.Log("AGR_AdsManager: Showing interstitial now!");
            Advertisement.Show(interstitialAdUnit, this);
        }
        else if (adUnitId == rewardedAdUnit && wantToShowRewarded)
        {
            wantToShowRewarded = false;
            Debug.Log("AGR_AdsManager: Showing rewarded ad now!");
            Advertisement.Show(rewardedAdUnit, this);
        }
    }

    public void OnUnityAdsFailedToLoad(string adUnitId, UnityAdsLoadError error, string message)
    {
        Debug.LogError("AGR_AdsManager: Ad load failed: " + adUnitId + " - " + error + " - " + message);
        // Reset flags so we don't get stuck
        wantToShowInterstitial = false;
        wantToShowRewarded = false;
    }

    // --- Showing ---
    public void OnUnityAdsShowFailure(string adUnitId, UnityAdsShowError error, string message)
    {
        Debug.LogError("AGR_AdsManager: Ad show failed: " + adUnitId + " - " + error + " - " + message);
    }

    public void OnUnityAdsShowStart(string adUnitId)
    {
        Debug.Log("AGR_AdsManager: Ad started: " + adUnitId);
    }

    public void OnUnityAdsShowClick(string adUnitId)
    {
        Debug.Log("AGR_AdsManager: Ad clicked: " + adUnitId);
    }

    public void OnUnityAdsShowComplete(string adUnitId, UnityAdsShowCompletionState showCompletionState)
    {
        Debug.Log("AGR_AdsManager: Ad completed: " + adUnitId + " - " + showCompletionState);

        if (adUnitId == rewardedAdUnit && showCompletionState == UnityAdsShowCompletionState.COMPLETED)
        {
            // User watched full ad — grant reward!
            onRewardedAdComplete?.Invoke();
        }

        // Pre-load the next ad
        Advertisement.Load(adUnitId, this);
    }

    void OnDestroy()
    {
        if (instance == this) instance = null;
    }
}
