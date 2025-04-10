using UnityEngine;
using UnityEditor;
using UnityEngine.UI;

public class UIAutoBuilder : EditorWindow
{
    [MenuItem("Tools/Generate Login UI")]
    public static void CreateLoginUIScene()
    {
        // Create Canvas
        GameObject canvasGO = new GameObject("Canvas", typeof(Canvas));
        canvasGO.layer = LayerMask.NameToLayer("UI");
        Canvas canvas = canvasGO.GetComponent<Canvas>();
        canvas.renderMode = RenderMode.ScreenSpaceOverlay;

        CanvasScaler scaler = canvasGO.AddComponent<CanvasScaler>();
        scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
        scaler.referenceResolution = new Vector2(1080, 1920); // Ideal for iPad/iPhone portrait
        scaler.screenMatchMode = CanvasScaler.ScreenMatchMode.MatchWidthOrHeight;
        scaler.matchWidthOrHeight = 0.5f;

        canvasGO.AddComponent<GraphicRaycaster>();

        // Create Background Panel
        GameObject bg = CreateUIObject("Background", canvasGO.transform);
        Image bgImage = bg.AddComponent<Image>();
        bgImage.color = new Color(0.1f, 0.2f, 0.4f); // Dark blue
        RectTransform bgRect = bg.GetComponent<RectTransform>();
        bgRect.anchorMin = Vector2.zero;
        bgRect.anchorMax = Vector2.one;
        bgRect.offsetMin = Vector2.zero;
        bgRect.offsetMax = Vector2.zero;

        // Create Login Panel
        GameObject loginPanel = CreateUIObject("LoginPanel", canvasGO.transform);
        Image panelImage = loginPanel.AddComponent<Image>();
        panelImage.color = new Color(1f, 1f, 1f, 0.95f); // Almost white
        RectTransform panelRect = loginPanel.GetComponent<RectTransform>();
        panelRect.sizeDelta = new Vector2(600, 700);
        panelRect.anchoredPosition = Vector2.zero;

        VerticalLayoutGroup layout = loginPanel.AddComponent<VerticalLayoutGroup>();
        layout.childAlignment = TextAnchor.MiddleCenter;
        layout.spacing = 30;
        layout.padding = new RectOffset(40, 40, 40, 40);

        // Title
        CreateText("Login", loginPanel.transform, 64, TextAnchor.MiddleCenter, Color.black);

        // Username Input
        CreateInputField(loginPanel.transform, "Username");

        // Password Input
        CreateInputField(loginPanel.transform, "Password", true);

        // Login Button
        CreateButton(loginPanel.transform, "Log In");

        Debug.Log("Login UI generated!");
    }

    static GameObject CreateUIObject(string name, Transform parent)
    {
        GameObject go = new GameObject(name, typeof(RectTransform));
        go.transform.SetParent(parent, false);
        return go;
    }

    static void CreateText(string text, Transform parent, int fontSize, TextAnchor alignment, Color color)
    {
        GameObject textGO = CreateUIObject("Text", parent);
        Text txt = textGO.AddComponent<Text>();
        txt.text = text;
        txt.font = Resources.GetBuiltinResource<Font>("Arial.ttf");
        txt.fontSize = fontSize;
        txt.alignment = alignment;
        txt.color = color;

        RectTransform rect = textGO.GetComponent<RectTransform>();
        rect.sizeDelta = new Vector2(500, 100);
    }

    static void CreateInputField(Transform parent, string placeholder, bool isPassword = false)
    {
        GameObject inputGO = CreateUIObject(placeholder + "Input", parent);
        Image image = inputGO.AddComponent<Image>();
        image.color = new Color(0.95f, 0.95f, 0.95f); // Light gray

        InputField inputField = inputGO.AddComponent<InputField>();
        inputField.textComponent = CreateTextComponent(inputGO.transform, "", TextAnchor.MiddleLeft);
        inputField.placeholder = CreateTextComponent(inputGO.transform, placeholder, TextAnchor.MiddleLeft, Color.gray);
        if (isPassword) inputField.contentType = InputField.ContentType.Password;

        RectTransform rect = inputGO.GetComponent<RectTransform>();
        rect.sizeDelta = new Vector2(500, 80);
    }

    static void CreateButton(Transform parent, string label)
    {
        GameObject btnGO = CreateUIObject(label + "Button", parent);
        Image image = btnGO.AddComponent<Image>();
        image.color = new Color(0.2f, 0.6f, 1f); // Blue

        Button button = btnGO.AddComponent<Button>();
        button.targetGraphic = image;

        Text txt = CreateTextComponent(btnGO.transform, label, TextAnchor.MiddleCenter, Color.white);
        txt.fontSize = 28;

        RectTransform rect = btnGO.GetComponent<RectTransform>();
        rect.sizeDelta = new Vector2(500, 80);
    }

    static Text CreateTextComponent(Transform parent, string text, TextAnchor alignment, Color? color = null)
    {
        GameObject txtGO = CreateUIObject("Text", parent);
        Text txt = txtGO.AddComponent<Text>();
        txt.text = text;
        txt.font = Resources.GetBuiltinResource<Font>("Arial.ttf");
        txt.fontSize = 24;
        txt.alignment = alignment;
        txt.color = color ?? Color.black;

        RectTransform rect = txtGO.GetComponent<RectTransform>();
        rect.anchorMin = Vector2.zero;
        rect.anchorMax = Vector2.one;
        rect.offsetMin = Vector2.zero;
        rect.offsetMax = Vector2.zero;

        return txt;
    }
}
