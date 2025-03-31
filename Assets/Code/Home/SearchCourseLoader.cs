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

    private string searchApiTemplate = "/course/company/{0}?title={1}";

    void Start()
    {
        searchButton.onClick.AddListener(() => StartCoroutine(SearchCourse(searchInput.text)));
    }

    IEnumerator SearchCourse(string title)
    {
        if (string.IsNullOrEmpty(title))
        {
            Debug.LogError("❌ Search title is empty!");
            yield break;
        }

        if (string.IsNullOrEmpty(UserManager.CompanyId))
        {
            Debug.LogError("❌ Company ID is missing! Cannot fetch courses.");
            yield break;
        }

        string endpoint = string.Format(searchApiTemplate, UserManager.CompanyId, title);
        string fullUrl = ApiConfig.GetBaseUrl() + endpoint;
        Debug.Log($"📡 Searching course: {fullUrl}");

        using (UnityWebRequest request = UnityWebRequest.Get(fullUrl))
        {
            request.downloadHandler = new DownloadHandlerBuffer();
            request.SetRequestHeader("Authorization", "Bearer " + PlayerPrefs.GetString("AuthToken", ""));
            request.SetRequestHeader("Content-Type", "application/json");

            yield return request.SendWebRequest();

            if (request.result == UnityWebRequest.Result.ConnectionError || request.result == UnityWebRequest.Result.ProtocolError)
            {
                Debug.LogError($"❌ API Error: {request.error}");
                nocourseText.SetActive(true);
            }
            else
            {
                string jsonData = request.downloadHandler.text;
                Debug.Log($"✅ API Response: {jsonData}");
                ProcessSearchResults(jsonData);
            }
        }
    }


    void ProcessSearchResults(string jsonData)
    {
        try
        {
            var response = JsonConvert.DeserializeObject<ApiResponse<PaginationResult<CourseResult>>>(jsonData);

            if (response == null || response.code != 1000 || response.result == null)
            {
                Debug.LogError("❌ Course not found!");
                nocourseText.SetActive(true);
                return;
            }

            nocourseText.SetActive(false);
            foreach (var course in response.result.objectList)
            {
                CreateCoursePanel(course);
            }
        }
        catch (System.Exception e)
        {
            Debug.LogError($"❌ JSON Parsing Error: {e.Message}\nRaw JSON: {jsonData}");
        }
    }

    void CreateCoursePanel(CourseResult course)
    {
        Debug.Log($"📌 Displaying course: {course.title}");

        // Clear previous results
        foreach (Transform child in contentParent)
        {
            Destroy(child.gameObject);
        }

        GameObject panel = Instantiate(coursePanelPrefab, contentParent);

        TMP_Text titleText = panel.transform.Find("course_titleText").GetComponent<TMP_Text>();
        TMP_Text descriptionText = panel.transform.Find("course_descriptionText").GetComponent<TMP_Text>();

        if (titleText != null) titleText.text = course.title;
        if (descriptionText != null) descriptionText.text = course.description;

        // ✅ Attach click event listener
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
            yield return request.SendWebRequest();

            if (request.result == UnityWebRequest.Result.ConnectionError || request.result == UnityWebRequest.Result.ProtocolError)
            {
                Debug.LogError($"❌ Image Download Error: {request.error}");
            }
            else
            {
                File.WriteAllBytes(localPath, request.downloadHandler.data);
                yield return LoadImageFromLocal(localPath, imageComponent);
            }
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
