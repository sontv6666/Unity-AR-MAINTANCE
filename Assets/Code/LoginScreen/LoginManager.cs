using UnityEngine;
using UnityEngine.Networking;
using TMPro;
using System.Collections;
using System.Text;
using System;

public class LoginManager : MonoBehaviour
{
    public TMP_InputField usernameInput;
    public TMP_InputField passwordInput;
    public TMP_Text warningText;

    public GameObject loginCanvas;
    public GameObject homeCanvas;
    public GameObject profileCanvas;

    private const string LOGIN_URL = "https://joey-lenient-ostrich.ngrok-free.app/api/v1/login";
    private const string UPDATE_USER_URL = "/users/update"; // Endpoint for updating user details

    public static string UserId;
    private string deviceId;

    void Start()
    {
        deviceId = SystemInfo.deviceUniqueIdentifier; // Get device ID
        Debug.Log($"📱 Device ID: {deviceId}");
    }

    public void OnLogin()
    {
        string username = usernameInput.text;
        string password = passwordInput.text;
        StartCoroutine(LoginRequestAPI(username, password));
    }

    private IEnumerator LoginRequestAPI(string email, string password)
    {
        string jsonBody = JsonUtility.ToJson(new LoginRequest { email = email, password = password });

        using (UnityWebRequest request = UnityWebRequest.PostWwwForm(LOGIN_URL, ""))
        {
            byte[] jsonToSend = Encoding.UTF8.GetBytes(jsonBody);
            request.uploadHandler = new UploadHandlerRaw(jsonToSend);
            request.downloadHandler = new DownloadHandlerBuffer();
            request.SetRequestHeader("Content-Type", "application/json");

            yield return request.SendWebRequest();

            if (request.result == UnityWebRequest.Result.Success)
            {
                Debug.Log("✅ Login Response: " + request.downloadHandler.text);
                LoginResponse response = JsonUtility.FromJson<LoginResponse>(request.downloadHandler.text);

                if (response.code == 1000 && response.result.message == "Login successfully" &&
                    response.result.token != null)
                {
                    UserProfile user = response.result.user;

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

                            if (string.IsNullOrEmpty(user.deviceId))
                            {
                                StartCoroutine(UpdateUserDeviceId(user.id));
                            }
                            else
                            {
                                SwitchToHomePage();
                            }
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
                    Debug.LogWarning("❌ Login failed: " + response.result.message);
                }
            }
            else
            {
                Debug.LogError("❌ Error: " + request.error);
                warningText.text = "⚠️ Unable to connect to the server!";
            }
        }
    }

    private void SaveUserData(string token, UserProfile user)
    {
        // ✅ Save token and user data in PlayerPrefs
        PlayerPrefs.SetString("AuthToken", token);  // Store Token
        PlayerPrefs.SetString("UserId", user.id);
        PlayerPrefs.SetString("RoleName", user.roleName);
        PlayerPrefs.SetString("CompanyName", user.company.companyName);
        PlayerPrefs.SetString("Email", user.email);
        PlayerPrefs.SetString("Username", user.username);
        PlayerPrefs.SetString("Phone", user.phone);
        PlayerPrefs.SetString("Status", user.status);
    
        PlayerPrefs.Save();  // ✅ Ensure data is saved!

        // ✅ Debugging
        Debug.Log($"🔐 Token Saved: {token}");
        Debug.Log($"👤 User ID Saved: {user.id}");

        // ✅ Also store in UserManager if needed
        UserManager.Token = token;
        UserManager.UserId = user.id;
        UserManager.RoleName = user.roleName;
        UserManager.CompanyName = user.company.companyName;
    }


    private IEnumerator UpdateUserDeviceId(string userId)
    {
        Debug.Log($"🔄 Updating Device ID for User: {userId}");

        string jsonBody = JsonUtility.ToJson(new UpdateUserDeviceRequest
        {
            id = userId,
            deviceId = deviceId
        });

        UnityWebRequest request = ApiConfig.CreateRequest(UPDATE_USER_URL, "POST", jsonBody);

        yield return request.SendWebRequest();

        if (request.result == UnityWebRequest.Result.Success)
        {
            Debug.Log("✅ Device ID updated successfully!");
            SwitchToHomePage();
        }
        else
        {
            Debug.LogError("❌ Failed to update device ID: " + request.error);
        }
    }

    private void SwitchToHomePage()
    {
        loginCanvas.SetActive(false);
        homeCanvas.SetActive(true);
        profileCanvas.SetActive(true);
    }
}

// ✅ Move these classes OUTSIDE of LoginManager

[Serializable]
public class LoginRequest
{
    public string email;
    public string password;
}

[Serializable]
public class LoginResponse
{
    public int code;
    public string message;
    public ResultData result;
}

[Serializable]
public class ResultData
{
    public string token;
    public string message;
    public UserProfile user;
}
