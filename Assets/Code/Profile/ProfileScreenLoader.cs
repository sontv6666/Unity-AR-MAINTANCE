using UnityEngine;
using UnityEngine.Networking;
using TMPro;
using System.Collections;
using System;

public class ProfileScreenLoader : MonoBehaviour
{
    [Header("UI References")] 
    public TMP_Text nameText;
    public TMP_Text companyText;
    public TMP_Text emailText;
    public TMP_Text roleText;
    public TMP_Text phoneText;
    public UnityEngine.UI.Image avatarImage;

    private string userId;
    private string authToken;

    void Start()
    {
        userId = PlayerPrefs.GetString("UserId", ""); 
        authToken = PlayerPrefs.GetString("AuthToken", "");
        if (!string.IsNullOrEmpty(userId) && !string.IsNullOrEmpty(authToken))
        {
            StartCoroutine(FetchUserProfile(userId, authToken));
        }
        else
        {
            Debug.LogError("❌ No User ID found! Cannot load profile.");
        }
        
    }

    public void ReloadUserInfo()
    {
        if (!string.IsNullOrEmpty(userId))
        {
            StartCoroutine(FetchUserProfile(userId, authToken));
        }
    }

    IEnumerator FetchUserProfile(string userId, string authToken)
    {
        string endpoint = "https://joey-lenient-ostrich.ngrok-free.app/api/v1/user/" + userId;
        UnityWebRequest request = UnityWebRequest.Get(endpoint);
        request.SetRequestHeader("Authorization", "Bearer " + authToken);

        yield return request.SendWebRequest();

        if (request.result == UnityWebRequest.Result.Success)
        {
            string jsonResponse = request.downloadHandler.text;
            UserProfileResponse response = JsonUtility.FromJson<UserProfileResponse>(jsonResponse);

            if (response != null && response.code == 1000 && response.result != null)
            {
                UpdateUserInfo(response.result);
            }
            else
            {
                Debug.LogError("❌ Invalid API response.");
            }
        }
        else
        {
            Debug.LogError("❌ API Request Failed: " + request.error);
        }
    }

    void UpdateUserInfo(UserProfile user)
    {
        if (gameObject.activeInHierarchy)
        {
            nameText.text = user.username;
            companyText.text = user.company.companyName;
            emailText.text = user.email;
            roleText.text = user.roleName;
            phoneText.text = user.phone;

            StartCoroutine(LoadAvatarImage(user.avatar));
        }
    }

    IEnumerator LoadAvatarImage(string url)
    {
        UnityWebRequest request = UnityWebRequestTexture.GetTexture(url);
        yield return request.SendWebRequest();

        if (request.result == UnityWebRequest.Result.Success)
        {
            Texture2D texture = DownloadHandlerTexture.GetContent(request);
            avatarImage.sprite = Sprite.Create(texture, new Rect(0, 0, texture.width, texture.height),
                new Vector2(0.5f, 0.5f));
        }
        else
        {
            Debug.LogError("❌ Failed to load avatar image: " + request.error);
        }
    }
}

// ✅ Move these classes OUTSIDE of the ProfileScreenLoader class
[Serializable]
public class UserProfileResponse
{
    public int code;
    public UserProfile result;
}

[Serializable]
public class UserProfile
{
    public string id;
    public Role role;
    public string roleName;
    public Company company;
    public string email;
    public string avatar;
    public string username;
    public string phone;
    public string status;
    public string expirationDate;
    public bool isPayAdmin;
    public string createdDate;
    public string updatedDate;
    public string deviceId;
}

[Serializable]
public class Role
{
    public string id;
    public string roleName;
}

[Serializable]
public class Company
{
    public string id;
    public string companyName;
}

[Serializable]
public class UpdateUserDeviceRequest
{
    public string id;
    public string deviceId;
}
