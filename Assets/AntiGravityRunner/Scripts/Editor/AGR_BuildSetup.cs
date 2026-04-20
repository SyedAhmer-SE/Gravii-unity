#if UNITY_EDITOR
using UnityEditor;
using UnityEngine;

// ============================================================
// AGR_BuildSetup.cs
// ============================================================
// 1-Click tool to configure Android Project Settings
// (App Name, Package Name, Orientation) 
// ============================================================

public class AGR_BuildSetup : EditorWindow
{
    [MenuItem("Tools/AntiGravity/Setup Android Build")]
    public static void ApplyAndroidSettings()
    {
        // 1. Set Company & Product Name
        PlayerSettings.companyName = "AntiGravity";
        PlayerSettings.productName = "Gravi";

        // 2. Set App Identifier (Package Name)
        PlayerSettings.SetApplicationIdentifier(BuildTargetGroup.Android, "com.antigravity.gravi");

        // 3. Set Orientation to AutoRotation — player chooses Portrait or Landscape in-game
        PlayerSettings.defaultInterfaceOrientation = UIOrientation.AutoRotation;
        PlayerSettings.allowedAutorotateToLandscapeLeft = true;
        PlayerSettings.allowedAutorotateToLandscapeRight = true;
        PlayerSettings.allowedAutorotateToPortrait = true;
        PlayerSettings.allowedAutorotateToPortraitUpsideDown = true;

        // 4. Try to find an icon named "Icon" or "AppIcon"
        string[] iconGuids = AssetDatabase.FindAssets("t:Texture2D AppIcon");
        if (iconGuids.Length == 0)
        {
            iconGuids = AssetDatabase.FindAssets("t:Texture2D Icon");
        }

        if (iconGuids.Length > 0)
        {
            string path = AssetDatabase.GUIDToAssetPath(iconGuids[0]);
            Texture2D iconTex = AssetDatabase.LoadAssetAtPath<Texture2D>(path);
            
            // Set for all sizes
            Texture2D[] icons = new Texture2D[PlayerSettings.GetIconSizesForTargetGroup(BuildTargetGroup.Unknown).Length];
            for (int i = 0; i < icons.Length; i++)
            {
                icons[i] = iconTex;
            }
            PlayerSettings.SetIconsForTargetGroup(BuildTargetGroup.Unknown, icons);
            Debug.Log("✅ Applied Icon: " + path);
        }
        else
        {
            Debug.LogWarning("⚠️ No image named 'AppIcon' or 'Icon' found. Skipping icon setup.");
        }

        // Force save preferences
        AssetDatabase.SaveAssets();

        Debug.Log("✅ Android Build Settings Applied!");
        Debug.Log("App Name: " + PlayerSettings.productName);
        Debug.Log("Package Name: " + PlayerSettings.GetApplicationIdentifier(BuildTargetGroup.Android));
        
        EditorUtility.DisplayDialog("Setup Complete", "App Name changed to 'Gravi'!\n\nOrientation set to Auto — player selects Portrait or Landscape in Settings.\n\nYou are ready to hit Ctrl+B and build the APK!", "Awesome!");
    }
}
#endif
