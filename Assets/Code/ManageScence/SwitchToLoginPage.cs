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
    private bool isFirstScreen = true; // ✅ Prevents multiple fast taps

    public SceneNavigator sceneNavigator; // ✅ Assign in Inspector
    public float transitionSpeed = 2f; // ✅ Adjust speed for smooth transitions

    private void Start()
    {
        // 🔥 Check if user is already logged in
        if (IsUserLoggedIn())
        {
            Debug.Log("✅ User already logged in! Skipping intro screens...");
            RestoreLastPage();
        }
        else
        {
            Debug.Log("🔄 New user or session expired. Showing first screen.");
            ShowScreen(screens[0].name);
        }
    }

    private bool IsUserLoggedIn()
    {
        string userId = PlayerPrefs.GetString("UserId", "");
        return !string.IsNullOrEmpty(userId);
    }

    public void GoToNextScreen()
    {
        if (currentScreenIndex < screens.Count - 3)
        {
            int nextScreenIndex = currentScreenIndex + 1;
            StartCoroutine(SlideTransition(nextScreenIndex));
        }
        else
        {
            Debug.Log("🔄 Reached the last screen!");
        }
    }

    private IEnumerator SlideTransition(int nextScreenIndex)
    {
        RectTransform previousScreen = screens[currentScreenIndex];
        RectTransform nextScreen = screens[nextScreenIndex];

        nextScreen.gameObject.SetActive(true);

        Vector3 startPosition = previousScreen.anchoredPosition;
        Vector3 targetPosition = new Vector3(-nextScreen.rect.width, 0, 0);

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
            if (courseDetailLoader != null)
            {
                courseDetailLoader.LoadCourseDetails(lastCourseId);
            }
        
            int detailPageIndex = screens.FindIndex(s => s.name == "DetailPage");
            currentScreenIndex = (detailPageIndex >= 0) ? detailPageIndex : 0; // ✅ Ensure valid index
        }
        else
        {
            Debug.Log("🏠 Restoring Home Page...");
            ShowScreen("HomePage");

            CourseLoader courseLoader = FindObjectOfType<CourseLoader>();
            if (courseLoader != null)
            {
                courseLoader.ReloadCourseData();
            }
        
            int homePageIndex = screens.FindIndex(s => s.name == "HomePage");
            currentScreenIndex = (homePageIndex >= 0) ? homePageIndex : 0; // ✅ Ensure valid index
        }

        // 🛑 Reset PlayerPrefs to prevent conflicts on next launch
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
    }
}
