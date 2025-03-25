using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using Code;

public class ScreenManager : MonoBehaviour
{
    [Header("📌 UI Screens")]
    public List<RectTransform> screens; // ✅ Supports RectTransform-based UI
    private int currentScreenIndex = 0;
    private bool isTransitioning = false; // ✅ Prevents multiple transitions

    
    
    public SceneNavigator sceneNavigator; // ✅ Assign in Inspector
    public float transitionSpeed = 2f; // ✅ Adjust speed for smooth transitions
    public float splashDuration = 3f; // ⏳ Adjustable Splash Screen duration

    private void Start()
    {
      
        
        if (screens == null || screens.Count == 0)
        {
            Debug.LogError("❌ No screens assigned in ScreenManager! Check Inspector.");
            return;
        }

        // ✅ Show Splash Screen (First screen in the list)
        ShowScreen(screens[0].name);

        // ✅ Auto Transition after Splash
        StartCoroutine(AutoTransitionFromSplash());
    }

    private IEnumerator AutoTransitionFromSplash()
    {
        yield return new WaitForSeconds(splashDuration); // ⏳ Wait for splash duration

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
            Button button = screens[i].GetComponentInChildren<Button>(); // 🔍 Find Button in each screen
            if (button != null)
            {
                int nextIndex = i + 1; // Set next screen index
                button.onClick.RemoveAllListeners();
                button.onClick.AddListener(() => GoToScreen(nextIndex)); // ✅ Assign click action
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
            courseLoader?.ReloadCourseData();

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
