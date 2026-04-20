// ============================================================
// AGR_VisualSetup.cs — Auto-finds and attaches character model!
// ============================================================
// If a character model exists in the scene but isn't parented
// to the player, this script will find it and fix it
// Also handles cube glow, edges, and trail
// ATTACH THIS TO: The Player GameObject
// ============================================================

using UnityEngine;

public class AGR_VisualSetup : MonoBehaviour
{
    [Header("Player Trail")]
    [SerializeField] private Color trailColor = new Color(0f, 1f, 1f); // Cyan
    [SerializeField] private float trailTime = 0.5f;
    [SerializeField] private float trailWidth = 0.3f;

    [Header("Glow Settings")]
    [SerializeField] private Color playerGlowColor = new Color(0f, 0.9f, 1f); // Cyan
    [SerializeField] private float glowIntensity = 2f;

    private Material playerMat;
    private bool usingCharacterModel = false;

    void Start()
    {
        // Step 1: Try to find character model as a child first
        Animator childAnimator = GetComponentInChildren<Animator>();
        bool hasChildModel = (childAnimator != null && childAnimator.gameObject != gameObject);

        // Step 2: If no child model, search the ENTIRE SCENE for orphaned character models
        // BUT skip anything that belongs to the Ghost!
        if (!hasChildModel)
        {
            SkinnedMeshRenderer[] allSkinned = FindObjectsOfType<SkinnedMeshRenderer>();
            foreach (SkinnedMeshRenderer smr in allSkinned)
            {
                // Don't grab UI elements or other players
                if (smr.transform.root == transform) continue;

                GameObject modelRoot = smr.transform.root.gameObject;

                // CRITICAL: Skip models that belong to the Ghost!
                if (modelRoot.name.ToLower().Contains("warrok")) continue;
                if (modelRoot.GetComponent<AGR_GhostChaser>() != null) continue;
                if (modelRoot.transform.parent != null && 
                    modelRoot.transform.parent.GetComponent<AGR_GhostChaser>() != null) continue;
                
                // Skip if the model's root parent is the Ghost
                Transform rootParent = smr.transform;
                bool isGhostChild = false;
                while (rootParent != null)
                {
                    if (rootParent.GetComponent<AGR_GhostChaser>() != null)
                    {
                        isGhostChild = true;
                        break;
                    }
                    rootParent = rootParent.parent;
                }
                if (isGhostChild) continue;

                // Check if it's a character model
                Animator modelAnim = modelRoot.GetComponentInChildren<Animator>();
                if (modelAnim != null || smr.bones.Length > 10)
                {
                    Debug.Log("AGR_VisualSetup: Found orphaned character model '" +
                        modelRoot.name + "' — attaching to player!");

                    // Re-parent to player
                    modelRoot.transform.SetParent(transform);
                    modelRoot.transform.localPosition = Vector3.zero;
                    modelRoot.transform.localRotation = Quaternion.identity;

                    // Auto-detect correct scale
                    Bounds bounds = CalculateBounds(modelRoot);
                    float modelHeight = bounds.size.y;

                    if (modelHeight > 10f)
                    {
                        float targetHeight = 1.8f;
                        float scaleFactor = targetHeight / modelHeight;
                        modelRoot.transform.localScale = Vector3.one * scaleFactor;
                        Debug.Log("AGR_VisualSetup: Scaled model from " +
                            modelHeight.ToString("F1") + " to " + targetHeight + " units");
                    }
                    else if (modelHeight < 0.1f)
                    {
                        modelRoot.transform.localScale = Vector3.one * 100f;
                    }

                    // Align feet to ground
                    bounds = CalculateBounds(modelRoot);
                    float feetOffset = bounds.min.y - transform.position.y;
                    Vector3 pos = modelRoot.transform.localPosition;
                    pos.y -= feetOffset + 0.5f;
                    modelRoot.transform.localPosition = pos;

                    // Disable root motion on Animator
                    if (modelAnim != null)
                    {
                        modelAnim.applyRootMotion = false;
                    }

                    hasChildModel = true;
                    childAnimator = modelAnim;
                    break;
                }
            }
        }

        // Step 3: Apply visuals based on what we have
        if (hasChildModel && childAnimator != null)
        {
            SetupCharacterModelVisuals(childAnimator.gameObject);
            usingCharacterModel = true;
        }
        else
        {
            SetupCubeVisuals();
        }

        // Add trail to player regardless of model type
        AddTrail();
    }

    private Bounds CalculateBounds(GameObject obj)
    {
        Renderer[] renderers = obj.GetComponentsInChildren<Renderer>();
        if (renderers.Length == 0) return new Bounds(obj.transform.position, Vector3.one);

        Bounds bounds = renderers[0].bounds;
        for (int i = 1; i < renderers.Length; i++)
        {
            bounds.Encapsulate(renderers[i].bounds);
        }
        return bounds;
    }

    // ==================== CUBE PLAYER (no FBX model) ====================
    private void SetupCubeVisuals()
    {
        Renderer playerRenderer = GetComponent<Renderer>();
        if (playerRenderer == null) return;

        // Create premium glowing material
        playerMat = new Material(Shader.Find("Standard"));
        playerMat.color = new Color(0f, 0.3f, 0.4f); // Dark cyan core
        playerMat.EnableKeyword("_EMISSION");
        playerMat.SetColor("_EmissionColor", playerGlowColor * glowIntensity);
        playerMat.SetFloat("_Glossiness", 0.9f);
        playerMat.SetFloat("_Metallic", 0.5f);
        playerRenderer.material = playerMat;

        // Add neon edge outlines to the cube
        CreatePlayerEdges();

        // Add a point light inside the player for ambient glow
        AddPlayerGlow(Vector3.zero);
    }

    // ==================== CHARACTER MODEL (FBX) ====================
    private void SetupCharacterModelVisuals(GameObject model)
    {
        // Hide the cube renderer (player body is the FBX now)
        Renderer cubeRenderer = GetComponent<Renderer>();
        if (cubeRenderer != null)
        {
            cubeRenderer.enabled = false;
        }

        // Also hide the cube's MeshFilter to prevent collision shape showing
        MeshRenderer cubeMeshRend = GetComponent<MeshRenderer>();
        if (cubeMeshRend != null)
        {
            cubeMeshRend.enabled = false;
        }

        // Apply glow material to all renderers in the character model
        Renderer[] modelRenderers = model.GetComponentsInChildren<Renderer>();
        foreach (Renderer r in modelRenderers)
        {
            if (r.material != null)
            {
                r.material.EnableKeyword("_EMISSION");
                r.material.SetColor("_EmissionColor", playerGlowColor * 0.3f);
            }
        }

        // Add ambient glow light
        AddPlayerGlow(new Vector3(0, 1f, 0));

        Debug.Log("AGR_VisualSetup: Character model visuals applied! Cube hidden.");
    }

    private void AddPlayerGlow(Vector3 offset)
    {
        GameObject glowLight = new GameObject("PlayerGlow");
        glowLight.transform.SetParent(transform, false);
        glowLight.transform.localPosition = offset;
        Light light = glowLight.AddComponent<Light>();
        light.type = LightType.Point;
        light.color = playerGlowColor;
        light.intensity = 0.4f;  // Much softer — no more "lightning beam"
        light.range = 2f;        // Smaller range
    }

    private void CreatePlayerEdges()
    {
        Material edgeMat = new Material(Shader.Find("Standard"));
        edgeMat.color = playerGlowColor;
        edgeMat.EnableKeyword("_EMISSION");
        edgeMat.SetColor("_EmissionColor", playerGlowColor * 3f);

        Vector3 s = transform.localScale;
        float t = 0.04f;

        // 4 vertical edges
        CreateEdge(new Vector3(-0.5f, 0, -0.5f), new Vector3(t/s.x, 1f, t/s.z), edgeMat);
        CreateEdge(new Vector3(0.5f, 0, -0.5f), new Vector3(t/s.x, 1f, t/s.z), edgeMat);
        CreateEdge(new Vector3(-0.5f, 0, 0.5f), new Vector3(t/s.x, 1f, t/s.z), edgeMat);
        CreateEdge(new Vector3(0.5f, 0, 0.5f), new Vector3(t/s.x, 1f, t/s.z), edgeMat);

        // Top edges
        CreateEdge(new Vector3(0, 0.5f, -0.5f), new Vector3(1f, t/s.y, t/s.z), edgeMat);
        CreateEdge(new Vector3(0, 0.5f, 0.5f), new Vector3(1f, t/s.y, t/s.z), edgeMat);
        CreateEdge(new Vector3(-0.5f, 0.5f, 0), new Vector3(t/s.x, t/s.y, 1f), edgeMat);
        CreateEdge(new Vector3(0.5f, 0.5f, 0), new Vector3(t/s.x, t/s.y, 1f), edgeMat);

        // Bottom edges
        CreateEdge(new Vector3(0, -0.5f, -0.5f), new Vector3(1f, t/s.y, t/s.z), edgeMat);
        CreateEdge(new Vector3(0, -0.5f, 0.5f), new Vector3(1f, t/s.y, t/s.z), edgeMat);
        CreateEdge(new Vector3(-0.5f, -0.5f, 0), new Vector3(t/s.x, t/s.y, 1f), edgeMat);
        CreateEdge(new Vector3(0.5f, -0.5f, 0), new Vector3(t/s.x, t/s.y, 1f), edgeMat);
    }

    private void CreateEdge(Vector3 localPos, Vector3 localScale, Material mat)
    {
        GameObject edge = GameObject.CreatePrimitive(PrimitiveType.Cube);
        edge.name = "PlayerEdge";
        edge.transform.SetParent(transform, false);
        edge.transform.localPosition = localPos;
        edge.transform.localScale = localScale;

        Collider col = edge.GetComponent<Collider>();
        if (col != null) Destroy(col);

        edge.GetComponent<Renderer>().material = mat;
    }

    private void AddTrail()
    {
        TrailRenderer trail = gameObject.AddComponent<TrailRenderer>();
        trail.time = trailTime;
        trail.startWidth = trailWidth;
        trail.endWidth = 0f;
        trail.material = CreateGlowMaterial(trailColor);
        trail.startColor = trailColor;
        trail.endColor = new Color(trailColor.r, trailColor.g, trailColor.b, 0f);
        trail.minVertexDistance = 0.1f;

        // Prevent "lightning" streak on spawn — disable trail and use REALTIME delay
        // (Invoke doesn't work when Time.timeScale = 0!)
        trail.emitting = false;
        trail.Clear();
        StartCoroutine(EnableTrailAfterDelay(trail, 0.5f));
    }

    private System.Collections.IEnumerator EnableTrailAfterDelay(TrailRenderer trail, float delay)
    {
        yield return new WaitForSecondsRealtime(delay);
        if (trail != null)
        {
            trail.Clear();
            trail.emitting = true;
        }
    }

    private Material CreateGlowMaterial(Color color)
    {
        Material mat = new Material(Shader.Find("Standard"));
        mat.color = color;
        mat.EnableKeyword("_EMISSION");
        mat.SetColor("_EmissionColor", color * 2f);
        return mat;
    }

    void Update()
    {
        // Subtle pulse on player material (only for cube mode)
        if (playerMat != null && !usingCharacterModel)
        {
            float pulse = Mathf.Lerp(1.5f, 2.5f,
                (Mathf.Sin(Time.time * 3f) + 1f) * 0.5f);
            playerMat.SetColor("_EmissionColor", playerGlowColor * pulse);
        }
    }
}
