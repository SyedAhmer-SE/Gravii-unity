// ============================================================
// AGR_Coin.cs — Collectible Currency
// ============================================================
// Rotates continuously and grants coins to the GameManager when hit
// ATTACH THIS TO: Coin Prefab
// ============================================================

using UnityEngine;

public class AGR_Coin : MonoBehaviour
{
    [Header("Settings")]
    public int coinValue = 1;
    public float rotationSpeed = 150f;
    
    private bool collected = false;

    void Update()
    {
        // Spin the coin around its Y axis in world space
        transform.Rotate(0, rotationSpeed * Time.deltaTime, 0, Space.World);

        // Auto-cleanup if it goes behind the camera
        if (Camera.main != null && transform.position.z < Camera.main.transform.position.z - 10f)
        {
            Destroy(gameObject);
        }
    }

    void OnTriggerEnter(Collider other)
    {
        if (collected) return;

        // If player hits the coin
        if (other.CompareTag("Player") || other.GetComponent<AGR_PlayerController>() != null)
        {
            collected = true;
            
            // Add coin to Game Manager
            AGR_GameManager gm = FindObjectOfType<AGR_GameManager>();
            if (gm != null)
            {
                gm.AddCoins(coinValue);
            }
            
            // Play Coin SFX
            if (AGR_SFXManager.Instance != null) AGR_SFXManager.Instance.PlayCoin();
            
            // Destroy coin immediately
            Destroy(gameObject);
        }
    }
}
