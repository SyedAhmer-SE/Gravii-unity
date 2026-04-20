// ============================================================
// AGR_CollisionHandler.cs — Strike System!
// ============================================================
// Strike 1: Player slows down, ghost surges closer, camera zooms
// Strike 2 (within 10 sec): DEAD!
// If 10 seconds pass after Strike 1: strike resets, you're safe
// ATTACH THIS TO: The Player GameObject
// ============================================================

using UnityEngine;

public class AGR_CollisionHandler : MonoBehaviour
{
    [Header("Strike Settings")]
    [Tooltip("Seconds before a strike resets")]
    [SerializeField] private float strikeResetTime = 10f;

    [Tooltip("How much to slow the player on hit (multiplier)")]
    [SerializeField] private float slowMultiplier = 0.6f;

    [Tooltip("How fast the player recovers speed after a hit")]
    [SerializeField] private float speedRecoveryTime = 3f;

    // Strike tracking
    private int currentStrikes = 0;
    private float strikeTimer = 0f;

    // Speed recovery
    private float originalSpeed;
    private float speedRecoverTimer = 0f;
    private bool isSlowed = false;

    // References
    private AGR_PlayerController playerCtrl;
    private AGR_GhostChaser ghost;
    private AGR_CameraFollow cam;
    private AGR_GameManager gameManager;

    void Start()
    {
        playerCtrl = GetComponent<AGR_PlayerController>();
        ghost = FindObjectOfType<AGR_GhostChaser>();
        cam = FindObjectOfType<AGR_CameraFollow>();
        gameManager = FindObjectOfType<AGR_GameManager>();

        if (playerCtrl != null)
            originalSpeed = playerCtrl.MoveSpeed;
    }

    void Update()
    {
        // Count down the strike reset timer
        if (currentStrikes > 0)
        {
            strikeTimer -= Time.deltaTime;
            if (strikeTimer <= 0f)
            {
                // 10 seconds passed without second hit — reset!
                currentStrikes = 0;
                Debug.Log("STRIKE RESET! You're safe again.");

                // Tell camera to zoom back out
                if (cam != null) cam.SetDangerZoom(false);
            }
        }

        // Recover speed gradually after being slowed
        if (isSlowed)
        {
            speedRecoverTimer -= Time.deltaTime;
            if (speedRecoverTimer <= 0f)
            {
                isSlowed = false;
                if (playerCtrl != null)
                    playerCtrl.MoveSpeed = originalSpeed;
                Debug.Log("Speed recovered!");
            }
        }
    }

    void OnTriggerEnter(Collider other)
    {
        // === COIN COLLECTION ===
        if (other.GetComponent<AGR_Coin>() != null)
        {
            // Coins are handled by AGR_Coin itself
            return;
        }

        // === OBSTACLE HIT ===
        if (other.GetComponent<AGR_Obstacle>() != null)
        {
            currentStrikes++;
            strikeTimer = strikeResetTime; // Reset the 10-second countdown

            // Destroy the obstacle we crashed into
            Destroy(other.gameObject);

            if (currentStrikes >= 2)
            {
                // ====== STRIKE 2: DEAD! ======
                if (gameManager != null)
                {
                    gameManager.GameOver("THE GHOST CAUGHT YOU");
                    Debug.Log("STRIKE 2! You're DEAD!");
                }
                
                // Force camera into extreme zoom on death
                if (cam != null) cam.SetDangerZoom(true);
                
                // Play Crash SFX
                if (AGR_SFXManager.Instance != null) AGR_SFXManager.Instance.PlayCrash();
                
                return;
            }

            // ====== STRIKE 1: Warning! ======
            Debug.Log("STRIKE 1! Ghost surges closer! You have " + strikeResetTime + "s to survive!");

            // 1. Slow the player down
            if (playerCtrl != null)
            {
                if (!isSlowed)
                    originalSpeed = playerCtrl.MoveSpeed;
                playerCtrl.MoveSpeed *= slowMultiplier;
                isSlowed = true;
                speedRecoverTimer = speedRecoveryTime;
            }

            // 2. Ghost surges closer!
            if (ghost != null)
                ghost.SurgeCloser();
                
            // Play SFX
            if (AGR_SFXManager.Instance != null)
            {
                AGR_SFXManager.Instance.PlayCrash();
                AGR_SFXManager.Instance.PlayGhostSurge();
            }

            // 3. Camera zooms in (danger mode)
            if (cam != null)
                cam.SetDangerZoom(true);

            // 4. Visual feedback — flash all renderers red
            StartCoroutine(FlashDanger());
        }
    }

    private System.Collections.IEnumerator FlashDanger()
    {
        // Flash every renderer on the player red
        Renderer[] renderers = GetComponentsInChildren<Renderer>();
        Color[][] originalColors = new Color[renderers.Length][];

        // Store original colors and set red
        for (int i = 0; i < renderers.Length; i++)
        {
            Material[] mats = renderers[i].materials;
            originalColors[i] = new Color[mats.Length];
            for (int j = 0; j < mats.Length; j++)
            {
                originalColors[i][j] = mats[j].color;
                mats[j].color = Color.red;
                if (mats[j].HasProperty("_EmissionColor"))
                {
                    mats[j].EnableKeyword("_EMISSION");
                    mats[j].SetColor("_EmissionColor", Color.red * 4f);
                }
            }
        }

        yield return new WaitForSeconds(0.4f);

        // Restore original colors
        for (int i = 0; i < renderers.Length; i++)
        {
            if (renderers[i] == null) continue;
            Material[] mats = renderers[i].materials;
            for (int j = 0; j < mats.Length; j++)
            {
                if (j < originalColors[i].Length)
                    mats[j].color = originalColors[i][j];
            }
        }
    }
}
