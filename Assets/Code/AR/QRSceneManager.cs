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

public class QRSceneManager : MonoBehaviour
{
    public static QRSceneManager Instance;

    [Header("AR & QR")]
    public ARCameraManager arCameraManager;
    public GameObject scanBoxUI;

    [Header("UI")]
    public GameObject loadingPanel;
    public TMP_Text statusText;
    public Transform dataLayoutGroup;
    public GameObject dataRowPrefab;
    public GameObject categoryHeaderPrefab;
    public TMP_Text machineNameText;
    public TMP_Text machineTypeText;
    public GameObject apiRealTimePanel;
    public Button togglePanelButton;

    [Header("Config")]
    public bool isScanning = true;
    public bool isDataLoaded = true;

    private Vector3 qrCodePosition;
    private Vector3 qrCodeRotation;
    private string previousJson = "";
    private Dictionary<string, string> apiHeaders = new Dictionary<string, string>();
    private bool isPanelVisible = false;
    public string currentMachineCode = "";

    private void Awake()
    {
        Instance = this;
    }

    private void Start()
    {
        togglePanelButton.onClick.AddListener(TogglePanel);
        
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

                ShowLoadingUI("Fetching machine data...");
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

                // Show real-time panel and start fetching data
                TogglePanel();
                
                if (!string.IsNullOrEmpty(machine.apiUrl))
                {
                    StartCoroutine(FetchRealTimeData(machine.apiUrl));
                }
                else
                {
                    Debug.LogError("❌ Failed to get real-time API URL.");
                    UpdateUIText("❌ No real-time data available", "");
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

        if (loadingPanel != null)
            loadingPanel.SetActive(false);
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
                    JObject parsedData = JObject.Parse(jsonResponse);
                    DisplayRealTimeData(parsedData);
                    previousJson = jsonResponse;
                }
            }
            else
            {
                Debug.LogError($"❌ Real-time API Failed: {request.error}");
            }

            yield return new WaitForSeconds(5f); // Wait 5 seconds before calling API again
        }
    }

    public void UpdateMachineUI(MachineResponse machine)
    {
        machineNameText.text = $"Machine: {machine.machineName ?? "Unknown"}";  
        machineTypeText.text = $"Type: {machine.machineType ?? "Unknown"}";

        ClearUI();

        if (machine.machineTypeValueResponses != null)
        {
            foreach (MachineTypeValueResponse attribute in machine.machineTypeValueResponses)
            {
                AddDataRow(attribute.machineTypeAttributeName, attribute.valueAttribute);
            }
        }
    }

    void DisplayRealTimeData(JObject data)
    {
        ClearUI();

        foreach (var category in data)
        {
            string categoryName = category.Key;
            JToken categoryValues = category.Value;

            if (categoryValues.Type == JTokenType.Object)
            {
                AddCategoryHeader(categoryName);
                foreach (var item in categoryValues.Children<JProperty>())
                {
                    AddDataRow(item.Name, FormatValue(item.Value));
                }
            }
            else
            {
                AddDataRow(categoryName, FormatValue(categoryValues));
            }
        }
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

    void AddDataRow(string key, string value)
    {
        GameObject newRow = Instantiate(dataRowPrefab, dataLayoutGroup);
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

    void AddCategoryHeader(string title)
    {
        GameObject newHeader = Instantiate(categoryHeaderPrefab, dataLayoutGroup);
        TMP_Text headerText = newHeader.GetComponentInChildren<TMP_Text>();

        headerText.text = title.ToUpper();
        headerText.fontSize = 28;
        headerText.fontStyle = FontStyles.Bold;
        headerText.alignment = TextAlignmentOptions.Center;
    }

    void ClearUI()
    {
        foreach (Transform child in dataLayoutGroup)
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

    void ShowLoadingUI(string message)
    {
        if (loadingPanel != null)
            loadingPanel.SetActive(true);

        if (statusText != null)
            statusText.text = message;
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
    
    public void ClosePanel() 
    {
        apiRealTimePanel.SetActive(false);
        isPanelVisible = false;
        Debug.Log("❌ Panel closed.");
    }
}