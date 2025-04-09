
using System;
using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using System.Threading.Tasks;

#if UNITY_ANDROID
using Unity.Notifications.Android;
#elif UNITY_IOS
using Unity.Notifications.iOS;
#endif

// Firebase imports
using Firebase;
using Firebase.Messaging;
using Models;
using Newtonsoft.Json;
using UnityEngine.Networking;

public class MaintenanceNotificationManager : MonoBehaviour
{
    private static MaintenanceNotificationManager _instance;
    


    public static MaintenanceNotificationManager Instance
    {
        get
        {
            if (_instance == null)
            {
                GameObject go = new GameObject("MaintenanceNotificationManager");
                _instance = go.AddComponent<MaintenanceNotificationManager>();
                DontDestroyOnLoad(go);
            }

            return _instance;
        }
    }

    // Add Firebase variables
    private FirebaseApp app;
    private bool firebaseInitialized = false;
    private string registrationEndpoint = "/notifications/register";

    void Awake()
    {
        if (_instance == null)
        {
            _instance = this;
            DontDestroyOnLoad(gameObject);
            InitializeNotifications();
            // Initialize Firebase
            StartCoroutine(InitializeFirebase());
        }
        else if (_instance != this)
        {
            Destroy(gameObject);
        }
    }

    // Initialize Firebase
    private IEnumerator InitializeFirebase()
    {
        Debug.Log("🔄 Initializing Firebase...");

        // Check dependencies
        var checkTask = FirebaseApp.CheckAndFixDependenciesAsync();
        yield return new WaitUntil(() => checkTask.IsCompleted);

        var dependencyStatus = checkTask.Result;
        if (dependencyStatus == DependencyStatus.Available)
        {
            // Initialize Firebase
            app = FirebaseApp.DefaultInstance;

            // Initialize Firebase Messaging
            FirebaseMessaging.TokenReceived += OnTokenReceived;
            FirebaseMessaging.MessageReceived += OnMessageReceived;

            Debug.Log("✅ Firebase initialized successfully!");
            firebaseInitialized = true;

            // Check if user is already logged in, and register if so
            if (!string.IsNullOrEmpty(UserManager.UserId) && !string.IsNullOrEmpty(UserManager.CompanyName))
            {
                RegisterDeviceWithBackend(UserManager.UserId, PlayerPrefs.GetString("CompanyId", ""));
            }
        }
        else
        {
            Debug.LogError($"❌ Firebase initialization failed: {dependencyStatus}");
        }
    }

    void OnTokenReceived(object sender, TokenReceivedEventArgs token)
    {
        Debug.Log($"📱 Firebase device token received: {token.Token}");

        // Store the token for future use
        PlayerPrefs.SetString("FirebaseToken", token.Token);
        PlayerPrefs.Save();

        // Register with backend if user is logged in
        if (!string.IsNullOrEmpty(UserManager.UserId) && !string.IsNullOrEmpty(PlayerPrefs.GetString("CompanyId", "")))
        {
            RegisterDeviceWithBackend(UserManager.UserId, PlayerPrefs.GetString("CompanyId", ""));
        }
    }


private string lastNotificationId = "";
private float lastNotificationTime = 0f;
private bool notificationReceived = false;

void OnMessageReceived(object sender, MessageReceivedEventArgs e)
{
    Debug.Log("📨 Firebase message received!");

    // Extract notification data
    string title = e.Message.Notification?.Title ?? "Notification";
    string body = e.Message.Notification?.Body ?? "You have a new notification";
    
    Debug.Log($"Title: {title}, Body: {body}");

    // Generate a unique ID for this notification
    string currentNotificationId = e.Message.MessageId ?? $"{title}_{body}_{Time.time}";
    
    // More flexible duplicate detection specific to iOS
    #if UNITY_IOS
    // For iOS, use a more lenient duplicate detection
    bool isDuplicate = currentNotificationId == lastNotificationId && 
                       Time.time - lastNotificationTime < 1.5f;
    #else
    // For other platforms (Android), use the original detection
    bool isDuplicate = currentNotificationId == lastNotificationId && 
                       Time.time - lastNotificationTime < 3f;
    #endif

    if (isDuplicate)
    {
        Debug.Log("🔁 Duplicate notification detected. Skipping...");
        return;
    }

    // Update tracking variables
    lastNotificationId = currentNotificationId;
    lastNotificationTime = Time.time;

    // Only skip completely empty notifications
    if (string.IsNullOrEmpty(title) && string.IsNullOrEmpty(body) && 
        (e.Message.Data == null || e.Message.Data.Count == 0))
    {
        Debug.Log("🚫 Notification has no content. Skipping...");
        return;
    }

    // Handle data payload
    if (e.Message.Data != null && e.Message.Data.Count > 0)
    {
        Debug.Log("Data payload:");
        foreach (var pair in e.Message.Data)
        {
            Debug.Log($"{pair.Key}: {pair.Value}");
        }

        // Check for notification type
        if (e.Message.Data.TryGetValue("type", out string notificationType))
        {
            switch (notificationType)
            {
                case "new_course":
                    if (e.Message.Data.TryGetValue("courseId", out string courseId) &&
                        e.Message.Data.TryGetValue("courseName", out string courseName))
                    {
                        HandleNewCourseNotification(courseId, courseName);
                    }
                    DisplayInAppNotification(title, body);
                    break;

                case "point_used":
                    DisplayInAppNotification(title, body);
                    break;

                case "maintenance":
                    DisplayInAppNotification(title, body);
                    break;

                case "point_request":
                    if (e.Message.Data.TryGetValue("status", out string status) &&
                        e.Message.Data.TryGetValue("points", out string pointsStr))
                    {
                        HandlePointRequestNotification(status, pointsStr);
                    }
                    DisplayInAppNotification(title, body);
                    break;

                default:
                    DisplayInAppNotification(title, body);
                    break;
            }
        }
        else
        {
            DisplayInAppNotification(title, body);
        }
    }
    else
    {
        // If there's no data payload but there's still a title/body, display it
        DisplayInAppNotification(title, body);
    }
}
    
    
    // Add this new method to handle point request notifications
    private void HandlePointRequestNotification(string status, string pointsStr)
    {
        Debug.Log($"💰 Point request notification received: Status={status}, Points={pointsStr}");
    
        // Parse points value
        if (int.TryParse(pointsStr, out int points))
        {
            // Update points in PlayerPrefs
            PlayerPrefs.SetInt("Point", points);
            PlayerPrefs.Save();
        
            // Update points in UserManager static class
            UserManager.Point = points;
        
            // Create notification message based on status
            string title = status.Equals("APPROVED", StringComparison.OrdinalIgnoreCase) ? 
                "Point Request Approved" : "Point Request Rejected";
        
            string message = status.Equals("APPROVED", StringComparison.OrdinalIgnoreCase) ?
                $"Your point request has been approved! New balance: {points} points." :
                $"Your point request has been rejected. Current balance: {points} points.";
        
            // Show notification
            ShowLocalNotification(title, message, "progress_updates");
            
        }
        else
        {
            Debug.LogError($"❌ Failed to parse points value: {pointsStr}");
        }
    }

    
 
    
// Add this method to manually refresh points from the server
    public void RefreshUserPoints()
    {
        StartCoroutine(FetchUserPoints());
    }
    private void HandleNewCourseNotification(string courseId, string courseName)
    {
        Debug.Log($"📚 New course notification: {courseName} (ID: {courseId})");

        // Store the course ID as "new" for UI highlighting
        PlayerPrefs.SetString($"new_course_{courseId}", "true");
        PlayerPrefs.SetString($"new_course_name_{courseId}", courseName);
        PlayerPrefs.Save();

        // You could also trigger a refresh of your course list UI here
        // Example: CourseListManager.Instance.RefreshCourseList();
    }

    private IEnumerator FetchUserPoints()
    {
        // Ensure user is logged in
        if (string.IsNullOrEmpty(UserManager.Token) || string.IsNullOrEmpty(UserManager.UserId))
        {
            Debug.LogWarning("⚠️ Cannot refresh points: User not logged in");
            yield break;
        }

        // Create request to fetch user profile
        string endpoint = "/api/users/profile";
        UnityWebRequest request = ApiConfig.CreateRequest(endpoint, "GET");

        yield return request.SendWebRequest();

        if (request.result == UnityWebRequest.Result.Success)
        {
            string jsonResponse = request.downloadHandler.text;
            var response = JsonConvert.DeserializeObject<UserProfileResult>(jsonResponse);

            if (response != null && response.id != null )
            {
                int points = response.points;

                // Update points in PlayerPrefs and UserManager
                PlayerPrefs.SetInt("Point", points);
                PlayerPrefs.Save();
                UserManager.Point = points;
                

                Debug.Log($"✅ User points refreshed: {points}");
            }
        }
        else
        {
            Debug.LogError($"❌ Failed to fetch user profile: {request.error}");
        }

    }

    // Display in-app notification
    private void DisplayInAppNotification(string title, string message)
    {
        // Implement in-app notification UI here
        Debug.Log($"📲 In-App Notification: {title} - {message}");

        // This would typically update some UI element in your game
        // If you have a notification panel, activate it here
        // For now, we'll just show a local notification as well
        ShowLocalNotification(title, message);
    }
    
    // Show local notification
    public void ShowLocalNotification(string title, string message, string channelId = "course_notifications")
    {
#if UNITY_ANDROID
        var notification = new AndroidNotification
        {
            Title = title,
            Text = message,
            SmallIcon = "icon_notification",
            LargeIcon = "icon_notification_large",
            FireTime = DateTime.Now.AddSeconds(1)
        };

        AndroidNotificationCenter.SendNotification(notification, channelId);
        Debug.Log($"📱 Local notification sent: {title}");
#elif UNITY_IOS
        var notification = new iOSNotification
        {
            Title = title,
            Body = message,
            ShowInForeground = true,
            ForegroundPresentationOption = (PresentationOption.Alert | PresentationOption.Sound),
            Trigger = new iOSNotificationTimeIntervalTrigger
            {
                TimeInterval = new TimeSpan(0, 0, 1),
                Repeats = false
            }
        };

        iOSNotificationCenter.ScheduleNotification(notification);
        Debug.Log($"📱 iOS notification sent: {title}");
#endif
    }

    void InitializeNotifications()
    {
#if UNITY_ANDROID
        AndroidNotificationChannel courseChannel = new AndroidNotificationChannel()
        {
            Id = "course_notifications",
            Name = "Course Notifications",
            Importance = Importance.High,
            Description = "Notifications for new courses and updates"
        };

        AndroidNotificationChannel maintenanceChannel = new AndroidNotificationChannel()
        {
            Id = "maintenance_alerts",
            Name = "Maintenance Alerts",
            Importance = Importance.High,
            Description = "Alerts for scheduled equipment maintenance"
        };

        AndroidNotificationChannel progressChannel = new AndroidNotificationChannel()
        {
            Id = "progress_updates",
            Name = "Progress Updates",
            Importance = Importance.Default,
            Description = "Updates on your training progress"
        };

        AndroidNotificationCenter.RegisterNotificationChannel(courseChannel);
        AndroidNotificationCenter.RegisterNotificationChannel(maintenanceChannel);
        AndroidNotificationCenter.RegisterNotificationChannel(progressChannel);
#elif UNITY_IOS
        if (!PlayerPrefs.HasKey("WelcomeNotificationSent"))
        {
            var timeTrigger = new iOSNotificationTimeIntervalTrigger()
            {
                TimeInterval = new TimeSpan(0, 0, 5),
                Repeats = false
            };

            var notification = new iOSNotification()
            {
                Identifier = "_notification_01",
                Title = "Welcome!",
                Body = "Enjoy your AR guideline",
                Subtitle = "Try your best and gain your skills",
                ShowInForeground = true,
                ForegroundPresentationOption = (PresentationOption.Alert | PresentationOption.Sound),
                Trigger = timeTrigger,
            };

            iOSNotificationCenter.ScheduleNotification(notification);
            PlayerPrefs.SetInt("WelcomeNotificationSent", 1);
            PlayerPrefs.Save();
        }
#endif
        Debug.Log("📱 Notification system initialized");
    }

    // Register device with backend
    public async void RegisterDeviceWithBackend(string userId, string companyId)
    {
        if (!firebaseInitialized)
        {
            Debug.LogWarning("⚠️ Firebase not initialized yet, skipping registration");
            return;
        }

        string token = PlayerPrefs.GetString("FirebaseToken", "");
        if (string.IsNullOrEmpty(token))
        {
            Debug.LogWarning("⚠️ No Firebase token available to register");
            return;
        }

        Debug.Log($"🔄 Registering device with backend - User: {userId}, Company: {companyId}, Token: {token}");

        // Create registration request
        Dictionary<string, string> requestData = new Dictionary<string, string>
        {
            { "token", token },
            { "userId", userId },
            { "companyId", companyId }
        };

        string jsonBody = JsonConvert.SerializeObject(requestData);

        // Send registration request to backend
        UnityWebRequest request = ApiConfig.CreateRequest(registrationEndpoint, "POST", jsonBody);

        // Send the request
        await request.SendWebRequest();

        if (request.result == UnityWebRequest.Result.Success)
        {
            Debug.Log("✅ Device registered successfully with backend");

            // Subscribe to company topic
            SubscribeToCompanyTopic(companyId);
        }
        else
        {
            Debug.LogError($"❌ Error registering device: {request.error}");
        }
    }

    public void SubscribeToCompanyTopic(string companyId)
    {
        if (!firebaseInitialized)
        {
            Debug.LogWarning("⚠️ Firebase not initialized yet");
            return;
        }

        string topic = $"company_{companyId}";
        Debug.Log($"🔄 Subscribing to topic: {topic}");

        FirebaseMessaging.SubscribeAsync(topic).ContinueWith(task =>
        {
            if (task.IsFaulted)
            {
                Debug.LogError($"❌ Failed to subscribe to company topic: {task.Exception?.Message}");
            }
            else if (task.IsCompleted)
            {
                Debug.Log($"✅ Successfully subscribed to company topic: {topic}");
            }
        });
    }

    // Unsubscribe from company topic
    public void UnsubscribeFromCompanyTopic(string companyId)
    {
        if (!firebaseInitialized)
        {
            Debug.LogWarning("⚠️ Firebase not initialized yet");
            return;
        }

        string topic = $"company_{companyId}";
        Debug.Log($"🔄 Unsubscribing from topic: {topic}");

        FirebaseMessaging.UnsubscribeAsync(topic).ContinueWith(task =>
        {
            if (task.IsFaulted)
            {
                Debug.LogError($"❌ Failed to unsubscribe from company topic: {task.Exception?.Message}");
            }
            else if (task.IsCompleted)
            {
                Debug.Log($"✅ Successfully unsubscribed from company topic: {topic}");
            }
        });
    }

   
    
    // Notification for points used
    public void NotifyPointUsed()
    {
        ShowLocalNotification("Point Used", "You've used 1 point to access this course.", "progress_updates");
    }

    public void CancelNotification(string courseName)
    {
#if UNITY_ANDROID
        int id = PlayerPrefs.GetInt($"notification_{courseName}", -1);
        if (id != -1)
        {
            AndroidNotificationCenter.CancelNotification(id);
            PlayerPrefs.DeleteKey($"notification_{courseName}");
        }
#elif UNITY_IOS
        iOSNotificationCenter.RemoveAllScheduledNotifications();
#endif
    }
    
    
    public void CancelAllNotifications()
    {
#if UNITY_ANDROID
        AndroidNotificationCenter.CancelAllNotifications();
#elif UNITY_IOS
        iOSNotificationCenter.RemoveAllScheduledNotifications();
        iOSNotificationCenter.RemoveAllDeliveredNotifications();
#endif
    }
    
}


// Support class for JSON serialization
[Serializable]
public class DeviceRegistrationRequest
{
    public string token;
    public string userId;
    public string companyId;
}

