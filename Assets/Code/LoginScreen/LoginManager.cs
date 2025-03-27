using UnityEngine;
using UnityEngine.Networking;
using TMPro;
using System.Collections;
using System.Text;
using System;
using Newtonsoft.Json;
using Models;

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

    [Header("API Settings")]
    private string loginEndpoint = "/login";
    private string updateDeviceEndpoint = "/users/update";
    public static string UserId;
    private string deviceId;

    void Start()
    {
        deviceId = SystemInfo.deviceUniqueIdentifier; // Get device ID
        Debug.Log($"📱 Device ID: {deviceId}");
        // Ẩn Spinner và Overlay khi bắt đầu
        loadingSpinner.SetActive(false);
        overlay.SetActive(false);
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

        string requestBody = JsonConvert.SerializeObject(new LoginRequest { email = username, password = password });
        
        // Hiển thị Spinner và Overlay khi gọi API
        loadingSpinner.SetActive(true);
        overlay.SetActive(true);

        StartCoroutine(SendLoginRequest(requestBody));
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

            if (request.result != UnityWebRequest.Result.Success)
            {
                Debug.LogError($"❌ Login Error: {request.error}");
                warningText.text = "Email or passsword is incorrect.";
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
                if (!string.IsNullOrEmpty(user.deviceId) && user.deviceId != deviceId)
                {
                    warningText.text = "Your account is linked to another device!";
                    Debug.LogWarning("❌ Device ID mismatch! User is already linked to another device.");
                }
                else
                {
                    SaveUserData(response.result.token, user);
                    PlayerPrefs.Save();
                    SwitchToHomePage();
                }
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
        PlayerPrefs.Save();

        Debug.Log($"🔐 Token Saved: {token}");
        Debug.Log($"👤 User ID Saved: {user.id}");

        UserManager.Token = token;
        UserManager.UserId = user.id;
        UserManager.RoleName = user.roleName;
        UserManager.CompanyName = user.company.companyName;
    }

    private void SwitchToHomePage()
    {
        Debug.Log("🔄 Switching UI: Hiding Login, Showing Home & Profile...");
    
        loginCanvas.SetActive(false);
        homeCanvas.SetActive(true);
        profileCanvas.SetActive(true);

        Debug.Log($"✅ UI State - loginCanvas: {loginCanvas.activeSelf}, homeCanvas: {homeCanvas.activeSelf}, profileCanvas: {profileCanvas.activeSelf}");
    }
}
