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

    [Header("API Settings")]
    private string loginEndpoint = "/login";
    private string updateDeviceEndpoint = "/users/update";
    public static string UserId;
    private string deviceId;

    void Start()
    {
        deviceId = SystemInfo.deviceUniqueIdentifier; // Get device ID
        Debug.Log($"📱 Device ID: {deviceId}");
    }

    public void OnLogin()
    {
        string username = usernameInput.text.Trim();
        string password = passwordInput.text.Trim();

        if (string.IsNullOrEmpty(username) || string.IsNullOrEmpty(password))
        {
            warningText.text = "⚠️ Please enter both email and password!";
            return;
        }

        string requestBody = JsonConvert.SerializeObject(new LoginRequest { email = username, password = password });
        StartCoroutine(SendLoginRequest(requestBody));
    }

    
    private IEnumerator SendLoginRequest(string jsonBody)
    {
        Debug.Log("🔄 Sending login request...");
        using (UnityWebRequest request = ApiConfig.CreateRequest(loginEndpoint, "POST", jsonBody))
        {
            yield return request.SendWebRequest();

            if (request.result != UnityWebRequest.Result.Success)
            {
                Debug.LogError($"❌ Login Error: {request.error}");
                warningText.text = "⚠️ Unable to connect to the server!";
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
                    warningText.text = "⚠️ Unexpected response from server!";
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
                    warningText.text = "⚠️ Your account is linked to another device!";
                    Debug.LogWarning("❌ Device ID mismatch! User is already linked to another device.");
                }
                else
                {
                    SaveUserData(response.result.token, user);
					SwitchToHomePage();

                    //if (string.IsNullOrEmpty(user.deviceId))
                    //{
                    //    string updateRequestBody = JsonConvert.SerializeObject(new UpdateUserDeviceRequest { id = user.id, deviceId = deviceId });
                   //     StartCoroutine(UpdateUserDeviceId(updateRequestBody));
                  //  }
                  //  else
                  //  {
                   //     Debug.Log("✅ Switching to Home Page...");   
                  //  }
                }
            }
            else
            {
                warningText.text = "⚠️ Only STAFF with ACTIVE status can log in.";
                Debug.LogWarning("❌ User is not STAFF or not ACTIVE.");
            }
        }
        else
        {
            warningText.text = "⚠️ Login failed: " + response.result.message;
            Debug.LogWarning($"❌ Login failed: {response.result.message}");
        }
    }

    
    
  


    private IEnumerator UpdateUserDeviceId(string jsonBody)
    {
        Debug.Log($"🔄 Updating Device ID...");
        using (UnityWebRequest request = ApiConfig.CreateRequest(updateDeviceEndpoint, "POST", jsonBody))
        {
            yield return request.SendWebRequest();

            if (request.result == UnityWebRequest.Result.Success)
            {
                Debug.Log("✅ Device ID updated successfully!");
                SwitchToHomePage();
            }
            else
            {
                Debug.LogError("❌ Failed to update device ID: " + request.error);
                SwitchToHomePage();
            }
        }
    }
    
    
    private void SaveUserData(string token, UserProfileResult user)
    {
        PlayerPrefs.SetString("AuthToken", token);
        PlayerPrefs.SetString("UserId", user.id);
        PlayerPrefs.SetString("RoleName", user.roleName);
        PlayerPrefs.SetString("CompanyName", user.company.companyName);
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

