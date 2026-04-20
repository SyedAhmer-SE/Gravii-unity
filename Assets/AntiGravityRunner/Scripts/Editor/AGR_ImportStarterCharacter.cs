#if UNITY_EDITOR
using UnityEngine;
using UnityEditor;
using UnityEditor.Animations;

public class AGR_ImportStarterCharacter : EditorWindow
{
    [MenuItem("Tools/AGR/Import Starter Asset Character")]
    public static void ImportCharacter()
    {
        AGR_PlayerController playerScript = FindObjectOfType<AGR_PlayerController>();
        if (playerScript == null) 
        {
            EditorUtility.DisplayDialog("Error", "Could not find Player object!", "OK");
            return;
        }
        
        GameObject player = playerScript.gameObject;
        
        // 1. Clean current visual models / Cube visuals
        for (int i = player.transform.childCount - 1; i >= 0; i--)
        {
            Transform child = player.transform.GetChild(i);
            if (child.GetComponentInChildren<SkinnedMeshRenderer>() != null || child.GetComponent<Animator>() != null || child.name.Contains("Armature"))
            {
                DestroyImmediate(child.gameObject);
            }
        }
        
        MeshRenderer mr = player.GetComponent<MeshRenderer>();
        if (mr != null) mr.enabled = false; // Hide the classic cube
        MeshFilter mf = player.GetComponent<MeshFilter>();
        if (mf != null) DestroyImmediate(mf);
        
        // 2. Load the Starter Asset prefab
        string prefabPath = "Assets/StarterAssets/ThirdPersonController/Prefabs/PlayerArmature.prefab";
        GameObject starterPrefab = AssetDatabase.LoadAssetAtPath<GameObject>(prefabPath);
        
        if (starterPrefab == null)
        {
            EditorUtility.DisplayDialog("Error", "Could not find StarterAssets PlayerArmature prefab at: " + prefabPath + "\n\nMake sure the Starter Assets package is fully imported.", "OK");
            return;
        }
        
        // 3. Instantiate it as a child of our Player
        GameObject newChar = (GameObject)PrefabUtility.InstantiatePrefab(starterPrefab);
        newChar.transform.SetParent(player.transform);
        newChar.transform.localPosition = Vector3.zero;
        newChar.transform.localRotation = Quaternion.identity;
        
        // 4. Strip the Starter Asset control scripts so they don't break our AntiGravityRunner logic!
        MonoBehaviour[] scripts = newChar.GetComponents<MonoBehaviour>();
        foreach (MonoBehaviour script in scripts)
        {
            if (script != null) DestroyImmediate(script); // Removes ThirdPersonController, StarterAssetsInputs, etc.
        }
        
        // Remove Unity Input components and CharacterController
        UnityEngine.InputSystem.PlayerInput pInput = newChar.GetComponent<UnityEngine.InputSystem.PlayerInput>();
        if (pInput != null) DestroyImmediate(pInput);
        
        CharacterController cc = newChar.GetComponent<CharacterController>();
        if (cc != null) DestroyImmediate(cc);
        
        // 5. Connect our custom Parkour Animator so they can slide and run forever
        Animator anim = newChar.GetComponent<Animator>();
        if (anim != null)
        {
            anim.applyRootMotion = false;
            RuntimeAnimatorController ctrl = Resources.Load<RuntimeAnimatorController>("ParkourAnimator");
            if (ctrl != null)
            {
                anim.runtimeAnimatorController = ctrl;
            }
            else
            {
                Debug.LogWarning("AGR: Could not find ParkourAnimator. Run Auto-Build Animations to generate it.");
            }
        }
        
        EditorUtility.SetDirty(player);
        UnityEditor.SceneManagement.EditorSceneManager.MarkSceneDirty(UnityEditor.SceneManagement.EditorSceneManager.GetActiveScene());
        UnityEditor.SceneManagement.EditorSceneManager.SaveOpenScenes();
        
        EditorUtility.DisplayDialog("Success!", "Imported the Starter Assets Unity character perfectly.\n\nHis extra movement scripts were stripped so he is now fully possessed by the AntiGravityRunner script!\n\nHit Play!", "Awesome");
    }
}
#endif
