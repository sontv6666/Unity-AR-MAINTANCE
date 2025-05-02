using UnityEngine;
using UnityEngine.Networking;
using TMPro;
using UnityEngine.UI;
using System.Collections;
using System;
using Code;
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

    // Default avatar sprite (optional)
    public Sprite defaultAvatarSprite;

    private string userId;
    private string authToken;
    
    // Flag to track if we need to retry loading when network is restored
    private bool needsProfileReload = false;

    void Start()
    {
        // Initialize UI components with default values
        ResetUIComponents();
        
        // Set up event listeners
        EventManager.StartListening("UserLoggedIn", OnUserLoggedIn);
        EventManager.StartListening("UserLoggedOut", OnUserLoggedOut);
        
        // Add listener to logout button
        if (logoutButton != null)
        {
            logoutButton.onClick.AddListener(OnLogout);
        }
        
        // Subscribe to network events
        NetworkAwareAPIHandler.Instance.OnNetworkRestored += HandleNetworkRestored;
        
        // Check if user is already logged in
        CheckForExistingLogin();
    }

    void OnDestroy()
    {
        // Clean up event listeners when component is destroyed
        EventManager.StopListening("UserLoggedIn", OnUserLoggedIn);
        EventManager.StopListening("UserLoggedOut", OnUserLoggedOut);
        
        // Unsubscribe from network events to prevent memory leaks
        if (NetworkAwareAPIHandler.Instance != null)
        {
            NetworkAwareAPIHandler.Instance.OnNetworkRestored -= HandleNetworkRestored;
        }
    }
    
    private void HandleNetworkRestored()
    {
        if (needsProfileReload && !string.IsNullOrEmpty(userId))
        {
            Debug.Log("🔄 Network restored - reloading user profile");
            needsProfileReload = false;
            StartCoroutine(FetchUserProfile(userId));
        }
    }
    
    private void CheckForExistingLogin()
    {
        userId = PlayerPrefs.GetString("UserId", ""); 
        authToken = PlayerPrefs.GetString("AuthToken", "");
        
        if (!string.IsNullOrEmpty(userId) && !string.IsNullOrEmpty(authToken))
        {
            StartCoroutine(FetchUserProfile(userId));
        }
        else
        {
            Debug.LogWarning("⚠️ No User ID found! Cannot load profile.");
            // Make sure login page is visible and profile page is hidden
            if (loginPage != null) loginPage.SetActive(true);
            if (profilePage != null) profilePage.SetActive(false);
        }
    }

    private void OnUserLoggedIn()
    {
        Debug.Log("🔔 ProfileScreenLoader: User logged in event received");
        
        // Refresh user ID and token from PlayerPrefs
        userId = PlayerPrefs.GetString("UserId", "");
        authToken = PlayerPrefs.GetString("AuthToken", "");
        
        if (!string.IsNullOrEmpty(userId) && !string.IsNullOrEmpty(authToken))
        {
            // Force reload profile data
            ResetUIComponents(); // Clear previous user data first
            StartCoroutine(FetchUserProfile(userId));
        }
    }

    private void OnUserLoggedOut()
    {
        Debug.Log("🔔 ProfileScreenLoader: User logged out event received");
        
        // Reset UI components when user logs out
        ResetUIComponents();
        
        // Clear cached user data
        userId = "";
        authToken = "";
        needsProfileReload = false; // Reset the reload flag since we're logged out
    }

    public void ReloadUserInfo()
    {
        userId = PlayerPrefs.GetString("UserId", ""); // Get fresh ID from PlayerPrefs
        if (!string.IsNullOrEmpty(userId))
        {
            StartCoroutine(FetchUserProfile(userId));
        }
        else
        {
            Debug.LogWarning("⚠️ Cannot reload user info - no user ID found");
        }
    }

    private IEnumerator FetchUserProfile(string userId)
    {
        string endpoint = $"/user/{userId}";
        UnityWebRequest request = ApiConfig.CreateRequest(endpoint, "GET");
        request.SetRequestHeader("Authorization", "Bearer " + PlayerPrefs.GetString("AuthToken", ""));

        Debug.Log($"📡 Fetching user profile: {endpoint} for userId: {userId}");

        // Use NetworkAwareAPIHandler instead of direct web request
        yield return NetworkAwareAPIHandler.Instance.SendAPIRequest(
            request,
            OnProfileRequestSuccess,
            OnProfileRequestFailure
        );
    }
    
    private void OnProfileRequestSuccess(UnityWebRequest request)
    {
        string jsonResponse = request.downloadHandler.text;
        Debug.Log($"✅ API Response: {jsonResponse}");
        needsProfileReload = false; // Clear the reload flag since we've succeeded
        
        try
        {
            // ✅ Debug before deserializing
            if (string.IsNullOrEmpty(jsonResponse))
            {
                Debug.LogError("❌ Empty JSON response!");
                return;
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
        
        request.Dispose(); // ✅ Dispose request to prevent memory leaks
    }
    
    private void OnProfileRequestFailure(string errorMessage)
    {
        Debug.LogError($"❌ Profile request failed: {errorMessage}");
        
        if (errorMessage.Contains("connection") || errorMessage.Contains("network"))
        {
            // This is likely a network error, set flag to reload when network is restored
            needsProfileReload = true;
        }
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
            
            // Update company text
            if (user.company != null && !string.IsNullOrEmpty(user.company.companyName))
            {
                companyText.text = user.company.companyName;
            }
            else
            {
                companyText.text = "N/A";
            }
            
            // Update email text if the field exists
            if (emailText != null)
            {
                emailText.text = !string.IsNullOrEmpty(user.email) ? user.email : "N/A";
            }
            
            // Update role text if the field exists
            if (roleText != null)
            {
                roleText.text = !string.IsNullOrEmpty(user.roleName) ? user.roleName : "N/A";
            }
            
            pointsText.text = $"{user.points} points";

            Debug.Log($"✅ Loaded User: {user.username} | Company: {companyText.text} | Email: {(emailText != null ? emailText.text : "N/A")}");

            if (!string.IsNullOrEmpty(user.avatar))
            {
                StartCoroutine(LoadAvatarImage(user.avatar));
            }
            else
            {
                Debug.LogWarning("⚠️ No avatar URL provided.");
                // Set default avatar if available
                if (defaultAvatarSprite != null && avatarImage != null)
                {
                    avatarImage.sprite = defaultAvatarSprite;
                }
            }
        }
    }

    IEnumerator LoadAvatarImage(string url)
    {
        UnityWebRequest request = UnityWebRequestTexture.GetTexture(url);
        
        // Use NetworkAwareAPIHandler for avatar loading too
        yield return NetworkAwareAPIHandler.Instance.SendAPIRequest(
            request,
            OnAvatarLoadSuccess,
            OnAvatarLoadFailure
        );
    }
    
    private void OnAvatarLoadSuccess(UnityWebRequest request)
    {
        if (avatarImage != null) // Check if the component still exists
        {
            Texture2D texture = DownloadHandlerTexture.GetContent(request);
            avatarImage.sprite = Sprite.Create(texture, new Rect(0, 0, texture.width, texture.height),
                new Vector2(0.5f, 0.5f));
        }
        request.Dispose(); // Dispose request to prevent memory leaks
    }
    
    private void OnAvatarLoadFailure(string errorMessage)
    {
        Debug.LogError("❌ Failed to load avatar image: " + errorMessage);
        // Set default avatar if available
        if (defaultAvatarSprite != null && avatarImage != null)
        {
            avatarImage.sprite = defaultAvatarSprite;
        }
    }
    
    private void ResetUIComponents()
    {
        // Reset all UI fields to empty or default values
        if (nameText != null) nameText.text = "";
        if (companyText != null) companyText.text = "";
        if (emailText != null) emailText.text = "";
        if (roleText != null) roleText.text = "";
        if (pointsText != null) pointsText.text = "0 points";
        
        // Reset avatar to default if available
        if (avatarImage != null && defaultAvatarSprite != null)
        {
            avatarImage.sprite = defaultAvatarSprite;
        }
        
        Debug.Log("🧹 UI components have been reset");
    }
    
    // 🔹 LOGOUT FUNCTION
    public void OnLogout()
    {
        Debug.Log("🔹 Logging out...");

        // Clear stored user data
        PlayerPrefs.DeleteKey("UserId");
        PlayerPrefs.DeleteKey("AuthToken");
        PlayerPrefs.DeleteKey("CompanyId");
        PlayerPrefs.DeleteKey("ShowHomePage");
        PlayerPrefs.DeleteKey("ShowDetailPage");
        PlayerPrefs.DeleteKey("SelectedCourseID");
        PlayerPrefs.DeleteKey("RoleName");
        PlayerPrefs.DeleteKey("CompanyName");
        PlayerPrefs.DeleteKey("Email");
        PlayerPrefs.DeleteKey("Username");
        PlayerPrefs.DeleteKey("Phone");
        PlayerPrefs.DeleteKey("Status");
        PlayerPrefs.DeleteKey("DeviceId");
        PlayerPrefs.DeleteKey("Point");
        PlayerPrefs.Save();

        // Reset any cached data in static managers
        UserManager.UserId = "";
        UserManager.CompanyId = "";
        UserManager.RoleName = "";
        UserManager.CompanyName = "";
        UserManager.Token = "";
        UserManager.Point = 0;
        CourseManager.SelectedCourseId = "";
    
        // Reset UI elements - this is now handled by OnUserLoggedOut via the event
        ResetUIComponents();
    
        // Clear local variables
        userId = "";
        authToken = "";
        needsProfileReload = false; // Reset the reload flag since we're logged out
    
        // Return to Login Screen
        if (profilePage != null) profilePage.SetActive(false);
        if (loginPage != null) loginPage.SetActive(true);
    
        // Important: Signal to any listeners that logout has occurred
        EventManager.TriggerEvent("UserLoggedOut");

        Debug.Log("✅ User logged out!");
    }
}