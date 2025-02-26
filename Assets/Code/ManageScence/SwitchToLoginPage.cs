using System.Collections.Generic;
using UnityEngine;

public class ScreenManagers : MonoBehaviour
{
    // List of all UI canvases/screens
    public List<GameObject> screens;

    // Current active screen index
    private int currentScreenIndex = 0;

    void Start()
    {
        // Ensure only the first screen is active initially
        for (int i = 0; i < screens.Count; i++)
        {
            if (screens[i] != null)
            {
                screens[i].SetActive(i == currentScreenIndex);
            }
        }
    }

    void Update()
    {
        // Detect touch input (mobile) or mouse click (PC)
        if (Input.GetMouseButtonDown(0) || Input.touchCount > 0)
        {
            // Proceed to the next screen, but only if it's not the last screen
            if (currentScreenIndex < screens.Count - 1)
            {
                GoToNextScreen();
            }
            else
            {
                Debug.Log("Reached the last screen. No more screens to show.");
            }
        }
    }

    public void GoToNextScreen()
    {
        // Calculate the next screen index (loop back to the first screen if at the end)
        int nextScreenIndex = (currentScreenIndex + 1) % screens.Count;

        // Switch to the next screen
        SwitchToScreen(nextScreenIndex);
    }

    public void SwitchToScreen(int screenIndex)
    {
        // Validate the screenIndex
        if (screenIndex < 0 || screenIndex >= screens.Count)
        {
            Debug.LogWarning("Invalid screen index: " + screenIndex);
            return;
        }

        // Disable the current screen and activate the new one
        if (screens[currentScreenIndex] != null)
        {
            screens[currentScreenIndex].SetActive(false);
        }

        if (screens[screenIndex] != null)
        {
            screens[screenIndex].SetActive(true);

            // Optional: Ensure the canvas is set to Screen Space - Overlay
            Canvas canvasComponent = screens[screenIndex].GetComponent<Canvas>();
            if (canvasComponent != null)
            {
                canvasComponent.renderMode = RenderMode.ScreenSpaceOverlay;
            }
        }

        // Update the current screen index
        currentScreenIndex = screenIndex;
    }
}
