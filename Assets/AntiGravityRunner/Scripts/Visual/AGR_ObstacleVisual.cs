// ============================================================
// AGR_ObstacleVisual.cs — Neon Glow Style
// ============================================================

using UnityEngine;

public class AGR_ObstacleVisual : MonoBehaviour
{
    private Material neonMat;

    void Start()
    {
        Renderer r = GetComponent<Renderer>();
        if (r != null)
        {
            // Match the ground theme: Dark bodies with Synthwave Cyan or Purple glow
            neonMat = new Material(Shader.Find("Standard"));
            
            // 50/50 chance for Cyan or Purple to match the ground's Tron grid lines
            Color synthColor = Random.value > 0.5f ? new Color(0.5f, 0f, 1f) : new Color(0f, 0.8f, 1f);
            
            neonMat.color = new Color(0.01f, 0.01f, 0.03f); // Same jet-black color as the void
            neonMat.EnableKeyword("_EMISSION");
            
            // Soft glowing body to separate them from the ground
            neonMat.SetColor("_EmissionColor", synthColor * 1.5f); 
            
            neonMat.SetFloat("_Metallic", 0f);
            neonMat.SetFloat("_Glossiness", 0f);
            
            // Generate a crisp procedural grid wireframe texture to match the floor!
            Texture2D gridTex = new Texture2D(64, 64);
            for (int y = 0; y < 64; y++)
            {
                for (int x = 0; x < 64; x++)
                {
                    // Thin wireframe outline on the border of each UV tile
                    if (x < 2 || x > 61 || y < 2 || y > 61)
                        gridTex.SetPixel(x, y, Color.white);
                    else
                        gridTex.SetPixel(x, y, Color.black);
                }
            }
            gridTex.Apply();

            // Apply texture
            neonMat.SetTexture("_MainTex", gridTex);
            neonMat.SetTexture("_EmissionMap", gridTex);

            // Scale texture based on the shape of the obstacle so grids remain perfectly square
            Vector3 scale = transform.localScale;
            neonMat.mainTextureScale = new Vector2(scale.x, scale.y);

            r.material = neonMat;
        }

        // Clean up legacy
        foreach (Transform child in transform)
        {
            if (child.name == "Edge")
            {
                Destroy(child.gameObject);
            }
        }
    }
}
