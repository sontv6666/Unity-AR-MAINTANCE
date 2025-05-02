using UnityEngine;
using UnityEngine.Networking;
using TMPro;
using System.Collections;
using System.Text;
using System;
using Code;
using Newtonsoft.Json;
using Models;
using UnityEngine.UI;

public class LoginManager : MonoBehaviour
{
    [Header("UI References")]
    public TMP_InputField usernameInput;
    public TMP_InputField passwordInput;
    public TMP_Text warningText;

    public GameObject loginCanvas;
    public GameObject homeCanvas;
    public GameObject profileCanvas;

    // Thêm Circular Progress Spinner và Overlay
    public GameObject loadingSpinner;  // Spinner Circular
    public GameObject overlay;         // Màn hình mờ
    
    // Internet connection UI elements
    public GameObject noInternetPanel;  // Panel showing no internet message
    public Button retryButton;          // Button to retry connection

    [Header("API Settings")]
    private string loginEndpoint = "/login";
    private string updateDeviceEndpoint = "/users/update";
    public static string UserId;
    private string deviceId;

    void Start()
    {
        // Get a cross-platform compatible device ID instead of using SystemInfo.deviceUniqueIdentifier directly
        deviceId = GetDeviceIdentifier();
        Debug.Log($"📱 Device ID: {deviceId}");
        
        // Ẩn Spinner và Overlay khi bắt đầu
        loadingSpinner.SetActive(false);
        overlay.SetActive(false);
        
        // Hide no internet panel at start
        if (noInternetPanel != null)
            noInternetPanel.SetActive(false);
        
        // Add listener to retry button if it exists
        if (retryButton != null)
            retryButton.onClick.AddListener(RetryConnection);
    }
    
    private string GetDeviceIdentifier()
    {
        // Try to load existing ID first
        string savedDeviceId = PlayerPrefs.GetString("UniqueDeviceId", "");
    
        if (!string.IsNullOrEmpty(savedDeviceId))
        {
            Debug.Log("✅ Using existing device ID from PlayerPrefs");
            return savedDeviceId;
        }
    
        // If no saved ID exists, generate one based on platform
        string newDeviceId = "";
    
#if UNITY_IOS
        // For iOS 14+, avoid using vendorIdentifier as it changes on reinstall
        // Instead, generate a persistent GUID and store it in PlayerPrefs
        newDeviceId = Guid.NewGuid().ToString();
        Debug.Log("🍏 iOS detected: Generated new GUID for device ID due to iOS privacy restrictions");
#else
        // For Android and other platforms, use deviceUniqueIdentifier
        newDeviceId = SystemInfo.deviceUniqueIdentifier;
        Debug.Log("📱 Non-iOS platform: Using SystemInfo.deviceUniqueIdentifier");
#endif
    
        // If we still don't have an ID, generate a random GUID as last resort
        if (string.IsNullOrEmpty(newDeviceId))
        {
            newDeviceId = Guid.NewGuid().ToString();
            Debug.Log("⚠️ Generated fallback random GUID as device ID");
        }
    
        // Save the ID for future use
        PlayerPrefs.SetString("UniqueDeviceId", newDeviceId);
        PlayerPrefs.Save();
    
        return newDeviceId;
    }

    public void OnLogin()
    {
        string username = usernameInput.text.Trim();
        string password = passwordInput.text.Trim();

        if (string.IsNullOrEmpty(username) || string.IsNullOrEmpty(password))
        {
            warningText.text = "Please enter both email and password!";
            return;
        }

        // Check internet connection before proceeding
        StartCoroutine(CheckInternetConnection((isConnected) => {
            if (isConnected)
            {
                string requestBody = JsonConvert.SerializeObject(new LoginRequest { email = username, password = password });
                
                // Hiển thị Spinner và Overlay khi gọi API
                loadingSpinner.SetActive(true);
                overlay.SetActive(true);

                StartCoroutine(SendLoginRequest(requestBody));
            }
            else
            {
                // Show no internet message
                ShowNoInternetMessage();
            }
        }));
    }
    
    private IEnumerator CheckInternetConnection(Action<bool> callback)
    {
        // Use Unity's ping to check connection
        UnityWebRequest request = new UnityWebRequest("https://google.com");
        request.timeout = 5; // 5 second timeout
        yield return request.SendWebRequest();
        
        bool isConnected = request.result != UnityWebRequest.Result.ConnectionError && 
                          request.result != UnityWebRequest.Result.DataProcessingError;
        
        callback(isConnected);
    }

    private void ShowNoInternetMessage()
    {
        // Hide spinner and overlay
        loadingSpinner.SetActive(false);
        overlay.SetActive(false);

        // Show no internet panel
        if (noInternetPanel != null)
        {
            noInternetPanel.SetActive(true);
            overlay.SetActive(true);
        }
        else
        {
            warningText.text = "No internet connection. Please check your network and try again.";
        }
            
        Debug.LogWarning("❌ Login failed: No internet connection");
    }
    
    private void RetryConnection()
    {
        // Hide no internet panel
        if (noInternetPanel != null)
        {
            noInternetPanel.SetActive(false);
            overlay.SetActive(false);
        }
        
        // Try login again
        OnLogin();
    }

    private IEnumerator SendLoginRequest(string jsonBody)
    {
        Debug.Log("🔄 Sending login request...");
        using (UnityWebRequest request = ApiConfig.CreateRequest(loginEndpoint, "POST", jsonBody))
        {
            yield return request.SendWebRequest();

            // Ẩn Spinner và Overlay khi hoàn tất API call
            loadingSpinner.SetActive(false);
            overlay.SetActive(false);

            if (request.result == UnityWebRequest.Result.ConnectionError)
            {
                Debug.LogError($"❌ Connection Error: {request.error}");
                ShowNoInternetMessage();
            }
            else if (request.result != UnityWebRequest.Result.Success)
            {
                Debug.LogError($"❌ Login Error: {request.error}");
                warningText.text = "Email or password is incorrect.";
            }
            else
            {
                string responseText = request.downloadHandler.text;
                Debug.Log($"✅ Login Response: {responseText}");

                try
                {
                    LoginResponse response = JsonConvert.DeserializeObject<LoginResponse>(responseText);
                    ProcessLoginResponse(response);
                }
                catch (Exception e)
                {
                    Debug.LogError($"❌ JSON Parsing Error: {e.Message}");
                    warningText.text = "Unexpected response from server!";
                }
            }
        }
    }

    private void ProcessLoginResponse(LoginResponse response)
    {
        Debug.Log($"🔍 Checking API Response: Code={response.code}, Message={response.result.message}");

        if (response.code == 1000 && response.result.message == "Login successfully" && response.result.token != null)
        {
            UserProfileResult user = response.result.user;

            if (user.roleName == "STAFF" && user.status == "ACTIVE")
            {
                SaveUserData(response.result.token, user);
                PlayerPrefs.Save();
                SwitchToHomePage();
            }
            else
            {
                warningText.text = "Only STAFF with ACTIVE status can log in.";
                Debug.LogWarning("❌ User is not STAFF or not ACTIVE.");
            }
        }
        else
        {
            warningText.text = "Login failed: " + response.result.message;
            Debug.LogWarning($"❌ Login failed: {response.result.message}");
        }
    }

    private void SaveUserData(string token, UserProfileResult user)
    {
        PlayerPrefs.SetString("AuthToken", token);
        PlayerPrefs.SetString("UserId", user.id);
        PlayerPrefs.SetString("RoleName", user.roleName);
        PlayerPrefs.SetString("CompanyName", user.company.companyName);
        PlayerPrefs.SetString("CompanyId", user.company.id);
        PlayerPrefs.SetString("Email", user.email);
        PlayerPrefs.SetString("Username", user.username);
        PlayerPrefs.SetString("Phone", user.phone);
        PlayerPrefs.SetString("Status", user.status);
        PlayerPrefs.SetString("DeviceId", deviceId); 
        PlayerPrefs.SetInt("Point", user.points); 
        PlayerPrefs.Save();

        Debug.Log($"🔐 Token Saved: {token}");
        Debug.Log($"👤 User ID Saved: {user.id}");

        UserManager.Token = token;
        UserManager.UserId = user.id;
        UserManager.RoleName = user.roleName;
        UserManager.CompanyName = user.company.companyName;
        
        if (MaintenanceNotificationManager.Instance != null)
        {
            Debug.Log("🔔 Registering with notification system...");
            MaintenanceNotificationManager.Instance.RegisterDeviceWithBackend(user.id, user.company.id);
        }
        else
        {
            Debug.LogWarning("⚠️ MaintenanceNotificationManager instance not found!");
        }
    }

    private void SwitchToHomePage()
    {
        Debug.Log("🔄 Switching UI: Hiding Login, Showing Home & Profile...");

        loginCanvas.SetActive(false);
        homeCanvas.SetActive(true);
        profileCanvas.SetActive(true);
    
        // Trigger event for other components to refresh
        EventManager.TriggerEvent("UserLoggedIn");

        Debug.Log($"✅ UI State - loginCanvas: {loginCanvas.activeSelf}, homeCanvas: {homeCanvas.activeSelf}, profileCanvas: {profileCanvas.activeSelf}");
    
        // Find and reload CourseLoader if it exists
        CourseLoader courseLoader = FindObjectOfType<CourseLoader>();
        if (courseLoader != null)
        {
            Debug.Log("🔄 Refreshing CourseLoader for new user");
            courseLoader.ReloadForNewUser();
            
          
            }
    }
}