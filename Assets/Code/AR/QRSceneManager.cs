using UnityEngine;
using UnityEngine.XR.ARFoundation;
using UnityEngine.XR.ARSubsystems;
using ZXing;
using System.Collections;
using System.Collections.Generic;
using TMPro;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using UnityEngine.Networking;
using UnityEngine.UI;
using Models;
using Unity.VisualScripting;
using UnityEngine.SceneManagement;

public class QRSceneManager : MonoBehaviour
{
    public static QRSceneManager Instance;

    [Header("AR & QR")]
    public ARCameraManager arCameraManager;
    public GameObject scanBoxUI;

    [Header("UI")]
    public TMP_Text statusText;
    public Transform dataLayoutGroup;  // For real-time data only
    public Transform machineInfoLayoutGroup;  // NEW: For static machine info
    public GameObject dataRowPrefab;
    public GameObject categoryHeaderPrefab;
    public TMP_Text machineNameText;
    public TMP_Text machineTypeText;
    public GameObject apiRealTimePanel;
    public Button togglePanelButton;
    public Button resetScanButton;

    [Header("Config")]
    public bool isScanning = true;
    public bool isDataLoaded = true;

    private Vector3 qrCodePosition;
    private Vector3 qrCodeRotation;
    private string previousJson = "";
    private Dictionary<string, string> apiHeaders = new Dictionary<string, string>();
    private bool isPanelVisible = false;
    public string currentMachineCode = "";
    private Coroutine realTimeDataCoroutine;
    private bool hasRealTimeApi = false;

    private void Awake()
    {
        Instance = this;
        
        // Create machineInfoLayoutGroup if it doesn't exist
        if (machineInfoLayoutGroup == null)
        {
            GameObject machineInfoObj = new GameObject("MachineInfoLayoutGroup");
            machineInfoLayoutGroup = machineInfoObj.transform;
            machineInfoLayoutGroup.SetParent(apiRealTimePanel.transform, false);
            
            // Position it above the dataLayoutGroup
            RectTransform machineInfoRect = machineInfoObj.AddComponent<RectTransform>();
            RectTransform dataRect = dataLayoutGroup.GetComponent<RectTransform>();
            
            // Set anchors and pivot
            machineInfoRect.anchorMin = new Vector2(0, 1);
            machineInfoRect.anchorMax = new Vector2(1, 1);
            machineInfoRect.pivot = new Vector2(0.5f, 1);
            
            // Position it above the data layout
            machineInfoRect.anchoredPosition = new Vector2(0, 50);
            
            // Add layout component
            VerticalLayoutGroup layout = machineInfoObj.AddComponent<VerticalLayoutGroup>();
            layout.childAlignment = TextAnchor.UpperCenter;
            layout.spacing = 5;
            layout.padding = new RectOffset(10, 10, 10, 10);
            
            // Add content size fitter
            ContentSizeFitter fitter = machineInfoObj.AddComponent<ContentSizeFitter>();
            fitter.horizontalFit = ContentSizeFitter.FitMode.PreferredSize;
            fitter.verticalFit = ContentSizeFitter.FitMode.PreferredSize;
        }
    }

    private void Start()
    {
        togglePanelButton.onClick.AddListener(TogglePanel);
        
        // Add reset button listener if it exists
        if (resetScanButton != null)
        {
            resetScanButton.onClick.AddListener(ResetForNewScan);
        }
        
        apiRealTimePanel.GameObject().SetActive(true);
        // Handle close button if it exists
        Button closeButton = apiRealTimePanel.transform.Find("CloseButton")?.GetComponent<Button>();
        if (closeButton != null)
        {
            closeButton.onClick.AddListener(ClosePanel);
        }

        apiRealTimePanel.SetActive(false);
        
        // Set initial status text
        if (statusText != null)
            statusText.text = "📷 Scan a QR Code";
    }

    void Update()
    {
        if (isScanning)
        {
            TryScanQRCode();
        }
    }

    void TryScanQRCode()
    {
        if (!isDataLoaded || !isScanning) return;

        if (arCameraManager.TryAcquireLatestCpuImage(out XRCpuImage image))
        {
            scanBoxUI?.SetActive(true);

            var conversionParams = new XRCpuImage.ConversionParams
            {
                inputRect = new RectInt(0, 0, image.width, image.height),
                outputDimensions = new Vector2Int(image.width, image.height),
                outputFormat = TextureFormat.RGBA32,
                transformation = XRCpuImage.Transformation.None
            };

            Texture2D texture = new Texture2D(image.width, image.height, TextureFormat.RGBA32, false);
            image.Convert(conversionParams, texture.GetRawTextureData<byte>());
            image.Dispose();
            texture.Apply();

            IBarcodeReader barcodeReader = new BarcodeReader();
            var result = barcodeReader.Decode(texture.GetPixels32(), texture.width, texture.height);
            Destroy(texture);

            if (result != null)
            {
                isScanning = false;
                scanBoxUI?.SetActive(false);

                string[] values = result.Text.Split('@');
                if (values.Length < 1 || string.IsNullOrWhiteSpace(values[0]))
                {
                    UpdateUIText("Invalid QR Format!", "");
                    Invoke(nameof(ResetScanning), 2f);
                    return;
                }

                string machineCode = values[0].Trim();
                currentMachineCode = machineCode;
                qrCodePosition = arCameraManager.transform.position + arCameraManager.transform.forward * 0.5f;
                qrCodeRotation = arCameraManager.transform.rotation.eulerAngles;

                UpdateUIText("🔍 Fetching machine data...", "");
                StartCoroutine(FetchMachineDataRoutine(machineCode));
            }
        }
    }

    IEnumerator FetchMachineDataRoutine(string machineCode)
    {
        string endpoint = "/machine/code/" + machineCode;
        UnityWebRequest request = ApiConfig.CreateRequest(endpoint);

        Debug.Log($"📡 Sending API Request to: {endpoint}");

        yield return request.SendWebRequest();

        if (request.result == UnityWebRequest.Result.Success)
        {
            string jsonResponse = request.downloadHandler.text;
            Debug.Log($"✅ Machine API Response: {jsonResponse}");

            ApiResponse<MachineResponse> response = JsonConvert.DeserializeObject<ApiResponse<MachineResponse>>(jsonResponse);

            if (response != null && response.result != null)
            {
                MachineResponse machine = response.result;
                
                // Update machine details UI
                machineNameText.text = $"Machine: {machine.machineName ?? "Unknown"}";  
                machineTypeText.text = $"Type: {machine.machineType ?? "Unknown"}";

                // Handle API headers
                apiHeaders.Clear();

                if (machine.headerResponses != null && machine.headerResponses.Count > 0)
                {
                    foreach (var header in machine.headerResponses)
                    {
                        if (!string.IsNullOrEmpty(header.keyHeader) && !string.IsNullOrEmpty(header.valueOfKey))
                        {
                            if (!apiHeaders.ContainsKey(header.keyHeader))
                            {
                                apiHeaders[header.keyHeader] = header.valueOfKey;
                            }
                            else
                            {
                                Debug.LogWarning($"⚠️ Key '{header.keyHeader}' already exists. Skipping.");
                            }
                        }
                        else
                        {
                            Debug.LogWarning("⚠️ Found null or empty key in headerResponses.");
                        }
                    }
                }
                else
                {
                    apiHeaders["API_KEY"] = "my_secure_token_123"; // Default API key
                }

                // Show panel immediately
                ShowPanel();
                
                // Clear both layout groups for new data
                ClearUI(dataLayoutGroup);
                ClearUI(machineInfoLayoutGroup);
                
                // Always display machine type values in machineInfoLayoutGroup
                DisplayMachineTypeValues(machine);
                
                hasRealTimeApi = !string.IsNullOrEmpty(machine.apiUrl);
                
                if (hasRealTimeApi)
                {
                    // Stop previous coroutine if running
                    if (realTimeDataCoroutine != null)
                    {
                        StopCoroutine(realTimeDataCoroutine);
                    }
                    
                    // Start fetching real-time data
                    realTimeDataCoroutine = StartCoroutine(FetchRealTimeData(machine.apiUrl));
                    
                    // Show loading indicator in real-time data section
                    AddCategoryHeader("LOADING REAL-TIME DATA...", dataLayoutGroup);
                }
                else
                {
                    // No real-time API available
                    Debug.Log("ℹ️ No real-time API URL. Displaying static machine data only.");
                    AddCategoryHeader("NO REAL-TIME DATA AVAILABLE", dataLayoutGroup);
                }
            }
            else
            {
                Debug.LogError("❌ Failed to parse machine response.");
                UpdateUIText("Failed to get machine data!", "");
                Invoke(nameof(ResetScanning), 2f);
            }
        }
        else
        {
            Debug.LogError($"❌ Machine API Request Failed: {request.error}");
            UpdateUIText("❌ Failed to fetch machine data", "");
            Invoke(nameof(ResetScanning), 2f);
        }
    }

    IEnumerator FetchRealTimeData(string url)
    {
        while (isPanelVisible)
        {
            UnityWebRequest request = UnityWebRequest.Get(url);

            foreach (var header in apiHeaders)
            {
                request.SetRequestHeader(header.Key, header.Value);
            }

            yield return request.SendWebRequest();

            if (request.result == UnityWebRequest.Result.Success)
            {
                string jsonResponse = request.downloadHandler.text;
                Debug.Log($"✅ Real-time API Response: {jsonResponse}");

                if (jsonResponse != previousJson)
                {
                    try
                    {
                        JObject parsedData = JObject.Parse(jsonResponse);
                        DisplayRealTimeData(parsedData);
                        previousJson = jsonResponse;
                    }
                    catch (System.Exception ex)
                    {
                        Debug.LogError($"❌ Error parsing real-time data: {ex.Message}");
                        ClearUI(dataLayoutGroup);
                        AddCategoryHeader("ERROR PARSING DATA", dataLayoutGroup);
                    }
                }
            }
            else
            {
                Debug.LogError($"❌ Real-time API Failed: {request.error}");
                ClearUI(dataLayoutGroup);
                AddCategoryHeader("CONNECTION ERROR", dataLayoutGroup);
                AddDataRow("Error", request.error, dataLayoutGroup);
            }

            yield return new WaitForSeconds(5f); // Wait 5 seconds before calling API again
        }
    }

    // Method to display machine type values
    void DisplayMachineTypeValues(MachineResponse machine)
    {
        AddCategoryHeader("MACHINE INFORMATION", machineInfoLayoutGroup);
        
        if (machine.machineTypeValueResponses != null && machine.machineTypeValueResponses.Count > 0)
        {
            foreach (MachineTypeValueResponse attribute in machine.machineTypeValueResponses)
            {
                AddDataRow(attribute.machineTypeAttributeName, attribute.valueAttribute, machineInfoLayoutGroup);
            }
        }
        else
        {
            AddDataRow("No machine type data", "No attributes available", machineInfoLayoutGroup);
        }
    }

    void DisplayRealTimeData(JObject data)
    {
        
        // Only clear the real-time data section
        ClearUI(dataLayoutGroup);

        AddCategoryHeader("REAL-TIME DATA", dataLayoutGroup);
        
        foreach (var category in data)
        {
            string categoryName = category.Key;
            JToken categoryValues = category.Value;

            if (categoryValues.Type == JTokenType.Object)
            {
                AddSubCategoryHeader(categoryName, dataLayoutGroup);
                foreach (var item in categoryValues.Children<JProperty>())
                {
                    AddDataRow(item.Name, FormatValue(item.Value), dataLayoutGroup);
                }
            }
            else
            {
                AddDataRow(categoryName, FormatValue(categoryValues), dataLayoutGroup);
            }
        }
        
        // Force layout update after all UI elements are added
        ForceCompleteLayoutUpdate();
    }

    string FormatValue(JToken value)
    {
        if (value.Type == JTokenType.Null || (value.Type == JTokenType.String && string.IsNullOrWhiteSpace(value.ToString())))
        {
            return "Unknown";
        }
        if (value.Type == JTokenType.Float || value.Type == JTokenType.Integer)
        {
            return $"{value:F2}";
        }
        return value.ToString();
    }

    void AddDataRow(string key, string value, Transform parent)
    {
        GameObject newRow = Instantiate(dataRowPrefab, parent);
        TMP_Text keyText = newRow.transform.Find("KeyText").GetComponent<TMP_Text>();
        TMP_Text valueText = newRow.transform.Find("ValueText").GetComponent<TMP_Text>();

        keyText.text = key + "";
        keyText.fontStyle = FontStyles.Bold;
        keyText.alignment = TextAlignmentOptions.Right;
        keyText.fontSize = 24;

        valueText.text = value;
        valueText.fontStyle = FontStyles.Normal;
        valueText.alignment = TextAlignmentOptions.Left;
        valueText.fontSize = 24;

        if (key.ToLower().Contains("percent") || key.ToLower().Contains("usage"))
        {
            if (float.TryParse(value.Replace("%", ""), out float percentage))
            {
                valueText.color = percentage > 80 ? Color.red :
                                  percentage > 50 ? new Color(1f, 0.5f, 0f) :
                                  Color.green;
            }
        }

        CanvasGroup canvasGroup = newRow.AddComponent<CanvasGroup>();
        canvasGroup.alpha = 0;
        StartCoroutine(FadeIn(canvasGroup, 0.5f));
    }

    void AddCategoryHeader(string title, Transform parent)
    {
        GameObject newHeader = Instantiate(categoryHeaderPrefab, parent);
        TMP_Text headerText = newHeader.GetComponentInChildren<TMP_Text>();

        headerText.text = title.ToUpper();
        headerText.fontSize = 28;
        headerText.fontStyle = FontStyles.Bold;
        headerText.alignment = TextAlignmentOptions.Center;
    }
    
    void AddSubCategoryHeader(string title, Transform parent)
    {
        GameObject newHeader = Instantiate(categoryHeaderPrefab, parent);
        TMP_Text headerText = newHeader.GetComponentInChildren<TMP_Text>();

        headerText.text = title.ToUpper();
        headerText.fontSize = 24;
        headerText.fontStyle = FontStyles.Bold;
        headerText.alignment = TextAlignmentOptions.Center;
        headerText.color = new Color(0.3f, 0.6f, 0.9f); // Different color for subcategories
    }

    void ClearUI(Transform layoutGroup)
    {
        foreach (Transform child in layoutGroup)
        {
            Destroy(child.gameObject);
        }
    }

    IEnumerator FadeIn(CanvasGroup canvasGroup, float duration)
    {
        float time = 0;
        while (time < duration)
        {
            time += Time.deltaTime;
            canvasGroup.alpha = Mathf.Lerp(0, 1, time / duration);
            yield return null;
        }
        canvasGroup.alpha = 1;
    }

    void UpdateUIText(string title, string message)
    {
        if (statusText != null)
            statusText.text = $"{title}\n{message}";
            
        // Also update machine text if available
        if (machineNameText != null)
            machineNameText.text = title;
            
        if (machineTypeText != null && !string.IsNullOrEmpty(message))
            machineTypeText.text = message;
    }

    public void ResetScanning()
    {
        scanBoxUI?.SetActive(true);
        isScanning = true;
        UpdateUIText("📷 Scan a QR Code", "");
    }
    
    // Method to show panel immediately
    void ShowPanel()
    {
        isPanelVisible = true;
        apiRealTimePanel.SetActive(true);
    
        // Force a layout update immediately when showing the panel
        ForceCompleteLayoutUpdate();
    }
    void ForceCompleteLayoutUpdate()
    {
        // First pass - process all layout components
        Canvas.ForceUpdateCanvases();
    
        // Wait for the end of frame
        StartCoroutine(SecondLayoutPass());
    }

    IEnumerator SecondLayoutPass()
    {
        yield return new WaitForEndOfFrame();
    
        // Second pass after frame rendering
        Canvas.ForceUpdateCanvases();
    
        // Force rebuilds on all important RectTransforms
        if (dataLayoutGroup != null)
            LayoutRebuilder.ForceRebuildLayoutImmediate(dataLayoutGroup.GetComponent<RectTransform>());
    
        if (machineInfoLayoutGroup != null)
            LayoutRebuilder.ForceRebuildLayoutImmediate(machineInfoLayoutGroup.GetComponent<RectTransform>());
    
        // Walk up the hierarchy and rebuild parent layouts
        Transform current = dataLayoutGroup;
        while (current != null && current.GetComponent<RectTransform>() != null)
        {
            LayoutRebuilder.ForceRebuildLayoutImmediate(current.GetComponent<RectTransform>());
            current = current.parent;
        }
    }
    IEnumerator DelayedPanelShow(CanvasGroup canvasGroup)
    {
        // Set panel as visible for layout calculations but not yet visible to user
        isPanelVisible = true;
    
        // Wait for several frames to ensure layouts are calculated
        for (int i = 0; i < 3; i++)
        {
            yield return new WaitForEndOfFrame();
        
            // Force layout rebuilds
            Canvas.ForceUpdateCanvases();
        
            if (dataLayoutGroup != null)
            {
                LayoutRebuilder.ForceRebuildLayoutImmediate(dataLayoutGroup.GetComponent<RectTransform>());
            }
            if (machineInfoLayoutGroup != null)
            {
                LayoutRebuilder.ForceRebuildLayoutImmediate(machineInfoLayoutGroup.GetComponent<RectTransform>());
            }
            LayoutRebuilder.ForceRebuildLayoutImmediate(apiRealTimePanel.GetComponent<RectTransform>());
        }
    
        // Fade in the panel
        float duration = 0.3f;
        float time = 0;
        while (time < duration)
        {
            time += Time.deltaTime;
            canvasGroup.alpha = Mathf.Lerp(0, 1, time / duration);
            yield return null;
        }
        canvasGroup.alpha = 1;
    }
    
    IEnumerator RefreshLayoutDelayed()
    {
        // Wait for the end of the frame to ensure all UI elements are instantiated
        yield return new WaitForEndOfFrame();
    
        // Force layout rebuild on both layout groups
        if (dataLayoutGroup != null)
        {
            LayoutRebuilder.ForceRebuildLayoutImmediate(dataLayoutGroup.GetComponent<RectTransform>());
        }
    
        if (machineInfoLayoutGroup != null)
        {
            LayoutRebuilder.ForceRebuildLayoutImmediate(machineInfoLayoutGroup.GetComponent<RectTransform>());
        }
    
        // Also rebuild the panel itself
        LayoutRebuilder.ForceRebuildLayoutImmediate(apiRealTimePanel.GetComponent<RectTransform>());
    
        // Wait another frame and do it again to ensure all nested layouts are properly updated
        yield return new WaitForEndOfFrame();
    
        if (dataLayoutGroup != null)
        {
            LayoutRebuilder.ForceRebuildLayoutImmediate(dataLayoutGroup.GetComponent<RectTransform>());
        }
    
        if (machineInfoLayoutGroup != null)
        {
            LayoutRebuilder.ForceRebuildLayoutImmediate(machineInfoLayoutGroup.GetComponent<RectTransform>());
        }
    
        LayoutRebuilder.ForceRebuildLayoutImmediate(apiRealTimePanel.GetComponent<RectTransform>());
    }
    
    // Keep the original toggle panel method for button functionality
    void TogglePanel()
    {
        isPanelVisible = !isPanelVisible;
        apiRealTimePanel.SetActive(isPanelVisible);

        if (isPanelVisible && !string.IsNullOrEmpty(currentMachineCode))
        {
            // Panel is now visible, make sure data is loaded
            if (previousJson == "")
            {
                StartCoroutine(FetchMachineDataRoutine(currentMachineCode));
            }
        }
    }
    
    // Reset button functionality
    public void ResetForNewScan()
    {
        // Stop any ongoing real-time data fetch
        if (realTimeDataCoroutine != null)
        {
            StopCoroutine(realTimeDataCoroutine);
            realTimeDataCoroutine = null;
        }
        
        // Hide the panel
        isPanelVisible = false;
        apiRealTimePanel.SetActive(false);
        
        // Reset variables
        previousJson = "";
        currentMachineCode = "";
        hasRealTimeApi = false;
        
        // Clear both UI sections
        ClearUI(dataLayoutGroup);
        ClearUI(machineInfoLayoutGroup);
        
        // Allow scanning again
        scanBoxUI?.SetActive(true);
        isScanning = true;
        UpdateUIText("📷 Scan a QR Code", "");
    }
    
    public void ClosePanel() 
    {
        apiRealTimePanel.SetActive(false);
        isPanelVisible = false;
        ResetForNewScan();
        Debug.Log("❌ Panel closed.");
    }
    
    public void GoBackToMainApp()
    {
        PlayerPrefs.Save();
        SceneManager.LoadScene("MainApp2");
    }
}