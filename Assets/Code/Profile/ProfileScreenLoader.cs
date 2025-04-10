using UnityEngine;
using UnityEngine.Networking;
using TMPro;
using UnityEngine.UI;
using System.Collections;
using System;
using Models;
public class ProfileScreenLoader : MonoBehaviour
{
    [Header("UI References")] 
    public TMP_Text nameText;
    public TMP_Text companyText;
    public TMP_Text emailText;
    public TMP_Text roleText;
    public TMP_Text pointsText;
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
    string endpoint = $"/user/{userId}";
    UnityWebRequest request = ApiConfig.CreateRequest(endpoint, "GET");
    request.SetRequestHeader("Authorization", "Bearer " + PlayerPrefs.GetString("AuthToken", ""));

    Debug.Log($"📡 Fetching user profile: {endpoint}");

    yield return request.SendWebRequest();

    if (request.result == UnityWebRequest.Result.Success)
    {
        string jsonResponse = request.downloadHandler.text;
        Debug.Log($"✅ API Response: {jsonResponse}");

        try
        {
            // ✅ Debug before deserializing
            if (string.IsNullOrEmpty(jsonResponse))
            {
                Debug.LogError("❌ Empty JSON response!");
                yield break;
            }

            ApiResponse<UserProfileResult> response = JsonUtility.FromJson<ApiResponse<UserProfileResult>>(jsonResponse);

            if (response != null && response.code == 1000)
            {
                if (response.result == null)
                {
                    Debug.LogError("❌ API returned NULL result object!");
                }
                else
                {
                    UpdateUserInfo(response.result);
                }
            }
            else
            {
                Debug.LogError($"❌ Invalid API response: {jsonResponse}");
            }
        }
        catch (Exception e)
        {
            Debug.LogError($"❌ JSON Parsing Error: {e.Message}\n{e.StackTrace}");
        }
    }
    else
    {
        Debug.LogError($"❌ API Request Failed: {request.error}");
    }

    request.Dispose(); // ✅ Dispose request to prevent memory leaks
}





   void UpdateUserInfo(UserProfileResult user)
{
    if (user == null)
    {
        Debug.LogError("❌ UserProfileResult is NULL!");
        return;
    }

    if (gameObject.activeInHierarchy)
    {
        // Check each field before assigning
        nameText.text = !string.IsNullOrEmpty(user.username) ? user.username : "N/A";
        companyText.text = (user.company != null && !string.IsNullOrEmpty(user.company.companyName)) ? user.company.companyName : "N/A";
        pointsText.text = $"{user.points} points";

        Debug.Log($"✅ Loaded User: {user.username} | Company: {companyText.text}");

        if (!string.IsNullOrEmpty(user.avatar))
        {
            StartCoroutine(LoadAvatarImage(user.avatar));
        }
        else
        {
            Debug.LogWarning("⚠️ No avatar URL provided.");
        }
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
        PlayerPrefs.DeleteKey("CompanyId");
        PlayerPrefs.DeleteKey("ShowHomePage");
        PlayerPrefs.DeleteKey("ShowDetailPage");
        PlayerPrefs.DeleteKey("SelectedCourseID");
        
        // Reset any cached data in static managers
        UserManager.UserId = "";
        UserManager.CompanyId = "";
        CourseManager.SelectedCourseId = "";
        
        // Clear UI elements
        if (nameText != null) nameText.text = "";
        if (companyText != null) companyText.text = "";
        if (emailText != null) emailText.text = "";
        if (roleText != null) roleText.text = "";
        if (pointsText != null) pointsText.text = "";
        PlayerPrefs.Save();

        // ✅ Return to Login Screen
        if (profilePage != null) profilePage.SetActive(false);
        if (loginPage != null) loginPage.SetActive(true);

        Debug.Log("✅ User logged out!");
    }

}

