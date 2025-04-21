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
using UnityEngine.SceneManagement;

public class CourseLoader: MonoBehaviour
{
    [Header("UI References")] 
    public TMP_Text greetingText;
    public TMP_Text usernameText;
    public TMP_Text pointsText;
    public TMP_Text roleText; // New: Display user role
    public TMP_Text companyNameText; // New: Display company name
    public TMP_Text statusText; // New: Display user status
    public Image profileImage;
    public GameObject coursePanelPrefab; // Prefab for each course item
    public Transform contentParent; // Parent object to hold all course panels
    public GameObject nocourseText;
    public GameObject detailPage;
    public GameObject homePage;
    public GameObject seeAllPage;
    
    [Header("Course Filtering")]
    public TMP_Dropdown courseFilterDropdown; // New: Filter dropdown
    public Toggle showMandatoryOnlyToggle; // New: Filter for mandatory courses
    
    [Header("Pagination Controls")]
    public Button nextPageButton; // New: Next page button
    public Button prevPageButton; // New: Previous page button
    public TMP_Text pageInfoText; // New: Shows current page/total pages
    private int currentPage = 1;
    private int pageSize = 3;
    
    [Header("Dashboard Stats")]
    public TMP_Text totalCoursesText; // New: Show total available courses
    public TMP_Text courseTypeText; // New: Show types of courses available
    public Image courseProgressFill; // New: Visual progress indicator
    
    [Header("API Settings")] 
    private string userEndpoint = "/user/{0}"; // API to fetch user details
    private string endpointTemplate = "/course/company/{0}?page={1}&size={2}&status=ACTIVE";

    void Start()
    {
        SetGreetingMessage();

        // Ensure UserId is set from PlayerPrefs
        if (string.IsNullOrEmpty(UserManager.UserId))
        {
            UserManager.UserId = PlayerPrefs.GetString("UserId", "");
        }
        
        if (string.IsNullOrEmpty(UserManager.CompanyId))
        {
            UserManager.CompanyId = PlayerPrefs.GetString("CompanyId", "");
        }

        // Initialize UI controls
        SetupFilteringControls();

        if (!string.IsNullOrEmpty(UserManager.UserId))
        {
            string endpoint = string.Format(endpointTemplate, UserManager.UserId, currentPage, pageSize);
            Debug.Log($"CourseLoader: Fetching course data for userId: {UserManager.UserId}");
        
            // Wait for CompanyId before fetching courses
            StartCoroutine(WaitForCompanyIdAndFetchCourses());

            // Fetch user data
            string userEndpointFormatted = string.Format(userEndpoint, UserManager.UserId);
            Debug.Log($"CourseLoader: Fetching user data for userId: {UserManager.UserId}");
            StartCoroutine(FetchUserData(userEndpointFormatted));
        }
        else
        {
            Debug.LogError("CourseLoader: UserId is not set. Unable to fetch courses or user data!");
        }
    }

    void SetupFilteringControls()
    {
        // Setup course filter dropdown if it exists
        if (courseFilterDropdown != null)
        {
            courseFilterDropdown.ClearOptions();
            courseFilterDropdown.AddOptions(new List<string> { "All Courses", "Active Only", "By Duration", "By Type" });
            courseFilterDropdown.onValueChanged.AddListener(OnFilterChanged);
        }
        
        // Setup mandatory toggle if it exists
        if (showMandatoryOnlyToggle != null)
        {
            showMandatoryOnlyToggle.onValueChanged.AddListener(OnMandatoryFilterChanged);
        }
        
        // Setup pagination buttons
        if (nextPageButton != null)
        {
            nextPageButton.onClick.AddListener(OnNextPageClicked);
        }
        
        if (prevPageButton != null)
        {
            prevPageButton.onClick.AddListener(OnPrevPageClicked);
        }
    }
    
    void OnFilterChanged(int index)
    {
        // Reset to first page when filter changes
        currentPage = 1;
    
        // Clear existing course panels immediately
        ClearCourseList();
    
        // Apply selected filter
        string endpoint = "";
        switch (index)
        {
            case 0: // All Courses
                endpoint = string.Format(endpointTemplate, UserManager.CompanyId, currentPage, pageSize);
                break;
            case 1: // Active Only
                endpoint = string.Format(endpointTemplate, UserManager.CompanyId, currentPage, pageSize) + "&status=ACTIVE";
                break;
            case 2: // By Duration
                endpoint = string.Format(endpointTemplate, UserManager.CompanyId, currentPage, pageSize) + "&sortBy=duration";
                break;
            case 3: // By Type
                endpoint = string.Format(endpointTemplate, UserManager.CompanyId, currentPage, pageSize) + "&sortBy=type";
                break;
        }
    
        StartCoroutine(FetchCourseData(endpoint));
    }

    void OnMandatoryFilterChanged(bool isOn)
    {
        // Clear existing course panels immediately
        ClearCourseList();
    
        // Apply mandatory filter
        string endpoint = string.Format(endpointTemplate, UserManager.CompanyId, currentPage, pageSize);
        if (isOn)
        {
            endpoint += "&mandatory=true";
        }
    
        StartCoroutine(FetchCourseData(endpoint));
    }

// Add this helper method to clear course list
    private void ClearCourseList()
    {
        // Show "no courses" text while loading
        nocourseText.SetActive(true);
    
        // Clear all existing course panels
        foreach (Transform child in contentParent)
        {
            Destroy(child.gameObject);
        }
    }
    
    
    void OnNextPageClicked()
    {
        currentPage++;
        string endpoint = string.Format(endpointTemplate, UserManager.CompanyId, currentPage, pageSize);
        StartCoroutine(FetchCourseData(endpoint));
    }
    
    void OnPrevPageClicked()
    {
        if (currentPage > 1)
        {
            currentPage--;
            string endpoint = string.Format(endpointTemplate, UserManager.CompanyId, currentPage, pageSize);
            StartCoroutine(FetchCourseData(endpoint));
        }
    }
    
    IEnumerator WaitForCompanyIdAndFetchCourses()
    {
        while (string.IsNullOrEmpty(UserManager.CompanyId))
        {
            Debug.Log("⌛ Waiting for CompanyId...");
            yield return new WaitForSeconds(0.5f);
        }

        string endpoint = string.Format(endpointTemplate, UserManager.CompanyId, currentPage, pageSize);
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
            pointsText.text = $"{user.points} ";
            
            // Add new user information to UI
            if (roleText != null)
                roleText.text = user.roleName ?? (user.role?.roleName ?? "Member");
                
            if (companyNameText != null && user.company != null)
                companyNameText.text = user.company.companyName;
                
            if (statusText != null)
            {
                statusText.text = user.status;
                
                // Color-code the status
                if (user.status == "ACTIVE")
                    statusText.color = new Color(0.2f, 0.8f, 0.2f); // Green
                else
                    statusText.color = new Color(0.8f, 0.2f, 0.2f); // Red
            }
            
            Debug.Log($"👤 User: {user.username}");

            // Save company.id to UserManager
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
        // Reset to first page
        currentPage = 1;
        string endpoint = string.Format(endpointTemplate, UserManager.CompanyId, currentPage, pageSize);
        StartCoroutine(FetchCourseData(endpoint));
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
        Debug.Log($"📜 Raw JSON Data: {jsonData}");

        try
        {
            var response = JsonConvert.DeserializeObject<ApiResponse<PaginationResult<CourseResult>>>(jsonData);

            if (response == null || response.result == null || response.result.objectList == null || response.result.objectList.Count == 0)
            {
                Debug.LogError("❌ No courses found or invalid response.");
                nocourseText.SetActive(true);
                
                // Update pagination controls
                UpdatePaginationInfo(0, 0, 0);
                return;
            }

            Debug.Log($"📌 Found {response.result.objectList.Count} courses.");
            
            // Update pagination controls
            UpdatePaginationInfo(response.result.page, response.result.totalPages, response.result.totalItems);
            
            // Update dashboard stats
            UpdateDashboardStats(response.result);

            // Clear old UI panels before adding new ones
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

                // Shorten title and description
                string truncatedTitle = course.title.Length > 20 ? course.title.Substring(0, 20) + "..." : course.title;
                string truncatedDescription = course.description.Length > 50 ? course.description.Substring(0, 50) + "..." : course.description;

                course.title = truncatedTitle;
                course.description = truncatedDescription;

                nocourseText.SetActive(false);

                // Create the UI panel
                CreateCoursePanel(course);
            }
        }
        catch (Exception e)
        {
            Debug.LogError($"❌ JSON Parsing Error: {e.Message}\nRaw JSON: {jsonData}");
        }
    }
    
    void UpdatePaginationInfo(int currentPage, int totalPages, int totalItems)
    {
        // Update page info text
        if (pageInfoText != null)
        {
            pageInfoText.text = $"Page {currentPage} of {totalPages} ({totalItems} courses)";
        }
        
        // Enable/disable pagination buttons
        if (prevPageButton != null)
        {
            prevPageButton.interactable = (currentPage > 1);
        }
        
        if (nextPageButton != null)
        {
            nextPageButton.interactable = (currentPage < totalPages);
        }
    }
    
    void UpdateDashboardStats(PaginationResult<CourseResult> data)
    {
        // Update total courses text
        if (totalCoursesText != null)
        {
            totalCoursesText.text = $"Total Courses: {data.totalItems}";
        }
        
        // Count course types
        Dictionary<string, int> courseTypes = new Dictionary<string, int>();
        foreach (var course in data.objectList)
        {
            if (!string.IsNullOrEmpty(course.type))
            {
                if (courseTypes.ContainsKey(course.type))
                    courseTypes[course.type]++;
                else
                    courseTypes[course.type] = 1;
            }
        }
        
        // Update course types text
        if (courseTypeText != null && courseTypes.Count > 0)
        {
            string typesText = "Course Types: ";
            foreach (var type in courseTypes)
            {
                typesText += $"{type.Key} ({type.Value}), ";
            }
            typesText = typesText.TrimEnd(' ', ',');
            courseTypeText.text = typesText;
        }
        
        // Update progress bar (this is just a visual element - you may want to modify based on actual progress data)
        if (courseProgressFill != null)
        {
            // Example: Fill based on how many pages viewed out of total
            float fillAmount = (float)currentPage / data.totalPages;
            courseProgressFill.fillAmount = fillAmount;
        }
    }

    void CreateCoursePanel(CourseResult course)
    {
        Debug.Log($"CourseLoader: Creating panel for course: {course.title}");
        GameObject panel = Instantiate(coursePanelPrefab, contentParent);

        TMP_Text titleText = panel.transform.Find("course_titleText").GetComponent<TMP_Text>();
        TMP_Text descriptionText = panel.transform.Find("course_descriptionText").GetComponent<TMP_Text>();
        TMP_Text scoreText = panel.transform.Find("course_scoreText").GetComponent<TMP_Text>();
        
        // Look for additional UI elements that might exist in your prefab
        TMP_Text typeText = panel.transform.Find("course_typeText")?.GetComponent<TMP_Text>();
        TMP_Text durationText = panel.transform.Find("course_durationText")?.GetComponent<TMP_Text>();
        GameObject mandatoryBadge = panel.transform.Find("mandatoryBadge")?.gameObject;
        
        // Set existing information
        if (titleText != null) titleText.text = course.title;
        if (descriptionText != null) descriptionText.text = course.description;
        
        // Enhanced information display
        if (scoreText != null)
        {
            string scoreInfo = $"Lessons: {course.numberOfLessons ?? 0}";
            
            // Add participants count if available
            if (course.numberOfParticipants.HasValue && course.numberOfParticipants > 0)
                scoreInfo += $" | Participants: {course.numberOfParticipants}";
                
            scoreText.text = scoreInfo;
        }
        
        // Set additional information if UI elements exist
        if (typeText != null && !string.IsNullOrEmpty(course.type))
        {
            typeText.text = course.type;
            typeText.gameObject.SetActive(true);
        }
        
        if (durationText != null && course.duration.HasValue)
        {
            durationText.text = $"Duration: {course.duration} min";
            durationText.gameObject.SetActive(true);
        }
        
        // Show mandatory badge if course is mandatory
        if (mandatoryBadge != null)
        {
            mandatoryBadge.SetActive(course.isMandatory);
        }

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

        string fullUrl = "/files/" + imageUrl;
        Debug.Log(fullUrl);
        string filename = Path.GetFileName(imageUrl);
        string localPath = Path.Combine(Application.persistentDataPath, filename);

        // Check if image is cached
        if (File.Exists(localPath))
        {
            Debug.Log($"📂 Loading cached image: {localPath}");
            yield return LoadImageFromLocal(localPath, imageComponent);
            yield break;
        }

        // Download image
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
            // Direct transition to detail page without animation
            ShowDetailPage(courseId);
        }
        else
        {
            Debug.LogError("CourseLoader: DetailPage is not assigned!");
        }
    }
    
    private void ShowDetailPage(string courseId)
    {
        // Show detail page
        detailPage.SetActive(true);
        
        // Hide home page
        homePage.SetActive(false);
        
        // Load course details
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

        // Update endpoint to get more courses
        string allCoursesEndpoint = string.Format("/course/company/{0}?page=1&size=20&status=ACTIVE", UserManager.CompanyId);
        
        // Fetch more courses for the "See All" page
        StartCoroutine(FetchCoursesForSeeAllPage(allCoursesEndpoint));
    }
    
    IEnumerator FetchCoursesForSeeAllPage(string endpoint)
    {
        // Show loading indicator if you have one
        
        // Fetch data for see all page
        using (UnityWebRequest request = ApiConfig.CreateRequest(endpoint))
        {
            yield return request.SendWebRequest();
            
            if (request.result == UnityWebRequest.Result.Success)
            {
                // Successfully loaded courses, switch to see all page
                if (homePage != null)
                {
                    homePage.SetActive(false);
                }

                if (seeAllPage != null)
                {
                    seeAllPage.SetActive(true);
                    
                    // If your SeeAllPage has a CourseListLoader component, use it
                    var courseListLoader = seeAllPage.GetComponent<CourseLoader>();
                    if (courseListLoader != null)
                    {
                        courseListLoader.ProcessCourseData(request.downloadHandler.text);
                    }
                    else
                    {
                        Debug.LogWarning("SeeAllPage does not have a CourseListLoader component");
                    }
                }
            }
            else
            {
                Debug.LogError($"Failed to fetch courses for See All page: {request.error}");
            }
        }
    }
    
    public void LoadVRScene()
    {
        // Load VR scene directly without transition
        SceneManager.LoadScene("QRScanner1");
    }
}