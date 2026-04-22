// ============================================================
// AGR_CameraFollow.cs — Camera follows the player (AUTO-FIND)
// ============================================================
// Camera stays behind and slightly above, locked Y range
// AUTO-FINDS the player if the reference is missing!
// ATTACH THIS TO: Main Camera
// ============================================================

using UnityEngine;

public class AGR_CameraFollow : MonoBehaviour
{
    [Header("Target")]
    [SerializeField] private Transform player;

    [Header("Offset")]
    [SerializeField] private Vector3 offset = new Vector3(0, 5, -10);

    [Header("Smoothing")]
    [SerializeField] private float smoothSpeed = 5f;

    [Header("Y Limits")]
    [Tooltip("Camera won't go below this Y")]
    [SerializeField] private float minY = 3f;
    [Tooltip("Camera won't go above this Y")]
    [SerializeField] private float maxY = 7f;

    // Danger zoom state
    private bool inDangerZoom = false;
    private Vector3 normalOffset;
    private Vector3 dangerOffset;
    private float baseNormalFOV = 60f;
    private float baseDangerFOV = 50f; // Tighter, more intense
    private Camera cam;

    void Start()
    {
        // AUTO-FIND player if not assigned
        if (player == null)
        {
            AGR_PlayerController pc = FindObjectOfType<AGR_PlayerController>();
            if (pc != null)
            {
                player = pc.transform;
                Debug.Log("AGR_CameraFollow: Auto-found Player!");
            }
        }

        // Cache camera and offsets
        cam = GetComponent<Camera>();
        normalOffset = offset;
        dangerOffset = new Vector3(offset.x, offset.y - 1.5f, offset.z + 3f); // Closer & lower
        if (cam != null)
        {
            baseNormalFOV = cam.fieldOfView;
            baseDangerFOV = baseNormalFOV - 10f; // Tighter
            cam.nearClipPlane = 1f; // Clip objects that are too close to camera
        }
    }

    void LateUpdate()
    {
        // Keep trying to find player if still null
        if (player == null)
        {
            AGR_PlayerController pc = FindObjectOfType<AGR_PlayerController>();
            if (pc != null) player = pc.transform;
            return;
        }

        // Smoothly transition between normal and danger zoom
        Vector3 currentOffset = Vector3.Lerp(
            inDangerZoom ? normalOffset : offset,
            inDangerZoom ? dangerOffset : normalOffset,
            Time.deltaTime * 3f
        );
        offset = currentOffset;

        // Smoothly adjust FOV
        if (cam != null)
        {
            float currentAspect = (float)Screen.width / Screen.height;
            float adjustedNormalFOV = baseNormalFOV;
            float adjustedDangerFOV = baseDangerFOV;

            // If in portrait mode (aspect < 1), increase vertical FOV to maintain horizontal visibility
            if (currentAspect < 1f)
            {
                // Target landscape aspect (16:9)
                float targetAspect = 16f / 9f;
                
                // Convert vertical FOV to horizontal FOV based on target aspect
                float hFOVNormal = 2f * Mathf.Atan(Mathf.Tan(baseNormalFOV * Mathf.Deg2Rad / 2f) * targetAspect);
                float hFOVDanger = 2f * Mathf.Atan(Mathf.Tan(baseDangerFOV * Mathf.Deg2Rad / 2f) * targetAspect);
                
                // Calculate new vertical FOVs to maintain the same horizontal FOV in current aspect
                adjustedNormalFOV = 2f * Mathf.Atan(Mathf.Tan(hFOVNormal / 2f) / currentAspect) * Mathf.Rad2Deg;
                adjustedDangerFOV = 2f * Mathf.Atan(Mathf.Tan(hFOVDanger / 2f) / currentAspect) * Mathf.Rad2Deg;
                
                // Clamp to prevent extreme distortion on very narrow screens
                adjustedNormalFOV = Mathf.Clamp(adjustedNormalFOV, 60f, 110f);
                adjustedDangerFOV = Mathf.Clamp(adjustedDangerFOV, 50f, 100f);
            }

            float targetFOV = inDangerZoom ? adjustedDangerFOV : adjustedNormalFOV;
            cam.fieldOfView = Mathf.Lerp(cam.fieldOfView, targetFOV, Time.deltaTime * 3f);
        }

        // Target position: follow player's Z, keep X centered
        Vector3 targetPosition = new Vector3(
            offset.x,
            Mathf.Clamp(player.position.y + offset.y, minY, maxY),
            player.position.z + offset.z
        );

        // Smoothly move camera
        transform.position = Vector3.Lerp(
            transform.position,
            targetPosition,
            smoothSpeed * Time.deltaTime
        );

        // Look at a point slightly ahead of the player
        Vector3 lookTarget = new Vector3(0, 4f, player.position.z + 5f);
        transform.LookAt(lookTarget);
    }

    /// <summary>
    /// Called by CollisionHandler to toggle danger zoom on/off
    /// </summary>
    public void SetDangerZoom(bool danger)
    {
        inDangerZoom = danger;
    }
}
