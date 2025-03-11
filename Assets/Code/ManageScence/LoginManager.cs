using UnityEngine;
using UnityEngine.Networking;
using TMPro; // Include TextMeshPro namespace
using System.Collections;

public class LoginManager : MonoBehaviour
{
    public TMP_InputField usernameInput; // Reference to the username input field
    public TMP_InputField passwordInput; // Reference to the password input field
    public TMP_Text warningText;         // Reference to the warning text

    public GameObject loginCanvas;      // Reference to the Login Canvas
    public GameObject homeCanvas;       // Reference to the Homepage Canvas

    public GameObject profileCanvas;

    private const string LOGIN_URL = "https://joey-lenient-ostrich.ngrok-free.app/api/v1/login"; // Login API endpoint
    
    public static string UserId; // Static variable to store the user ID

    // Method called when the "Enter" button is clicked
    public void OnLogin()
    {
        string username = usernameInput.text;
        string password = passwordInput.text;

        // Start the login coroutine
        StartCoroutine(LoginRequesAPI(username, password));
    }

    // Coroutine to send login request
   private IEnumerator LoginRequesAPI(string email, string password)
{
    string jsonBody = JsonUtility.ToJson(new LoginRequest { email = email, password = password });

    using (UnityWebRequest request = UnityWebRequest.PostWwwForm(LOGIN_URL, ""))
    {
        byte[] jsonToSend = new System.Text.UTF8Encoding().GetBytes(jsonBody);
        request.uploadHandler = new UploadHandlerRaw(jsonToSend);
        request.downloadHandler = new DownloadHandlerBuffer();
        request.SetRequestHeader("Content-Type", "application/json");

        yield return request.SendWebRequest();

        if (request.result == UnityWebRequest.Result.Success)
        {
            Debug.Log("Response: " + request.downloadHandler.text);

            LoginResponse response = JsonUtility.FromJson<LoginResponse>(request.downloadHandler.text);

            if (response.code == 1000 && response.result.message != "Login failed"  && response.result.token != null && response.result.user != null)
            {
                Debug.Log("Login successful!");

                
                // Sau khi đăng nhập thành công:
                PlayerPrefs.SetString("UserId", response.result.user.id);
                PlayerPrefs.Save();

                // Populate UserManager with user details
                UserManager.Token = response.result.token;
                UserManager.UserId = response.result.user.id;
                UserManager.RoleId = response.result.user.role.id;
                UserManager.RoleName = response.result.user.role.roleName;
                UserManager.CompanyId = response.result.user.company.id;
                UserManager.CompanyName = response.result.user.company.companyName;
                UserManager.Email = response.result.user.email;
                UserManager.Avatar = response.result.user.avatar;
                UserManager.Username = response.result.user.username;
                UserManager.Phone = response.result.user.phone;
                UserManager.Status = response.result.user.status;
                UserManager.ExpirationDate = response.result.user.expirationDate;
                UserManager.IsPayAdmin = response.result.user.isPayAdmin;
                UserManager.CreatedDate = response.result.user.createdDate;
                UserManager.UpdatedDate = response.result.user.updatedDate;

                warningText.text = "";
                ClearInputs();
                SwitchToHomePage();
            }
            else
            {
                warningText.text = "Error: " + response.result.message;
                Debug.LogWarning("Login failed: " + response.result.message);
                ClearInputs();
            }
        }
        else
        {
            Debug.LogError("Error: " + request.error);
            warningText.text = "Error: Unable to connect to the server!";
        }
    }
}


    // Method to clear the input fields
    private void ClearInputs()
    {
        usernameInput.text = "";
        passwordInput.text = "";
    }

    // Switch to the homepage canvas
    private void SwitchToHomePage()
    {
        // Disable the login canvas
        loginCanvas.SetActive(false);
        
        // Enable the homepage canvas
        homeCanvas.SetActive(true);
        
        profileCanvas.SetActive(true);
        
    }

    // Classes for serializing/deserializing JSON data
    [System.Serializable]
    private class LoginRequest
    {
        public string email;
        public string password;
    }

    [System.Serializable]
    private class LoginResponse
    {
        public int code;
        public string message;
        public ResultData result;

        [System.Serializable]
        public class ResultData
        {
            public string token;
            public string message;
            public User user;

            [System.Serializable]
            public class User
            {
                public string id;
                public Role role;
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

                [System.Serializable]
                public class Role
                {
                    public string id;
                    public string roleName;
                }

                [System.Serializable]
                public class Company
                {
                    public string id;
                    public string companyName;
                }
            }
        }
    }
}
