using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using Code;
using UnityEngine.Networking;
using System;
#if UNITY_ANDROID && !UNITY_EDITOR
using UnityEngine.Android;
#endif

public class ScreenManager : MonoBehaviour
{
    [Header("📌 UI Screens")]
    public List<RectTransform> screens; // ✅ Supports RectTransform-based UI
    private int currentScreenIndex = 0;
    private bool isTransitioning = false; // ✅ Prevents multiple transitions

    [Header("📱 Network Settings")]
    public GameObject noInternetPanel;  // Panel to show when no internet
    public Button retryConnectionButton; // Button to retry connection
    private bool isCheckingConnection = false;
    public float connectionCheckInterval = 5f; // How often to check connection when offline
    public float activeConnectionCheckInterval = 30f; // Check interval during normal app usage
    private bool wasConnectedBefore = true; // Track previous connection state
    
    public SceneNavigator sceneNavigator; // ✅ Assign in Inspector
    public float transitionSpeed = 2f; // ✅ Adjust speed for smooth transitions
    public float splashDuration = 3f; // ⏳ Adjustable Splash Screen duration

    // Event that other scripts can subscribe to for network status changes
    public static event Action<bool> OnConnectionStatusChanged;

    private void Start()
    {
        // 🔔 Request notification permission (Android 13+)
#if UNITY_ANDROID && !UNITY_EDITOR
        if (GetAndroidSDKVersion() >= 33 && !Permission.HasUserAuthorizedPermission("android.permission.POST_NOTIFICATIONS"))
        {
            Debug.Log("🔔 Requesting notification permission...");
            Permission.RequestUserPermission("android.permission.POST_NOTIFICATIONS");
        }
#endif
        // 🔥 Initialize the notification manager (Firebase + channels)
        var _ = MaintenanceNotificationManager.Instance;

        if (screens == null || screens.Count == 0)
        {
            Debug.LogError("❌ No screens assigned in ScreenManager! Check Inspector.");
            return;
        }

        // Hide no internet panel at start
        if (noInternetPanel != null)
            noInternetPanel.SetActive(false);
            
        // Add listener to retry button if it exists
        if (retryConnectionButton != null)
            retryConnectionButton.onClick.AddListener(RetryInternetConnection);

        // ✅ Show Splash Screen (First screen in the list)
        ShowScreen(screens[0].name);

        // ✅ Auto Transition after Splash
        StartCoroutine(AutoTransitionFromSplash());
        
        // Start monitoring internet connection during app usage
        StartCoroutine(MonitorInternetConnection());
    }
    
#if UNITY_ANDROID && !UNITY_EDITOR
    private int GetAndroidSDKVersion()
    {
        using (var version = new AndroidJavaClass("android.os.Build$VERSION"))
        {
            return version.GetStatic<int>("SDK_INT");
        }
    }
#endif    

    private IEnumerator AutoTransitionFromSplash()
    {
        yield return new WaitForSeconds(splashDuration); // ⏳ Wait for splash duration

        // Check internet connection before proceeding
        yield return StartCoroutine(CheckInternetConnection((isConnected) => {
            wasConnectedBefore = isConnected; // Initialize connection state tracker
            
            if (isConnected)
            {
                if (IsUserLoggedIn())
                {
                    Debug.Log("✅ User already logged in! Restoring last page...");
                    RestoreLastPage();
                }
                else
                {
                    AssignNextButtons();
                    Debug.Log("🔄 New user detected. Moving to Login Screen.");
                    GoToNextScreen(); // Move to next screen (Login)
                }
            }
            else
            {
                Debug.LogWarning("⚠️ No internet connection detected at startup!");
                ShowNoInternetMessage();
                
                // If user was logged in before, we should still try to restore their session
                if (IsUserLoggedIn())
                {
                    Debug.Log("⚠️ User was logged in. Routing to login screen due to no connectivity.");
                    // Find index of login screen
                    int loginScreenIndex = screens.FindIndex(s => s.name.Contains("LoginScreen") || s.name.Contains("Login"));
                    if (loginScreenIndex >= 0)
                    {
                        GoToScreen(loginScreenIndex);
                    }
                }
                else
                {
                    AssignNextButtons();
                    GoToNextScreen(); // Still move to login screen
                }
            }
        }));
    }
    
    private IEnumerator MonitorInternetConnection()
    {
        while (true)
        {
            // Skip check if already checking or if offline panel is active
            if (!isCheckingConnection && (noInternetPanel == null || !noInternetPanel.activeSelf))
            {
                yield return StartCoroutine(CheckInternetConnection((isConnected) => {
                    // Only notify if connection status changed
                    if (wasConnectedBefore != isConnected)
                    {
                        wasConnectedBefore = isConnected;
                        
                        // If connection was lost
                        if (!isConnected)
                        {
                            Debug.LogWarning("⚠️ Internet connection lost during app usage!");
                            ShowNoInternetMessage();
                            
                            // Notify other components about connectivity loss
                            OnConnectionStatusChanged?.Invoke(false);
                        }
                        else
                        {
                            Debug.Log("✅ Internet connection restored!");
                            
                            // Notify other components about connectivity restoration
                            OnConnectionStatusChanged?.Invoke(true);
                        }
                    }
                }));
            }
            
            // Wait before next check - use shorter interval if connection is lost
            if (wasConnectedBefore)
                yield return new WaitForSeconds(activeConnectionCheckInterval);
            else
                yield return new WaitForSeconds(connectionCheckInterval);
        }
    }
    
    private IEnumerator CheckInternetConnection(Action<bool> callback)
    {
        isCheckingConnection = true;
        
        // Use Unity's ping to check connection
        UnityWebRequest request = new UnityWebRequest("https://google.com");
        request.timeout = 5; // 5 second timeout
        yield return request.SendWebRequest();
        
        bool isConnected = request.result != UnityWebRequest.Result.ConnectionError && 
                          request.result != UnityWebRequest.Result.DataProcessingError;
                          
        isCheckingConnection = false;
        callback(isConnected);
    }
    
    public void ShowNoInternetMessage()
    {
        // Save current screen before showing no internet panel
        string currentScreenName = screens[currentScreenIndex].name;
        PlayerPrefs.SetString("LastActiveScreen", currentScreenName);
    
        // Show no internet panel if it exists
        if (noInternetPanel != null)
        {
            noInternetPanel.SetActive(true);
            StartCoroutine(AutoCheckInternetConnection());
        }
    
        // If the user is on a screen that requires network connectivity
        // (except login screen), navigate back to login
        if (currentScreenName != "LoginScreen" && 
            (currentScreenName == "HomePage" || currentScreenName == "DetailPage"))
        {
            // Find index of login screen
            int loginScreenIndex = screens.FindIndex(s => s.name.Contains("LoginScreen") || s.name.Contains("Login"));
            if (loginScreenIndex >= 0)
            {
                // Use SlideTransition to go to login screen
                StartCoroutine(SlideTransition(loginScreenIndex));
            }
        }
    }
    
    
    
    private IEnumerator AutoCheckInternetConnection()
    {
        while (noInternetPanel != null && noInternetPanel.activeSelf)
        {
            if (!isCheckingConnection)
            {
                yield return StartCoroutine(CheckInternetConnection((isConnected) => {
                    if (isConnected && noInternetPanel.activeSelf)
                    {
                        noInternetPanel.SetActive(false);
                        Debug.Log("✅ Internet connection restored automatically!");
                        
                        // Restore previous screen if possible
                        RestoreScreenAfterConnectionRestored();
                        
                        // Update connection state
                        wasConnectedBefore = true;
                        
                        // Notify other components
                        OnConnectionStatusChanged?.Invoke(true);
                    }
                }));
            }
            
            yield return new WaitForSeconds(connectionCheckInterval);
        }
    }
    
    private void RestoreScreenAfterConnectionRestored()
    {
        string lastActiveScreen = PlayerPrefs.GetString("LastActiveScreen", "");
        
        if (!string.IsNullOrEmpty(lastActiveScreen))
        {
            // If user was on a screen that requires login and they're logged in
            if ((lastActiveScreen == "HomePage" || lastActiveScreen == "DetailPage") && IsUserLoggedIn())
            {
                ShowScreen(lastActiveScreen);
                
                // Refresh data if needed
                if (lastActiveScreen == "HomePage")
                {
                    CourseLoader courseLoader = FindObjectOfType<CourseLoader>();
                    if (courseLoader != null)
                    {
                        courseLoader.ReloadCourseData();
                    }
                }
                else if (lastActiveScreen == "DetailPage")
                {
                    string courseId = PlayerPrefs.GetString("SelectedCourseID", "");
                    if (!string.IsNullOrEmpty(courseId))
                    {
                        CourseDetailLoader detailLoader = FindObjectOfType<CourseDetailLoader>();
                        if (detailLoader != null)
                        {
                            detailLoader.LoadCourseDetails(courseId);
                        }
                    }
                }
            }
            // If user was on a login-related screen or isn't logged in
            else
            {
                // Find login screen index
                int loginScreenIndex = screens.FindIndex(s => s.name.Contains("LoginScreen") || s.name.Contains("Login"));
                if (loginScreenIndex >= 0)
                {
                    ShowScreen(screens[loginScreenIndex].name);
                }
            }
        }
    }
    
    public void RetryInternetConnection()
    {
        StartCoroutine(CheckInternetConnection((isConnected) => {
            if (isConnected)
            {
                if (noInternetPanel != null)
                    noInternetPanel.SetActive(false);
                
                Debug.Log("✅ Internet connection restored via retry!");
                
                // Restore previous screen
                RestoreScreenAfterConnectionRestored();
                
                // Update connection state
                wasConnectedBefore = true;
                
                // Notify other components
                OnConnectionStatusChanged?.Invoke(true);
            }
            else
            {
                Debug.LogWarning("⚠️ Still no internet connection!");
                // Keep showing the no internet panel
            }
        }));
    }

    private bool IsUserLoggedIn()
    {
        string userId = PlayerPrefs.GetString("UserId", "");
        return !string.IsNullOrEmpty(userId);
    }

    public void GoToNextScreen()
    {
        if (isTransitioning || screens == null || currentScreenIndex >= screens.Count - 1)
        {
            Debug.LogWarning("⚠ Cannot transition: Already at the last screen or transition in progress.");
            return;
        }

        int nextScreenIndex = currentScreenIndex + 1;
        StartCoroutine(SlideTransition(nextScreenIndex));
    }
    
    private void AssignNextButtons()
    {
        for (int i = 0; i < screens.Count; i++)
        {
            // Chỉ xử lý các screen có tên chứa "LoadingScreen"
            if (screens[i].name.Contains("LoadingScreen"))
            {
                Button button = screens[i].GetComponentInChildren<Button>(); // 🔍 Tìm Button trong mỗi screen
                if (button != null)
                {
                    int nextIndex = i + 1; // Xác định chỉ số của screen tiếp theo
                    button.onClick.RemoveAllListeners();
                    button.onClick.AddListener(() => GoToScreen(nextIndex)); // ✅ Gán sự kiện click
                }
            }
        }
    }

    private void GoToScreen(int index)
    {
        if (index < screens.Count)
        {
            StartCoroutine(SlideTransition(index));
        }
        else
        {
            Debug.Log("🏁 Reached the last screen!");
        }
    }

    private IEnumerator SlideTransition(int nextScreenIndex)
    {
        isTransitioning = true;

        RectTransform previousScreen = screens[currentScreenIndex];
        RectTransform nextScreen = screens[nextScreenIndex];

        nextScreen.gameObject.SetActive(true);

        Vector3 startPosition = previousScreen.anchoredPosition;
        Vector3 targetPosition = new Vector3(-previousScreen.rect.width, 0, 0);

        float t = 0;
        while (t < 1)
        {
            t += Time.deltaTime * transitionSpeed;
            previousScreen.anchoredPosition = Vector3.Lerp(startPosition, targetPosition, t);
            nextScreen.anchoredPosition = Vector3.Lerp(new Vector3(nextScreen.rect.width, 0, 0), Vector3.zero, t);
            yield return null;
        }

        previousScreen.gameObject.SetActive(false);
        currentScreenIndex = nextScreenIndex;
        isTransitioning = false;
    }

    private void RestoreLastPage()
    {
        bool showDetail = PlayerPrefs.GetInt("ShowDetailPage", 0) == 1;
        string lastCourseId = PlayerPrefs.GetString("SelectedCourseID", "");

        if (showDetail && !string.IsNullOrEmpty(lastCourseId))
        {
            Debug.Log("📌 Restoring Detail Page...");
            ShowScreen("DetailPage");

            CourseDetailLoader courseDetailLoader = FindObjectOfType<CourseDetailLoader>();
            courseDetailLoader?.LoadCourseDetails(lastCourseId);

            currentScreenIndex = screens.FindIndex(s => s.name == "DetailPage");
        }
        else
        {
            Debug.Log("🏠 Restoring Home Page...");
            ShowScreen("HomePage");

            CourseLoader courseLoader = FindObjectOfType<CourseLoader>();
            if (courseLoader != null)
            {
                // Reload user data first
                courseLoader.ReloadUserData();
            
                // Then reload course data
                courseLoader.ReloadCourseData();
            }

            currentScreenIndex = screens.FindIndex(s => s.name == "HomePage");
        }

        // 🛑 Reset PlayerPrefs
        PlayerPrefs.SetInt("ShowHomePage", 0);
        PlayerPrefs.SetInt("ShowDetailPage", 0);
        PlayerPrefs.SetString("SelectedCourseID", "");
        PlayerPrefs.Save();
    }
    
    public void ShowScreen(string screenName)
    {
        foreach (RectTransform screen in screens)
        {
            screen.gameObject.SetActive(screen.name == screenName);
        }

        // ✅ Update `currentScreenIndex` properly
        int screenIndex = screens.FindIndex(s => s.name == screenName);
        if (screenIndex >= 0)
        {
            currentScreenIndex = screenIndex;
        }
    }
}