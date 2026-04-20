// ============================================================
// AGR_Obstacle.cs — Obstacle behavior with camera occlusion!
// ============================================================
// Obstacles can move left/right at higher scores.
// Obstacles fade to transparent when between camera and player
// so the player can always see what's ahead.
// ATTACH THIS TO: Obstacle objects (auto-added by spawner)
// ============================================================

using UnityEngine;

public class AGR_Obstacle : MonoBehaviour
{
    private float destroyDistance = 20f;
    private Transform player;
    private Transform cam;

    // Movement (set by spawner at higher scores)
    private bool isMoving = false;
    private float moveSpeed = 0f;
    private float moveRange = 2.5f;
    private float startX;

    // Occlusion fade
    private Renderer obstacleRenderer;
    private Material obstacleMat;
    private Color originalColor;
    private Color originalEmission;
    private bool isFaded = false;
    private float fadeAlpha = 1f;

    void Start()
    {
        GameObject playerObj = GameObject.FindGameObjectWithTag("Player");
        if (playerObj != null)
        {
            player = playerObj.transform;
        }
        else
        {
            // Fallback: find by component
            AGR_PlayerController pc = FindObjectOfType<AGR_PlayerController>();
            if (pc != null) player = pc.transform;
        }

        if (Camera.main != null)
        {
            cam = Camera.main.transform;
        }

        startX = transform.position.x;

        // Cache renderer for occlusion fading
        obstacleRenderer = GetComponent<Renderer>();
        if (obstacleRenderer != null && obstacleRenderer.material != null)
        {
            obstacleMat = obstacleRenderer.material;
            originalColor = obstacleMat.color;
            if (obstacleMat.HasProperty("_EmissionColor"))
            {
                originalEmission = obstacleMat.GetColor("_EmissionColor");
            }
        }
    }

    void Update()
    {
        if (player == null) return;

        // Destroy when behind player
        if (transform.position.z < player.position.z - destroyDistance)
        {
            Destroy(gameObject);
        }

        // Move left/right if enabled
        if (isMoving)
        {
            float newX = startX + Mathf.Sin(Time.time * moveSpeed) * moveRange;
            Vector3 pos = transform.position;
            pos.x = newX;
            transform.position = pos;
        }

        // === CAMERA OCCLUSION: Fade when blocking player's view ===
        if (cam != null && obstacleMat != null)
        {
            HandleOcclusion();
        }
    }

    private void HandleOcclusion()
    {
        // Check if this obstacle is between the camera and the player
        float camZ = cam.position.z;
        float playerZ = player.position.z;
        float obstacleZ = transform.position.z;

        // Obstacle is "blocking" if it's between camera and player (or slightly ahead)
        bool isBlocking = obstacleZ > camZ && obstacleZ < playerZ + 5f;

        // Also check if it's in a similar X range as the player
        float xDist = Mathf.Abs(transform.position.x - player.position.x);
        bool isInPlayerPath = xDist < (transform.localScale.x * 0.5f + 2f);

        // Calculate how close the obstacle is to the camera (closer = more fade needed)
        float distToCamera = Mathf.Abs(obstacleZ - camZ);
        bool isTooClose = distToCamera < 15f;

        if (isBlocking && isInPlayerPath && isTooClose)
        {
            // Fade out — the closer to camera, the more transparent
            float targetAlpha = Mathf.Lerp(0.1f, 0.6f, distToCamera / 15f);
            fadeAlpha = Mathf.Lerp(fadeAlpha, targetAlpha, Time.deltaTime * 8f);

            // Switch material to transparent mode if not already
            if (!isFaded)
            {
                SetMaterialTransparent(obstacleMat);
                isFaded = true;
            }

            // Apply fade
            Color c = originalColor;
            c.a = fadeAlpha;
            obstacleMat.color = c;

            // Dim emission too
            if (obstacleMat.HasProperty("_EmissionColor"))
            {
                obstacleMat.SetColor("_EmissionColor", originalEmission * fadeAlpha);
            }
        }
        else if (isFaded)
        {
            // Fade back in
            fadeAlpha = Mathf.Lerp(fadeAlpha, 1f, Time.deltaTime * 8f);

            if (fadeAlpha > 0.95f)
            {
                fadeAlpha = 1f;
                isFaded = false;
                SetMaterialOpaque(obstacleMat);
            }

            Color c = originalColor;
            c.a = fadeAlpha;
            obstacleMat.color = c;

            if (obstacleMat.HasProperty("_EmissionColor"))
            {
                obstacleMat.SetColor("_EmissionColor", originalEmission * fadeAlpha);
            }
        }
    }

    private void SetMaterialTransparent(Material mat)
    {
        mat.SetFloat("_Mode", 3); // Transparent
        mat.SetInt("_SrcBlend", (int)UnityEngine.Rendering.BlendMode.SrcAlpha);
        mat.SetInt("_DstBlend", (int)UnityEngine.Rendering.BlendMode.OneMinusSrcAlpha);
        mat.SetInt("_ZWrite", 0);
        mat.DisableKeyword("_ALPHATEST_ON");
        mat.EnableKeyword("_ALPHABLEND_ON");
        mat.DisableKeyword("_ALPHAPREMULTIPLY_ON");
        mat.renderQueue = 3000;
    }

    private void SetMaterialOpaque(Material mat)
    {
        mat.SetFloat("_Mode", 0); // Opaque
        mat.SetInt("_SrcBlend", (int)UnityEngine.Rendering.BlendMode.One);
        mat.SetInt("_DstBlend", (int)UnityEngine.Rendering.BlendMode.Zero);
        mat.SetInt("_ZWrite", 1);
        mat.EnableKeyword("_ALPHATEST_ON");
        mat.DisableKeyword("_ALPHABLEND_ON");
        mat.DisableKeyword("_ALPHAPREMULTIPLY_ON");
        mat.renderQueue = -1;
    }

    // Called by spawner to make this obstacle move
    public void SetMoving(float speed, float range)
    {
        isMoving = true;
        moveSpeed = speed;
        moveRange = range;
    }
}
