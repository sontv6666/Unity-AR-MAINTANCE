using UnityEngine;
using UnityEngine.Networking;
using System.Collections;
using System.Collections.Generic;
using TMPro;
using Newtonsoft.Json.Linq;
using Models;
using UnityEngine.UI;
using Newtonsoft.Json;

public class APIRealTime : MonoBehaviour
{
    public static APIRealTime Instance;

    [Header("UI Elements")]
    public Transform dataLayoutGroup;
    public GameObject dataRowPrefab;
    public GameObject categoryHeaderPrefab;
    public TMP_Text machineNameText;
    public TMP_Text machineTypeText;
    public GameObject apiRealTimePanel; // Panel to toggle visibility
    public Button togglePanelButton; // Button to open/close the panel

    private string previousJson = "";
    private Dictionary<string, string> apiHeaders = new Dictionary<string, string>();
    private bool isPanelVisible = false; // Track visibility state
    
    private string currentMachineCode = "";

    private void Awake()
    {
        Instance = this;
    }

    private void Start()
    {
        togglePanelButton.onClick.AddListener(TogglePanel);
    
        // 🔹 Nếu có nút đóng, gán sự kiện đóng panel
        Button closeButton = apiRealTimePanel.transform.Find("CloseButton")?.GetComponent<Button>();
        if (closeButton != null)
        {
            closeButton.onClick.AddListener(ClosePanel);
        }

        apiRealTimePanel.SetActive(false); // Panel starts hidden
    }


    void TogglePanel()
    {
        isPanelVisible = !isPanelVisible;
        apiRealTimePanel.SetActive(isPanelVisible);

        // ✅ Ensure QRCodeScanner exists before accessing it
        if (QRCodeScanner.Instance != null && isPanelVisible)
        {
            currentMachineCode = QRCodeScanner.Instance.currentMachineCode; // ✅ Get latest machine code

            if (!string.IsNullOrEmpty(currentMachineCode))
            {
                StartCoroutine(FetchMachineDataRoutine(currentMachineCode, "", ""));
            }
        }
    }



    public void FetchMachineData(string machineCode, string secondValue, string courseId)
    {
        StartCoroutine(FetchMachineDataRoutine(machineCode, secondValue, courseId));
    }

IEnumerator FetchMachineDataRoutine(string machineCode, string secondValue, string courseId)
{
    currentMachineCode = machineCode;
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

            // 🔹 Xử lý Header nếu bị thiếu
            apiHeaders.Clear(); // ✅ Reset trước khi thêm mới

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
                apiHeaders["API_KEY"] = "my_secure_token_123"; // ✅ Thêm API_KEY mặc định
            }


            // 🔹 Nếu có API URL, bắt đầu real-time fetch khi panel mở
            if (!string.IsNullOrEmpty(machine.apiUrl))
            {
               
                StartCoroutine(FetchRealTimeData(machine.apiUrl));
            }
            else
            {
                Debug.LogError("❌ Failed to call api real tine response.");
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
        UpdateUIText("Failed to get machine data!", "");
    }
}

IEnumerator FetchRealTimeData(string url)
{
    while (isPanelVisible) // Chỉ gọi API khi panel đang mở
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

        yield return new WaitForSeconds(5f); // Chờ 5 giây trước khi gọi lại API
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

    public void UpdateUIText(string title, string message)
    {
        machineNameText.text = title;
        machineTypeText.text = message;
    }
    
    public void ClosePanel() 
    {
        // 🚫 Đóng panel hiển thị trước đó
        apiRealTimePanel.SetActive(false);
        isPanelVisible = false; // Cập nhật trạng thái panel

        Debug.Log("❌ Panel closed.");
    }

}
