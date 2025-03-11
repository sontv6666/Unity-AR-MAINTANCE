using System.Collections.Generic;
using Code;
using UnityEngine;

public class ScreenManager : MonoBehaviour
{
    [Header("Screens")] public List<GameObject> screens; // Drag and drop all UI screens here in the Inspector

    private void Start()
    {
        // Optional: Hide all screens at the start except the first one
        if (screens.Count > 0)
        {
            ShowScreen(screens[0].name);
        }
        
     
            RestoreLastPage();
        

    }
    
    private void RestoreLastPage()
    {
        bool showHome = PlayerPrefs.GetInt("ShowHomePage", 0) == 1;
        bool showDetail = PlayerPrefs.GetInt("ShowDetailPage", 0) == 1;
        string lastCourseId = PlayerPrefs.GetString("SelectedCourseID", "");

        if (showHome)
        {
            ShowScreen("HomePage"); // ✅ Show HomePage
        }

        if (showDetail)
        {
            ShowScreen("DetailPage"); // ✅ Show DetailPage

            // ✅ Reload course details if we have a saved Course ID
            CourseDetailLoader courseDetailLoader = FindObjectOfType<CourseDetailLoader>();
            if (courseDetailLoader != null && !string.IsNullOrEmpty(lastCourseId))
            {
                courseDetailLoader.LoadCourseDetails(lastCourseId);
            }
        }

        // ✅ Clear stored values after restoring
        PlayerPrefs.SetInt("ShowHomePage", 0);
        PlayerPrefs.SetInt("ShowDetailPage", 0);
        PlayerPrefs.SetString("SelectedCourseID", ""); 
        PlayerPrefs.Save();
    }


    // Show a specific screen and hide all others
    public void ShowScreen(string screenName)
    {
        foreach (GameObject screen in screens)
        {
            screen.SetActive(screen.name == screenName);
        }
    }

    // Turn off a specific screen without showing another
    public void TurnOffScreen(string screenName)
    {
        foreach (GameObject screen in screens)
        {
            if (screen.name == screenName)
            {
                screen.SetActive(false);
                break;
            }
        }
    }

    // Turn off all screens
    public void TurnOffAllScreens()
    {
        foreach (GameObject screen in screens)
        {
            screen.SetActive(false);
        }
    }

    public void ReturnHomeFromDetailpage()
    {
        foreach (GameObject screen in screens)
        {
            if (screen.name == "DetailPage")
            {
                screen.SetActive(false); // Hide DetailPage
            }
            else if (screen.name == "HomePage")
            {
                screen.SetActive(true); // Show HomePage
            }
        }
    }
}