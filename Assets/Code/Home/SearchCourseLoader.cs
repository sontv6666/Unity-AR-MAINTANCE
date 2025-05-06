using System.Collections;
using System.IO;
using Code;
using UnityEngine;
using UnityEngine.Networking;
using UnityEngine.UI;
using TMPro;
using Newtonsoft.Json;
using Models;

public class SearchCourseLoader : MonoBehaviour
{
    [Header("UI References")] 
    public TMP_InputField searchInput;
    public Button searchButton;
    public GameObject coursePanelPrefab;
    public Transform contentParent;
    public GameObject nocourseText;
    public GameObject searchPage;
    public GameObject detailPage;
    
    [Header("Pagination Controls")]
    public Button nextPageButton; // Next page button
    public Button prevPageButton; // Previous page button
    public TMP_Text pageInfoText; // Shows current page/total pages
    private int currentPage = 1;
    private int pageSize = 3;
    
    private string currentSearchQuery = "";
    // Updated to include staffId parameter
    private string searchApiTemplate = "/course/company/{0}?title={1}&page={2}&size={3}&status=ACTIVE&staffId={4}";
    
    // Flag to track if we need to retry loading when network is restored
    private bool needsReload = false;

    void Start()
    {
        searchButton.onClick.AddListener(() => {
            currentPage = 1; // Reset to first page on new search
            currentSearchQuery = searchInput.text;
            StartCoroutine(SearchCourse(currentSearchQuery, currentPage));
        });
        
        // Setup pagination buttons
        if (nextPageButton != null)
        {
            nextPageButton.onClick.AddListener(OnNextPageClicked);
        }
        
        if (prevPageButton != null)
        {
            prevPageButton.onClick.AddListener(OnPrevPageClicked);
        }
        
        // 🔄 Auto-load all courses when entering the page
        StartCoroutine(SearchCourse("", currentPage));
        
        // Subscribe to network events
        NetworkAwareAPIHandler.Instance.OnNetworkRestored += HandleNetworkRestored;
    }
    
    private void OnDestroy()
    {
        // Unsubscribe when destroyed to prevent memory leaks
        if (NetworkAwareAPIHandler.Instance != null)
        {
            NetworkAwareAPIHandler.Instance.OnNetworkRestored -= HandleNetworkRestored;
        }
    }
    
    private void HandleNetworkRestored()
    {
        if (needsReload)
        {
            Debug.Log("🔄 Network restored - reloading search results");
            needsReload = false;
            StartCoroutine(SearchCourse(currentSearchQuery, currentPage));
        }
    }
    
    void OnNextPageClicked()
    {
        currentPage++;
        StartCoroutine(SearchCourse(currentSearchQuery, currentPage));
    }
    
    void OnPrevPageClicked()
    {
        if (currentPage > 1)
        {
            currentPage--;
            StartCoroutine(SearchCourse(currentSearchQuery, currentPage));
        }
    }
    
    public void ReloadSearchResults()
    {
        currentPage = 1; // Reset to first page
        StartCoroutine(SearchCourse(currentSearchQuery, currentPage)); // Reload with current search query
    }

    IEnumerator SearchCourse(string title, int page)
    {
        if (string.IsNullOrEmpty(UserManager.CompanyId))
        {
            Debug.LogError("❌ Company ID is missing! Cannot fetch courses.");
            yield break;
        }
        
        if (string.IsNullOrEmpty(UserManager.UserId))
        {
            Debug.LogError("❌ User ID is missing! Cannot fetch courses.");
            yield break;
        }

        // If title is empty, fetch ALL courses
        string searchQuery = string.IsNullOrEmpty(title) ? "" : title;
        // Added UserManager.UserId as staffId parameter
        string endpoint = string.Format(searchApiTemplate, UserManager.CompanyId, searchQuery, page, pageSize, UserManager.UserId);
        string fullUrl = ApiConfig.GetBaseUrl() + endpoint;

        Debug.Log($"📡 Fetching courses from: {fullUrl}");

        using (UnityWebRequest request = UnityWebRequest.Get(fullUrl))
        {
            request.downloadHandler = new DownloadHandlerBuffer();
            request.SetRequestHeader("Authorization", "Bearer " + PlayerPrefs.GetString("AuthToken", ""));
            request.SetRequestHeader("Content-Type", "application/json");

            // Use NetworkAwareAPIHandler instead of direct web request
            yield return NetworkAwareAPIHandler.Instance.SendAPIRequest(
                request,
                OnSearchRequestSuccess,
                OnSearchRequestFailure
            );
        }
    }
    
    private void OnSearchRequestSuccess(UnityWebRequest request)
    {
        string jsonData = request.downloadHandler.text;
        Debug.Log($"✅ API Response: {jsonData}");
        ProcessSearchResults(jsonData);
        needsReload = false; // Clear the reload flag as we've successfully loaded
    }
    
    private void OnSearchRequestFailure(string errorMessage)
    {
        Debug.LogError($"❌ Search request failed: {errorMessage}");
        nocourseText.SetActive(true);
        UpdatePaginationInfo(0, 0, 0); // Reset pagination UI
        needsReload = true; // Set flag to reload when network is restored
    }

    void ProcessSearchResults(string jsonData)
    {
        try
        {
            var response = JsonConvert.DeserializeObject<ApiResponse<PaginationResult<CourseResult>>>(jsonData);

            // Clear previous results before checking for new ones
            foreach (Transform child in contentParent)
            {
                Destroy(child.gameObject);
            }
            
            if (response == null || response.code != 1000 || response.result == null || response.result.objectList.Count == 0)
            {
                Debug.LogError("❌ No courses found!");
                nocourseText.SetActive(true);
                UpdatePaginationInfo(0, 0, 0); // Reset pagination UI
                return;
            }

            nocourseText.SetActive(false);

            // Update pagination controls
            UpdatePaginationInfo(response.result.page, response.result.totalPages, response.result.totalItems);

            foreach (var course in response.result.objectList)
            {
                string truncatedTitle = course.title.Length > 25 ? course.title.Substring(0, 25) + "..." : course.title;
                string truncatedDescription = course.description.Length > 80 ? course.description.Substring(0, 80) + "..." : course.description;

                course.title = truncatedTitle;
                course.description = truncatedDescription;
                CreateCoursePanel(course);
            }
        }
        catch (System.Exception e)
        {
            Debug.LogError($"❌ JSON Parsing Error: {e.Message}\nRaw JSON: {jsonData}");
        }
    }
    
    void UpdatePaginationInfo(int currentPage, int totalPages, int totalItems)
    {
        // Update page info text
        if (pageInfoText != null)
        {
            pageInfoText.text = $"Page {currentPage} of {totalPages}";
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

    void CreateCoursePanel(CourseResult course)
    {
        Debug.Log($"📌 Displaying course: {course.title}");

        GameObject panel = Instantiate(coursePanelPrefab, contentParent);

        TMP_Text titleText = panel.transform.Find("course_titleText").GetComponent<TMP_Text>();
        TMP_Text descriptionText = panel.transform.Find("course_descriptionText").GetComponent<TMP_Text>();
        TMP_Text scanCountText = panel.transform.Find("course_scanCountText")?.GetComponent<TMP_Text>();
        if (titleText != null) titleText.text = course.title;
        if (descriptionText != null) descriptionText.text = course.description;

        // ✅ Attach click event listener
        Button courseButton = panel.GetComponent<Button>();
        if (courseButton != null)
        {
            courseButton.onClick.AddListener(() => OnCourseClicked(course.id));
        }
        if (scanCountText != null)
        {
            string scanCountInfo = $"Scan Number: {course.numberOfStaffScan ?? 0}.";
                
            scanCountText.text = scanCountInfo;
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
    
    public void OnCourseClicked(string courseId)
    {
        Debug.Log($"📌 Course Clicked: {courseId}");
        CourseManager.SelectedCourseId = courseId;

        if (detailPage != null)
        {
            detailPage.SetActive(true);
        }
        else
        {
            Debug.LogError("❌ DetailPage is not assigned in the Inspector!");
        }

        // Hide search page if needed
        if (searchPage != null)
        {
            searchPage.SetActive(false);
        }

        // Load course details
        CourseDetailLoader courseDetailLoader = detailPage.GetComponent<CourseDetailLoader>();
        if (courseDetailLoader != null)
        {
            courseDetailLoader.LoadCourseDetails(courseId);
        }
        else
        {
            Debug.LogError("❌ CourseDetailLoader component is missing on DetailPage!");
        }
    }

    IEnumerator DownloadAndLoadCourseImage(string imageUrl, Image imageComponent)
    {
        string fullUrl = ApiConfig.GetBaseUrl() + "/files/" + imageUrl;
        string localPath = Path.Combine(Application.persistentDataPath, Path.GetFileName(imageUrl));

        if (File.Exists(localPath))
        {
            yield return LoadImageFromLocal(localPath, imageComponent);
            yield break;
        }

        using (UnityWebRequest request = UnityWebRequest.Get(fullUrl))
        {
            // Use NetworkAwareAPIHandler for image downloads too
            yield return NetworkAwareAPIHandler.Instance.SendAPIRequest(
                request,
                (successRequest) => {
                    File.WriteAllBytes(localPath, successRequest.downloadHandler.data);
                    StartCoroutine(LoadImageFromLocal(localPath, imageComponent));
                },
                (errorMessage) => {
                    Debug.LogError($"❌ Image Download Error: {errorMessage}");
                }
            );
        }
    }

    IEnumerator LoadImageFromLocal(string path, Image imageComponent)
    {
        if (!File.Exists(path)) yield break;

        byte[] imageData = File.ReadAllBytes(path);
        Texture2D texture = new Texture2D(2, 2);
        if (texture.LoadImage(imageData))
        {
            imageComponent.sprite = Sprite.Create(texture, new Rect(0, 0, texture.width, texture.height), Vector2.one * 0.5f);
        }

        yield return null;
    }
}