#if UNITY_EDITOR
using UnityEngine;
using UnityEditor;
using UnityEditor.Animations;

public class AGR_AnimatorBuilder : EditorWindow
{
    [MenuItem("Tools/AGR/Auto-Build Animations")]
    public static void BuildAnimator()
    {
        // 1. Find the Player
        AGR_PlayerController playerScript = FindObjectOfType<AGR_PlayerController>();
        if (playerScript == null) {
            EditorUtility.DisplayDialog("AGR Error", "Could not find Player!", "OK");
            return;
        }
        GameObject player = playerScript.gameObject;

        // Clean out ANY rogue things on Player root
        Animator rogueAnim = player.GetComponent<Animator>();
        if (rogueAnim != null) DestroyImmediate(rogueAnim);
        Animation rogueLegacy = player.GetComponent<Animation>();
        if (rogueLegacy != null) DestroyImmediate(rogueLegacy);

        // 2. Set ALL FBX files in AntiGravityRunner to Humanoid rig
        string[] modelGuids = AssetDatabase.FindAssets("t:Model", new[] { "Assets/AntiGravityRunner" });
        string mainFBXPath = null;
        
        int rigChanges = 0;
        foreach (string guid in modelGuids)
        {
            string path = AssetDatabase.GUIDToAssetPath(guid);
            ModelImporter importer = AssetImporter.GetAtPath(path) as ModelImporter;
            if (importer != null)
            {
                if (importer.animationType != ModelImporterAnimationType.Human)
                {
                    importer.animationType = ModelImporterAnimationType.Human;
                    importer.SaveAndReimport();
                    rigChanges++;
                }

                if (!path.Contains("@")) 
                {
                    mainFBXPath = path;
                }
            }
        }
        
        if (rigChanges > 0)
        {
            AssetDatabase.Refresh();
        }

        if (mainFBXPath == null)
        {
            EditorUtility.DisplayDialog("AGR Error", "Could not find main character FBX!", "OK");
            return;
        }

        // Extract Avatar
        Avatar mainAvatar = null;
        Object[] mainAssets = AssetDatabase.LoadAllAssetsAtPath(mainFBXPath);
        foreach (Object o in mainAssets)
        {
            if (o is Avatar av) { mainAvatar = av; break; }
        }

        // 3. Delete old character and create FRESH instance from the FBX
        for (int i = player.transform.childCount - 1; i >= 0; i--)
        {
            Transform child = player.transform.GetChild(i);
            if (child.GetComponentInChildren<Renderer>() != null || child.GetComponent<Animator>() != null || child.GetComponent<Animation>() != null)
            {
                DestroyImmediate(child.gameObject);
            }
        }

        // Instantiate
        GameObject fbxPrefab = AssetDatabase.LoadAssetAtPath<GameObject>(mainFBXPath);
        GameObject freshChar = (GameObject)PrefabUtility.InstantiatePrefab(fbxPrefab);
        freshChar.transform.SetParent(player.transform);
        freshChar.transform.localPosition = Vector3.zero;
        freshChar.transform.localRotation = Quaternion.identity;
        freshChar.transform.localScale = Vector3.one;

        // Strip legacy, ensure Animator
        Animation freshLegacy = freshChar.GetComponent<Animation>();
        if (freshLegacy != null) DestroyImmediate(freshLegacy);
        
        Animator childAnim = freshChar.GetComponent<Animator>();
        if (childAnim == null) childAnim = freshChar.AddComponent<Animator>();
        childAnim.avatar = mainAvatar;
        childAnim.applyRootMotion = false; // CRITICAL for smooth movement

        // 4. Find valid Animation Clips with Humanoid retargeting
        AnimationClip runClip = null, jumpClip = null, slideClip = null, leftClip = null, rightClip = null;
        foreach (string guid in modelGuids)
        {
            string path = AssetDatabase.GUIDToAssetPath(guid);
            Object[] assets = AssetDatabase.LoadAllAssetsAtPath(path);
            foreach (Object o in assets)
            {
                if (o is AnimationClip clip && !clip.name.StartsWith("__"))
                {
                    string nm = clip.name.ToLower();
                    string p = path.ToLower();

                    // Specifically target the "Running.fbx" or similar for run
                    if ((nm.Contains("run") || p.Contains("run")) && !p.Contains("slide") && !p.Contains("jump") && !p.Contains("left") && !p.Contains("right")) 
                        runClip = clip;
                    else if (p.Contains("jump")) jumpClip = clip;
                    else if (p.Contains("slide")) slideClip = clip;
                    else if (p.Contains("left")) leftClip = clip;
                    else if (p.Contains("right")) rightClip = clip;
                }
            }
        }

        if (runClip == null) {
            EditorUtility.DisplayDialog("AGR Error", "Could not find Run animation!", "OK");
            return;
        }

        // Set Loop Time on Run Clip! Mixamo files MUST have Loop Time enabled on the FBX
        // Unity doesn't let us modify imported clip loop settings here directly without modifying the ModelImporter clipAnimations.
        // So we will modify the importer settings to ensure loop time is ON!
        foreach (string guid in modelGuids)
        {
            string path = AssetDatabase.GUIDToAssetPath(guid);
            if (path.ToLower().Contains("run") && !path.ToLower().Contains("jump") && !path.ToLower().Contains("slide"))
            {
                ModelImporter importer = AssetImporter.GetAtPath(path) as ModelImporter;
                if (importer != null)
                {
                    var clips = importer.defaultClipAnimations;
                    if (clips != null && clips.Length > 0)
                    {
                        bool needsReimport = false;
                        for (int i = 0; i < clips.Length; i++)
                        {
                            if (!clips[i].loopTime)
                            {
                                clips[i].loopTime = true;
                                needsReimport = true;
                            }
                        }
                        if (needsReimport)
                        {
                            importer.clipAnimations = clips;
                            importer.SaveAndReimport();
                        }
                    }
                }
            }
        }

        // 5. Build Animator Controller
        string savePath = "Assets/AntiGravityRunner/ParkourAnimator.controller";
        if (AssetDatabase.LoadAssetAtPath<AnimatorController>(savePath) != null)
            AssetDatabase.DeleteAsset(savePath);

        AnimatorController controller = AnimatorController.CreateAnimatorControllerAtPath(savePath);
        controller.AddParameter("Jump", AnimatorControllerParameterType.Trigger);
        controller.AddParameter("Slide", AnimatorControllerParameterType.Trigger);
        controller.AddParameter("DodgeLeft", AnimatorControllerParameterType.Trigger);
        controller.AddParameter("DodgeRight", AnimatorControllerParameterType.Trigger);

        AnimatorStateMachine sm = controller.layers[0].stateMachine;

        // Run State
        AnimatorState runState = sm.AddState("Run");
        runState.motion = runClip;
        sm.defaultState = runState;

        AnimatorState jumpState = sm.AddState("Jump");
        if (jumpClip != null) jumpState.motion = jumpClip;
        AnimatorState slideState = sm.AddState("Slide");
        if (slideClip != null) slideState.motion = slideClip;
        AnimatorState leftState = sm.AddState("DodgeLeft");
        if (leftClip != null) leftState.motion = leftClip;
        AnimatorState rightState = sm.AddState("DodgeRight");
        if (rightClip != null) rightState.motion = rightClip;

        void AddTrans(AnimatorState from, AnimatorState to, string trigger)
        {
            if (to.motion == null) return;
            var t = from.AddTransition(to);
            t.AddCondition(AnimatorConditionMode.If, 0, trigger);
            t.hasExitTime = false; t.duration = 0.1f;
            
            // Return transition
            var b = to.AddTransition(from);
            b.hasExitTime = true; 
            b.exitTime = 0.85f; // Blend out near the very end
            b.duration = 0.15f; // Short blend
        }

        AddTrans(runState, jumpState, "Jump");
        AddTrans(runState, slideState, "Slide");
        AddTrans(runState, leftState, "DodgeLeft");
        AddTrans(runState, rightState, "DodgeRight");

        // 6. Apply to character
        childAnim.runtimeAnimatorController = controller;
        EditorUtility.SetDirty(childAnim);
        EditorUtility.SetDirty(freshChar);

        // Copy to Resources for runtime
        if (!AssetDatabase.IsValidFolder("Assets/Resources"))
            AssetDatabase.CreateFolder("Assets", "Resources");
        if (AssetDatabase.LoadAssetAtPath<AnimatorController>("Assets/Resources/ParkourAnimator.controller") != null)
            AssetDatabase.DeleteAsset("Assets/Resources/ParkourAnimator.controller");
        AssetDatabase.CopyAsset(savePath, "Assets/Resources/ParkourAnimator.controller");

        AssetDatabase.SaveAssets();
        AssetDatabase.Refresh();
        UnityEditor.SceneManagement.EditorSceneManager.MarkSceneDirty(
            UnityEditor.SceneManagement.EditorSceneManager.GetActiveScene());
        UnityEditor.SceneManagement.EditorSceneManager.SaveOpenScenes();

        string msg = "DONE - RETURNED TO MECHAMIN / HUMANOID!\n\n";
        msg += "Rig changes: " + rigChanges + " files set back to Humanoid\n";
        msg += "Looping: Run has been FORCED to Loop in importer!\n";
        msg += "\nScene saved! Hit Play!";
        EditorUtility.DisplayDialog("AGR Animation Setup", msg, "Let's Go!");
    }
}
#endif
