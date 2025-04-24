using System;
using System.Collections;
using System.Collections.Generic;
using Models;
using UnityEngine;
using UnityEngine.Networking;
using Newtonsoft.Json;

public class NotificationService : MonoBehaviour
{
    private static NotificationService _instance;
    public static NotificationService Instance
    {
        get
        {
            if (_instance == null)
            {
                GameObject go = new GameObject("NotificationService");
                _instance = go.AddComponent<NotificationService>();
                DontDestroyOnLoad(go);
            }
            return _instance;
        }
    }

    private void Awake()
    {
        if (_instance == null)
        {
            _instance = this;
            DontDestroyOnLoad(gameObject);
        }
        else if (_instance != this)
        {
            Destroy(gameObject);
        }
    }

    // API Endpoints
    private const string REGISTER_DEVICE = "/notifications/register";
    private const string SUBSCRIBE_TO_TOPIC = "/notifications/subscribe";
    private const string UNSUBSCRIBE_FROM_TOPIC = "/notifications/unsubscribe";
    private const string SEND_TO_TOPIC = "/notifications/send/topic";
    private const string SEND_TO_TOKEN = "/notifications/send/token";
    private const string UNREGISTER_DEVICE = "/notifications/unregister";
    private const string SEND_TO_USER = "/notifications/send/user";

    // Register device with backend
    public void RegisterDevice(string userId, string companyId, string token, Action<bool> callback = null)
    {
        if (string.IsNullOrEmpty(token))
        {
            Debug.LogError("❌ Cannot register device: Firebase token is empty");
            callback?.Invoke(false);
            return;
        }

        StartCoroutine(RegisterDeviceCoroutine(userId, companyId, token, callback));
    }

    private IEnumerator RegisterDeviceCoroutine(string userId, string companyId, string token, Action<bool> callback)
    {
        var registrationData = new DeviceRegistrationRequest
        {
            token = token,
            userId = userId,
            companyId = companyId
        };

        string jsonData = JsonConvert.SerializeObject(registrationData);
        UnityWebRequest request = ApiConfig.CreateRequest(REGISTER_DEVICE, "POST", jsonData);

        yield return request.SendWebRequest();

        bool success = request.result == UnityWebRequest.Result.Success;
        if (success)
        {
            Debug.Log("✅ Device registered successfully with backend");
            
            // Parse the response
            string jsonResponse = request.downloadHandler.text;
            var response = JsonConvert.DeserializeObject<ApiResponse<bool>>(jsonResponse);
            
            // Use the actual result from the response if available
            success = response?.result ?? success;
        }
        else
        {
            Debug.LogError($"❌ Failed to register device: {request.error}");
        }

        callback?.Invoke(success);
    }

    // Unregister device
    public void UnregisterDevice(string userId, string token, Action<bool> callback = null)
    {
        StartCoroutine(UnregisterDeviceCoroutine(userId, token, callback));
    }

    private IEnumerator UnregisterDeviceCoroutine(string userId, string token, Action<bool> callback)
    {
        string endpoint = $"{UNREGISTER_DEVICE}?userId={UnityWebRequest.EscapeURL(userId)}&token={UnityWebRequest.EscapeURL(token)}";
        UnityWebRequest request = ApiConfig.CreateRequest(endpoint, "POST");

        yield return request.SendWebRequest();

        bool success = request.result == UnityWebRequest.Result.Success;
        if (success)
        {
            Debug.Log("✅ Device unregistered successfully");
            
            // Parse the response
            string jsonResponse = request.downloadHandler.text;
            var response = JsonConvert.DeserializeObject<ApiResponse<bool>>(jsonResponse);
            
            // Use the actual result from the response if available
            success = response?.result ?? success;
        }
        else
        {
            Debug.LogError($"❌ Failed to unregister device: {request.error}");
        }

        callback?.Invoke(success);
    }

    // Subscribe to topic
    public void SubscribeToTopic(string token, string topic, Action<bool> callback = null)
    {
        StartCoroutine(SubscribeToTopicCoroutine(token, topic, callback));
    }

    private IEnumerator SubscribeToTopicCoroutine(string token, string topic, Action<bool> callback)
    {
        string endpoint = $"{SUBSCRIBE_TO_TOPIC}?token={UnityWebRequest.EscapeURL(token)}&topic={UnityWebRequest.EscapeURL(topic)}";
        UnityWebRequest request = ApiConfig.CreateRequest(endpoint, "POST");

        yield return request.SendWebRequest();

        bool success = request.result == UnityWebRequest.Result.Success;
        if (success)
        {
            Debug.Log($"✅ Successfully subscribed to topic: {topic}");
            
            // Parse the response
            string jsonResponse = request.downloadHandler.text;
            var response = JsonConvert.DeserializeObject<ApiResponse<bool>>(jsonResponse);
            
            // Use the actual result from the response if available
            success = response?.result ?? success;
        }
        else
        {
            Debug.LogError($"❌ Failed to subscribe to topic: {request.error}");
        }

        callback?.Invoke(success);
    }

    // Unsubscribe from topic
    public void UnsubscribeFromTopic(string token, string topic, Action<bool> callback = null)
    {
        StartCoroutine(UnsubscribeFromTopicCoroutine(token, topic, callback));
    }

    private IEnumerator UnsubscribeFromTopicCoroutine(string token, string topic, Action<bool> callback)
    {
        string endpoint = $"{UNSUBSCRIBE_FROM_TOPIC}?token={UnityWebRequest.EscapeURL(token)}&topic={UnityWebRequest.EscapeURL(topic)}";
        UnityWebRequest request = ApiConfig.CreateRequest(endpoint, "POST");

        yield return request.SendWebRequest();

        bool success = request.result == UnityWebRequest.Result.Success;
        if (success)
        {
            Debug.Log($"✅ Successfully unsubscribed from topic: {topic}");
            
            // Parse the response
            string jsonResponse = request.downloadHandler.text;
            var response = JsonConvert.DeserializeObject<ApiResponse<bool>>(jsonResponse);
            
            // Use the actual result from the response if available
            success = response?.result ?? success;
        }
        else
        {
            Debug.LogError($"❌ Failed to unsubscribe from topic: {request.error}");
        }

        callback?.Invoke(success);
    }

    // Send notification to topic
    public void SendNotificationToTopic(string topic, string title, string body, string data = null, Action<string> callback = null)
    {
        StartCoroutine(SendNotificationToTopicCoroutine(topic, title, body, data, callback));
    }

    private IEnumerator SendNotificationToTopicCoroutine(string topic, string title, string body, string data, Action<string> callback)
    {
        string endpoint = $"{SEND_TO_TOPIC}?topic={UnityWebRequest.EscapeURL(topic)}&title={UnityWebRequest.EscapeURL(title)}&body={UnityWebRequest.EscapeURL(body)}";
        
        if (!string.IsNullOrEmpty(data))
        {
            endpoint += $"&data={UnityWebRequest.EscapeURL(data)}";
        }
        
        UnityWebRequest request = ApiConfig.CreateRequest(endpoint, "POST");

        yield return request.SendWebRequest();

        string result = null;
        if (request.result == UnityWebRequest.Result.Success)
        {
            Debug.Log($"✅ Notification sent to topic: {topic}");
            
            // Parse the response
            string jsonResponse = request.downloadHandler.text;
            var response = JsonConvert.DeserializeObject<ApiResponse<string>>(jsonResponse);
            
            // Use the result from the response
            result = response?.result;
        }
        else
        {
            Debug.LogError($"❌ Failed to send notification to topic: {request.error}");
        }

        callback?.Invoke(result);
    }

    // Send notification to token
    public void SendNotificationToToken(string token, string title, string body, string data = null, Action<string> callback = null)
    {
        StartCoroutine(SendNotificationToTokenCoroutine(token, title, body, data, callback));
    }

    private IEnumerator SendNotificationToTokenCoroutine(string token, string title, string body, string data, Action<string> callback)
    {
        string endpoint = $"{SEND_TO_TOKEN}?token={UnityWebRequest.EscapeURL(token)}&title={UnityWebRequest.EscapeURL(title)}&body={UnityWebRequest.EscapeURL(body)}";
        
        if (!string.IsNullOrEmpty(data))
        {
            endpoint += $"&data={UnityWebRequest.EscapeURL(data)}";
        }
        
        UnityWebRequest request = ApiConfig.CreateRequest(endpoint, "POST");

        yield return request.SendWebRequest();

        string result = null;
        if (request.result == UnityWebRequest.Result.Success)
        {
            Debug.Log($"✅ Notification sent to token: {token}");
            
            // Parse the response
            string jsonResponse = request.downloadHandler.text;
            var response = JsonConvert.DeserializeObject<ApiResponse<string>>(jsonResponse);
            
            // Use the result from the response
            result = response?.result;
        }
        else
        {
            Debug.LogError($"❌ Failed to send notification to token: {request.error}");
        }

        callback?.Invoke(result);
    }

    // Send notification to user
    public void SendNotificationToUser(string userId, string title, string body, string data = null, Action<List<string>> callback = null)
    {
        StartCoroutine(SendNotificationToUserCoroutine(userId, title, body, data, callback));
    }

    private IEnumerator SendNotificationToUserCoroutine(string userId, string title, string body, string data, Action<List<string>> callback)
    {
        string endpoint = $"{SEND_TO_USER}?userId={UnityWebRequest.EscapeURL(userId)}&title={UnityWebRequest.EscapeURL(title)}&body={UnityWebRequest.EscapeURL(body)}";
        
        if (!string.IsNullOrEmpty(data))
        {
            endpoint += $"&data={UnityWebRequest.EscapeURL(data)}";
        }
        
        UnityWebRequest request = ApiConfig.CreateRequest(endpoint, "POST");

        yield return request.SendWebRequest();

        List<string> result = null;
        if (request.result == UnityWebRequest.Result.Success)
        {
            Debug.Log($"✅ Notification sent to user: {userId}");
            
            // Parse the response
            string jsonResponse = request.downloadHandler.text;
            var response = JsonConvert.DeserializeObject<ApiResponse<List<string>>>(jsonResponse);
            
            // Use the result from the response
            result = response?.result;
        }
        else
        {
            Debug.LogError($"❌ Failed to send notification to user: {request.error}");
        }

        callback?.Invoke(result);
    }
}


[Serializable]
public class DeviceRegistrationRequest
{
    public string token;
    public string userId;
    public string companyId;
}
