using UnityEngine;
using UnityEngine.Networking;
using System.Collections;
using System;

// This is a utility class that wraps API calls with network checking functionality
public class NetworkAwareAPIHandler : MonoBehaviour
{
    // Singleton pattern
    private static NetworkAwareAPIHandler _instance;
    public static NetworkAwareAPIHandler Instance 
    {
        get 
        {
            if (_instance == null)
            {
                GameObject go = new GameObject("NetworkAwareAPIHandler");
                _instance = go.AddComponent<NetworkAwareAPIHandler>();
                DontDestroyOnLoad(go);
            }
            return _instance;
        }
    }

    private void Awake()
    {
        if (_instance != null && _instance != this)
        {
            Destroy(gameObject);
            return;
        }
        
        _instance = this;
        DontDestroyOnLoad(gameObject);
        
        // Listen for network status changes from ScreenManager 
        ScreenManager.OnConnectionStatusChanged += HandleConnectionStatusChanged;
    }
    
    private void OnDestroy()
    {
        // Unsubscribe when destroyed
        ScreenManager.OnConnectionStatusChanged -= HandleConnectionStatusChanged;
    }
    
    private void HandleConnectionStatusChanged(bool isConnected)
    {
        if (isConnected)
        {
            // Network restored - notify any listeners
            OnNetworkRestored?.Invoke();
        }
        else
        {
            // Network lost - notify any listeners
            OnNetworkLost?.Invoke();
        }
    }
    
    // Events that other scripts can subscribe to
    public event Action OnNetworkLost;
    public event Action OnNetworkRestored;
    
    // Coroutine that handles API calls with network awareness
    public IEnumerator SendAPIRequest(UnityWebRequest request, Action<UnityWebRequest> onSuccess, Action<string> onFailure)
    {
        bool isNetworkError = false;
        
        yield return request.SendWebRequest();
        
        if (request.result == UnityWebRequest.Result.ConnectionError)
        {
            Debug.LogWarning($"❌ Network error during API call: {request.error}");
            isNetworkError = true;
            onFailure?.Invoke("No internet connection. Please check your network.");
            
            // Find the ScreenManager and show no internet message
            ScreenManager screenManager = FindObjectOfType<ScreenManager>();
            if (screenManager != null)
            {
                // Use reflection to access private method if necessary
                var method = screenManager.GetType().GetMethod("ShowNoInternetMessage", 
                    System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
                if (method != null)
                {
                    method.Invoke(screenManager, null);
                }
            }
        }
        else if (request.result != UnityWebRequest.Result.Success)
        {
            Debug.LogError($"❌ API Error: {request.error}");
            onFailure?.Invoke($"Error: {request.error}");
        }
        else
        {
            onSuccess?.Invoke(request);
        }
    }

}