// ============================================================
// AGR_MobileButtons.cs — Only shows in Buttons mode!
// ============================================================
// Creates LEFT, RIGHT, JUMP, FALL buttons
// Only visible when control type is set to Buttons
// ATTACH THIS TO: Canvas object
// ============================================================

using UnityEngine;
using UnityEngine.UI;
using UnityEngine.EventSystems;

public class AGR_MobileButtons : MonoBehaviour
{
    public static bool leftHeld = false;
    public static bool rightHeld = false;
    public static bool jumpPressed = false;
    public static bool fallPressed = false;

    private GameObject buttonContainer;

    void Start()
    {
        CreateButtons();
        UpdateVisibility();
    }

    void Update()
    {
        // Check if control type changed
        UpdateVisibility();
    }

    void LateUpdate()
    {
        jumpPressed = false;
        fallPressed = false;
    }

    private void UpdateVisibility()
    {
        if (buttonContainer != null)
        {
            // Only show buttons when Buttons mode is selected!
            bool show = AGR_SettingsManager.CurrentControl == AGR_SettingsManager.ControlType.Buttons;
            buttonContainer.SetActive(show);
        }
    }

    private void CreateButtons()
    {
        // Container for all buttons
        buttonContainer = new GameObject("ButtonContainer");
        buttonContainer.transform.SetParent(transform, false);
        RectTransform crt = buttonContainer.AddComponent<RectTransform>();
        crt.anchorMin = Vector2.zero;
        crt.anchorMax = Vector2.one;
        crt.offsetMin = Vector2.zero;
        crt.offsetMax = Vector2.zero;

        float btnSize = 220f;
        float margin = 40f;

        // LEFT <
        CreateHoldButton("LeftBtn", "<",
            new Vector2(0, 0), new Vector2(0, 0),
            new Vector2(margin + btnSize / 2, margin + btnSize / 2),
            new Vector2(btnSize, btnSize),
            true, false);

        // RIGHT >
        CreateHoldButton("RightBtn", ">",
            new Vector2(0, 0), new Vector2(0, 0),
            new Vector2(margin * 2 + btnSize * 1.5f, margin + btnSize / 2),
            new Vector2(btnSize, btnSize),
            false, true);

        // JUMP ^
        CreateTapButton("JumpBtn", "^",
            new Vector2(1, 0), new Vector2(1, 0),
            new Vector2(-(margin + btnSize / 2), margin + btnSize / 2),
            new Vector2(btnSize, btnSize),
            true);

        // FALL v
        CreateTapButton("FallBtn", "v",
            new Vector2(1, 0), new Vector2(1, 0),
            new Vector2(-(margin * 2 + btnSize * 1.5f), margin + btnSize / 2),
            new Vector2(btnSize, btnSize),
            false);
    }

    private void CreateHoldButton(string name, string label,
        Vector2 anchorMin, Vector2 anchorMax, Vector2 pos, Vector2 size,
        bool isLeft, bool isRight)
    {
        GameObject btnObj = CreateButtonBase(name, label, anchorMin, anchorMax, pos, size);
        HoldHandler handler = btnObj.AddComponent<HoldHandler>();
        handler.isLeft = isLeft;
        handler.isRight = isRight;
    }

    private void CreateTapButton(string name, string label,
        Vector2 anchorMin, Vector2 anchorMax, Vector2 pos, Vector2 size,
        bool isJump)
    {
        GameObject btnObj = CreateButtonBase(name, label, anchorMin, anchorMax, pos, size);
        TapHandler handler = btnObj.AddComponent<TapHandler>();
        handler.isJump = isJump;
    }

    private GameObject CreateButtonBase(string name, string label,
        Vector2 anchorMin, Vector2 anchorMax, Vector2 pos, Vector2 size)
    {
        GameObject btnObj = new GameObject(name);
        btnObj.transform.SetParent(buttonContainer.transform, false);

        RectTransform rt = btnObj.AddComponent<RectTransform>();
        rt.anchorMin = anchorMin;
        rt.anchorMax = anchorMax;
        rt.pivot = new Vector2(0.5f, 0.5f);
        rt.anchoredPosition = pos;
        rt.sizeDelta = size;

        Image img = btnObj.AddComponent<Image>();
        img.color = new Color(1f, 1f, 1f, 0.25f);

        GameObject textObj = new GameObject("Label");
        textObj.transform.SetParent(btnObj.transform, false);
        RectTransform trt = textObj.AddComponent<RectTransform>();
        trt.anchorMin = Vector2.zero;
        trt.anchorMax = Vector2.one;
        trt.offsetMin = Vector2.zero;
        trt.offsetMax = Vector2.zero;
        Text txt = textObj.AddComponent<Text>();
        txt.text = label;
        txt.fontSize = 100;
        txt.alignment = TextAnchor.MiddleCenter;
        txt.color = Color.white;
        txt.font = Font.CreateDynamicFontFromOSFont("Arial", 100);

        return btnObj;
    }
}

public class HoldHandler : MonoBehaviour, IPointerDownHandler, IPointerUpHandler
{
    public bool isLeft, isRight;

    public void OnPointerDown(PointerEventData e)
    {
        if (isLeft) AGR_MobileButtons.leftHeld = true;
        if (isRight) AGR_MobileButtons.rightHeld = true;
    }

    public void OnPointerUp(PointerEventData e)
    {
        if (isLeft) AGR_MobileButtons.leftHeld = false;
        if (isRight) AGR_MobileButtons.rightHeld = false;
    }
}

public class TapHandler : MonoBehaviour, IPointerDownHandler
{
    public bool isJump;

    public void OnPointerDown(PointerEventData e)
    {
        if (isJump)
            AGR_MobileButtons.jumpPressed = true;
        else
            AGR_MobileButtons.fallPressed = true;
    }
}
