using System.Collections.Generic;
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