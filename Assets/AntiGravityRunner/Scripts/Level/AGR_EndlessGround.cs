// ============================================================
// AGR_EndlessGround.cs — NO MORE GLITCH + Neon Grid Skin!
// ============================================================
// Uses a MASSIVE ground plane + FOG to hide edges
// Adds neon grid lines on the ground AND walls for a TRON look
// ATTACH THIS TO: LevelGenerator object
// ============================================================

using UnityEngine;

public class AGR_EndlessGround : MonoBehaviour
{
    [Header("References")]
    [SerializeField] private Transform player;

    [Header("Settings")]
    [SerializeField] private float groundWidth = 10f;
    [SerializeField] private float groundLength = 2000f; // MUCH longer!
    [SerializeField] private float groundY = 0f;

    private GameObject ground;
    private GameObject leftWall;
    private GameObject rightWall;
    
    // Grid Lines Arrays
    private GameObject[] gridLinesX; // Lateral ground lines
    private GameObject[] gridLinesZ; // Forward ground lines
    
    private GameObject[] wallLinesY_L; // Horizontal lines on left wall
    private GameObject[] wallLinesY_R; // Horizontal lines on right wall
    private GameObject[] wallLinesZ_L; // Vertical lines on left wall
    private GameObject[] wallLinesZ_R; // Vertical lines on right wall

    private int gridLineCountZ = 40;  // Lines going forward (lateral repeats)
    private int gridLineCountX = 6;   // Lines going left-right
    
    private int wallLineCountY = 4;   // Horizontal lines on the wall
    
    private float gridSpacingZ = 5f;  // Distance between Z lines

    void Awake()
    {
        // Delete old Ground and Ceiling IMMEDIATELY
        GameObject oldGround = GameObject.Find("Ground");
        if (oldGround != null) DestroyImmediate(oldGround);

        GameObject oldCeiling = GameObject.Find("Ceiling");
        if (oldCeiling != null) DestroyImmediate(oldCeiling);

        // Also destroy any previously created endless ground (scene reload)
        GameObject oldEndless = GameObject.Find("EndlessGround");
        if (oldEndless != null && oldEndless != ground) DestroyImmediate(oldEndless);

        // === CREATE GROUND ===
        ground = GameObject.CreatePrimitive(PrimitiveType.Cube);
        ground.name = "EndlessGround";
        ground.transform.localScale = new Vector3(groundWidth, 0.5f, groundLength);

        Renderer gR = ground.GetComponent<Renderer>();
        Material gMat = new Material(Shader.Find("Standard"));
        gMat.color = new Color(0.02f, 0.02f, 0.05f); // Very dark
        gMat.EnableKeyword("_EMISSION");
        gMat.SetColor("_EmissionColor", new Color(0f, 0.05f, 0.1f) * 0.3f);
        gR.material = gMat;

        // === SIDE WALLS ===
        leftWall = CreateSideWall(-groundWidth / 2f - 0.25f, "LeftWall");
        rightWall = CreateSideWall(groundWidth / 2f + 0.25f, "RightWall");

        // === NEON GRID LINES (TRON effect!) ===
        CreateGridLines();

        // === FOG — hides the ground edges in the distance ===
        RenderSettings.fog = true;
        RenderSettings.fogMode = FogMode.Exponential;
        RenderSettings.fogDensity = 0.008f;
        RenderSettings.fogColor = new Color(0.01f, 0.01f, 0.03f); // Match dark bg

        // === AMBIENT — very dark ===
        RenderSettings.ambientMode = UnityEngine.Rendering.AmbientMode.Flat;
        RenderSettings.ambientLight = new Color(0.03f, 0.03f, 0.06f);

        // Snap positions immediately
        SnapPositions();
    }

    private void CreateGridLines()
    {
        Material lineMat = new Material(Shader.Find("Standard"));
        lineMat.color = new Color(0f, 0.5f, 0.8f, 0.6f);
        lineMat.EnableKeyword("_EMISSION");
        lineMat.SetColor("_EmissionColor", new Color(0f, 0.3f, 0.6f) * 1.5f);

        Material lineMat2 = new Material(Shader.Find("Standard"));
        lineMat2.color = new Color(0.3f, 0f, 0.6f, 0.4f);
        lineMat2.EnableKeyword("_EMISSION");
        lineMat2.SetColor("_EmissionColor", new Color(0.2f, 0f, 0.5f) * 1.2f);

        // --- GROUND: Forward-facing grid lines (run along Z axis) ---
        gridLinesX = new GameObject[gridLineCountX];
        float xStep = groundWidth / (gridLineCountX + 1);

        for (int i = 0; i < gridLineCountX; i++)
        {
            float x = -groundWidth / 2f + xStep * (i + 1);
            GameObject line = GameObject.CreatePrimitive(PrimitiveType.Cube);
            line.name = "GridLineX_" + i;
            line.transform.localScale = new Vector3(0.03f, 0.52f, groundLength);

            Collider col = line.GetComponent<Collider>();
            if (col != null) Destroy(col);

            line.GetComponent<Renderer>().material = lineMat;
            gridLinesX[i] = line;
        }

        // --- GROUND: Lateral grid lines (run along X axis, repeating) ---
        gridLinesZ = new GameObject[gridLineCountZ];
        for (int i = 0; i < gridLineCountZ; i++)
        {
            GameObject line = GameObject.CreatePrimitive(PrimitiveType.Cube);
            line.name = "GridLineZ_" + i;
            line.transform.localScale = new Vector3(groundWidth, 0.52f, 0.03f);

            Collider col = line.GetComponent<Collider>();
            if (col != null) Destroy(col);

            line.GetComponent<Renderer>().material = lineMat2;
            gridLinesZ[i] = line;
        }

        // --- WALLS: Horizontal lines (run along Z axis) ---
        wallLinesY_L = new GameObject[wallLineCountY];
        wallLinesY_R = new GameObject[wallLineCountY];
        float yStep = 8f / (wallLineCountY + 1); // Wall height is 8

        for (int i = 0; i < wallLineCountY; i++)
        {
            // Left Wall
            GameObject lineL = GameObject.CreatePrimitive(PrimitiveType.Cube);
            lineL.name = "WallLineY_L_" + i;
            lineL.transform.localScale = new Vector3(0.32f, 0.03f, groundLength);
            Destroy(lineL.GetComponent<Collider>());
            lineL.GetComponent<Renderer>().material = lineMat;
            wallLinesY_L[i] = lineL;

            // Right Wall
            GameObject lineR = GameObject.CreatePrimitive(PrimitiveType.Cube);
            lineR.name = "WallLineY_R_" + i;
            lineR.transform.localScale = new Vector3(0.32f, 0.03f, groundLength);
            Destroy(lineR.GetComponent<Collider>());
            lineR.GetComponent<Renderer>().material = lineMat;
            wallLinesY_R[i] = lineR;
        }

        // --- WALLS: Vertical lines (repeating the lateral pattern) ---
        // Need gridLineCountZ vertical lines to match the ground Z lines!
        wallLinesZ_L = new GameObject[gridLineCountZ];
        wallLinesZ_R = new GameObject[gridLineCountZ];

        for (int i = 0; i < gridLineCountZ; i++)
        {
            // Left Wall
            GameObject lineL = GameObject.CreatePrimitive(PrimitiveType.Cube);
            lineL.name = "WallLineZ_L_" + i;
            lineL.transform.localScale = new Vector3(0.32f, 8f, 0.03f);
            Destroy(lineL.GetComponent<Collider>());
            lineL.GetComponent<Renderer>().material = lineMat2;
            wallLinesZ_L[i] = lineL;

            // Right Wall
            GameObject lineR = GameObject.CreatePrimitive(PrimitiveType.Cube);
            lineR.name = "WallLineZ_R_" + i;
            lineR.transform.localScale = new Vector3(0.32f, 8f, 0.03f);
            Destroy(lineR.GetComponent<Collider>());
            lineR.GetComponent<Renderer>().material = lineMat2;
            wallLinesZ_R[i] = lineR;
        }
    }

    void Update()
    {
        if (player == null) return;
        UpdatePositions();
    }

    private void SnapPositions()
    {
        float z = 0f;
        if (player != null) z = player.position.z;

        if (ground != null)
            ground.transform.position = new Vector3(0, groundY, z);

        UpdateWallPositions(z);
        UpdateGridPositions(z);
    }

    private void UpdatePositions()
    {
        float z = player.position.z;

        if (ground != null)
            ground.transform.position = new Vector3(0, groundY, z);

        UpdateWallPositions(z);
        UpdateGridPositions(z);
    }

    private void UpdateWallPositions(float z)
    {
        if (leftWall != null)
            leftWall.transform.position = new Vector3(-groundWidth / 2f - 0.25f, 3f, z);

        if (rightWall != null)
            rightWall.transform.position = new Vector3(groundWidth / 2f + 0.25f, 3f, z);
    }

    private void UpdateGridPositions(float z)
    {
        // Ground Forward grid lines follow player
        if (gridLinesX != null)
        {
            float xStep = groundWidth / (gridLineCountX + 1);
            for (int i = 0; i < gridLinesX.Length; i++)
            {
                if (gridLinesX[i] != null)
                {
                    float x = -groundWidth / 2f + xStep * (i + 1);
                    gridLinesX[i].transform.position = new Vector3(x, groundY + 0.01f, z);
                }
            }
        }

        // Wall Horizontal grid lines follow player
        if (wallLinesY_L != null && wallLinesY_R != null)
        {
            float yStep = 8f / (wallLineCountY + 1); // Wall is at y=3, h=8. From y=-1 to y=7
            for (int i = 0; i < wallLinesY_L.Length; i++)
            {
                if (wallLinesY_L[i] != null && wallLinesY_R[i] != null)
                {
                    float y = -1f + yStep * (i + 1);
                    wallLinesY_L[i].transform.position = new Vector3(-groundWidth / 2f - 0.25f, y, z);
                    wallLinesY_R[i].transform.position = new Vector3(groundWidth / 2f + 0.25f, y, z);
                }
            }
        }

        // Lateral / Vertical grid lines — tile infinitely using modulo
        if (gridLinesZ != null && wallLinesZ_L != null && wallLinesZ_R != null)
        {
            float baseZ = Mathf.Floor(z / gridSpacingZ) * gridSpacingZ;
            float startZ = baseZ - (gridLineCountZ / 2f) * gridSpacingZ;

            for (int i = 0; i < gridLinesZ.Length; i++)
            {
                float lineZ = startZ + i * gridSpacingZ;
                
                // Ground lateral
                if (gridLinesZ[i] != null)
                {
                    gridLinesZ[i].transform.position = new Vector3(0, groundY + 0.01f, lineZ);
                }

                // Wall vertical left
                if (wallLinesZ_L[i] != null)
                {
                    wallLinesZ_L[i].transform.position = new Vector3(-groundWidth / 2f - 0.25f, 3f, lineZ);
                }

                // Wall vertical right
                if (wallLinesZ_R[i] != null)
                {
                    wallLinesZ_R[i].transform.position = new Vector3(groundWidth / 2f + 0.25f, 3f, lineZ);
                }
            }
        }
    }

    private GameObject CreateSideWall(float x, string name)
    {
        GameObject wall = GameObject.CreatePrimitive(PrimitiveType.Cube);
        wall.name = name;
        wall.transform.localScale = new Vector3(0.3f, 8f, groundLength);

        Renderer r = wall.GetComponent<Renderer>();
        Material mat = new Material(Shader.Find("Standard"));
        
        // Exact same as ground material to seamlessly match
        mat.color = new Color(0.02f, 0.02f, 0.05f);
        mat.EnableKeyword("_EMISSION");
        mat.SetColor("_EmissionColor", new Color(0f, 0.05f, 0.1f) * 0.3f);
        
        r.material = mat;

        return wall;
    }
}
