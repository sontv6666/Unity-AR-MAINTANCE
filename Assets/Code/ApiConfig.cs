using UnityEngine;
using UnityEngine.Networking;
using System.Collections;

public static class ApiConfig
{
    
    //https://joey-lenient-ostrich.ngrok-free.app
    //https://pure-wondrous-hippo.ngrok-free.app
    private static readonly string baseUrl = "https://joey-lenient-ostrich.ngrok-free.app/api/v1";
    // private static readonly string baseUrl = "http://localhost:8086/api/v1";
        

    public static string GetBaseUrl()
    {
        return baseUrl;
    }

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