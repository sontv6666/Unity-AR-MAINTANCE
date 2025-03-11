using UnityEngine;
using System.Collections.Generic;

public class SceneNavigator : MonoBehaviour
{
    [Header("UI Screens")]
    public List<GameObject> screens; // Danh sách tất cả màn hình (gán trong Inspector)

    private void Start()
    {
        // Đảm bảo chỉ có màn hình HomePage bật lúc đầu
        ShowScreen("HomePage");
    }
    
    
    
    public void NavigateToProfile()
    {
        Debug.Log("Navigating to ProfileScreen...");
        ShowScreen("ProfileScreen");

        // 🔥 Reload profile data when navigating to ProfileScreen
        ProfileScreenLoader profileLoader = FindObjectOfType<ProfileScreenLoader>();
        if (profileLoader != null)
        {
            profileLoader.ReloadUserInfo();
            Debug.Log("✅ Profile data reloaded!");
        }
        else
        {
            Debug.LogError("❌ ProfileScreenLoader not found!");
        }
    }


    public void NavigateToExplore()
    {
        Debug.Log("Navigating to ExploreScreen...");
        ShowScreen("HomePage");

        // 🔥 Gọi lại `ReloadCourseData()` khi vào HomePage
        CourseLoader courseLoader = FindObjectOfType<CourseLoader>();
        if (courseLoader != null)
        {
            courseLoader.ReloadCourseData();
            Debug.Log("✅ Course data reloaded!");
        }
        else
        {
            Debug.LogError("❌ CourseLoader not found!");
        }
    }
    
    public void NavigateToSearch()
    {
        Debug.Log("Navigating to SearchScreen...");
        ShowScreen("SearchScreen");
    }

    private void ShowScreen(string screenName)
    {
        bool found = false;
        foreach (GameObject screen in screens)
        {
            if (screen.name == screenName)
            {
                screen.SetActive(true);
                found = true;
                Debug.Log($"✅ {screenName} activated.");
            }
            else
            {
                screen.SetActive(false);
            }
        }

        if (!found)
        {
            Debug.LogError($"❌ Screen '{screenName}' not found in list!");
        }
    }
}