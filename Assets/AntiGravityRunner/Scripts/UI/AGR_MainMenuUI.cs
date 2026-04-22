// ============================================================
// AGR_MainMenuUI.cs — FULLY OPAQUE Main Menu + Settings
// ============================================================
// Creates a full main menu with PLAY, SETTINGS, and
// a settings panel with control type, sensitivity, music
// Menu is 100% opaque — you CANNOT see the game behind it!
// Also disables camera rendering while menu is shown
// ATTACH THIS TO: Canvas object
// ============================================================

using UnityEngine;
using UnityEngine.UI;

public class AGR_MainMenuUI : MonoBehaviour
{
    private GameObject menuPanel;
    private GameObject settingsPanel;
    private AGR_GameManager gameManager;

    // Settings UI references
    private Text controlTypeText;
    private Slider swipeSensSlider;
    private Slider gyroSensSlider;
    private Text swipeSensLabel;
    private Text gyroSensLabel;
    private Toggle musicToggle;
    private Toggle sfxToggle;

    // Orientation card references
    private Image landscapeCardBg;
    private Image portraitCardBg;
    private Image landscapeCardBorder;
    private Image portraitCardBorder;
    private Text landscapeCardText;
    private Text portraitCardText;

    private Font uiFont;

    // Camera blocking
    private int originalCullingMask;

    void Start()
    {
        gameManager = FindObjectOfType<AGR_GameManager>();
        AGR_SettingsManager.Load();

        uiFont = Font.CreateDynamicFontFromOSFont("Arial", 30);

        // BLOCK CAMERA: Hide ALL game objects while menu is open
        if (Camera.main != null)
        {
            originalCullingMask = Camera.main.cullingMask;
            Camera.main.cullingMask = 0; // Render NOTHING behind the menu
            Camera.main.clearFlags = CameraClearFlags.SolidColor;
            Camera.main.backgroundColor = new Color(0.01f, 0.01f, 0.03f); // Pure dark
        }

        CreateMainMenu();
        CreateSettingsPanel();

        // If we've already played a run this session, do NOT show the main menu again
        if (AGR_GameManager.hasPlayedOnce)
        {
            menuPanel.SetActive(false);
            settingsPanel.SetActive(false);
            
            // DON'T pause the game! The GameManager will handle TimeScale for the Tap To Start screen.
            // RESTORE CAMERA: Show game world!
            if (Camera.main != null)
            {
                Camera.main.cullingMask = originalCullingMask;
            }
        }
        else
        {
            // Start with menu visible, settings hidden
            menuPanel.SetActive(true);
            settingsPanel.SetActive(false);

            // Pause the game until player hits PLAY
            Time.timeScale = 0f;
        }
    }

    // ==================== MAIN MENU ====================
    private void CreateMainMenu()
    {
        // FULLY OPAQUE dark background — alpha = 1.0!
        menuPanel = CreatePanel("MenuPanel", new Color(0.01f, 0.01f, 0.03f, 1.0f));

        // Decorative gradient overlay (top = slightly lighter)
        GameObject gradientTop = new GameObject("GradientTop");
        gradientTop.transform.SetParent(menuPanel.transform, false);
        RectTransform grt = gradientTop.AddComponent<RectTransform>();
        grt.anchorMin = new Vector2(0, 0.5f);
        grt.anchorMax = Vector2.one;
        grt.offsetMin = Vector2.zero;
        grt.offsetMax = Vector2.zero;
        Image gradImg = gradientTop.AddComponent<Image>();
        gradImg.color = new Color(0.02f, 0.03f, 0.08f, 0.6f);

        // Decorative side accent lines
        CreateAccentLine(menuPanel.transform, new Vector2(-200, 0), new Vector2(2, 400), new Color(0f, 0.8f, 1f, 0.3f));
        CreateAccentLine(menuPanel.transform, new Vector2(200, 0), new Vector2(2, 400), new Color(0f, 0.8f, 1f, 0.3f));
        CreateAccentLine(menuPanel.transform, new Vector2(0, 220), new Vector2(300, 1), new Color(0f, 1f, 1f, 0.5f));

        // Game Title — with glow effect (double text trick)
        CreateText(menuPanel.transform, "TitleGlow", "GRAVI",
            new Vector2(0, 150), new Vector2(500, 120), 72, new Color(0f, 0.4f, 0.5f, 0.5f));
        CreateText(menuPanel.transform, "Title", "GRAVI",
            new Vector2(0, 150), new Vector2(500, 120), 70, new Color(0f, 1f, 1f));

        // Subtitle
        CreateText(menuPanel.transform, "Subtitle", "R U N .   J U M P .   S U R V I V E .",
            new Vector2(0, 70), new Vector2(500, 50), 20, new Color(0.5f, 0.5f, 0.7f));

        // Decorative line under subtitle
        CreateAccentLine(menuPanel.transform, new Vector2(0, 45), new Vector2(180, 1), new Color(0f, 1f, 1f, 0.4f));

        // PLAY button
        CreateMenuButton(menuPanel.transform, "PlayBtn", "▶  P L A Y",
            new Vector2(0, -20), new Vector2(260, 60),
            new Color(0f, 0.7f, 0.7f), new Color(0f, 0.9f, 0.9f), OnPlayClicked);

        // SETTINGS button
        CreateMenuButton(menuPanel.transform, "SettingsBtn", "⚙  S E T T I N G S",
            new Vector2(0, -100), new Vector2(260, 55),
            new Color(0.2f, 0.2f, 0.3f), new Color(0.3f, 0.3f, 0.45f), OnSettingsClicked);

        // High Score display
        int highScore = PlayerPrefs.GetInt("HighScore", 0);
        CreateText(menuPanel.transform, "HighScoreMenu", "◆  BEST: " + highScore + "  ◆",
            new Vector2(0, -190), new Vector2(300, 40), 22, new Color(1f, 0.8f, 0f));

        // Version / credit
        CreateText(menuPanel.transform, "Version", "v1.0",
            new Vector2(0, -250), new Vector2(200, 30), 14, new Color(0.3f, 0.3f, 0.4f));

        // Music Credit
        string credits = "Music from #Uppbeat (free for Creators!):\nhttps://uppbeat.io/t/d0d/voltage\nLicense code: KY3GTGMX2NGHEO6M";
        CreateText(menuPanel.transform, "MusicCredit", credits,
            new Vector2(0, -300), new Vector2(600, 80), 14, new Color(0.3f, 0.3f, 0.4f));
    }

    // ==================== SETTINGS PANEL ====================
    private void CreateSettingsPanel()
    {
        // FULLY OPAQUE dark background — alpha = 1.0!
        settingsPanel = CreatePanel("SettingsPanel", new Color(0.01f, 0.01f, 0.03f, 1.0f));

        // Title
        CreateText(settingsPanel.transform, "SettingsTitle", "S E T T I N G S",
            new Vector2(0, 260), new Vector2(400, 60), 40, new Color(0f, 1f, 1f));

        // Decorative line
        CreateAccentLine(settingsPanel.transform, new Vector2(0, 220), new Vector2(200, 1), new Color(0f, 1f, 1f, 0.4f));

        // --- CONTROL TYPE ---
        CreateText(settingsPanel.transform, "ControlLabel", "Controls:",
            new Vector2(-100, 170), new Vector2(200, 40), 22, Color.white);

        controlTypeText = CreateText(settingsPanel.transform, "ControlValue",
            AGR_SettingsManager.CurrentControl.ToString(),
            new Vector2(80, 170), new Vector2(150, 40), 22, new Color(0f, 1f, 0.5f)).GetComponent<Text>();

        // Control type buttons: < >
        CreateMenuButton(settingsPanel.transform, "CtrlPrev", "◀",
            new Vector2(0, 170), new Vector2(40, 40),
            new Color(0.3f, 0.3f, 0.4f), new Color(0.4f, 0.4f, 0.55f), OnControlPrev);

        CreateMenuButton(settingsPanel.transform, "CtrlNext", "▶",
            new Vector2(160, 170), new Vector2(40, 40),
            new Color(0.3f, 0.3f, 0.4f), new Color(0.4f, 0.4f, 0.55f), OnControlNext);

        // --- ORIENTATION MODE ---
        CreateText(settingsPanel.transform, "OrientLabel", "Orientation:",
            new Vector2(0, 110), new Vector2(300, 35), 20, new Color(0.7f, 0.7f, 0.8f));

        // Two toggle cards: Landscape | Portrait
        CreateOrientCard(settingsPanel.transform, "LandscapeCard",
            "▬  Landscape", new Vector2(-80, 60),
            out landscapeCardBg, out landscapeCardBorder, out landscapeCardText,
            () => OnOrientSelected(AGR_SettingsManager.OrientationMode.Landscape));

        CreateOrientCard(settingsPanel.transform, "PortraitCard",
            "▯  Portrait", new Vector2(80, 60),
            out portraitCardBg, out portraitCardBorder, out portraitCardText,
            () => OnOrientSelected(AGR_SettingsManager.OrientationMode.Portrait));

        // Set initial visual state
        RefreshOrientCards();

        // --- SWIPE SENSITIVITY ---
        CreateText(settingsPanel.transform, "SwipeLabel", "Swipe Sensitivity:",
            new Vector2(0, 0), new Vector2(350, 35), 20, Color.white);

        swipeSensSlider = CreateSlider(settingsPanel.transform, "SwipeSlider",
            new Vector2(0, -40), 0.2f, 3f, AGR_SettingsManager.SwipeSensitivity);
        swipeSensSlider.onValueChanged.AddListener(OnSwipeSensChanged);

        swipeSensLabel = CreateText(settingsPanel.transform, "SwipeVal",
            AGR_SettingsManager.SwipeSensitivity.ToString("F1"),
            new Vector2(170, -40), new Vector2(60, 30), 18, Color.yellow).GetComponent<Text>();

        // --- GYRO SENSITIVITY ---
        CreateText(settingsPanel.transform, "GyroLabel", "Gyro Sensitivity:",
            new Vector2(0, -90), new Vector2(350, 35), 20, Color.white);

        gyroSensSlider = CreateSlider(settingsPanel.transform, "GyroSlider",
            new Vector2(0, -130), 0.5f, 4f, AGR_SettingsManager.GyroSensitivity);
        gyroSensSlider.onValueChanged.AddListener(OnGyroSensChanged);

        gyroSensLabel = CreateText(settingsPanel.transform, "GyroVal",
            AGR_SettingsManager.GyroSensitivity.ToString("F1"),
            new Vector2(170, -130), new Vector2(60, 30), 18, Color.yellow).GetComponent<Text>();

        // --- MUSIC ---
        CreateText(settingsPanel.transform, "MusicLabel", "Music:",
            new Vector2(-50, -190), new Vector2(200, 35), 22, Color.white);

        musicToggle = CreateToggle(settingsPanel.transform, "MusicToggle",
            new Vector2(80, -190), AGR_SettingsManager.MusicOn);
        musicToggle.onValueChanged.AddListener(OnMusicToggled);

        // --- SFX (8-Bit Audio) ---
        CreateText(settingsPanel.transform, "SFXLabel", "SFX:",
            new Vector2(-50, -250), new Vector2(200, 35), 22, Color.white);

        sfxToggle = CreateToggle(settingsPanel.transform, "SFXToggle",
            new Vector2(80, -250), AGR_SettingsManager.SFXOn);
        sfxToggle.onValueChanged.AddListener(OnSFXToggled);

        // BACK button
        CreateMenuButton(settingsPanel.transform, "BackBtn", "◀  B A C K",
            new Vector2(0, -320), new Vector2(200, 50),
            new Color(0.5f, 0.15f, 0.15f), new Color(0.65f, 0.2f, 0.2f), OnBackClicked);
    }

    // ==================== BUTTON CALLBACKS ====================
    private void OnPlayClicked()
    {
        menuPanel.SetActive(false);
        settingsPanel.SetActive(false);
        Time.timeScale = 1f;

        // RESTORE CAMERA: Show game world again!
        if (Camera.main != null)
        {
            Camera.main.cullingMask = originalCullingMask;
        }

        // We have officially passed the Main Menu screen! Mark it for restarts.
        AGR_GameManager.hasPlayedOnce = true;

        // Tell GameManager the menu is closed — game can now start!
        if (gameManager == null) 
            gameManager = FindObjectOfType<AGR_GameManager>();
            
        if (gameManager != null)
        {
            gameManager.menuOpen = false;
        }
    }

    private void OnSettingsClicked()
    {
        menuPanel.SetActive(false);
        settingsPanel.SetActive(true);
    }

    private void OnBackClicked()
    {
        AGR_SettingsManager.Save();
        settingsPanel.SetActive(false);
        menuPanel.SetActive(true);
    }

    private void OnControlPrev()
    {
        int current = (int)AGR_SettingsManager.CurrentControl;
        current--;
        if (current < 0) current = 2;
        AGR_SettingsManager.CurrentControl = (AGR_SettingsManager.ControlType)current;
        controlTypeText.text = AGR_SettingsManager.CurrentControl.ToString();
    }

    private void OnControlNext()
    {
        int current = (int)AGR_SettingsManager.CurrentControl;
        current++;
        if (current > 2) current = 0;
        AGR_SettingsManager.CurrentControl = (AGR_SettingsManager.ControlType)current;
        controlTypeText.text = AGR_SettingsManager.CurrentControl.ToString();
    }

    private void OnOrientSelected(AGR_SettingsManager.OrientationMode mode)
    {
        AGR_SettingsManager.CurrentOrientation = mode;
        AGR_SettingsManager.ApplyOrientation();
        RefreshOrientCards();
    }

    private void RefreshOrientCards()
    {
        bool isLandscape = AGR_SettingsManager.CurrentOrientation == AGR_SettingsManager.OrientationMode.Landscape;

        // Active card: bright glow  |  Inactive card: dim
        Color activeBg = new Color(0f, 0.15f, 0.2f, 1f);
        Color inactiveBg = new Color(0.06f, 0.06f, 0.1f, 1f);
        Color activeBorder = new Color(0f, 1f, 1f, 0.8f);
        Color inactiveBorder = new Color(0.15f, 0.15f, 0.25f, 1f);
        Color activeText = new Color(0f, 1f, 1f);
        Color inactiveText = new Color(0.35f, 0.35f, 0.45f);

        landscapeCardBg.color = isLandscape ? activeBg : inactiveBg;
        landscapeCardBorder.color = isLandscape ? activeBorder : inactiveBorder;
        landscapeCardText.color = isLandscape ? activeText : inactiveText;

        portraitCardBg.color = !isLandscape ? activeBg : inactiveBg;
        portraitCardBorder.color = !isLandscape ? activeBorder : inactiveBorder;
        portraitCardText.color = !isLandscape ? activeText : inactiveText;
    }

    private void OnSwipeSensChanged(float val)
    {
        AGR_SettingsManager.SwipeSensitivity = val;
        swipeSensLabel.text = val.ToString("F1");
    }

    private void OnGyroSensChanged(float val)
    {
        AGR_SettingsManager.GyroSensitivity = val;
        gyroSensLabel.text = val.ToString("F1");
    }

    private void OnMusicToggled(bool on)
    {
        AGR_SettingsManager.MusicOn = on;
        AudioListener.volume = on ? AGR_SettingsManager.MusicVolume : 0f;
    }

    private void OnSFXToggled(bool on)
    {
        AGR_SettingsManager.SFXOn = on;
    }

    // ==================== UI HELPERS ====================
    private GameObject CreatePanel(string name, Color color)
    {
        GameObject panel = new GameObject(name);
        panel.transform.SetParent(transform, false);

        RectTransform rt = panel.AddComponent<RectTransform>();
        rt.anchorMin = Vector2.zero;
        rt.anchorMax = Vector2.one;
        rt.offsetMin = Vector2.zero;
        rt.offsetMax = Vector2.zero;

        Image img = panel.AddComponent<Image>();
        img.color = color;

        return panel;
    }

    private void CreateAccentLine(Transform parent, Vector2 pos, Vector2 size, Color color)
    {
        GameObject obj = new GameObject("AccentLine");
        obj.transform.SetParent(parent, false);
        RectTransform rt = obj.AddComponent<RectTransform>();
        rt.anchoredPosition = pos;
        rt.sizeDelta = size;
        Image img = obj.AddComponent<Image>();
        img.color = color;
    }

    private GameObject CreateText(Transform parent, string name, string content,
        Vector2 pos, Vector2 size, int fontSize, Color color)
    {
        GameObject obj = new GameObject(name);
        obj.transform.SetParent(parent, false);

        RectTransform rt = obj.AddComponent<RectTransform>();
        rt.anchoredPosition = pos;
        rt.sizeDelta = size;

        Text txt = obj.AddComponent<Text>();
        txt.text = content;
        txt.fontSize = fontSize;
        txt.color = color;
        txt.alignment = TextAnchor.MiddleCenter;
        txt.font = uiFont;

        return obj;
    }

    private void CreateMenuButton(Transform parent, string name, string label,
        Vector2 pos, Vector2 size, Color normalColor, Color hoverColor,
        UnityEngine.Events.UnityAction onClick)
    {
        GameObject obj = new GameObject(name);
        obj.transform.SetParent(parent, false);

        RectTransform rt = obj.AddComponent<RectTransform>();
        rt.anchoredPosition = pos;
        rt.sizeDelta = size;

        Image img = obj.AddComponent<Image>();
        img.color = normalColor;

        Button btn = obj.AddComponent<Button>();
        btn.onClick.AddListener(onClick);

        // Button color transitions for hover effect
        ColorBlock colors = btn.colors;
        colors.normalColor = Color.white;
        colors.highlightedColor = new Color(1.2f, 1.2f, 1.2f);
        colors.pressedColor = new Color(0.8f, 0.8f, 0.8f);
        colors.selectedColor = Color.white;
        btn.colors = colors;

        // Button text
        GameObject txtObj = new GameObject("Label");
        txtObj.transform.SetParent(obj.transform, false);
        RectTransform trt = txtObj.AddComponent<RectTransform>();
        trt.anchorMin = Vector2.zero;
        trt.anchorMax = Vector2.one;
        trt.offsetMin = Vector2.zero;
        trt.offsetMax = Vector2.zero;
        Text txt = txtObj.AddComponent<Text>();
        txt.text = label;
        txt.fontSize = 22;
        txt.color = Color.white;
        txt.alignment = TextAnchor.MiddleCenter;
        txt.font = uiFont;
    }

    // Overload for old-style calls without hover color
    private void CreateMenuButton(Transform parent, string name, string label,
        Vector2 pos, Vector2 size, Color color, UnityEngine.Events.UnityAction onClick)
    {
        CreateMenuButton(parent, name, label, pos, size, color, color, onClick);
    }

    private Slider CreateSlider(Transform parent, string name,
        Vector2 pos, float min, float max, float value)
    {
        GameObject obj = new GameObject(name);
        obj.transform.SetParent(parent, false);

        RectTransform rt = obj.AddComponent<RectTransform>();
        rt.anchoredPosition = pos;
        rt.sizeDelta = new Vector2(250, 20);

        Slider slider = obj.AddComponent<Slider>();
        slider.minValue = min;
        slider.maxValue = max;
        slider.value = value;

        // Background
        GameObject bg = new GameObject("Background");
        bg.transform.SetParent(obj.transform, false);
        RectTransform bgrt = bg.AddComponent<RectTransform>();
        bgrt.anchorMin = Vector2.zero;
        bgrt.anchorMax = Vector2.one;
        bgrt.offsetMin = Vector2.zero;
        bgrt.offsetMax = Vector2.zero;
        Image bgImg = bg.AddComponent<Image>();
        bgImg.color = new Color(0.15f, 0.15f, 0.25f);

        // Fill area
        GameObject fillArea = new GameObject("Fill Area");
        fillArea.transform.SetParent(obj.transform, false);
        RectTransform fillrt = fillArea.AddComponent<RectTransform>();
        fillrt.anchorMin = Vector2.zero;
        fillrt.anchorMax = Vector2.one;
        fillrt.offsetMin = Vector2.zero;
        fillrt.offsetMax = Vector2.zero;

        GameObject fill = new GameObject("Fill");
        fill.transform.SetParent(fillArea.transform, false);
        RectTransform frt = fill.AddComponent<RectTransform>();
        frt.anchorMin = Vector2.zero;
        frt.anchorMax = Vector2.one;
        frt.offsetMin = Vector2.zero;
        frt.offsetMax = Vector2.zero;
        Image fillImg = fill.AddComponent<Image>();
        fillImg.color = new Color(0f, 0.8f, 0.8f);

        slider.fillRect = frt;
        slider.targetGraphic = bgImg;

        // Handle
        GameObject handleArea = new GameObject("Handle Slide Area");
        handleArea.transform.SetParent(obj.transform, false);
        RectTransform hart = handleArea.AddComponent<RectTransform>();
        hart.anchorMin = Vector2.zero;
        hart.anchorMax = Vector2.one;
        hart.offsetMin = Vector2.zero;
        hart.offsetMax = Vector2.zero;

        GameObject handle = new GameObject("Handle");
        handle.transform.SetParent(handleArea.transform, false);
        RectTransform hrt = handle.AddComponent<RectTransform>();
        hrt.sizeDelta = new Vector2(20, 30);
        Image hImg = handle.AddComponent<Image>();
        hImg.color = Color.white;

        slider.handleRect = hrt;

        return slider;
    }

    private Toggle CreateToggle(Transform parent, string name, Vector2 pos, bool isOn)
    {
        GameObject obj = new GameObject(name);
        obj.transform.SetParent(parent, false);

        RectTransform rt = obj.AddComponent<RectTransform>();
        rt.anchoredPosition = pos;
        rt.sizeDelta = new Vector2(40, 40);

        // Background
        Image bg = obj.AddComponent<Image>();
        bg.color = new Color(0.2f, 0.2f, 0.3f);

        Toggle toggle = obj.AddComponent<Toggle>();
        toggle.isOn = isOn;

        // Checkmark
        GameObject checkObj = new GameObject("Checkmark");
        checkObj.transform.SetParent(obj.transform, false);
        RectTransform crt = checkObj.AddComponent<RectTransform>();
        crt.anchorMin = Vector2.zero;
        crt.anchorMax = Vector2.one;
        crt.offsetMin = new Vector2(5, 5);
        crt.offsetMax = new Vector2(-5, -5);
        Image checkImg = checkObj.AddComponent<Image>();
        checkImg.color = new Color(0f, 1f, 0.5f);

        toggle.graphic = checkImg;
        toggle.targetGraphic = bg;

        return toggle;
    }

    /// <summary>
    /// Creates a premium toggle-card for orientation selection.
    /// Layered: outer border → inner bg → label text.
    /// Active state glows cyan, inactive is dim.
    /// </summary>
    private void CreateOrientCard(Transform parent, string name, string label,
        Vector2 pos,
        out Image bgOut, out Image borderOut, out Text textOut,
        UnityEngine.Events.UnityAction onClick)
    {
        Vector2 cardSize = new Vector2(120, 38);

        // Outer border frame
        GameObject borderObj = new GameObject(name + "_Border");
        borderObj.transform.SetParent(parent, false);
        RectTransform brt = borderObj.AddComponent<RectTransform>();
        brt.anchoredPosition = pos;
        brt.sizeDelta = cardSize;
        borderOut = borderObj.AddComponent<Image>();
        borderOut.color = new Color(0.15f, 0.15f, 0.25f);

        // Inner background (slightly inset for border effect)
        GameObject bgObj = new GameObject(name + "_Bg");
        bgObj.transform.SetParent(borderObj.transform, false);
        RectTransform bgrt = bgObj.AddComponent<RectTransform>();
        bgrt.anchorMin = Vector2.zero;
        bgrt.anchorMax = Vector2.one;
        bgrt.offsetMin = new Vector2(2, 2);
        bgrt.offsetMax = new Vector2(-2, -2);
        bgOut = bgObj.AddComponent<Image>();
        bgOut.color = new Color(0.06f, 0.06f, 0.1f);

        // Button on the border (full card is clickable)
        Button btn = borderObj.AddComponent<Button>();
        btn.targetGraphic = borderOut;
        btn.onClick.AddListener(onClick);

        // Press effect
        ColorBlock colors = btn.colors;
        colors.normalColor = Color.white;
        colors.highlightedColor = new Color(1.15f, 1.15f, 1.15f);
        colors.pressedColor = new Color(0.7f, 0.7f, 0.7f);
        colors.selectedColor = Color.white;
        btn.colors = colors;

        // Label text
        GameObject txtObj = new GameObject(name + "_Text");
        txtObj.transform.SetParent(borderObj.transform, false);
        RectTransform trt = txtObj.AddComponent<RectTransform>();
        trt.anchorMin = Vector2.zero;
        trt.anchorMax = Vector2.one;
        trt.offsetMin = Vector2.zero;
        trt.offsetMax = Vector2.zero;
        textOut = txtObj.AddComponent<Text>();
        textOut.text = label;
        textOut.fontSize = 16;
        textOut.color = Color.white;
        textOut.alignment = TextAnchor.MiddleCenter;
        textOut.font = uiFont;
    }
}
