using System;
using UnityEngine;
#if UNITY_ANDROID
using Unity.Notifications.Android;
#elif UNITY_IOS
using Unity.Notifications.iOS;
#endif

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

    void Awake()
    {
        if (_instance == null)
        {
            _instance = this;
            DontDestroyOnLoad(gameObject);
            InitializeNotifications();
        }
        else if (_instance != this)
        {
            Destroy(gameObject);
        }
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
        iOSNotificationCenter.RequestAuthorization(AuthorizationOption.Alert |
                                                 AuthorizationOption.Badge |
                                                 AuthorizationOption.Sound);
#endif
        Debug.Log("Notification system initialized");
    }

    public void ScheduleTrainingReminder(string courseName, DateTime scheduleTime)
    {
#if UNITY_ANDROID
        var notification = new AndroidNotification
        {
            Title = "Training Reminder",
            Text = $"Don't forget your scheduled training: {courseName}",
            SmallIcon = "icon_training",
            LargeIcon = "icon_maintenance",
            FireTime = scheduleTime
        };

        int id = AndroidNotificationCenter.SendNotification(notification, "training_reminders");
        PlayerPrefs.SetInt($"notification_{courseName}", id);
        Debug.Log($"Scheduled training reminder for {courseName} at {scheduleTime}");
#elif UNITY_IOS
        var notification = new iOSNotification
        {
            Title = "Training Reminder",
            Body = $"Don't forget your scheduled training: {courseName}",
            ShowInForeground = true,
            Badge = 1,
            Trigger = new iOSNotificationTimeIntervalTrigger
            {
                TimeInterval = (scheduleTime - DateTime.Now),
                Repeats = false
            }
        };

        iOSNotificationCenter.ScheduleNotification(notification);
        Debug.Log($"Scheduled iOS training reminder for {scheduleTime}");
#endif
    }

    public void ScheduleMaintenanceAlert(string equipmentName, string instructionName, DateTime scheduleTime)
    {
#if UNITY_ANDROID
        var notification = new AndroidNotification
        {
            Title = "Maintenance Required",
            Text = $"Time to perform maintenance on {equipmentName}: {instructionName}",
            SmallIcon = "icon_alert",
            LargeIcon = "icon_wrench",
            FireTime = scheduleTime
        };

        AndroidNotificationCenter.SendNotification(notification, "maintenance_alerts");
        Debug.Log($"Scheduled maintenance alert for {equipmentName} at {scheduleTime}");
#elif UNITY_IOS
        var notification = new iOSNotification
        {
            Title = "Maintenance Required",
            Body = $"Time to perform maintenance on {equipmentName}: {instructionName}",
            ShowInForeground = true,
            Badge = 1,
            Trigger = new iOSNotificationTimeIntervalTrigger
            {
                TimeInterval = (scheduleTime - DateTime.Now),
                Repeats = false
            }
        };

        iOSNotificationCenter.ScheduleNotification(notification);
#endif
    }

    public void SendProgressUpdate(string courseName, int completedSteps, int totalSteps)
    {
        float progressPercentage = (float)completedSteps / totalSteps * 100;

#if UNITY_ANDROID
        var notification = new AndroidNotification
        {
            Title = "Training Progress Update",
            Text = $"You've completed {progressPercentage:0}% of {courseName}",
            SmallIcon = "icon_progress",
            FireTime = DateTime.Now.AddSeconds(5)
        };

        AndroidNotificationCenter.SendNotification(notification, "progress_updates");
#elif UNITY_IOS
        var notification = new iOSNotification
        {
            Title = "Training Progress Update",
            Body = $"You've completed {progressPercentage:0}% of {courseName}",
            ShowInForeground = true,
            Trigger = new iOSNotificationTimeIntervalTrigger
            {
                TimeInterval = new TimeSpan(0, 0, 5),
                Repeats = false
            }
        };

        iOSNotificationCenter.ScheduleNotification(notification);
#endif
    }

    // ✅ NEW METHOD FOR POINT USAGE
    public void NotifyPointUsed()
    {
#if UNITY_ANDROID
        var notification = new AndroidNotification
        {
            Title = "Point Used",
            Text = "You’ve used 1 points to access this course.",
            SmallIcon = "icon_progress",
            FireTime = DateTime.Now.AddSeconds(1)
        };

        AndroidNotificationCenter.SendNotification(notification, "progress_updates");
#elif UNITY_IOS
        var notification = new iOSNotification
        {
            Title = "Point Used",
            Body = "You’ve used 1P to access this course.",
            ShowInForeground = true,
            Trigger = new iOSNotificationTimeIntervalTrigger
            {
                TimeInterval = new TimeSpan(0, 0, 1),
                Repeats = false
            }
        };

        iOSNotificationCenter.ScheduleNotification(notification);
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
