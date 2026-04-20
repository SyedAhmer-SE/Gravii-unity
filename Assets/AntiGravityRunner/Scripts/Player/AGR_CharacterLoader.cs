// ============================================================
// AGR_CharacterLoader.cs — Auto-loads character model!
// ============================================================
// Finds Ch44_nonPBR in the scene and parents it under Player.
// Hides the cube. No manual dragging needed.
// ATTACH THIS TO: Player GameObject
// ============================================================

using UnityEngine;

public class AGR_CharacterLoader : MonoBehaviour
{
    [Header("Character Model")]
    [Tooltip("Drag your FBX character model here from the Project window")]
    [SerializeField] private GameObject characterPrefab;

    [Header("Transform Settings")]
    [Tooltip("Y offset to align feet with ground")]
    [SerializeField] private float yOffset = 0f;

    [Tooltip("Rotate character to face forward (Z axis)")]
    [SerializeField] private float yRotation = 0f;

    private GameObject spawnedCharacter;

    // Use Start, NOT Awake — GameObject.Find is unreliable in Awake
    void Start()
    {
        // Option 1: User dragged a prefab into the slot
        if (characterPrefab != null)
        {
            SpawnFromPrefab();
            HideCube();
            return;
        }

        // Option 2: Already a child (from editor fix scripts)
        Animator existingChild = GetComponentInChildren<Animator>();
        if (existingChild != null && existingChild.gameObject != gameObject)
        {
            Debug.Log("AGR_CharacterLoader: Character already attached as child!");
            HideCube();
            return;
        }

        // Option 3: Auto-find Ch44_nonPBR floating in the scene
        GameObject sceneChar = GameObject.Find("Ch44_nonPBR");
        if (sceneChar != null)
        {
            Debug.Log("AGR_CharacterLoader: Found Ch44_nonPBR, adopting!");
            sceneChar.transform.SetParent(transform, false);
            sceneChar.transform.localPosition = new Vector3(0, yOffset, 0);
            sceneChar.transform.localRotation = Quaternion.Euler(0, yRotation, 0);
            spawnedCharacter = sceneChar;
            HideCube();
            return;
        }

        Debug.Log("AGR_CharacterLoader: No character found. Running in Cube mode.");
    }

    private void SpawnFromPrefab()
    {
        spawnedCharacter = Instantiate(characterPrefab, transform);
        spawnedCharacter.name = "CharacterModel";
        spawnedCharacter.transform.localPosition = new Vector3(0, yOffset, 0);
        spawnedCharacter.transform.localRotation = Quaternion.Euler(0, yRotation, 0);
    }

    private void HideCube()
    {
        MeshRenderer cubeMesh = GetComponent<MeshRenderer>();
        if (cubeMesh != null) cubeMesh.enabled = false;
    }
}
