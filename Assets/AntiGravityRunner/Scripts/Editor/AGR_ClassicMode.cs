#if UNITY_EDITOR
using UnityEngine;
using UnityEditor;

public class AGR_ClassicMode : EditorWindow
{
    [MenuItem("Tools/AGR/Revert To Glowing Cube")]
    public static void RevertToCube()
    {
        AGR_PlayerController playerScript = FindObjectOfType<AGR_PlayerController>();
        if (playerScript == null) 
        {
            EditorUtility.DisplayDialog("Error", "Could not find Player object!", "OK");
            return;
        }
        
        GameObject player = playerScript.gameObject;
        
        // 1. Delete all child character models
        int removed = 0;
        for (int i = player.transform.childCount - 1; i >= 0; i--)
        {
            Transform child = player.transform.GetChild(i);
            if (child.GetComponentInChildren<SkinnedMeshRenderer>() != null || child.GetComponent<Animator>() != null || child.name.Contains("Ch44") || child.name.Contains("Character"))
            {
                DestroyImmediate(child.gameObject);
                removed++;
            }
        }
        
        // 2. Remove Animator on the player if any exist
        if (player.GetComponent<Animator>() != null) DestroyImmediate(player.GetComponent<Animator>());
        if (player.GetComponent<Animation>() != null) DestroyImmediate(player.GetComponent<Animation>());
        
        // 3. Ensure player has a MeshRenderer and MeshFilter
        MeshRenderer mr = player.GetComponent<MeshRenderer>();
        if (mr == null)
        {
            mr = player.AddComponent<MeshRenderer>();
            mr.sharedMaterial = new Material(Shader.Find("Standard"));
        }
        mr.enabled = true; // Make sure it's visible again!
        
        MeshFilter mf = player.GetComponent<MeshFilter>();
        if (mf == null) 
        {
            mf = player.AddComponent<MeshFilter>();
            GameObject cube = GameObject.CreatePrimitive(PrimitiveType.Cube);
            mf.sharedMesh = cube.GetComponent<MeshFilter>().sharedMesh;
            DestroyImmediate(cube);
        }
        
        // 4. Reset Scale
        player.transform.localScale = new Vector3(1, 1, 1);
        
        EditorUtility.SetDirty(player);
        UnityEditor.SceneManagement.EditorSceneManager.MarkSceneDirty(UnityEditor.SceneManagement.EditorSceneManager.GetActiveScene());
        UnityEditor.SceneManagement.EditorSceneManager.SaveOpenScenes();
        
        EditorUtility.DisplayDialog("Classic Mode Activated", "Removed " + removed + " character models.\n\nThe classic glowing shape is back!\nJump and Slide physics will still work perfectly.", "Awesome!");
    }
}
#endif
