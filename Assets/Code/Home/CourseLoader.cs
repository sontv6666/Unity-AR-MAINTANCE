using System;
using System.Collections;
using System.Collections.Generic;
using Code;
using UnityEngine;
using UnityEngine.Networking;
using UnityEngine.UI;
using TMPro;
using System.IO;
using Models;
using Newtonsoft.Json; 
public class CourseLoader: MonoBehaviour
{
    [Header("UI References")] 
    public TMP_Text greetingText;
    public TMP_Text usernameText;
    public TMP_Text pointsText ;
    public Image profileImage;
    public GameObject coursePanelPrefab; // Prefab for each course item
    public Transform contentParent; // Parent object to hold all course panels
    public GameObject nocourseText;
    public GameObject detailPage;
    public GameObject homePage;
    public GameObject seeAllPage; 
    private string userEndpoint = "/user/{0}"; // API to fetch user details
    [Header("API Settings")] 
    private string endpointTemplate = "/course/company/{0}?page=1&size=4";

    void Start()
    {
        SetGreetingMessage(); // ✅ Set greeting message based on time of day

        // ✅ Ensure UserId is set from PlayerPrefs
        if (string.IsNullOrEmpty(UserManager.UserId))
        {
            UserManager.UserId = PlayerPrefs.GetString("UserId", "");
        }
        
        if (string.IsNullOrEmpty(UserManager.CompanyId))
        {
            UserManager.CompanyId = PlayerPrefs.GetString("CompanyId", "");
        }


        if (!string.IsNullOrEmpty(UserManager.UserId))
        {
            string endpoint = string.Format(endpointTemplate, UserManager.UserId);
            Debug.Log($"CourseLoader: Fetching course data for userId: {UserManager.UserId}");
        
            // ✅ Wait for CompanyId before fetching courses
            StartCoroutine(WaitForCompanyIdAndFetchCourses());

            // ✅ Fetch user data
            string userEndpointFormatted = string.Format(userEndpoint, UserManager.UserId);
            Debug.Log($"CourseLoader: Fetching user data for userId: {UserManager.UserId}");
            StartCoroutine(FetchUserData(userEndpointFormatted));
        }
        else
        {
            Debug.LogError("CourseLoader: UserId is not set. Unable to fetch courses or user data!");
        }
    }

    
    IEnumerator WaitForCompanyIdAndFetchCourses()
    {
        
        while (string.IsNullOrEmpty(UserManager.CompanyId))
        {
            Debug.Log("⌛ Waiting for CompanyId...");
            yield return new WaitForSeconds(0.5f);
        }

        string endpoint = string.Format(endpointTemplate, UserManager.CompanyId);
        Debug.Log($"📡 Fetching courses for Company ID: {UserManager.CompanyId}");
        StartCoroutine(FetchCourseData(endpoint));
    }
    
    void SetGreetingMessage()
    {
        int hour = DateTime.Now.Hour;
        string greeting = "Hello"; // Default greeting

        if (hour >= 5 && hour < 12)
            greeting = "Good Morning!";
        else if (hour >= 12 && hour < 18)
            greeting = "Good Afternoon!";
        else
            greeting = "Good Evening!";

        greetingText.text = greeting;
    }

    IEnumerator FetchUserData(string endpoint)
    {
        string authToken = PlayerPrefs.GetString("AuthToken", "");
    
        if (string.IsNullOrEmpty(authToken))
        {
            Debug.LogError("❌ Auth token is missing! Cannot fetch user data.");
            yield break;
        }

        string fullUrl = ApiConfig.GetBaseUrl() + endpoint;
        Debug.Log($"🔑 Using Auth Token to Fetch User Data from: {fullUrl}");

        using (UnityWebRequest request = UnityWebRequest.Get(fullUrl))
        {
            request.downloadHandler = new DownloadHandlerBuffer();
            request.SetRequestHeader("Authorization", "Bearer " + authToken);
            request.SetRequestHeader("Content-Type", "application/json");

            yield return request.SendWebRequest();

            if (request.result == UnityWebRequest.Result.ConnectionError || request.result == UnityWebRequest.Result.ProtocolError)
            {
                Debug.LogError($"❌ User API Error: {request.error}");
            }
            else
            {
                string jsonData = request.downloadHandler.text;
                Debug.Log($"✅ User API Response: {jsonData}");

                try
                {
                    ProcessUserData(jsonData);
                }
                catch (Exception e)
                {
                    Debug.LogError($"❌ Error Parsing User Data: {e.Message}");
                }
            }
        }
    }



    void ProcessUserData(string jsonData)
    {
        ApiResponse<UserProfileResult> response = JsonUtility.FromJson<ApiResponse<UserProfileResult>>(jsonData);

        if (response != null && response.result != null)
        {
            UserProfileResult user = response.result;
            usernameText.text = user.username;
            pointsText.text= $"Points: {user.points}";
            Debug.Log($"👤 User: {user.username}");

            // ✅ Lưu company.id vào UserManager
            if (user.company != null && !string.IsNullOrEmpty(user.company.id))
            {
                UserManager.CompanyId = user.company.id;
                Debug.Log($"🏢 Company ID: {UserManager.CompanyId}");
            }
        
            if (!string.IsNullOrEmpty(user.avatar))
            {
                StartCoroutine(DownloadAndLoadProfileImage(user.avatar));
            }
        }
        else
        {
            Debug.LogError("❌ Failed to parse user data.");
        }
    }


    IEnumerator DownloadAndLoadProfileImage(string imageUrl)
    {
        if (string.IsNullOrEmpty(imageUrl))
        {
            Debug.LogError("Profile image URL is null or empty!");
            yield break;
        }

        string fullUrl = "/files/" + imageUrl;
        string filename = Path.GetFileName(imageUrl);
        string localPath = Path.Combine(Application.persistentDataPath, filename);

        if (File.Exists(localPath))
        {
            yield return LoadImageFromLocal(localPath, profileImage);
            yield break;
        }

        yield return StartCoroutine(DownloadFile(fullUrl, localPath));

        if (File.Exists(localPath))
        {
            yield return LoadImageFromLocal(localPath, profileImage);
        }
    }
    
    
    public void ReloadCourseData()
    {
        Start(); // Gọi lại Start() để load dữ liệu
    }
    
    IEnumerator FetchCourseData(string endpoint)
    {
        Debug.Log("CourseLoader: Sending request to API.");
        using (UnityWebRequest request = ApiConfig.CreateRequest(endpoint))
        {
            yield return request.SendWebRequest();

            if (request.result == UnityWebRequest.Result.ConnectionError ||
                request.result == UnityWebRequest.Result.ProtocolError)
            {
                Debug.LogError($"API Error: {request.error}");
            }
            else
            {
                string jsonData = request.downloadHandler.text;
                Debug.Log($"API Response: {jsonData}");

                ProcessCourseData(jsonData);
            }
        }
    }
    

    void ProcessCourseData(string jsonData)
    {
        Debug.Log("📡 CourseLoader: Processing course data.");
        Debug.Log($"📜 Raw JSON Data: {jsonData}"); // ✅ Debug JSON before parsing

        try
        {
            // ✅ Corrected to match new API structure
            var response = JsonConvert.DeserializeObject<ApiResponse<PaginationResult<CourseResult>>>(jsonData);

            if (response == null || response.result == null || response.result.objectList == null || response.result.objectList.Count == 0)
            {
                Debug.LogError("❌ No courses found or invalid response.");
                nocourseText.SetActive(true);
                return;
            }

            Debug.Log($"📌 Found {response.result.objectList.Count} courses.");

            // ✅ Clear old UI panels before adding new ones
            foreach (Transform child in contentParent)
            {
                Destroy(child.gameObject);
            }

            foreach (CourseResult course in response.result.objectList)
            {
                if (string.IsNullOrEmpty(course.title) || string.IsNullOrEmpty(course.description))
                {
                    Debug.LogWarning("⚠️ Course title or description is missing!");
                    continue;
                }

                // ✅ Shorten title and description
                string truncatedTitle = course.title.Length > 20 ? course.title.Substring(0, 20) + "..." : course.title;
                string truncatedDescription = course.description.Length > 50 ? course.description.Substring(0, 50) + "..." : course.description;

                course.title = truncatedTitle;
                course.description = truncatedDescription;

                nocourseText.SetActive(false);

                // ✅ Create the UI panel
                CreateCoursePanel(course);
            }
        }
        catch (Exception e)
        {
            Debug.LogError($"❌ JSON Parsing Error: {e.Message}\nRaw JSON: {jsonData}");
        }
    }



    void CreateCoursePanel(CourseResult course)
    {
        Debug.Log($"CourseLoader: Creating panel for course: {course.title}");
        GameObject panel = Instantiate(coursePanelPrefab, contentParent);

        TMP_Text titleText = panel.transform.Find("course_titleText").GetComponent<TMP_Text>();
        TMP_Text descriptionText = panel.transform.Find("course_descriptionText").GetComponent<TMP_Text>();
        TMP_Text scoreText = panel.transform.Find("course_scoreText").GetComponent<TMP_Text>();

        if (titleText != null) titleText.text = course.title;
        if (descriptionText != null) descriptionText.text = course.description;
        if (scoreText != null) scoreText.text = $"Lessons: {course.numberOfLessons}";

        Button courseButton = panel.GetComponent<Button>();
        if (courseButton != null)
        {
            courseButton.onClick.AddListener(() => OnCourseClicked(course.id));
        }

        if (!string.IsNullOrEmpty(course.imageUrl))
        {
            Image imageComponent = panel.transform.Find("courseImage_background/course_image").GetComponent<Image>();
            if (imageComponent != null)
            {
                StartCoroutine(DownloadAndLoadCourseImage(course.imageUrl, imageComponent));
            }
        }
    }

    IEnumerator DownloadAndLoadCourseImage(string imageUrl, Image imageComponent)
    {
        if (string.IsNullOrEmpty(imageUrl))
        {
            Debug.LogError("❌ Image URL is null or empty!");
            yield break;
        }

        // ✅ Use ApiConfig to get base URL
        string fullUrl = "/files/" + imageUrl;
        Debug.Log(fullUrl);
        string filename = Path.GetFileName(imageUrl);
        string localPath = Path.Combine(Application.persistentDataPath, filename);

        // ✅ Check if image is cached
        if (File.Exists(localPath))
        {
            Debug.Log($"📂 Loading cached image: {localPath}");
            yield return LoadImageFromLocal(localPath, imageComponent);
            yield break;
        }

        // ✅ Download image
        yield return StartCoroutine(DownloadFile(fullUrl, localPath));

        if (File.Exists(localPath))
        {
            yield return LoadImageFromLocal(localPath, imageComponent);
        }
    }


    

    IEnumerator DownloadFile(string url, string localPath)
    {
        Debug.Log($"🌐 Attempting to download: {url}");
   
        using (UnityWebRequest request = ApiConfig.CreateRequest(url))
        {
            yield return request.SendWebRequest();

            if (request.result == UnityWebRequest.Result.ConnectionError || request.result == UnityWebRequest.Result.ProtocolError)
            {
                Debug.LogError($"❌ Download Error: {request.error} \nURL: {url}");
            }
            else
            {
                File.WriteAllBytes(localPath, request.downloadHandler.data);
                Debug.Log($"✅ Downloaded and saved: {localPath}");
            }
        }
    }



    IEnumerator LoadImageFromLocal(string path, Image imageComponent)
    {
        if (!File.Exists(path))
        {
            Debug.LogError($"❌ Image file not found at path: {path}");
            yield break;
        }

        Debug.Log($"📂 Loading image from: {path}");

        byte[] imageData = File.ReadAllBytes(path);
        if (imageData == null || imageData.Length == 0)
        {
            Debug.LogError($"❌ Failed to read image data from: {path}");
            yield break;
        }

        Texture2D texture = new Texture2D(2, 2);
        bool isLoaded = texture.LoadImage(imageData);

        if (!isLoaded)
        {
            Debug.LogError("❌ Failed to load image data into Texture2D!");
            yield break;
        }

        if (imageComponent == null)
        {
            Debug.LogError("❌ Image component is null! Cannot assign sprite.");
            yield break;
        }

        imageComponent.sprite = Sprite.Create(texture, new Rect(0, 0, texture.width, texture.height), Vector2.one * 0.5f);
        Debug.Log("✅ Image successfully loaded and applied to UI.");
    
        yield return null;
    }



  

    public void OnCourseClicked(string courseId)
    {
        Debug.Log($"CourseLoader: Course clicked with ID {courseId}");
        CourseManager.SelectedCourseId = courseId;

        if (detailPage != null)
        {
            detailPage.SetActive(true);
        }
        else
        {
            Debug.LogError("CourseLoader: DetailPage is not assigned!");
        }

        CourseDetailLoader courseDetailLoader = detailPage.GetComponent<CourseDetailLoader>();
        if (courseDetailLoader != null)
        {
            courseDetailLoader.LoadCourseDetails(courseId);
        }
        else
        {
            Debug.LogError("CourseLoader: CourseDetailLoader component is missing on DetailPage!");
        }
    }

    public void GoToSeeAllPage()
    {
        Debug.Log("📌 Navigating to See All Page...");

        if (homePage != null)
        {
            homePage.SetActive(false); // Hide Home Page
        }

        if (seeAllPage != null)
        {
            seeAllPage.SetActive(true); // Show See All Page
        }
    }
}




