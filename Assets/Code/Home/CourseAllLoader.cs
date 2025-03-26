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

public class CourseAllLoader : MonoBehaviour
{
    [Header("UI References")]
    public GameObject coursePanelPrefab;
    public Transform contentParent;
    public GameObject noCourseText;
    public GameObject loadingIndicator; 
    public GameObject detailPage;
    [Header("API Settings")]
    private string endpointTemplate = "/course/company/{0}";

    void Start()
    {
        if (!string.IsNullOrEmpty(UserManager.CompanyId))
        {
            LoadCourses();
        }
        else
        {
            StartCoroutine(WaitForCompanyIdAndFetchCourses());
        }
    }

    IEnumerator WaitForCompanyIdAndFetchCourses()
    {
        while (string.IsNullOrEmpty(UserManager.CompanyId))
        {
            Debug.Log("⌛ Waiting for CompanyId...");
            yield return new WaitForSeconds(0.5f);
        }

        LoadCourses();
    }

    public void LoadCourses()
    {
        string endpoint = string.Format(endpointTemplate, UserManager.CompanyId);
        Debug.Log($"📡 Fetching courses from {endpoint}...");
        StartCoroutine(FetchCourseData(endpoint));
    }

    IEnumerator FetchCourseData(string endpoint)
    {
        using (UnityWebRequest request = ApiConfig.CreateRequest(endpoint))
        {
            yield return request.SendWebRequest();

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

    void ProcessCourseData(string jsonData)
    {
        try
        {
            if (string.IsNullOrWhiteSpace(jsonData))
            {
                Debug.LogError("❌ JSON Error: API trả về chuỗi rỗng!");
                return;
            }

            var response = JsonConvert.DeserializeObject<ApiResponseList<CourseResult>>(jsonData);

            if (response == null || response.result == null || response.result.Count == 0)
            {
                Debug.Log("✅ Không có khóa học nào.");
                noCourseText.SetActive(true);
                return;
            }

            Debug.Log($"📌 Đã tải {response.result.Count} khóa học.");
            noCourseText.SetActive(false);

            foreach (CourseResult course in response.result)
            {
                CreateCoursePanel(course);
            }
        }
        catch (Exception e)
        {
            Debug.LogError($"❌ JSON Parsing Exception: {e.Message}");
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
