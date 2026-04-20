// ============================================================
// AGR_ObstacleSpawner.cs — Spawns obstacles with difficulty!
// ============================================================
// At higher scores: obstacles move left/right, spawn faster
// ATTACH THIS TO: ObstacleSpawner object
// ============================================================

using UnityEngine;

public class AGR_ObstacleSpawner : MonoBehaviour
{
    [Header("References")]
    [SerializeField] private Transform player;

    [Header("Spawn Settings")]
    [SerializeField] private float spawnDistance = 80f;
    [SerializeField] private float minSpawnInterval = 1.2f;
    [SerializeField] private float maxSpawnInterval = 2.5f;

    [Header("Positions")]
    [SerializeField] private float groundY = 0.25f;

    private float spawnTimer;
    private bool isActive = false;
    private float currentDifficultyMultiplier = 1f;
    private AGR_GameManager gameManager;

    void Start()
    {
        ResetTimer();
        gameManager = FindObjectOfType<AGR_GameManager>();
        
        // AUTO-FIND player if not assigned
        if (player == null)
        {
            AGR_PlayerController pc = FindObjectOfType<AGR_PlayerController>();
            if (pc != null) player = pc.transform;
        }
    }

    void Update()
    {
        if (player == null) return;

        AGR_PlayerController playerCtrl = player.GetComponent<AGR_PlayerController>();
        if (playerCtrl != null && !playerCtrl.GameStarted) return;

        if (!isActive)
        {
            isActive = true;
            ResetTimer();
        }

        spawnTimer -= Time.deltaTime;
        if (spawnTimer <= 0f)
        {
            SpawnRandomObstacle();
            ResetTimer();
        }
    }

    private int GetScore()
    {
        return gameManager != null ? gameManager.Score : 0;
    }

    private void SpawnRandomObstacle()
    {
        int score = GetScore();

        // More obstacle variety at higher scores
        int maxType = 4; // Start with 4 types
        if (score > 100) maxType = 5;
        if (score > 200) maxType = 6;

        int type = Random.Range(0, maxType);

        switch (type)
        {
            case 0: SpawnLowHurdle(); break;
            case 1: SpawnMediumWall(); break;
            case 2: SpawnSideWall(); break;
            case 3: SpawnDoubleHurdle(); break;
            case 4: SpawnNarrowGap(); break;
            case 5: SpawnFloatingBarrier(); break;
        }

        // 60% chance to also spawn a line of coins nearby in an empty lane!
        if (Random.value < 0.6f)
        {
            SpawnCoinLine();
        }
    }

    // Should this obstacle move? (only at higher scores)
    private void TryMakeMoving(GameObject obs)
    {
        int score = GetScore();

        // After score 150, some obstacles start moving
        if (score > 150 && Random.value > 0.5f)
        {
            AGR_Obstacle obstacle = obs.GetComponent<AGR_Obstacle>();
            if (obstacle != null)
            {
                float speed = Mathf.Clamp(score / 100f, 1.5f, 4f);
                float range = Mathf.Clamp(score / 200f, 1f, 2.5f);
                obstacle.SetMoving(speed, range);
            }
        }
    }

    // TYPE 1: Low hurdle — easy, just jump
    private void SpawnLowHurdle()
    {
        float spawnZ = player.position.z + spawnDistance;
        Vector3 pos = new Vector3(0, groundY + 0.5f, spawnZ);
        GameObject obs = CreateObstacle(pos, new Vector3(8f, 1f, 0.5f));
        obs.name = "Hurdle_Low";
        TryMakeMoving(obs);
    }

    // TYPE 2: Medium wall
    private void SpawnMediumWall()
    {
        float spawnZ = player.position.z + spawnDistance;
        Vector3 pos = new Vector3(0, groundY + 1f, spawnZ);
        GameObject obs = CreateObstacle(pos, new Vector3(8f, 2f, 0.5f));
        obs.name = "Wall_Medium";
        TryMakeMoving(obs);
    }

    // TYPE 3: Side wall — dodge left or right (Too high to jump)
    private void SpawnSideWall()
    {
        float spawnZ = player.position.z + spawnDistance;
        float xPos = Random.value > 0.5f ? -2f : 2f;
        Vector3 pos = new Vector3(xPos, groundY + 2f, spawnZ);
        GameObject obs = CreateObstacle(pos, new Vector3(4f, 4f, 0.5f));
        obs.name = "Wall_Side";
        TryMakeMoving(obs);
    }

    // TYPE 4: Double hurdle
    private void SpawnDoubleHurdle()
    {
        float spawnZ = player.position.z + spawnDistance;
        Vector3 pos1 = new Vector3(0, groundY + 0.5f, spawnZ);
        CreateObstacle(pos1, new Vector3(8f, 1f, 0.5f));
        Vector3 pos2 = new Vector3(0, groundY + 0.5f, spawnZ + 5f);
        CreateObstacle(pos2, new Vector3(8f, 1f, 0.5f));
    }

    // TYPE 5: Narrow gap — walls with a gap to pass through
    private void SpawnNarrowGap()
    {
        float spawnZ = player.position.z + spawnDistance;
        Vector3 posL = new Vector3(-3f, groundY + 2f, spawnZ);
        GameObject oL = CreateObstacle(posL, new Vector3(3f, 4f, 0.5f));
        TryMakeMoving(oL);

        Vector3 posR = new Vector3(3f, groundY + 2f, spawnZ);
        GameObject oR = CreateObstacle(posR, new Vector3(3f, 4f, 0.5f));
        TryMakeMoving(oR);
    }

    // TYPE 6: Floating Barrier (Archway) — Must slide under!
    private void SpawnFloatingBarrier()
    {
        float spawnZ = player.position.z + spawnDistance;
        
        // The Top bridge (floating high)
        Vector3 posTop = new Vector3(0, groundY + 2.5f, spawnZ);
        GameObject topBar = CreateObstacle(posTop, new Vector3(8f, 2f, 0.5f));
        topBar.name = "Barrier_Floating_Top";
        
        // The Left Pillar
        Vector3 posL = new Vector3(-3.5f, groundY + 1f, spawnZ);
        GameObject pillarL = CreateObstacle(posL, new Vector3(1f, 2f, 0.5f));
        pillarL.name = "Barrier_Pillar_L";

        // The Right Pillar
        Vector3 posR = new Vector3(3.5f, groundY + 1f, spawnZ);
        GameObject pillarR = CreateObstacle(posR, new Vector3(1f, 2f, 0.5f));
        pillarR.name = "Barrier_Pillar_R";

        TryMakeMoving(topBar); // Only move the top bar (optional) or group them? 
        // For simplicity, we just leave them static if not handled carefully, 
        // but TryMakeMoving operates independently. Let's make the pillars static.
    }

    private GameObject CreateObstacle(Vector3 position, Vector3 scale)
    {
        GameObject obs = GameObject.CreatePrimitive(PrimitiveType.Cube);
        obs.transform.position = position;
        obs.transform.localScale = scale;

        BoxCollider col = obs.GetComponent<BoxCollider>();
        col.isTrigger = true;

        obs.AddComponent<AGR_Obstacle>();
        obs.AddComponent<AGR_ObstacleVisual>();

        return obs;
    }

    private void SpawnCoinLine()
    {
        // Give the coins a little buffer distance so they don't spawn exactly inside the obstacle
        float spawnZ = player.position.z + spawnDistance + Random.Range(5f, 15f);
        
        // Pick a random lane
        float[] lanes = { -3f, 0f, 3f }; // Left, Center, Right (match obstacle logic)
        float xPos = lanes[Random.Range(0, 3)];
        
        // Should we spawn a ground line or a jumping arc?
        bool isJumpArc = Random.value > 0.5f;

        // Line of 3 to 7 coins
        int numCoins = Random.Range(3, 8);
        for (int i = 0; i < numCoins; i++)
        {
            float yPos = groundY + 0.5f; // Ground level

            if (isJumpArc)
            {
                // Create a beautiful jump arc! Middle coin is highest.
                float normalizedProgress = (float)i / (numCoins - 1); // interpolates 0.0 to 1.0
                // Use sine wave for arc: sin(0) = 0, sin(pi/2) = 1, sin(pi) = 0
                float arcHeight = Mathf.Sin(normalizedProgress * Mathf.PI) * 4f; 
                yPos += arcHeight + 0.5f; // Add a little base height to force a jump
            }

            Vector3 pos = new Vector3(xPos, yPos, spawnZ + (i * 2.5f));
            CreateCoin(pos);
        }
    }

    private GameObject CreateCoin(Vector3 position)
    {
        // Use a cube scaled as a diamond instead of a realistic coin
        GameObject coin = GameObject.CreatePrimitive(PrimitiveType.Cube);
        coin.name = "Coin";
        coin.transform.position = position;
        
        // Make it look like a diamond gem
        coin.transform.localScale = new Vector3(0.5f, 0.5f, 0.5f);
        // Turn it 45 degrees so it stands on its point
        coin.transform.rotation = Quaternion.Euler(45f, 45f, 45f);

        BoxCollider col = coin.GetComponent<BoxCollider>();
        if (col != null)
        {
            col.isTrigger = true;
            // Massively expand the invisible hitbox so they are very easy and fun to collect!
            col.size = new Vector3(3f, 3f, 3f);
        }

        // Apply a cool neon diamond material
        Renderer r = coin.GetComponent<Renderer>();
        Material mat = new Material(Shader.Find("Standard"));
        
        // Neon Cyan/Turquoise instead of realistic gold
        Color neonCyan = new Color(0f, 1f, 0.8f);
        mat.color = new Color(0.1f, 0.2f, 0.2f);
        mat.EnableKeyword("_EMISSION");
        mat.SetColor("_EmissionColor", neonCyan * 2f);
        mat.SetFloat("_Metallic", 0f); // Make it glow like energy, not metal
        mat.SetFloat("_Glossiness", 0f);
        r.material = mat;

        // Add the behavior script
        coin.AddComponent<AGR_Coin>();

        return coin;
    }

    private void ResetTimer()
    {
        // Use the difficulty multiplier set by the DifficultyManager
        float speedMult = currentDifficultyMultiplier;
        spawnTimer = Random.Range(minSpawnInterval * speedMult, maxSpawnInterval * speedMult);
    }

    public void SetDifficultyMultiplier(float multiplier)
    {
        currentDifficultyMultiplier = multiplier;
    }
}
