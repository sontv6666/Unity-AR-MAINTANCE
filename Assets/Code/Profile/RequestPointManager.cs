using UnityEngine;
using UnityEngine.Networking;
using TMPro;
using System.Collections;
using System.Text;
using System;
using Newtonsoft.Json;

using UnityEngine.UI;

public class RequestPointManager : MonoBehaviour
{
    [Header("UI References")]
    public GameObject requestPointPage;
    public GameObject profilePage;
    public TMP_InputField reasonInput;   // Reason Input Field
    public TMP_InputField pointInput;    // Point Input Field
    public TMP_Text warningText;         // Warning Text

    [Header("API Settings")]
    private string requestPointEndpoint = "/point-request";  // API Endpoint
    private const int MAX_POINTS = 30;  // Maximum allowed points per request

    private void Start()
    {
        // Ensure only numbers can be entered
        pointInput.contentType = TMP_InputField.ContentType.IntegerNumber;
        pointInput.onValueChanged.AddListener(ValidatePointInput);
    }

    // Restrict input to numbers and max 30 points
    private void ValidatePointInput(string input)
    {
        if (string.IsNullOrEmpty(input)) return;

        if (!int.TryParse(input, out int value) || value < 0)
        {
            pointInput.text = "0";
            return;
        }

        if (value > MAX_POINTS)
        {
            pointInput.text = MAX_POINTS.ToString();
            warningText.text = "⚠️ Max 30 points allowed per request!";
        }
        else
        {
            warningText.text = "";  // Clear warning if valid
        }
    }

    public void OnRequestPoints()
    {
        string reason = reasonInput.text.Trim();
        string amountText = pointInput.text.Trim();
        
        if (string.IsNullOrEmpty(reason) || string.IsNullOrEmpty(amountText))
        {
            warningText.text = "Please enter both reason and amount!";
            return;
        }

        if (!int.TryParse(amountText, out int amount) || amount <= 0)
        {
            warningText.text = "Invalid amount!";
            return;
        }

        if (amount > MAX_POINTS)
        {
            warningText.text = "⚠️ Max 30 points allowed per request!";
            return;
        }

        string companyId = PlayerPrefs.GetString("CompanyId", "");
        string employeeId = PlayerPrefs.GetString("UserId", "");
        string token = PlayerPrefs.GetString("AuthToken", ""); // 🔥 Get API Token
        
        Debug.Log($"👤 User: {employeeId}, Company: {companyId}, Token: {token}");

        if (string.IsNullOrEmpty(companyId) || string.IsNullOrEmpty(employeeId))
        {
            warningText.text = "User data missing!";
            return;
        }

        if (string.IsNullOrEmpty(token))
        {
            warningText.text = "⚠️ Authentication token is missing! Please log in again.";
            return;
        }

        // Create Request Object
        RequestPointPostData requestData = new RequestPointPostData
        {
            reason = reason,
            amount = amount,
            companyId = companyId,
            employeeId = employeeId
        };

        string requestBody = JsonConvert.SerializeObject(requestData);
        StartCoroutine(SendPointRequest(requestBody, token));
    }

    private IEnumerator SendPointRequest(string jsonBody, string token)
    {
        Debug.Log("🔄 Sending point request...");

        using (UnityWebRequest request = ApiConfig.CreateRequest(requestPointEndpoint, "POST", jsonBody))
        {
            request.SetRequestHeader("Authorization", "Bearer " + token); // 🔥 Add Auth Token

            yield return request.SendWebRequest();

            if (request.result != UnityWebRequest.Result.Success)
            {
                Debug.LogError($"❌ Request Error: {request.error}");
                warningText.text = "Failed to request points.";
            }
            else
            {
                string responseText = request.downloadHandler.text;
                Debug.Log($"✅ Request Response: {responseText}");

                try
                {
                    RequestPointPostResponse response = JsonConvert.DeserializeObject<RequestPointPostResponse>(responseText);
                    if (response != null && response.code.Equals("1000"))
                    {
                        warningText.text = "Point request submitted successfully!";
                        reasonInput.text = "";
                        pointInput.text = "";

                        // 🔥 Automatically navigate to the Request List Page
                        FindObjectOfType<RequestPointScreenLoader>().OpenRequestPointListFromRequestPage();
                    }
                    else
                    {
                        warningText.text = "Failed to request points.";
                    }
                }
                catch (Exception e)
                {
                    Debug.LogError($"❌ JSON Parsing Error: {e.Message}");
                    warningText.text = "Unexpected response from server!";
                }
            }
        }
    }


    public void OpenRequestPointPage()
    {
        Debug.Log("📌 Opening Request Point Page...");

        if (requestPointPage != null)
        {
            requestPointPage.SetActive(true);
        }
        else
        {
            Debug.LogWarning("⚠️ requestPointPage is missing! Assign it in the inspector.");
        }
    }

    public void BackToProfilePage()
    {
        Debug.Log("🔙 Returning to Profile Page...");

        if (requestPointPage != null)
        {
            requestPointPage.SetActive(false); // Hide RequestPointPage
        }

        if (profilePage != null)
        {
            profilePage.SetActive(true); // Show Profile Page
        }
        else
        {
            Debug.LogWarning("⚠️ ProfilePage reference is missing! Make sure to assign it.");
        }
    }
}

[Serializable]
public class RequestPointPostData
{
    public string reason;
    public int amount;
    public string companyId;
    public string employeeId;
}

[Serializable]
public class RequestPointPostResponse
{
    public string code;
    public string message;
}
