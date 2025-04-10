using UnityEngine;
using UnityEditor;
using UnityEngine.UI;
using TMPro;
using System.Collections.Generic;

public class CourseDetailUIBuilder : EditorWindow
{
    private Color primaryColor = new Color(0.13f, 0.2f, 0.7f); // Deep blue
    private Color accentColor = new Color(0.2f, 0.6f, 1f); // Light blue
    private Color backgroundColor = new Color(0.95f, 0.97f, 1f); // Very light blue
    private Color textColor = new Color(0.2f, 0.2f, 0.2f); // Dark gray
    private Color successColor = new Color(0.2f, 0.7f, 0.3f); // Green
   

    [MenuItem("Tools/Generate Course Detail UI")]
    public static void ShowWindow()
    {
        GetWindow<CourseDetailUIBuilder>("Course Detail UI Builder");
    }

    private void OnGUI()
    {
        GUILayout.Label("Course Detail UI Generator", EditorStyles.boldLabel);
        
        if (GUILayout.Button("Generate Course Detail UI"))
        {
            CreateCourseDetailUI();
        }
    }

    private void CreateCourseDetailUI()
    {
        // Create the main detail page
        GameObject detailPage = CreateUIObject("DetailPage", null);
        
        // Add Canvas if it doesn't exist
        Canvas canvas = FindObjectOfType<Canvas>();
        if (canvas == null)
        {
            GameObject canvasGO = new GameObject("Canvas", typeof(Canvas));
            canvas = canvasGO.GetComponent<Canvas>();
            canvas.renderMode = RenderMode.ScreenSpaceOverlay;
            
            CanvasScaler scaler = canvasGO.AddComponent<CanvasScaler>();
            scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            scaler.referenceResolution = new Vector2(1080, 1920); // Mobile portrait
            scaler.screenMatchMode = CanvasScaler.ScreenMatchMode.MatchWidthOrHeight;
            scaler.matchWidthOrHeight = 0.5f;
            
            canvasGO.AddComponent<GraphicRaycaster>();
            
            detailPage.transform.SetParent(canvas.transform, false);
            // After creating the main detail page
            Debug.Log("Created detail page");
        }
        else
        {
            detailPage.transform.SetParent(canvas.transform, false);
        }
        
        // Background panel
        GameObject background = CreateUIObject("Background", detailPage.transform);
        Image bgImage = background.AddComponent<Image>();
        bgImage.color = backgroundColor;
        RectTransform bgRect = background.GetComponent<RectTransform>();
        SetFullRect(bgRect);
        
        // Create the header area
        GameObject headerArea = CreateUIObject("HeaderArea", detailPage.transform);
        Image headerImage = headerArea.AddComponent<Image>();
        headerImage.color = primaryColor;
        RectTransform headerRect = headerArea.GetComponent<RectTransform>();
        headerRect.anchorMin = new Vector2(0, 1);
        headerRect.anchorMax = new Vector2(1, 1);
        headerRect.pivot = new Vector2(0.5f, 1);
        headerRect.sizeDelta = new Vector2(0, 120);
        
        // Back button
        GameObject backButton = CreateUIObject("BackButton", headerArea.transform);
        Image backBtnImage = backButton.AddComponent<Image>();
        backBtnImage.color = new Color(1, 1, 1, 0.2f);
        Button backBtn = backButton.AddComponent<Button>();
        backBtn.targetGraphic = backBtnImage;
        RectTransform backRect = backButton.GetComponent<RectTransform>();
        backRect.anchorMin = new Vector2(0, 0.5f);
        backRect.anchorMax = new Vector2(0, 0.5f);
        backRect.pivot = new Vector2(0, 0.5f);
        backRect.anchoredPosition = new Vector2(20, 0);
        backRect.sizeDelta = new Vector2(40, 40);
        
        // Back icon
        GameObject backIcon = CreateUIObject("BackIcon", backButton.transform);
        TMP_Text backText = backIcon.AddComponent<TextMeshProUGUI>();
        backText.text = "<";
        backText.fontSize = 28;
        backText.color = Color.white;
        backText.alignment = TextAlignmentOptions.Center;
        RectTransform backIconRect = backIcon.GetComponent<RectTransform>();
        SetFullRect(backIconRect);
        
        // Course title text
        GameObject courseTitleGO = CreateUIObject("CourseTitleText", headerArea.transform);
        TMP_Text courseTitleText = courseTitleGO.AddComponent<TextMeshProUGUI>();
        courseTitleText.text = "Course Title";
        courseTitleText.fontSize = 28;
        courseTitleText.fontStyle = FontStyles.Bold;
        courseTitleText.color = Color.white;
        courseTitleText.alignment = TextAlignmentOptions.Center;
        RectTransform titleRect = courseTitleGO.GetComponent<RectTransform>();
        titleRect.anchorMin = new Vector2(0, 0.5f);
        titleRect.anchorMax = new Vector2(1, 0.5f);
        titleRect.pivot = new Vector2(0.5f, 0.5f);
        titleRect.sizeDelta = new Vector2(-120, 60);
        
        // ScrollView
        GameObject scrollView = CreateScrollView(detailPage.transform);
        RectTransform scrollRect = scrollView.GetComponent<RectTransform>();
        scrollRect.anchorMin = new Vector2(0, 0);
        scrollRect.anchorMax = new Vector2(1, 1);
        scrollRect.offsetMin = new Vector2(0, 0);
        scrollRect.offsetMax = new Vector2(0, -120); // Header size
        
        // Content container - getting the content from scroll view
        GameObject contentContainer = scrollView.transform.Find("Viewport/Content").gameObject;
        RectTransform contentRect = contentContainer.GetComponent<RectTransform>();
        VerticalLayoutGroup layoutGroup = contentContainer.AddComponent<VerticalLayoutGroup>();
        layoutGroup.padding = new RectOffset(20, 20, 20, 20);
        layoutGroup.spacing = 15;
        layoutGroup.childControlHeight = false;
        layoutGroup.childForceExpandHeight = false;
        ContentSizeFitter contentFitter = contentContainer.AddComponent<ContentSizeFitter>();
        contentFitter.verticalFit = ContentSizeFitter.FitMode.PreferredSize;
        
        // Course image
        GameObject courseImageGO = CreateUIObject("CourseImage", contentContainer.transform);
        Image courseImage = courseImageGO.AddComponent<Image>();
        courseImage.color = Color.white;
        courseImage.preserveAspect = true;
        AspectRatioFitter imageFitter = courseImageGO.AddComponent<AspectRatioFitter>();
        imageFitter.aspectMode = AspectRatioFitter.AspectMode.FitInParent;
        imageFitter.aspectRatio = 16f / 9f; // Default aspect ratio
        RectTransform imageRect = courseImageGO.GetComponent<RectTransform>();
        imageRect.sizeDelta = new Vector2(0, 200);
        
        // Main info panel
        GameObject courseInfoPanel = CreateUIObject("CourseInfoPanel", contentContainer.transform);
        Image courseInfoImage = courseInfoPanel.AddComponent<Image>();
        courseInfoImage.color = Color.white;
        RectTransform courseInfoRect = courseInfoPanel.GetComponent<RectTransform>();
        courseInfoRect.sizeDelta = new Vector2(0, 300);
        
        // Create shadow effect for panel
        Shadow shadow = courseInfoPanel.AddComponent<Shadow>();
        shadow.effectColor = new Color(0, 0, 0, 0.2f);
        shadow.effectDistance = new Vector2(2, -2);
        
        // Create course info panel internal layout
        VerticalLayoutGroup infoLayout = courseInfoPanel.AddComponent<VerticalLayoutGroup>();
        infoLayout.padding = new RectOffset(20, 20, 20, 20);
        infoLayout.spacing = 10;
        infoLayout.childControlHeight = false;
        infoLayout.childForceExpandHeight = false;
        
        // Course type panel
        GameObject courseTypePanel = CreateInfoSection(courseInfoPanel.transform, "Course Type", 60);
        CreateInfoField(courseTypePanel.transform, "Type:", "Training", 30);
        
        // Target audience panel
        GameObject targetAudiencePanel = CreateInfoSection(courseInfoPanel.transform, "Target Audience", 60);
        CreateInfoField(targetAudiencePanel.transform, "Target Audience:", "Optional Course", 30);
        
        // Description panel
        GameObject courseDescPanel = CreateInfoSection(courseInfoPanel.transform, "Description", 120);
        GameObject descriptionField = CreateUIObject("CourseDescriptionText", courseDescPanel.transform);
        TMP_InputField descInput = descriptionField.AddComponent<TMP_InputField>();
        
        // Create background and text area for input field
        GameObject textAreaBg = CreateUIObject("TextAreaBackground", descriptionField.transform);
        Image textAreaImage = textAreaBg.AddComponent<Image>();
        textAreaImage.color = new Color(0.95f, 0.95f, 0.95f);
        RectTransform textAreaRect = textAreaBg.GetComponent<RectTransform>();
        SetFullRect(textAreaRect);
        
        // Create text component for input field
        GameObject textComponent = CreateUIObject("Text", descriptionField.transform);
        TMP_Text inputText = textComponent.AddComponent<TextMeshProUGUI>();
        inputText.text = "This is a sample course description that explains what the course is about.";
        inputText.fontSize = 16;
        inputText.color = textColor;
        RectTransform textCompRect = textComponent.GetComponent<RectTransform>();
        SetFullRect(textCompRect);
        textCompRect.offsetMin = new Vector2(5, 5);
        textCompRect.offsetMax = new Vector2(-5, -5);
        
        // Setup input field
        descInput.textComponent = inputText;
        descInput.textViewport = textAreaRect;
        descInput.readOnly = true; // Make it read-only
        RectTransform descRect = descriptionField.GetComponent<RectTransform>();
        descRect.anchorMin = Vector2.zero;
        descRect.anchorMax = Vector2.one;
        descRect.offsetMin = new Vector2(0, 30); // Reserve space for the label
        descRect.offsetMax = Vector2.zero;
        
        // Course details panel
        GameObject courseDetailsPanel = CreateInfoSection(contentContainer.transform, "Course Details", 180);
        VerticalLayoutGroup detailsLayout = courseDetailsPanel.GetComponent<VerticalLayoutGroup>();
        detailsLayout.spacing = 5;
        
        // Course details fields
        CreateInfoText(courseDetailsPanel.transform, "Duration", "2h 30m");
        CreateInfoText(courseDetailsPanel.transform, "Participants", "10-15 people");
        CreateInfoText(courseDetailsPanel.transform, "Number of Lessons", "5");
        CreateInfoText(courseDetailsPanel.transform, "Status", "Active");
        
        // Add mandatory status with highlight
        GameObject mandatoryGO = CreateUIObject("MandatoryText", courseDetailsPanel.transform);
        TMP_Text mandatoryText = mandatoryGO.AddComponent<TextMeshProUGUI>();
        mandatoryText.text = "Optional Course";
        mandatoryText.fontSize = 16;
        mandatoryText.fontStyle = FontStyles.Bold;
        mandatoryText.color = successColor; // Green for optional
        mandatoryText.alignment = TextAlignmentOptions.Center;
        RectTransform mandatoryRect = mandatoryGO.GetComponent<RectTransform>();
        mandatoryRect.sizeDelta = new Vector2(0, 30);
        
        // Machine Type section
        GameObject machineTypeSection = CreateInfoSection(contentContainer.transform, "Machine Type: Seiko Fan", 180);
        VerticalLayoutGroup machineLayout = machineTypeSection.GetComponent<VerticalLayoutGroup>();
        machineLayout.spacing = 5;
        
        // Machine type container for attributes
        GameObject attributesContainer = CreateUIObject("MachineAttributesContainer", machineTypeSection.transform);
        VerticalLayoutGroup attrLayout = attributesContainer.AddComponent<VerticalLayoutGroup>();
        attrLayout.spacing = 5;
        attrLayout.childControlHeight = false;
        attrLayout.childForceExpandHeight = false;
        RectTransform attrRect = attributesContainer.GetComponent<RectTransform>();
        attrRect.sizeDelta = new Vector2(0, 120);
        
        // Sample machine attributes
        CreateMachineAttribute(attributesContainer.transform, "Size", "10-20 cm");
        CreateMachineAttribute(attributesContainer.transform, "Color", "White, Blue");
        CreateMachineAttribute(attributesContainer.transform, "Shape", "Circle, Square");
        
        // AR Button
        GameObject arButtonGO = CreateUIObject("ARButton", contentContainer.transform);
        Image arBtnImage = arButtonGO.AddComponent<Image>();
        arBtnImage.color = accentColor;
        Button arBtn = arButtonGO.AddComponent<Button>();
        arBtn.targetGraphic = arBtnImage;
        
        // Add rounded corners to button
        RoundedCorners arBtnCorners = arButtonGO.AddComponent<RoundedCorners>();
        RectTransform arBtnRect = arButtonGO.GetComponent<RectTransform>();
        arBtnRect.sizeDelta = new Vector2(0, 60);
        
        // AR Button text
        GameObject arBtnTextGO = CreateUIObject("Text", arButtonGO.transform);
        TMP_Text arBtnText = arBtnTextGO.AddComponent<TextMeshProUGUI>();
        arBtnText.text = "Enter AR";
        arBtnText.fontSize = 20;
        arBtnText.fontStyle = FontStyles.Bold;
        arBtnText.color = Color.white;
        arBtnText.alignment = TextAlignmentOptions.Center;
        RectTransform arBtnTextRect = arBtnTextGO.GetComponent<RectTransform>();
        SetFullRect(arBtnTextRect);
        
        // Loading UI Panel
        GameObject loadingUI = CreateUIObject("LoadingUIPanel", detailPage.transform);
        loadingUI.SetActive(false);
        Image loadingBgImage = loadingUI.AddComponent<Image>();
        loadingBgImage.color = new Color(0, 0, 0, 0.7f);
        RectTransform loadingRect = loadingUI.GetComponent<RectTransform>();
        SetFullRect(loadingRect);
        
        // Progress panel
        GameObject progressPanel = CreateUIObject("ProgressPanel", loadingUI.transform);
        Image progressBgImage = progressPanel.AddComponent<Image>();
        progressBgImage.color = Color.white;
        RectTransform progressPanelRect = progressPanel.GetComponent<RectTransform>();
        progressPanelRect.anchorMin = new Vector2(0.5f, 0.5f);
        progressPanelRect.anchorMax = new Vector2(0.5f, 0.5f);
        progressPanelRect.pivot = new Vector2(0.5f, 0.5f);
        progressPanelRect.sizeDelta = new Vector2(400, 200);
        
        // Progress layout
        VerticalLayoutGroup progressLayout = progressPanel.AddComponent<VerticalLayoutGroup>();
        progressLayout.padding = new RectOffset(20, 20, 20, 20);
        progressLayout.spacing = 15;
        progressLayout.childAlignment = TextAnchor.MiddleCenter;
        
        // Progress message
        GameObject progressMsgGO = CreateUIObject("ProgressText", progressPanel.transform);
        TMP_Text progressMsg = progressMsgGO.AddComponent<TextMeshProUGUI>();
        progressMsg.text = "Downloading...";
        progressMsg.fontSize = 20;
        progressMsg.color = textColor;
        progressMsg.alignment = TextAlignmentOptions.Center;
        RectTransform progressMsgRect = progressMsgGO.GetComponent<RectTransform>();
        progressMsgRect.sizeDelta = new Vector2(0, 30);
        
        // Progress bar
        GameObject progressBarGO = CreateUIObject("ProgressBar", progressPanel.transform);
        Image progressBarBg = progressBarGO.AddComponent<Image>();
        progressBarBg.color = new Color(0.9f, 0.9f, 0.9f);
        Slider progressBar = progressBarGO.AddComponent<Slider>();
        progressBar.minValue = 0;
        progressBar.maxValue = 1;
        progressBar.value = 0.5f;
        RectTransform progressBarRect = progressBarGO.GetComponent<RectTransform>();
        progressBarRect.sizeDelta = new Vector2(0, 30);
        
        // Progress bar fill
        GameObject fillArea = CreateUIObject("Fill Area", progressBarGO.transform);
        RectTransform fillAreaRect = fillArea.GetComponent<RectTransform>();
        fillAreaRect.anchorMin = Vector2.zero;
        fillAreaRect.anchorMax = Vector2.one;
        fillAreaRect.offsetMin = new Vector2(5, 5);
        fillAreaRect.offsetMax = new Vector2(-5, -5);
        
        GameObject fill = CreateUIObject("Fill", fillArea.transform);
        Image fillImage = fill.AddComponent<Image>();
        fillImage.color = accentColor;
        RectTransform fillRect = fill.GetComponent<RectTransform>();
        fillRect.anchorMin = Vector2.zero;
        fillRect.anchorMax = new Vector2(0.5f, 1);
        fillRect.pivot = new Vector2(0.5f, 0.5f);
        fillRect.sizeDelta = Vector2.zero;
        
        progressBar.fillRect = fillRect;
        
        // Loading spinner
        GameObject spinnerGO = CreateUIObject("LoadingSpinner", detailPage.transform);
        spinnerGO.SetActive(false);
        Image spinnerImage = spinnerGO.AddComponent<Image>();
        spinnerImage.color = accentColor;
        RectTransform spinnerRect = spinnerGO.GetComponent<RectTransform>();
        spinnerRect.anchorMin = new Vector2(0.5f, 0.5f);
        spinnerRect.anchorMax = new Vector2(0.5f, 0.5f);
        spinnerRect.pivot = new Vector2(0.5f, 0.5f);
        spinnerRect.sizeDelta = new Vector2(80, 80);
        
        // Add rotation script (simple approximation - you'll need a proper spinner animation)
        spinnerGO.AddComponent<SpinnerRotator>();
        
        // Overlay for loading
        GameObject overlayGO = CreateUIObject("Overlay", detailPage.transform);
        overlayGO.SetActive(false);
        Image overlayImage = overlayGO.AddComponent<Image>();
        overlayImage.color = new Color(0, 0, 0, 0.5f);
        RectTransform overlayRect = overlayGO.GetComponent<RectTransform>();
        SetFullRect(overlayRect);
        
        // Navigation bar
        GameObject navBarGO = CreateUIObject("NavBarUI", detailPage.transform);
        Image navBarImage = navBarGO.AddComponent<Image>();
        navBarImage.color = primaryColor;
        RectTransform navBarRect = navBarGO.GetComponent<RectTransform>();
        navBarRect.anchorMin = new Vector2(0, 0);
        navBarRect.anchorMax = new Vector2(1, 0);
        navBarRect.pivot = new Vector2(0.5f, 0);
        navBarRect.sizeDelta = new Vector2(0, 80);
        
        // Navigation buttons - home, search, profile
        CreateNavButton(navBarGO.transform, "HomeButton", "🏠", 0.2f);
        CreateNavButton(navBarGO.transform, "SearchButton", "🔍", 0.5f);
        CreateNavButton(navBarGO.transform, "ProfileButton", "👤", 0.8f);
        
        // Add CourseDetailLoader component
        var loader = detailPage.AddComponent<Code.CourseDetailLoader>();
        
        // Assign references to the loader
        loader.courseTitleText = courseTitleText;
        loader.courseDescriptionText = descInput;
        loader.courseDurationText = FindTextInChildrenByName(courseDetailsPanel.transform, "Duration");
        loader.courseParticipantsText = FindTextInChildrenByName(courseDetailsPanel.transform, "Participants");
        loader.courseTypeText = FindTextInChildrenByName(courseTypePanel.transform, "TypeValue");
        loader.shortDescriptionText = FindTextInChildrenByName(courseDescPanel.transform, "LabelText");
        loader.targetAudienceText = FindTextInChildrenByName(targetAudiencePanel.transform, "TargetAudienceValue");
        loader.statusText = FindTextInChildrenByName(courseDetailsPanel.transform, "Status");
        loader.mandatoryText = mandatoryText;
        loader.numberOfLessonsText = FindTextInChildrenByName(courseDetailsPanel.transform, "Number of Lessons");
        loader.companyIdText = CreateInfoText(courseDetailsPanel.transform, "Company ID", "12345");
        
        // Additional references
        loader.loadingSpinner = spinnerGO;
        loader.overlay = overlayGO;
        loader.machineTypeNameText = FindTextInChildrenByName(machineTypeSection.transform, "SectionTitle");
        loader.machineAttributesContainer = attributesContainer.transform;
        
        // Create machine attribute prefab
        GameObject prefab = CreateMachineAttributePrefab();
        loader.machineAttributePrefab = prefab;
        
        loader.ARButton = arBtn;
        loader.backButton = backBtn;
        loader.courseImage = courseImage;
        loader.backgroundPanel = bgImage;
        loader.courseInfoPanel = courseInfoPanel.transform;
        loader.progressText = progressMsg;
        loader.progressBar = progressBar;
        loader.homePage = null; // You'll need to assign this manually
        loader.loadingUIPanel = loadingUI;
        loader.detailPage = detailPage;
        
        EditorUtility.SetDirty(detailPage);
        // After calling CreateMachineAttributePrefab()
        Debug.Log("Prefab created");
        Debug.Log("✅ Course Detail UI successfully generated!");
    }
    
    private GameObject CreateScrollView(Transform parent)
    {
        GameObject scrollView = CreateUIObject("ScrollView", parent);
        scrollView.AddComponent<Image>();
        ScrollRect scrollRect = scrollView.AddComponent<ScrollRect>();
        
        GameObject viewport = CreateUIObject("Viewport", scrollView.transform);
        viewport.AddComponent<Image>().color = Color.clear;
        viewport.AddComponent<Mask>().showMaskGraphic = false;
        RectTransform viewportRect = viewport.GetComponent<RectTransform>();
        SetFullRect(viewportRect);
        
        GameObject content = CreateUIObject("Content", viewport.transform);
        RectTransform contentRect = content.GetComponent<RectTransform>();
        contentRect.anchorMin = new Vector2(0, 1);
        contentRect.anchorMax = new Vector2(1, 1);
        contentRect.pivot = new Vector2(0.5f, 1);
        contentRect.sizeDelta = new Vector2(0, 1000);
        
        scrollRect.content = contentRect;
        scrollRect.viewport = viewportRect;
        scrollRect.horizontal = false;
        scrollRect.vertical = true;
        scrollRect.scrollSensitivity = 10;
        scrollRect.movementType = ScrollRect.MovementType.Clamped;
        
        return scrollView;
    }
    
    private GameObject CreateInfoSection(Transform parent, string title, float height)
    {
        GameObject section = CreateUIObject(title + "Section", parent);
        Image sectionBg = section.AddComponent<Image>();
        sectionBg.color = Color.white;
        
        VerticalLayoutGroup layout = section.AddComponent<VerticalLayoutGroup>();
        layout.padding = new RectOffset(15, 15, 15, 15);
        layout.spacing = 5;
        layout.childControlHeight = false;
        layout.childForceExpandHeight = false;
        
        RectTransform sectionRect = section.GetComponent<RectTransform>();
        sectionRect.sizeDelta = new Vector2(0, height);
        
        // Section title
        GameObject titleGO = CreateUIObject("SectionTitle", section.transform);
        TMP_Text titleText = titleGO.AddComponent<TextMeshProUGUI>();
        titleText.text = title;
        titleText.fontSize = 20;
        titleText.fontStyle = FontStyles.Bold;
        titleText.color = primaryColor;
        RectTransform titleRect = titleGO.GetComponent<RectTransform>();
        titleRect.sizeDelta = new Vector2(0, 30);
        
        return section;
    }
    
    private void CreateInfoField(Transform parent, string label, string value, float height)
    {
        GameObject container = CreateUIObject(label.Replace(":", "") + "Container", parent);
        HorizontalLayoutGroup layout = container.AddComponent<HorizontalLayoutGroup>();
        layout.spacing = 10;
        layout.childControlWidth = false;
        layout.childForceExpandWidth = false;
        
        RectTransform containerRect = container.GetComponent<RectTransform>();
        containerRect.sizeDelta = new Vector2(0, height);
        
        // Label
        GameObject labelGO = CreateUIObject("LabelText", container.transform);
        TMP_Text labelText = labelGO.AddComponent<TextMeshProUGUI>();
        labelText.text = label;
        labelText.fontSize = 16;
        labelText.fontStyle = FontStyles.Bold;
        labelText.color = textColor;
        RectTransform labelRect = labelGO.GetComponent<RectTransform>();
        labelRect.sizeDelta = new Vector2(150, height);
        
        // Value
        GameObject valueGO = CreateUIObject(label.Replace(":", "") + "Value", container.transform);
        TMP_Text valueText = valueGO.AddComponent<TextMeshProUGUI>();
        valueText.text = value;
        valueText.fontSize = 16;
        valueText.color = textColor;
        RectTransform valueRect = valueGO.GetComponent<RectTransform>();
        valueRect.sizeDelta = new Vector2(300, height);
    }
    
    private TMP_Text CreateInfoText(Transform parent, string label, string value)
    {
        GameObject infoGO = CreateUIObject(label, parent);
        TMP_Text infoText = infoGO.AddComponent<TextMeshProUGUI>();
        infoText.text = $"<b>{label}:</b> {value}";
        infoText.fontSize = 16;
        infoText.color = textColor;
        RectTransform infoRect = infoGO.GetComponent<RectTransform>();
        infoRect.sizeDelta = new Vector2(0, 25);
        return infoText;
    }
    
    private void CreateMachineAttribute(Transform parent, string attributeName, string value)
    {
        GameObject attrGO = CreateUIObject(attributeName + "Attribute", parent);
        HorizontalLayoutGroup layout = attrGO.AddComponent<HorizontalLayoutGroup>();
        layout.spacing = 10;
        layout.childAlignment = TextAnchor.MiddleLeft;
        
        // Icon
        GameObject iconGO = CreateUIObject("Icon", attrGO.transform);
        TMP_Text iconText = iconGO.AddComponent<TextMeshProUGUI>();
        iconText.text = "⚫";
        iconText.fontSize = 16;
        iconText.color = primaryColor;
        iconText.alignment = TextAlignmentOptions.Center;
        RectTransform iconRect = iconGO.GetComponent<RectTransform>();
        iconRect.sizeDelta = new Vector2(20, 30);
        
        // Text
        GameObject textGO = CreateUIObject("MachineType", attrGO.transform);
        TMP_Text attrText = textGO.AddComponent<TextMeshProUGUI>();
        attrText.text = $"<b>{attributeName}</b>: {value}";
        attrText.fontSize = 16;
        attrText.color = textColor;
        RectTransform textRect = textGO.GetComponent<RectTransform>();
        textRect.sizeDelta = new Vector2(400, 30);
        
        RectTransform attrRect = attrGO.GetComponent<RectTransform>();
        attrRect.sizeDelta = new Vector2(0, 30);
    }
    
    private GameObject CreateMachineAttributePrefab()
    {
        GameObject prefab = new GameObject("MachineAttributePrefab");
        prefab.SetActive(false);
        
        HorizontalLayoutGroup layout = prefab.AddComponent<HorizontalLayoutGroup>();
        layout.spacing = 10;
        layout.childAlignment = TextAnchor.MiddleLeft;
        
        // Icon
        GameObject iconGO = CreateUIObject("Icon", prefab.transform);
        TMP_Text iconText = iconGO.AddComponent<TextMeshProUGUI>();
        iconText.text = "⚫";
        iconText.fontSize = 16;
        iconText.color = primaryColor;
        iconText.alignment = TextAlignmentOptions.Center;
        RectTransform iconRect = iconGO.GetComponent<RectTransform>();
        iconRect.sizeDelta = new Vector2(20, 30);
        
        // Text
        GameObject textGO = CreateUIObject("MachineType", prefab.transform);
        TMP_Text attrText = textGO.AddComponent<TextMeshProUGUI>();
        attrText.text = "<b>Attribute</b>: Value";
        attrText.fontSize = 16;
        attrText.color = textColor;
        RectTransform textRect = textGO.GetComponent<RectTransform>();
        textRect.sizeDelta = new Vector2(400, 30);
        
        RectTransform attrRect = prefab.GetComponent<RectTransform>();
        attrRect.sizeDelta = new Vector2(0, 30);
        
        return prefab;
    }
    
    private void CreateNavButton(Transform parent, string name, string icon, float xPosition)
    {
        GameObject navBtn = CreateUIObject(name, parent);
        Button btn = navBtn.AddComponent<Button>();
        ColorBlock colors = btn.colors;
        colors.normalColor = Color.clear;
        colors.highlightedColor = new Color(1, 1, 1, 0.1f);
        colors.pressedColor = new Color(1, 1, 1, 0.2f);
        btn.colors = colors;
        
        RectTransform btnRect = navBtn.GetComponent<RectTransform>();
        btnRect.anchorMin = new Vector2(xPosition - 0.1f, 0);
        btnRect.anchorMax = new Vector2(xPosition + 0.1f, 1);
        btnRect.pivot = new Vector2(0.5f, 0.5f);
        btnRect.sizeDelta = Vector2.zero;
        
        // Icon text
        GameObject iconGO = CreateUIObject("IconText", navBtn.transform);
        TMP_Text iconText = iconGO.AddComponent<TextMeshProUGUI>();
        iconText.text = icon;
        iconText.fontSize = 24;
        iconText.color = Color.white;
        iconText.alignment = TextAlignmentOptions.Center;
        RectTransform iconRect = iconGO.GetComponent<RectTransform>();
        SetFullRect(iconRect);
        
        // Label text
        GameObject labelGO = CreateUIObject("LabelText", navBtn.transform);
        TMP_Text labelText = labelGO.AddComponent<TextMeshProUGUI>();
        labelText.text = name.Replace("Button", "");
        labelText.fontSize = 12;
        labelText.color = Color.white;
        labelText.alignment = TextAlignmentOptions.Center;
        RectTransform labelRect = labelGO.GetComponent<RectTransform>();
        labelRect.anchorMin = new Vector2(0, 0);
        labelRect.anchorMax = new Vector2(1, 0.3f);
        labelRect.pivot = new Vector2(0.5f, 0);
        labelRect.sizeDelta = Vector2.zero;
    }
    
    private GameObject CreateUIObject(string name, Transform parent)
    {
        GameObject go = new GameObject(name);
        go.AddComponent<RectTransform>();
        if (parent != null)
        {
            go.transform.SetParent(parent, false);
        }
        return go;
    }
    
    private void SetFullRect(RectTransform rectTransform)
    {
        rectTransform.anchorMin = Vector2.zero;
        rectTransform.anchorMax = Vector2.one;
        rectTransform.sizeDelta = Vector2.zero;
        rectTransform.anchoredPosition = Vector2.zero;
    }
    
    private TMP_Text FindTextInChildrenByName(Transform parent, string name)
    {
        Transform child = parent.Find(name);
        if (child != null)
        {
            return child.GetComponent<TMP_Text>();
        }
        
        // If the transform isn't found directly, look at containers with values
        Transform container = parent.Find(name + "Container");
        if (container != null)
        {
            Transform valueTransform = container.Find(name + "Value");
            if (valueTransform != null)
            {
                return valueTransform.GetComponent<TMP_Text>();
            }
        }
        
        Debug.LogWarning($"⚠️ Couldn't find text component for: {name} in {parent.name}");
        return null;
    }
}

// Adding the spinner rotator component referenced in the code
public class SpinnerRotator : MonoBehaviour
{
    public float rotationSpeed = 200f;
    
    private void Update()
    {
        transform.Rotate(0, 0, -rotationSpeed * Time.deltaTime);
    }
}

// Adding the RoundedCorners component referenced in the code
[RequireComponent(typeof(Image))]
public class RoundedCorners : MonoBehaviour
{
    public float radius = 10f;
    
    private void Start()
    {
        // Apply rounded corners to the image
        Image image = GetComponent<Image>();
        if (image != null)
        {
            // Set the default sprite with rounded corners
            // In a real implementation, this would use a custom material with rounded corners shader
            // or a custom-created rounded rectangle sprite
            
            // Simple approximation for the editor preview
            image.type = Image.Type.Sliced;
            image.pixelsPerUnitMultiplier = 1;
            
            // Note: For a complete implementation, you would need to use a custom shader 
            // or create a proper 9-slice sprite with rounded corners
        }
    }
}



