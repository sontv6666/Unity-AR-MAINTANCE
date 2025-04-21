using UnityEngine;
using UnityEngine.Networking;
using System.Collections;
using System.Collections.Generic;
using TMPro;
using Newtonsoft.Json.Linq;
using Models;
using UnityEngine.UI;
using Newtonsoft.Json;
using UnityEngine.SceneManagement;

public class APIRealTime : MonoBehaviour
{
    public static APIRealTime Instance;

    [Header("UI Elements")]
    public Transform dataLayoutGroup;        // For real-time data
    public Transform machineInfoLayoutGroup; // For static machine info
    public GameObject dataRowPrefab;
    public GameObject categoryHeaderPrefab;
    public TMP_Text machineNameText;
    public TMP_Text machineTypeText;
    public GameObject apiRealTimePanel;      // Panel to toggle visibility
    public Button togglePanelButton;         // Button to open/close the panel
    public Button resetScanButton;           // Button to reset the scan

    [Header("Status")]
    public TMP_Text statusText;              // For showing status messages

    private string previousJson = "";
    private Dictionary<string, string> apiHeaders = new Dictionary<string, string>();
    private bool isPanelVisible = false;     // Track visibility state
    private string currentMachineCode = "";
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
        
        // Set the panel to active first, but we'll turn it off later
        apiRealTimePanel.SetActive(true);
        
        // Handle close button if it exists
        Button closeButton = apiRealTimePanel.transform.Find("CloseButton")?.GetComponent<Button>();
        if (closeButton != null)
        {
            closeButton.onClick.AddListener(ClosePanel);
        }

        // Start with the panel hidden
        apiRealTimePanel.SetActive(false);
        
        // Set initial status text if available
        if (statusText != null)
            statusText.text = "Scan a QR Code";
    }

    void TogglePanel()
    {
        isPanelVisible = !isPanelVisible;
        apiRealTimePanel.SetActive(isPanelVisible);

        // Check for QRCodeScanner before accessing it
        if (QRCodeScanner.Instance != null && isPanelVisible)
        {
            currentMachineCode = QRCodeScanner.Instance.currentMachineCode;

            if (!string.IsNullOrEmpty(currentMachineCode))
            {
                // If panel is now visible, make sure data is loaded
                if (previousJson == "")
                {
                    StartCoroutine(FetchMachineDataRoutine(currentMachineCode, "", ""));
                }
            }
        }
        
        // Force layout update when showing the panel
        if (isPanelVisible)
        {
            ForceCompleteLayoutUpdate();
        }
    }

    public void FetchMachineData(string machineCode, string secondValue, string courseId)
    {
        currentMachineCode = machineCode;
        StartCoroutine(FetchMachineDataRoutine(machineCode, secondValue, courseId));
    }

    IEnumerator FetchMachineDataRoutine(string machineCode, string secondValue, string courseId)
    {
        if (statusText != null)
            statusText.text = "🔍 Fetching machine data...";
            
        // Get company ID from PlayerPrefs
        string companyId = PlayerPrefs.GetString("CompanyId", "");
        
        if (string.IsNullOrEmpty(companyId))
        {
            Debug.LogError("❌ Company ID is missing! User might need to log in again.");
            UpdateUIText("Missing company information", "Please log in again");
            yield break;
        }
        
        // Updated endpoint to include company ID
        string endpoint = $"/machine/code/{machineCode}/company/{companyId}";
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
                UpdateMachineUI(machine);

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
            }
        }
        else
        {
            Debug.LogError($"❌ Machine API Request Failed: {request.error}");
            UpdateUIText("❌ Failed to fetch machine data", "");
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

    // This is the backward compatible method used by other classes
    public void UpdateMachineUI(MachineResponse machine)
    {
        // Update machine details UI
        machineNameText.text = $"Machine: {machine.machineName ?? "Unknown"}";  
        machineTypeText.text = $"Type: {machine.machineType ?? "Unknown"}";

        // Clear both layout groups for new data
        ClearUI(dataLayoutGroup);
        ClearUI(machineInfoLayoutGroup);
        
        // Always display machine type values in machineInfoLayoutGroup
        DisplayMachineTypeValues(machine);
        
        // Make sure panel is visible
        //ShowPanel();
        
        // Store API headers for potential real-time data fetch later
        if (machine.headerResponses != null && machine.headerResponses.Count > 0)
        {
            apiHeaders.Clear();
            foreach (var header in machine.headerResponses)
            {
                if (!string.IsNullOrEmpty(header.keyHeader) && !string.IsNullOrEmpty(header.valueOfKey))
                {
                    apiHeaders[header.keyHeader] = header.valueOfKey;
                }
            }
        }
        
        // Start real-time data update if apiUrl is available
        if (!string.IsNullOrEmpty(machine.apiUrl))
        {
            if (realTimeDataCoroutine != null)
            {
                StopCoroutine(realTimeDataCoroutine);
            }
            realTimeDataCoroutine = StartCoroutine(FetchRealTimeData(machine.apiUrl));
        }
        
        // Force layout update
        ForceCompleteLayoutUpdate();
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

    // Backward compatible method that calls the new version
    void AddDataRow(string key, string value)
    {
        AddDataRow(key, value, dataLayoutGroup);
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

    // Backward compatible method that calls the new version
    void AddCategoryHeader(string title)
    {
        AddCategoryHeader(title, dataLayoutGroup);
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

    // Backward compatible method
    void ClearUI()
    {
        ClearUI(dataLayoutGroup);
        ClearUI(machineInfoLayoutGroup);
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

    public void UpdateUIText(string title, string message)
    {
        if (statusText != null)
            statusText.text = $"{title}\n{message}";
            
        // Also update machine text if available
        if (machineNameText != null)
            machineNameText.text = title;
            
        if (machineTypeText != null && !string.IsNullOrEmpty(message))
            machineTypeText.text = message;
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
        
        // Update status text
        if (statusText != null)
            statusText.text = "Scan a QR Code";
            
        // If there's a QRCodeScanner, we can notify it to start scanning again
        if (QRCodeScanner.Instance != null)
        {
            QRCodeScanner.Instance.ResetScanning();
        }
    }
    
    public void ClosePanel() 
    {
        apiRealTimePanel.SetActive(false);
        isPanelVisible = false;
        Debug.Log("❌ Panel closed.");
    }
    
    public void GoBackToMainApp()
    {
        PlayerPrefs.Save();
        SceneManager.LoadScene("MainApp2");
    }
}