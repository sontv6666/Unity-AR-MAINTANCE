using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class ScreenManagers : MonoBehaviour
{
    public List<RectTransform> screens; // 🛠 Use RectTransform instead of GameObject
    private int currentScreenIndex = 0;
    private bool isFirstScreen = true; // Track if we are on the first screen

    public SceneNavigator sceneNavigator; // Assign in Inspector
    public float transitionSpeed = 2f; // 🛠 Adjust speed for smooth transition

    void Start()
    {
        // Ensure only the first screen is visible
        for (int i = 0; i < screens.Count; i++)
        {
            if (screens[i] != null)
            {
                screens[i].gameObject.SetActive(i == currentScreenIndex);
            }
        }
    }

    void Update()
    {
        if (isFirstScreen && (Input.GetMouseButtonDown(0) || Input.touchCount > 0))
        {
            // Prevent multiple rapid clicks
            isFirstScreen = false;
            StartCoroutine(GoToNextScreenWithDelay());
        }
    }

    private IEnumerator GoToNextScreenWithDelay()
    {
        yield return new WaitForSeconds(1f); // 👈 Adjust delay as needed (1 second here)
        GoToNextScreen();
    }

    public void GoToNextScreen()
    {
        Debug.Log("Current Screen: " + currentScreenIndex);

        if (currentScreenIndex < screens.Count - 1)
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

        AssignButtonEvents(nextScreenIndex);

        if (screens[nextScreenIndex].name == "LoginPage" && sceneNavigator != null)
        {
            sceneNavigator.gameObject.SetActive(true);
            Debug.Log("✅ SceneNavigator Activated!");
        }
    }

    private void AssignButtonEvents(int screenIndex)
    {
        if (screenIndex >= screens.Count - 1)
        {
            Debug.Log($"🚫 No button assignment for the last screen: {screens[screenIndex].name}");
            return; // ❌ Skip assigning buttons for the last page (LoginPage)
        }

        Button nextButton = screens[screenIndex].GetComponentInChildren<Button>();
        if (nextButton != null)
        {
            nextButton.onClick.RemoveAllListeners();
            nextButton.onClick.AddListener(() => GoToNextScreen());
            Debug.Log($"✅ Button reassigned on Screen {screenIndex}");
        }
        else
        {
            Debug.LogWarning($"⚠ No button found on Screen {screenIndex}");
        }
    }

}



    