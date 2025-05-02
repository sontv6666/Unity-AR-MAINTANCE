using UnityEngine;
using UnityEngine.Networking;
using UnityEngine.UI;
using TMPro;
using System.Collections;
using System.Collections.Generic;
using Newtonsoft.Json;

public class RequestPointScreenLoader : MonoBehaviour
{
    [Header("UI References")] public GameObject requestPointPrefab; // Prefab để tạo danh sách
    public Transform requestPointLayoutGroup; 
    public Sprite processingSprite, completeSprite; 

    public GameObject requestPointListPage; // Page for list of request points
    public GameObject requestPointPage; // Page for requesting new points
    public GameObject profilePage; // Profile page

    private GameObject previousPage;
    
    [Header("Empty State")]
    public GameObject emptyStatePanel; // Panel to show when no requests exist


    public Button backButton;


    [Header("API Settings")] private string getRequestPointEndpoint = "/point-request/employee/"; // API Endpoint

    private void Start()
    {
        LoadRequests();

        backButton.onClick.AddListener(GoBack);
    }

  


    public void OpenRequestPointListFromRequestPage()
    {
        LoadRequests();
        Debug.Log("📌 Opening Request Point List from Request Page...");
        previousPage = requestPointPage; // Track the previous page
        OpenRequestPointListPage();
    }

    public void OpenRequestPointListFromProfile()
    {
        LoadRequests();
        Debug.Log("📌 Opening Request Point List from Profile Page...");
        previousPage = profilePage; // Track the previous page
        OpenRequestPointListPage();
    }

    private void OpenRequestPointListPage()
    {
        LoadRequests();
        if (requestPointListPage != null)
        {
            requestPointListPage.SetActive(true);
        }
        else
        {
            Debug.LogWarning("⚠️ requestPointListPage is missing! Assign it in the inspector.");
        }
    }

    public void GoBack()
    {
        Debug.Log("🔙 Returning to Previous Page...");

        if (requestPointListPage != null)
        {
            requestPointListPage.SetActive(false); // Hide current page
        }

        if (previousPage != null)
        {
            previousPage.SetActive(true); // Go back to where user came from
        }
        else
        {
            Debug.LogWarning("⚠️ No previous page recorded! Defaulting to Profile Page.");
            if (profilePage != null)
            {
                profilePage.SetActive(true); // Default to profile page
            }
        }
    }

    public void LoadRequests()
    {
        string userId = PlayerPrefs.GetString("UserId", "");
        string token = PlayerPrefs.GetString("AuthToken", "");

        if (string.IsNullOrEmpty(userId) || string.IsNullOrEmpty(token))
        {
            Debug.LogError("Missing employee ID or authentication token.");
            return;
        }

        StartCoroutine(FetchRequests(userId, token));
    }

    private IEnumerator FetchRequests(string employeeId, string token)
    {
        string endpoint = $"/point-request/employee/{employeeId}";
        UnityWebRequest request = ApiConfig.CreateRequest(endpoint, "GET");

        yield return request.SendWebRequest();

        if (request.result != UnityWebRequest.Result.Success)
        {
            Debug.LogError($"❌ API Error: {request.error}");
        }
        else
        {
            string responseText = request.downloadHandler.text;
            Debug.Log($"✅ API Response: {responseText}");

            try
            {
                RequestPointResponse response = JsonConvert.DeserializeObject<RequestPointResponse>(responseText);
                if (response != null && response.code == 1000)
                {
                    DisplayRequests(response.result);
                }
            }
            catch (System.Exception e)
            {
                Debug.LogError($"❌ JSON Parsing Error: {e.Message}");
            }
        }
    }


private void DisplayRequests(List<RequestPointData> requests)
{
    
    foreach (Transform child in requestPointLayoutGroup)
    {
        Destroy(child.gameObject);
    }

    Debug.Log($"🔄 Displaying {requests.Count} requests...");

    foreach (var request in requests)
    {
        GameObject newRequest = Instantiate(requestPointPrefab, requestPointLayoutGroup);
        newRequest.transform.localScale = Vector3.one; // Đảm bảo không bị scale sai

        TMP_Text statusName = newRequest.transform.Find("statusName")?.GetComponent<TMP_Text>();
        Image statusImage = newRequest.transform.Find("statusImage")?.GetComponent<Image>();
        TMP_Text requestNumber = newRequest.transform.Find("requestNumber")?.GetComponent<TMP_Text>();
        TMP_Text createdDate = newRequest.transform.Find("createdDate")?.GetComponent<TMP_Text>();
        TMP_InputField requestReasonField = newRequest.transform.Find("requestReason/InputField (TMP)")?.GetComponent<TMP_InputField>();
        TMP_Text point = newRequest.transform.Find("point")?.GetComponent<TMP_Text>();

        if (statusName == null || requestNumber == null || createdDate == null || requestReasonField == null || point == null)
        {
            Debug.LogError("⚠️ Missing UI Elements! Check prefab structure.");
            continue;
        }

        statusName.text = request.status;
        requestNumber.text = $"Request Number: {request.requestNumber}";
        createdDate.text = $"Created: {FormatDate(request.createdAt)}";

        requestReasonField.text = request.reason; // Gán text vào InputField
        requestReasonField.interactable = false; // Đảm bảo user không thể chỉnh sửa
        point.text = $"Points: {request.amount}";

        // 🟢 Cập nhật màu sắc theo trạng thái
        if (statusImage != null)
        {
            switch (request.status)
            {
                case "PROCESSING":
                    statusImage.color = Color.yellow; // Màu vàng cho đang xử lý
                    break;
                case "APPROVED":
                    statusImage.color = Color.green; // Màu xanh lá cho hoàn thành
                    break;
                case "REJECT":
                    statusImage.color = Color.red; // Màu đỏ cho từ chối
                    break;
                default:
                    statusImage.color = Color.gray; // Màu xám cho trạng thái không xác định
                    break;
            }
        }
    }

    // Ẩn/hiện empty state
    ShowEmptyState(requests.Count == 0);
}

private string FormatDate(string dateTime)
{
    if (System.DateTime.TryParse(dateTime, out System.DateTime parsedDate))
    {
        return parsedDate.ToString("dd/MM/yyyy HH:mm:ss");
    }
    return dateTime; // Nếu lỗi, giữ nguyên
}

public void OpenRequestPointPage()
{
    Debug.Log($"📌 Opening Request Point Page from {previousPage?.name}...");

    if (requestPointPage != null)
    {
        requestPointPage.SetActive(true);
        requestPointListPage.SetActive(false);
    }
    
}






private void ShowEmptyState(bool show)
{
    if (emptyStatePanel)
    {
        emptyStatePanel.SetActive(show);
    }
    requestPointLayoutGroup.gameObject.SetActive(!show);
}
}


[System.Serializable]
public class RequestPointResponse
{
    public int code;
    public string message;
    public List<RequestPointData> result;
}

[System.Serializable]
public class RequestPointData
{
    public string id;
    public string reason;
    public int amount;
    public string requestNumber;
    public string status;
    public string createdAt;
}
