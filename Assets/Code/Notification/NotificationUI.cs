using UnityEngine;
using UnityEngine.UI;
using TMPro;
using System.Collections;

public class NotificationUI : MonoBehaviour
{
    public static NotificationUI Instance;
    
    [Header("UI References")]
    public GameObject notificationPanel;
    public TMP_Text titleText;
    public TMP_Text messageText;
    public Button closeButton;
    public float displayTime = 5f; // Auto-hide after 5 seconds
    
    private void Awake()
    {
        if (Instance == null)
        {
            Instance = this;
            DontDestroyOnLoad(gameObject);
        }
        else if (Instance != this)
        {
            Destroy(gameObject);
        }
        
        // Hide notification panel initially
        if (notificationPanel != null)
        {
            notificationPanel.SetActive(false);
        }
        
        // Add close button listener
        if (closeButton != null)
        {
            closeButton.onClick.AddListener(HideNotification);
        }
    }
    
    public void ShowNotification(string title, string message)
    {
        // Stop any running coroutines
        StopAllCoroutines();
        
        // Set text
        if (titleText != null)
            titleText.text = title;
        
        if (messageText != null)
            messageText.text = message;
        
        // Show panel
        if (notificationPanel != null)
            notificationPanel.SetActive(true);
        
        // Auto-hide after delay
        StartCoroutine(AutoHideNotification());
    }
    
    public void HideNotification()
    {
        if (notificationPanel != null)
            notificationPanel.SetActive(false);
    }
    
    private IEnumerator AutoHideNotification()
    {
        yield return new WaitForSeconds(displayTime);
        HideNotification();
    }
}