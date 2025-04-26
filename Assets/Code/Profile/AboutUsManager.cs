using UnityEngine;

public class AboutUsManager : MonoBehaviour
{
    public GameObject profilePage;

    private void Start()
    {
    }

    public void OpenBrowser()
    {
        Debug.Log("📂 Opening Browser ...");
        
        // Optional: Hide the profile page
        if (profilePage != null)
            profilePage.SetActive(false);

        // Open the external browser
        Application.OpenURL("https://ar-maintance-guideline-ui.vercel.app/");
    }
}