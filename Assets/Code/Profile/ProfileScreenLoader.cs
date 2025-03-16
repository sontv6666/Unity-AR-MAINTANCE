using UnityEngine;
using UnityEngine.Networking;
using TMPro;
using UnityEngine.UI;
using System.Collections;
using System;

public class ProfileScreenLoader : MonoBehaviour
{
    [Header("UI References")] 
    public TMP_Text nameText;
    public TMP_Text companyText;
    public TMP_Text emailText;
    public TMP_Text roleText;
    public TMP_Text phoneText;
    public UnityEngine.UI.Image avatarImage;
    
    public GameObject profilePage; // 🔹 Profile Page UI
    public GameObject loginPage;   // 🔹 Login Page UI
    public Button logoutButton;    // 🔹 Logout Button

    private string userId;
    private string authToken;

    void Start()
    {
        userId = PlayerPrefs.GetString("UserId", ""); 
        authToken = PlayerPrefs.GetString("AuthToken", "");
        if (!string.IsNullOrEmpty(userId) && !string.IsNullOrEmpty(authToken))
        {
            StartCoroutine(FetchUserProfile(userId));
        }
        else
        {
            Debug.LogError("❌ No User ID found! Cannot load profile.");
        }
        
        if (logoutButton != null)
        {
            logoutButton.onClick.AddListener(OnLogout);
        }
        
    }

    public void ReloadUserInfo()
    {
        if (!string.IsNullOrEmpty(userId))
        {
            StartCoroutine(FetchUserProfile(userId));
        }
    }

    private IEnumerator FetchUserProfile(string userId)
    {
        string endpoint = $"/user/{userId}";  // Ensure correct API route

        using (UnityWebRequest request = ApiConfig.CreateRequest(endpoint, "GET"))
        {
            // Ensure AuthToken is set before sending request
            request.SetRequestHeader("Authorization", "Bearer " + PlayerPrefs.GetString("AuthToken", ""));

            Debug.Log($"📡 Fetching user profile: {endpoint}");

            yield return request.SendWebRequest();

            if (request.result == UnityWebRequest.Result.Success)
            {
                string jsonResponse = request.downloadHandler.text;
                Debug.Log($"✅ API Response: {jsonResponse}");

                try
                {
                    UserProfileResponse response = JsonUtility.FromJson<UserProfileResponse>(jsonResponse);

                    if (response != null && response.code == 1000 && response.result != null)
                    {
                        UpdateUserInfo(response.result);
                    }
                    else
                    {
                        Debug.LogError("❌ Invalid API response: " + jsonResponse);
                    }
                }
                catch (Exception e)
                {
                    Debug.LogError($"❌ JSON Parsing Error: {e.Message}");
                }
            }
            else
            {
                Debug.LogError($"❌ API Request Failed: {request.error}");
            }
        }
    }



    void UpdateUserInfo(UserProfile user)
    {
        if (gameObject.activeInHierarchy)
        {
            nameText.text = user.username;
            companyText.text = user.company.companyName;
            emailText.text = user.email;
            roleText.text = user.roleName;
            phoneText.text = user.phone;

            StartCoroutine(LoadAvatarImage(user.avatar));
        }
    }

    IEnumerator LoadAvatarImage(string url)
    {
        UnityWebRequest request = UnityWebRequestTexture.GetTexture(url);
        yield return request.SendWebRequest();

        if (request.result == UnityWebRequest.Result.Success)
        {
            Texture2D texture = DownloadHandlerTexture.GetContent(request);
            avatarImage.sprite = Sprite.Create(texture, new Rect(0, 0, texture.width, texture.height),
                new Vector2(0.5f, 0.5f));
        }
        else
        {
            Debug.LogError("❌ Failed to load avatar image: " + request.error);
        }
        
    }
    
    
    // 🔹 LOGOUT FUNCTION
    public void OnLogout()
    {
        Debug.Log("🔹 Logging out...");

        // ✅ Clear stored user data
        PlayerPrefs.DeleteKey("UserId");
        PlayerPrefs.DeleteKey("AuthToken");
        PlayerPrefs.Save();

        // ✅ Return to Login Screen
        if (profilePage != null) profilePage.SetActive(false);
        if (loginPage != null) loginPage.SetActive(true);

        Debug.Log("✅ User logged out!");
    }

}

// ✅ Move these classes OUTSIDE of the ProfileScreenLoader class
[Serializable]
public class UserProfileResponse
{
    public int code;
    public UserProfile result;
}
