using UnityEngine;
using UnityEngine.Networking;
using System.Collections;

public static class ApiConfig
{
    private static readonly string baseUrl = "https://capital-earwig-vertically.ngrok-free.app/api/v1";
    
    public static UnityWebRequest CreateRequest(string endpoint, string method = "GET", string jsonBody = null)
    {
        string url = baseUrl + endpoint;
        UnityWebRequest request;

        if (method == "POST" || method == "PUT")
        {
            request = new UnityWebRequest(url, method);
            byte[] bodyRaw = jsonBody != null ? System.Text.Encoding.UTF8.GetBytes(jsonBody) : null;
            request.uploadHandler = new UploadHandlerRaw(bodyRaw);
            request.downloadHandler = new DownloadHandlerBuffer();
            request.SetRequestHeader("Content-Type", "application/json");
        }
        else
        {
            request = UnityWebRequest.Get(url);
        }

        // Add Authorization header if token exists
        if (!string.IsNullOrEmpty(UserManager.Token))
        {
            request.SetRequestHeader("Authorization", "Bearer " + UserManager.Token);
        }

        return request;
    }
}