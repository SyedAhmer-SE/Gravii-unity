// ============================================================
// AGR_DifficultyManager.cs — Makes game harder over time
// ============================================================
// Gradually increases speed and obstacle frequency
// ATTACH THIS TO: GameManager object (or a new empty object)
// ============================================================

using UnityEngine;

public class AGR_DifficultyManager : MonoBehaviour
{
    [Header("References")]
    [SerializeField] private AGR_PlayerController player;
    [SerializeField] private AGR_ObstacleSpawner spawner;

    [Header("Speed Settings")]
    [Tooltip("Starting player speed")]
    [SerializeField] private float startSpeed = 10f;

    [Tooltip("Maximum speed the game can reach")]
    [SerializeField] private float maxSpeed = 30f;

    [Tooltip("How much speed increases per second")]
    [SerializeField] private float speedIncreaseRate = 0.3f;

    [Header("Obstacle Settings")]
    [Tooltip("How fast spawn interval shrinks (lower = faster spawns)")]
    [SerializeField] private float spawnRateIncrease = 0.01f;

    [Tooltip("Minimum spawn timer multiplier (caps how fast obstacles can spawn)")]
    [SerializeField] private float minSpawnMultiplier = 0.35f;

    private bool isActive = false;
    private float elapsedTime = 0f;
    private float currentSpawnMultiplier = 1f;

    // Public so spawner can read it
    public float SpawnTimerMultiplier => currentSpawnMultiplier;

    void Update()
    {
        if (player == null) return;
        if (player.IsDead || !player.GameStarted) return;

        // First frame of gameplay — set initial speed
        if (!isActive)
        {
            isActive = true;
            player.MoveSpeed = startSpeed;
            currentSpawnMultiplier = 1f;
            elapsedTime = 0f;
        }

        elapsedTime += Time.deltaTime;

        // === SPEED RAMP ===
        // Smoothly increase speed over time
        float targetSpeed = startSpeed + (speedIncreaseRate * elapsedTime);
        targetSpeed = Mathf.Min(targetSpeed, maxSpeed);
        player.MoveSpeed = targetSpeed;

        // === SPAWN RATE RAMP ===
        // Obstacles spawn faster over time
        currentSpawnMultiplier = Mathf.Max(
            minSpawnMultiplier,
            1f - (spawnRateIncrease * elapsedTime)
        );

        // Tell spawner about current difficulty
        if (spawner != null)
        {
            spawner.SetDifficultyMultiplier(currentSpawnMultiplier);
        }
    }
}
