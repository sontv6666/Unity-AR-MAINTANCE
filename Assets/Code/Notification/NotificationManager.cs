using System;
using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using System.Threading.Tasks;

#if UNITY_ANDROID
using Unity.Notifications.Android;
#elif UNITY_IOS
using Unity.Notifications.iOS;
using UnityEngine.iOS;
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
    
    // Added retry mechanism for token registration
    private const int MAX_REGISTRATION_RETRIES = 3;
    private int registrationRetryCount = 0;
    private const float RETRY_DELAY = 5.0f; // 5 seconds

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

            // Force token refresh to ensure we have the latest token
            StartCoroutine(ForceTokenRefresh());

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

    // Force token refresh to ensure we have a valid token
    private IEnumerator ForceTokenRefresh()
    {
        var tokenTask = FirebaseMessaging.GetTokenAsync();
        yield return new WaitUntil(() => tokenTask.IsCompleted);

        if (tokenTask.Exception != null)
        {
            Debug.LogError($"❌ Failed to get Firebase token: {tokenTask.Exception.Message}");
        }
        else
        {
            string token = tokenTask.Result;
            Debug.Log($"📱 Firebase token refreshed: {token}");
            
            // Store the token
            PlayerPrefs.SetString("FirebaseToken", token);
            PlayerPrefs.Save();
            
            // Register with backend if user is logged in
            if (!string.IsNullOrEmpty(UserManager.UserId) && !string.IsNullOrEmpty(PlayerPrefs.GetString("CompanyId", "")))
            {
                RegisterDeviceWithBackend(UserManager.UserId, PlayerPrefs.GetString("CompanyId", ""));
            }
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

    void OnMessageReceived(object sender, MessageReceivedEventArgs e)
    {
        Debug.Log("📨 Firebase message received!");

        // Extract notification data
        string title = e.Message.Notification?.Title ?? "Notification";
        string body = e.Message.Notification?.Body ?? "You have a new notification";

        Debug.Log($"Title: {title}, Body: {body}");
        Debug.Log($"Message data count: {e.Message.Data.Count}");

        // Debug the data payload
        if (e.Message.Data.Count > 0)
        {
            Debug.Log("Data payload:");
            foreach (var pair in e.Message.Data)
            {
                Debug.Log($"{pair.Key}: {pair.Value}");
            }

            // Check for notification type
            if (e.Message.Data.TryGetValue("type", out string notificationType))
            {
                // Handle different notification types
                switch (notificationType)
                {
                    case "new_course":
                        // Show new course notification
                        if (e.Message.Data.TryGetValue("courseId", out string courseId) &&
                            e.Message.Data.TryGetValue("courseName", out string courseName))
                        {
                            HandleNewCourseNotification(courseId, courseName);
                        }

                        DisplayInAppNotification(title, body);
                        break;
                    case "point_used":
                        // Show in-app notification for points
                        DisplayInAppNotification(title, body);
                        break;
                    case "maintenance":
                        // Show maintenance notification
                        DisplayInAppNotification(title, body);
                        break;
                    case "point_request":
                        if (e.Message.Data.TryGetValue("status", out string status))
                        {
                            string pointsStr = "";
                            // Try to get points from either field (for compatibility)
                            if (e.Message.Data.TryGetValue("points", out pointsStr) || 
                                e.Message.Data.TryGetValue("amount", out pointsStr))
                            {
                                HandlePointRequestNotification(status, pointsStr);
                            }
                            else
                            {
                                Debug.LogWarning("⚠️ Point request notification missing points/amount data");
                            }
                        }
                        DisplayInAppNotification(title, body);
                        break;
                    default:
                        // Default case
                        DisplayInAppNotification(title, body);
                        break;
                }
            }
            else
            {
                // If no type specified, display default notification
                DisplayInAppNotification(title, body);
            }
        }
        else
        {
            // If no data payload, display default notification
            DisplayInAppNotification(title, body);
        }
    }
    
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
            
            // Refresh points from server to ensure consistency
            RefreshUserPoints();
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
        // Setup iOS notification categories
        var courseCategory = new iOSNotificationCategory("course_notifications", new List<iOSNotificationAction>());
        var maintenanceCategory = new iOSNotificationCategory("maintenance_alerts", new List<iOSNotificationAction>());
        var progressCategory = new iOSNotificationCategory("progress_updates", new List<iOSNotificationAction>());
    
        // Register notification categories
        iOSNotificationCenter.SetNotificationCategories(new List<iOSNotificationCategory>
        {
            courseCategory,
            maintenanceCategory,
            progressCategory
        });
    
        // The authorization will be requested automatically when scheduling the first notification
        // No need for explicit RequestAuthorization call
        Debug.Log("iOS notification categories registered. Authorization will be requested when first notification is scheduled.");
#endif
        Debug.Log("📱 Notification system initialized");
    }

    // Register device with backend with retry mechanism
    public void RegisterDeviceWithBackend(string userId, string companyId)
    {
        if (!firebaseInitialized)
        {
            Debug.LogWarning("⚠️ Firebase not initialized yet, scheduling registration for later");
            StartCoroutine(RetryRegistration(userId, companyId));
            return;
        }

        string token = PlayerPrefs.GetString("FirebaseToken", "");
        if (string.IsNullOrEmpty(token))
        {
            Debug.LogWarning("⚠️ No Firebase token available, requesting token and scheduling registration");
            StartCoroutine(ForceTokenRefresh());
            StartCoroutine(RetryRegistration(userId, companyId));
            return;
        }

        Debug.Log($"🔄 Registering device with backend - User: {userId}, Company: {companyId}, Token: {token}");
        registrationRetryCount = 0;
        
        // Use the NotificationService to register the device
        NotificationService.Instance.RegisterDevice(userId, companyId, token, success => {
            if (success)
            {
                Debug.Log("✅ Device registered successfully with backend");
                
                // Subscribe to company topic
                SubscribeToCompanyTopic(companyId);
            }
            else
            {
                Debug.LogError("❌ Error registering device");
                
                // Retry if possible
                if (registrationRetryCount < MAX_REGISTRATION_RETRIES)
                {
                    StartCoroutine(RetryRegistration(userId, companyId));
                }
            }
        });
    }

    private IEnumerator RetryRegistration(string userId, string companyId)
    {
        if (registrationRetryCount >= MAX_REGISTRATION_RETRIES)
        {
            Debug.LogError("❌ Maximum registration retries reached");
            yield break;
        }
        
        registrationRetryCount++;
        Debug.Log($"🔁 Scheduling registration retry {registrationRetryCount}/{MAX_REGISTRATION_RETRIES} in {RETRY_DELAY} seconds");
        
        yield return new WaitForSeconds(RETRY_DELAY);
        RegisterDeviceWithBackend(userId, companyId);
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

        // Subscribe via Firebase directly (client-side subscription)
        FirebaseMessaging.SubscribeAsync(topic).ContinueWith(task =>
        {
            if (task.IsFaulted)
            {
                Debug.LogError($"❌ Failed to subscribe to company topic: {task.Exception?.Message}");
            }
            else if (task.IsCompleted)
            {
                Debug.Log($"✅ Successfully subscribed to Firebase topic: {topic}");
                
                // Also subscribe through backend (if needed)
                string token = PlayerPrefs.GetString("FirebaseToken", "");
                if (!string.IsNullOrEmpty(token))
                {
                    NotificationService.Instance.SubscribeToTopic(token, topic, success => {
                        if (success)
                        {
                            Debug.Log($"✅ Successfully subscribed to topic via backend: {topic}");
                        }
                        else
                        {
                            Debug.LogError($"❌ Failed to subscribe to topic via backend: {topic}");
                        }
                    });
                }
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

        // Unsubscribe via Firebase directly
        FirebaseMessaging.UnsubscribeAsync(topic).ContinueWith(task =>
        {
            if (task.IsFaulted)
            {
                Debug.LogError($"❌ Failed to unsubscribe from company topic: {task.Exception?.Message}");
            }
            else if (task.IsCompleted)
            {
                Debug.Log($"✅ Successfully unsubscribed from Firebase topic: {topic}");
                
                // Also unsubscribe through backend (if needed)
                string token = PlayerPrefs.GetString("FirebaseToken", "");
                if (!string.IsNullOrEmpty(token))
                {
                    NotificationService.Instance.UnsubscribeFromTopic(token, topic, success => {
                        if (success)
                        {
                            Debug.Log($"✅ Successfully unsubscribed from topic via backend: {topic}");
                        }
                        else
                        {
                            Debug.LogError($"❌ Failed to unsubscribe from topic via backend: {topic}");
                            }
                    });
                }
            }
        });
    }

    // Send notification to user
    public void SendNotificationToUser(string userId, string title, string body, string data = null)
    {
        Debug.Log($"🔄 Sending notification to user: {userId}");
        
        NotificationService.Instance.SendNotificationToUser(userId, title, body, data, result => {
            if (result != null && result.Count > 0)
            {
                Debug.Log($"✅ Successfully sent notification to {result.Count} devices of user {userId}");
            }
            else
            {
                Debug.LogWarning($"⚠️ No devices found for user {userId} or notification failed");
            }
        });
    }
    
    // Send notification to topic
    public void SendNotificationToTopic(string topic, string title, string body, string data = null)
    {
        Debug.Log($"🔄 Sending notification to topic: {topic}");
        
        NotificationService.Instance.SendNotificationToTopic(topic, title, body, data, result => {
            if (!string.IsNullOrEmpty(result))
            {
                Debug.Log($"✅ Successfully sent notification to topic {topic}");
            }
            else
            {
                Debug.LogWarning($"⚠️ Failed to send notification to topic {topic}");
            }
        });
    }
    
    // Send notification to a specific device token
    public void SendNotificationToDevice(string token, string title, string body, string data = null)
    {
        Debug.Log($"🔄 Sending notification to device: {token}");
        
        NotificationService.Instance.SendNotificationToToken(token, title, body, data, result => {
            if (!string.IsNullOrEmpty(result))
            {
                Debug.Log($"✅ Successfully sent notification to device {token}");
            }
            else
            {
                Debug.LogWarning($"⚠️ Failed to send notification to device {token}");
            }
        });
    }
    
    // Send maintenance notification to all users in a company
    public void SendMaintenanceNotification(string companyId, string title, string body, DateTime scheduledTime)
    {
        string topic = $"company_{companyId}";
        
        // Create data payload with maintenance information
        Dictionary<string, string> dataDict = new Dictionary<string, string>
        {
            { "type", "maintenance" },
            { "scheduledTime", scheduledTime.ToString("o") } // ISO 8601 format
        };
        
        // Convert data dictionary to JSON string
        string dataPayload = JsonConvert.SerializeObject(dataDict);
        
        // Send notification to company topic
        SendNotificationToTopic(topic, title, body, dataPayload);
    }
    
    // Send course notification to specific user
    public void SendCourseNotification(string userId, string courseId, string courseName)
    {
        string title = "New Course Available";
        string body = $"A new course '{courseName}' is now available for you!";
        
        // Create data payload with course information
        Dictionary<string, string> dataDict = new Dictionary<string, string>
        {
            { "type", "new_course" },
            { "courseId", courseId },
            { "courseName", courseName }
        };
        
        // Convert data dictionary to JSON string
        string dataPayload = JsonConvert.SerializeObject(dataDict);
        
        // Send notification to user
        SendNotificationToUser(userId, title, body, dataPayload);
    }
    
    // Send point usage notification
    public void SendPointUsageNotification(string userId, int pointsUsed, int remainingPoints, string reason)
    {
        string title = "Points Used";
        string body = $"You've used {pointsUsed} point(s) for {reason}. Remaining balance: {remainingPoints} points.";
        
        // Create data payload
        Dictionary<string, string> dataDict = new Dictionary<string, string>
        {
            { "type", "point_used" },
            { "pointsUsed", pointsUsed.ToString() },
            { "remainingPoints", remainingPoints.ToString() },
            { "reason", reason }
        };
        
        // Convert data dictionary to JSON string
        string dataPayload = JsonConvert.SerializeObject(dataDict);
        
        // Send notification to user
        SendNotificationToUser(userId, title, body, dataPayload);
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
    
    // Unregister device when logging out
    public void UnregisterDeviceOnLogout()
    {
        string token = PlayerPrefs.GetString("FirebaseToken", "");
        string userId = UserManager.UserId;
        
        if (!string.IsNullOrEmpty(token) && !string.IsNullOrEmpty(userId))
        {
            Debug.Log($"🔄 Unregistering device on logout - User: {userId}, Token: {token}");
            
            NotificationService.Instance.UnregisterDevice(userId, token, success => {
                if (success)
                {
                    Debug.Log("✅ Device unregistered successfully on logout");
                }
                else
                {
                    Debug.LogError("❌ Failed to unregister device on logout");
                }
            });
        }
    }
    
    // Add this to handle application focus changes
    void OnApplicationPause(bool pause)
    {
        if (!pause)
        {
            // App returned to foreground
            Debug.Log("App is back in foreground, refreshing user points...");
            RefreshUserPoints();
            
            // Refresh Firebase token
            StartCoroutine(ForceTokenRefresh());
        }
    }
}