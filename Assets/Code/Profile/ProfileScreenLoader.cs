using UnityEngine;
using TMPro;

public class ProfileScreenLoader : MonoBehaviour
{
    [Header("UI References")]
    public TMP_Text nameText;       // Reference to the text component for the name
    public TMP_Text companyText;    // Reference to the text component for the company

    void Start()
    {
        LoadUserInfo();
    }

    void LoadUserInfo()
    {
        // Ensure UserManager has data
        if (!string.IsNullOrEmpty(UserManager.Username) && !string.IsNullOrEmpty(UserManager.CompanyName))
        {
            Debug.Log($"Loading user info: Username = {UserManager.Username}, Company = {UserManager.CompanyName}");

            // Assign values from UserManager
            nameText.text = UserManager.Username;
            companyText.text = UserManager.CompanyName;
        }
        else
        {
            Debug.LogWarning("UserManager data is missing or incomplete.");
        }
    }
}