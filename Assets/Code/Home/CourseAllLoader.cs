using System;
using System.Collections;
using System.Collections.Generic;
using Code;
using UnityEngine;
using UnityEngine.Networking;
using UnityEngine.UI;
using TMPro;
using Newtonsoft.Json;
using Models;
using System.IO;
public class CourseAllLoader : MonoBehaviour
{
    [Header("UI References")]
    public GameObject coursePanelPrefab;
    public Transform contentParent;
    public GameObject noCourseText;
    public GameObject loadingIndicator; 
    public GameObject detailPage;
    
    public ScrollRect scrollRect; // Scroll Rect for detecting scroll position
    [Header("API Settings")]
    private string endpointTemplate = "/course/company/{0}?page={1}&size={2}";
    
    private int currentPage = 1;
    private int pageSize = 10; // Number of courses per page
    private int totalPages = 1;
    private bool isLoading = false; // Prevent multiple simultaneous requests
    void Start()
    {
        if (!string.IsNullOrEmpty(UserManager.CompanyId))
        {
            LoadCourses(true); // Load first page
        }
        else
        {
            StartCoroutine(WaitForCompanyIdAndFetchCourses());
        }

        // Attach Scroll Listener
        scrollRect.onValueChanged.AddListener(OnScroll);
    }
    
    void OnScroll(Vector2 position)
    {
        if (scrollRect.verticalNormalizedPosition <= 0.05f) // User scrolled to bottom
        {
            if (currentPage <= totalPages)
            {
                Debug.Log("🔄 Loading more courses...");
                LoadCourses(false);
            }
            else
            {
                Debug.Log("✅ No more courses to load.");
            }
        }
    }

    IEnumerator WaitForCompanyIdAndFetchCourses()
    {
        while (string.IsNullOrEmpty(UserManager.CompanyId))
        {
            Debug.Log("⌛ Waiting for CompanyId...");
            yield return new WaitForSeconds(0.5f);
        }

        LoadCourses(true);
    }
    public void LoadCourses(bool isFirstLoad)
    {
        if (isLoading) return;

        isLoading = true;
        loadingIndicator.SetActive(true);

        if (isFirstLoad)
        {
            currentPage = 1;
            totalPages = 1;
            foreach (Transform child in contentParent)
            {
                Destroy(child.gameObject); // Clear old courses on fresh load
            }
        }

        string endpoint = string.Format(endpointTemplate, UserManager.CompanyId, currentPage, pageSize);
        Debug.Log($"📡 Fetching courses from {endpoint}... (Page {currentPage})");

        StartCoroutine(FetchCourseData(endpoint));
    }

    IEnumerator FetchCourseData(string endpoint)
    {
        using (UnityWebRequest request = ApiConfig.CreateRequest(endpoint))
        {
            yield return request.SendWebRequest();
            isLoading = false;
            loadingIndicator.SetActive(false);

            if (request.result == UnityWebRequest.Result.ConnectionError || request.result == UnityWebRequest.Result.ProtocolError)
            {
                Debug.LogError($"❌ API Error: {request.error}");
            }
            else
            {
                string jsonData = request.downloadHandler.text;
                Debug.Log($"📜 API Response: {jsonData}");
                ProcessCourseData(jsonData);
            }
        }
    }

    private void ProcessCourseData(string jsonData)
    {
        if (string.IsNullOrWhiteSpace(jsonData))
        {
            Debug.LogError("❌ JSON Error: Empty response");
            return;
        }

        try
        {
            var response = JsonConvert.DeserializeObject<ApiResponse<PaginationResult<CourseResult>>>(jsonData);

            if (response?.result == null || response.result.objectList == null || response.result.objectList.Count == 0)
            {
                if (currentPage == 1)
                {
                    noCourseText.SetActive(true);
                }
                return;
            }

            noCourseText.SetActive(false);
            totalPages = response.result.totalPages;
            currentPage++; // Move to next page for future loads

            foreach (var course in response.result.objectList)
            {
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
        GameObject panel = Instantiate(coursePanelPrefab, contentParent);
        TMP_Text titleText = panel.transform.Find("course_titleText")?.GetComponent<TMP_Text>();
        TMP_Text descriptionText = panel.transform.Find("course_descriptionText")?.GetComponent<TMP_Text>();
        TMP_Text scoreText = panel.transform.Find("course_scoreText")?.GetComponent<TMP_Text>();

        if (titleText != null) titleText.text = course.title;
        if (descriptionText != null) descriptionText.text = course.description;
        if (scoreText != null) scoreText.text = $"Lessons: {course.numberOfLessons ?? 0}";

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
    
}
// using System;
// using System.Collections;
// using System.Collections.Generic;
// using UnityEngine;
// using UnityEngine.Networking;
// using UnityEngine.UI;
// using TMPro;
// using Newtonsoft.Json;
// using System.IO;
// using Models;
//
// public class CourseAllLoader : MonoBehaviour
// {
//     [Header("UI References")]
//     public GameObject coursePanelPrefab;
//     public Transform contentParent;
//     public GameObject noCourseText;
//     public GameObject loadingIndicator; // Hiển thị khi tải dữ liệu
//     public Button loadMoreButton; // Nút "Xem thêm"
//
//     [Header("API Settings")]
//     private string endpointTemplate = "/course/company/{0}?page={1}&size={2}";
//     private int currentPage = 1;
//     private int pageSize = 10;
//     private bool isLoading = false;
//     private bool hasMoreData = true;
//
//     void Start()
//     {
//         
//         if (!string.IsNullOrEmpty(UserManager.CompanyId))
//         {
//             LoadCourses();
//         }
//         else
//         {
//             StartCoroutine(WaitForCompanyIdAndFetchCourses());
//         }
//     }
//
//     IEnumerator WaitForCompanyIdAndFetchCourses()
//     {
//         while (string.IsNullOrEmpty(UserManager.CompanyId))
//         {
//             Debug.Log("⌛ Waiting for CompanyId...");
//             yield return new WaitForSeconds(0.5f);
//         }
//
//         LoadCourses();
//     }
//
//     public void LoadCourses()
//     {
//         if (isLoading || !hasMoreData) return;
//
//         isLoading = true;
//         loadingIndicator.SetActive(true);
//         string endpoint = string.Format(endpointTemplate, UserManager.CompanyId, currentPage, pageSize);
//         Debug.Log($"📡 Fetching page {currentPage} of courses...");
//         StartCoroutine(FetchCourseData(endpoint));
//     }
//
//     IEnumerator FetchCourseData(string endpoint)
//     {
//         using (UnityWebRequest request = ApiConfig.CreateRequest(endpoint))
//         {
//             yield return request.SendWebRequest();
//
//             if (request.result == UnityWebRequest.Result.ConnectionError || request.result == UnityWebRequest.Result.ProtocolError)
//             {
//                 Debug.LogError($"❌ API Error: {request.error}");
//             }
//             else
//             {
//                 string jsonData = request.downloadHandler.text;
//                 Debug.Log($"📜 API Response: {jsonData}");
//
//                 ProcessCourseData(jsonData);
//             }
//         }
//
//         isLoading = false;
//         loadingIndicator.SetActive(false);
//     }
//
//     void ProcessCourseData(string jsonData)
//     {
//         try
//         {
//             if (string.IsNullOrWhiteSpace(jsonData))
//             {
//                 Debug.LogError("❌ JSON Error: API trả về chuỗi rỗng!");
//                 return;
//             }
//
//             var response = JsonConvert.DeserializeObject<ApiResponseList<CourseResult>>(jsonData);
//
//             if (response == null || response.result == null)
//             {
//                 Debug.LogError("❌ JSON Parsing Error: Response bị null!");
//                 return;
//             }
//
//             if (response.result.Count == 0)
//             {
//                 Debug.Log("✅ Không có khóa học nào.");
//                 hasMoreData = false;
//                 loadMoreButton.gameObject.SetActive(false);
//                 noCourseText.SetActive(true);
//                 return;
//             }
//
//             Debug.Log($"📌 Đã tải {response.result.Count} khóa học.");
//             noCourseText.SetActive(false);
//
//             foreach (CourseResult course in response.result)
//             {
//                 CreateCoursePanel(course);
//             }
//
//             // ✅ Kiểm tra có còn dữ liệu để tải không
//             hasMoreData = response.result.Count >= pageSize;
//             loadMoreButton.gameObject.SetActive(hasMoreData);
//         
//             if (hasMoreData) currentPage++;
//         }
//         catch (Exception e)
//         {
//             Debug.LogError($"❌ JSON Parsing Exception: {e.Message}");
//         }
//     }
//
//
//     void CreateCoursePanel(CourseResult course)
//     {
//         GameObject panel = Instantiate(coursePanelPrefab, contentParent);
//         TMP_Text titleText = panel.transform.Find("course_titleText")?.GetComponent<TMP_Text>();
//         TMP_Text descriptionText = panel.transform.Find("course_descriptionText")?.GetComponent<TMP_Text>();
//         TMP_Text scoreText = panel.transform.Find("course_scoreText")?.GetComponent<TMP_Text>();
//
//         if (titleText != null) titleText.text = course.title;
//         if (descriptionText != null) descriptionText.text = course.description;
//         if (scoreText != null) scoreText.text = $"Lessons: {course.numberOfLessons ?? 0}";
//
//         Button courseButton = panel.GetComponent<Button>();
//         if (courseButton != null)
//         {
//             courseButton.onClick.AddListener(() => OnCourseClicked(course.id));
//         }
//
//         if (!string.IsNullOrEmpty(course.imageUrl))
//         {
//             Image imageComponent = panel.transform.Find("courseImage_background/course_image")?.GetComponent<Image>();
//             if (imageComponent != null)
//             {
//                 StartCoroutine(DownloadAndLoadCourseImage(course.imageUrl, imageComponent));
//             }
//         }
//     }
//
//     IEnumerator DownloadAndLoadCourseImage(string imageUrl, Image imageComponent)
//     {
//         if (string.IsNullOrEmpty(imageUrl)) yield break;
//
//         string baseUrl = "https://your-server.com/files/"; // Thay đổi URL nếu cần
//         string fullUrl = baseUrl + imageUrl;
//         string filename = Path.GetFileName(imageUrl);
//         string localPath = Path.Combine(Application.persistentDataPath, filename);
//
//         if (File.Exists(localPath))
//         {
//             yield return LoadImageFromLocal(localPath, imageComponent);
//             yield break;
//         }
//
//         yield return StartCoroutine(DownloadFile(fullUrl, localPath));
//
//         if (File.Exists(localPath))
//         {
//             yield return LoadImageFromLocal(localPath, imageComponent);
//         }
//     }
//
//     IEnumerator DownloadFile(string url, string localPath)
//     {
//         using (UnityWebRequest request = UnityWebRequest.Get(url))
//         {
//             yield return request.SendWebRequest();
//
//             if (request.result == UnityWebRequest.Result.ConnectionError || request.result == UnityWebRequest.Result.ProtocolError)
//             {
//                 Debug.LogError($"❌ Download Error: {request.error}");
//             }
//             else
//             {
//                 File.WriteAllBytes(localPath, request.downloadHandler.data);
//                 Debug.Log($"✅ Downloaded and saved: {localPath}");
//             }
//         }
//     }
//
//     IEnumerator LoadImageFromLocal(string path, Image imageComponent)
//     {
//         if (!File.Exists(path)) yield break;
//
//         byte[] imageData = File.ReadAllBytes(path);
//         Texture2D texture = new Texture2D(2, 2);
//         if (!texture.LoadImage(imageData)) yield break;
//
//         imageComponent.sprite = Sprite.Create(texture, new Rect(0, 0, texture.width, texture.height), Vector2.one * 0.5f);
//         yield return null;
//     }
//
//     public void OnCourseClicked(string courseId)
//     {
//         Debug.Log($"CourseAllLoader: Course clicked with ID {courseId}");
//         CourseManager.SelectedCourseId = courseId;
//     }
// }
