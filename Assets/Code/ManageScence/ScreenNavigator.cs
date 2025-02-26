using UnityEngine;

public class SceneNavigator : MonoBehaviour
{
    // This method activates the ProfileScreen and deactivates other screens.
    public void NavigateToProfile()
    {
        Debug.Log("Navigating to ProfileScreen...");

        // Activate ProfileScreen
        GameObject profileScreen = GameObject.Find("ProfileScreen");
        if (profileScreen != null)
        {
            profileScreen.SetActive(true);
            Debug.Log("ProfileScreen activated.");
        }
        else
        {
            Debug.LogError("ProfileScreen not found!");
        }

        // Deactivate other screens
        DeactivateScreen("HomePage");
        Debug.Log("Navigated to ProfileScreen");
    }

    // This method activates the Explore/HomePage and deactivates other screens.
    public void NavigateToExplore()
    {
        Debug.Log("Navigating to ExploreScreen...");

        // Activate HomePage (ExploreScreen)
        GameObject exploreScreen = GameObject.Find("HomePage");
        if (exploreScreen != null)
        {
            exploreScreen.SetActive(true);
            Debug.Log("HomePage activated.");
        }
        else
        {
            Debug.LogError("HomePage not found!");
        }

        // Deactivate other screens
        DeactivateScreen("ProfileScreen");
        Debug.Log("Navigated to ExploreScreen");
    }

    // Helper method to deactivate a screen by name.
    private void DeactivateScreen(string screenName)
    {
        GameObject screen = GameObject.Find(screenName);
        if (screen != null)
        {
            screen.SetActive(false);
            Debug.Log($"{screenName} deactivated.");
        }
        else
        {
            Debug.LogError($"{screenName} not found!");
        }
    }
}