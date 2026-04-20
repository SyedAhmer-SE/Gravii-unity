// ============================================================
// AGR_GhostChaser.cs — The Ghost that chases the player!
// ============================================================
// A dark shadow that follows behind the player, getting
// closer over time. If it catches you — GAME OVER!
// It follows the player's exact path with a delay (echo).
//
// ATTACH THIS TO: A new 3D object (Sphere) called "Ghost"
// ============================================================

using UnityEngine;
using System.Collections.Generic;

public class AGR_GhostChaser : MonoBehaviour
{
    [Header("Chase Settings")]
    [Tooltip("How far behind the player the ghost starts")]
    [SerializeField] private float startDistance = 15f;

    [Tooltip("The ghost slowly catches up — how fast it closes the gap")]
    [SerializeField] private float catchUpRate = 0.1f;

    [Tooltip("If the ghost gets this close, game over!")]
    [SerializeField] private float catchDistance = 1.5f;

    [Tooltip("Delay in seconds — ghost replays player path after this delay")]
    [SerializeField] private float echoDelay = 1.5f;

    [Header("Visual")]
    [SerializeField] private Color ghostColor = new Color(0.5f, 0f, 1f, 0.7f);

    [Header("References")]
    [SerializeField] private Transform player;

    // Stores the player's past positions (like a recording)
    private List<Vector3> positionHistory = new List<Vector3>();
    private List<Quaternion> rotationHistory = new List<Quaternion>();
    private float recordInterval = 0.02f; // Record 50 times per second
    private float recordTimer = 0f;

    private float currentDistance;
    private bool isActive = false;
    private AGR_GameManager gameManager;
    private Transform warrokModel;
    private float lastGhostX = 0f;

    void Start()
    {
        currentDistance = startDistance;
        gameManager = FindObjectOfType<AGR_GameManager>();

        // AUTO-FIND player if not assigned
        if (player == null)
        {
            AGR_PlayerController pc = FindObjectOfType<AGR_PlayerController>();
            if (pc != null) player = pc.transform;
        }

        // HIDE the capsule/sphere mesh — we use particles + Warrok model now
        MeshRenderer mr = GetComponent<MeshRenderer>();
        if (mr != null) mr.enabled = false;
        MeshFilter mf = GetComponent<MeshFilter>();
        if (mf != null) Destroy(mf);

        // AUTO-FIND: Look for Warrok monster model in the scene
        GameObject warrok = null;
        
        // First check if already a child of Ghost
        foreach (Transform child in transform)
        {
            if (child.name.ToLower().Contains("warrok"))
            {
                warrok = child.gameObject;
                break;
            }
        }

        // Search the entire scene
        if (warrok == null)
        {
            foreach (GameObject go in FindObjectsOfType<GameObject>())
            {
                if (go.name.ToLower().Contains("warrok") && go.transform != transform)
                {
                    warrok = go;
                    break;
                }
            }
        }

        // Last resort: Load from Resources
        if (warrok == null)
        {
            GameObject warrokPrefab = Resources.Load<GameObject>("Warrok");
            if (warrokPrefab != null)
            {
                warrok = Instantiate(warrokPrefab);
                warrok.name = "Warrok_Monster";
                Debug.Log("AGR_GhostChaser: Loaded Warrok from Resources!");
            }
        }

        if (warrok != null)
        {
            warrok.transform.SetParent(transform, false);
            warrok.transform.localPosition = new Vector3(0, -1f, 0);
            warrok.transform.localRotation = Quaternion.Euler(0, 180, 0);
            warrok.transform.localScale = Vector3.one;
            
            // Store reference for strafing animation in Update
            warrokModel = warrok.transform;

            // Set up the Animator to play whatever animation is baked into the FBX
            Animator warrokAnim = warrok.GetComponentInChildren<Animator>();
            if (warrokAnim != null)
            {
                warrokAnim.applyRootMotion = false;
                
                // If no controller assigned, create a simple runtime one
                if (warrokAnim.runtimeAnimatorController == null)
                {
                    // Try loading ParkourAnimator from Resources (shared with player)
                    RuntimeAnimatorController ctrl = Resources.Load<RuntimeAnimatorController>("ParkourAnimator");
                    if (ctrl != null)
                    {
                        warrokAnim.runtimeAnimatorController = ctrl;
                        Debug.Log("AGR_GhostChaser: Applied ParkourAnimator to Warrok!");
                    }
                }
                
                // Force play the first available animation
                warrokAnim.speed = 1.2f; // Slightly faster than player for menacing feel
                Debug.Log("AGR_GhostChaser: Warrok Animator activated!");
            }
            
            Debug.Log("AGR_GhostChaser: Warrok monster applied as Ghost skin!");
        }

        // Position ghost behind player
        if (player != null)
        {
            transform.position = player.position - Vector3.forward * startDistance;
        }

        // Clear any trail renderers that might have drawn a streak during teleport
        TrailRenderer tr = GetComponent<TrailRenderer>();
        if (tr != null) tr.Clear();
        TrailRenderer[] childTrails = GetComponentsInChildren<TrailRenderer>();
        foreach (TrailRenderer ct in childTrails) ct.Clear();
    }

    void Update()
    {
        if (player == null) return;

        // Check if game has started
        AGR_PlayerController playerCtrl = player.GetComponent<AGR_PlayerController>();
        if (playerCtrl == null || !playerCtrl.GameStarted) return;

        // If player is dead, the ghost catches them! Let it zoom to their exact position.
        if (playerCtrl.IsDead)
        {
            // Rapidly fly directly into the player
            transform.position = Vector3.Lerp(transform.position, player.position, 15f * Time.deltaTime);
            transform.rotation = Quaternion.Lerp(transform.rotation, player.rotation, 15f * Time.deltaTime);
            
            // Pulse rapidly
            float deadPulse = 1f + Mathf.Sin(Time.time * 20f) * 0.15f;
            transform.localScale = Vector3.one * deadPulse;
            return;
        }

        if (!isActive) isActive = true;

        // Record player's position over time
        recordTimer += Time.deltaTime;
        if (recordTimer >= recordInterval)
        {
            recordTimer = 0f;
            positionHistory.Add(player.position);
            rotationHistory.Add(player.rotation);
        }

        // Calculate which recorded position to play back
        // (based on echo delay)
        int framesDelay = Mathf.FloorToInt(echoDelay / recordInterval);
        int targetIndex = positionHistory.Count - 1 - framesDelay;

        if (targetIndex >= 0 && targetIndex < positionHistory.Count)
        {
            // Follow the player's recorded path!
            Vector3 targetPos = positionHistory[targetIndex];
            Quaternion targetRot = rotationHistory[targetIndex];

            // Smoothly move to the recorded position
            transform.position = Vector3.Lerp(transform.position, targetPos, 10f * Time.deltaTime);
            transform.rotation = Quaternion.Lerp(transform.rotation, targetRot, 10f * Time.deltaTime);
        }

        // Ghost slowly catches up (reduce echo delay over time)
        if (echoDelay > 0.3f)
        {
            echoDelay -= catchUpRate * Time.deltaTime;
        }

        // Check if ghost caught the player during normal gameplay!
        float dist = Vector3.Distance(transform.position, player.position);
        if (dist < catchDistance && !playerCtrl.IsDead)
        {
            if (gameManager != null)
            {
                gameManager.GameOver("THE GHOST CAUGHT YOU");
            }
        }

        // Pulsing scale effect (ghost breathes/pulses)
        float pulse = 1f + Mathf.Sin(Time.time * 5f) * 0.1f;
        transform.localScale = Vector3.one * pulse;

        // STRAFE TILT: Tilt the Warrok model based on horizontal movement
        if (warrokModel != null)
        {
            float xVelocity = transform.position.x - lastGhostX;
            lastGhostX = transform.position.x;

            // Calculate a tilt angle based on horizontal speed
            float tiltAngle = Mathf.Clamp(xVelocity * 80f, -25f, 25f);
            
            // Smoothly tilt the model (Z-axis roll for strafe lean)
            Quaternion targetRot = Quaternion.Euler(0, 180, -tiltAngle);
            warrokModel.localRotation = Quaternion.Lerp(
                warrokModel.localRotation, 
                targetRot, 
                Time.deltaTime * 8f
            );
        }
    }

    private void SetupVisual()
    {
        Renderer r = GetComponent<Renderer>();
        if (r != null)
        {
            // Semi-transparent glowing material
            Material mat = new Material(Shader.Find("Standard"));
            mat.SetFloat("_Mode", 3); // Transparent mode
            mat.SetInt("_SrcBlend", (int)UnityEngine.Rendering.BlendMode.SrcAlpha);
            mat.SetInt("_DstBlend", (int)UnityEngine.Rendering.BlendMode.OneMinusSrcAlpha);
            mat.SetInt("_ZWrite", 0);
            mat.DisableKeyword("_ALPHATEST_ON");
            mat.EnableKeyword("_ALPHABLEND_ON");
            mat.DisableKeyword("_ALPHAPREMULTIPLY_ON");
            mat.renderQueue = 3000;

            mat.color = ghostColor;
            mat.EnableKeyword("_EMISSION");
            mat.SetColor("_EmissionColor", new Color(ghostColor.r, ghostColor.g, ghostColor.b) * 3f);
            r.material = mat;
        }
    }

    /// <summary>
    /// Called by CollisionHandler when player hits an obstacle.
    /// Ghost jumps forward dramatically!
    /// </summary>
    public void SurgeCloser()
    {
        // Cut the echo delay by 40% — ghost leaps forward!
        echoDelay *= 0.6f;
        
        // Minimum delay so ghost doesn't instantly catch you
        if (echoDelay < 0.4f) echoDelay = 0.4f;

        Debug.Log("GHOST SURGES! Echo delay now: " + echoDelay.ToString("F2") + "s");
    }
}
