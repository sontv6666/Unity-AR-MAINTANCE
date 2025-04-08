
using System;
using UnityEngine;
using System.Collections;
using System.Threading.Tasks;

#if UNITY_ANDROID
using Unity.Notifications.Android;
#elif UNITY_IOS
using Unity.Notifications.iOS;
#endif

// Firebase imports
using Firebase;
using Firebase.Messaging;

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
            
            // Subscribe to topic for broadcasts
            FirebaseMessaging.SubscribeAsync("maintenance_alerts");
            FirebaseMessaging.SubscribeAsync("training_updates");
        }
        else
        {
            Debug.LogError($"❌ Firebase initialization failed: {dependencyStatus}");
        }
    }

    void OnTokenReceived(object sender, TokenReceivedEventArgs token)
    {
        Debug.Log($"📱 Firebase device token received: {token.Token}");
        // You can send this token to your server for targeted notifications
        
        // Store the token for future use
        PlayerPrefs.SetString("FirebaseToken", token.Token);
        PlayerPrefs.Save();
    }

    void OnMessageReceived(object sender, MessageReceivedEventArgs e)
    {
        Debug.Log("📨 Firebase message received!");
        
        // Extract notification data
        string title = e.Message.Notification?.Title ?? "Notification";
        string body = e.Message.Notification?.Body ?? "You have a new notification";
        
        Debug.Log($"Title: {title}, Body: {body}");
        
        // Handle data payload
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
                    case "point_used":
                        // Show in-app notification for points
                        DisplayInAppNotification(title, body);
                        break;
                    case "maintenance":
                        // Show maintenance notification
                        DisplayInAppNotification(title, body);
                        break;
                    default:
                        // Default case
                        DisplayInAppNotification(title, body);
                        break;
                }
            }
        }
    }

    // Display in-app notification
    private void DisplayInAppNotification(string title, string message)
    {
        // Implement in-app notification UI here
        Debug.Log($"📲 In-App Notification: {title} - {message}");
        
        // This would typically update some UI element in your game
        // If you have a notification panel, activate it here
    }

    void InitializeNotifications()
    {
#if UNITY_ANDROID
        AndroidNotificationChannel trainingChannel = new AndroidNotificationChannel()
        {
            Id = "training_reminders",
            Name = "Training Reminders",
            Importance = Importance.High,
            Description = "Notifications for scheduled training sessions"
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

        AndroidNotificationCenter.RegisterNotificationChannel(trainingChannel);
        AndroidNotificationCenter.RegisterNotificationChannel(maintenanceChannel);
        AndroidNotificationCenter.RegisterNotificationChannel(progressChannel);
#elif UNITY_IOS
        var timeTrigger = new iOSNotificationTimeIntervalTrigger()
        {
            TimeInterval = new TimeSpan(0, 0, 5),
            Repeats = false
        };

        var notification = new iOSNotification()
        {
            Identifier = "_notification_01",
            Title = "Hello",
            Body = "This is a test notification!",
            Subtitle = "Subtitle here",
            ShowInForeground = true,
            ForegroundPresentationOption = (PresentationOption.Alert | PresentationOption.Sound),
            Trigger = timeTrigger,
        };

        iOSNotificationCenter.ScheduleNotification(notification);
#endif
        Debug.Log("Notification system initialized");
    }

    // Existing notification methods...
    
    // Add Firebase notification method for points used
    public void NotifyPointUsedFirebase(string userId, string courseName)
    {
        if (!firebaseInitialized)
        {
            Debug.LogWarning("⚠️ Firebase not initialized yet");
            // Fall back to local notification
            NotifyPointUsed();
            return;
        }
        
        // Local notification (existing)
        NotifyPointUsed();
        
        // Send analytics event to Firebase (optional)
        // If you have Firebase Analytics set up
        /*
        Firebase.Analytics.FirebaseAnalytics.LogEvent(
            "points_used",
            new Firebase.Analytics.Parameter[] {
                new Firebase.Analytics.Parameter("user_id", userId),
                new Firebase.Analytics.Parameter("course_name", courseName),
                new Firebase.Analytics.Parameter("points", 1)
            }
        );
        */
    }

    // Your existing NotifyPointUsed method
    public void NotifyPointUsed()
    {
#if UNITY_ANDROID
        var notification = new AndroidNotification
        {
            Title = "Point Used",
            Text = "You've used 1 point to access this course.",
            SmallIcon = "icon_progress",
            FireTime = DateTime.Now.AddSeconds(1)
        };

        AndroidNotificationCenter.SendNotification(notification, "progress_updates");
        Debug.Log("📱 Local notification: Point Used");
#elif UNITY_IOS
        var notification = new iOSNotification
        {
            Title = "Point Used",
            Body = "You've used 1P to access this course.",
            ShowInForeground = true,
            Trigger = new iOSNotificationTimeIntervalTrigger
            {
                TimeInterval = new TimeSpan(0, 0, 1),
                Repeats = false
            }
        };

        iOSNotificationCenter.ScheduleNotification(notification);
        Debug.Log("📱 iOS notification: Point Used");
#endif
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
