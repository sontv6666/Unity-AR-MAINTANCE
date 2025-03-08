using System.Collections;
using System.Collections.Generic;
using Code;
using UnityEngine;
using UnityEngine.Networking;
using UnityEngine.UI;
using TMPro;

using System.IO;

public class CourseLoader : MonoBehaviour
{
    [Header("UI References")] public GameObject coursePanelPrefab; // Prefab for each course item
    public Transform contentParent; // Parent object to hold all course panels
    public GameObject nocourseText;
    public GameObject detailPage;
    public GameObject homePage;
    [Header("API Settings")] private string endpointTemplate = "/course?page=1&size=50&userId={0}";

    void Start()
    {
        // Ensure userId is set in UserManager
        if (!string.IsNullOrEmpty(UserManager.UserId))
        {
            string endpoint = string.Format(endpointTemplate, UserManager.UserId);
            Debug.Log($"CourseLoader: Fetching course data for userId: {UserManager.UserId}");
            StartCoroutine(FetchCourseData(endpoint));
        }
        else
        {
            Debug.LogError("CourseLoader: UserId is not set. Unable to fetch courses!");
        }
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
        Debug.Log("CourseLoader: Processing course data.");
        // Deserialize JSON to object
        CourseResponse1 response = JsonUtility.FromJson<CourseResponse1>(jsonData);
        if (response != null && response.result != null && response.result.objectList != null)
        {
            Debug.Log($"CourseLoader: Found {response.result.objectList.Count} courses.");
            foreach (CourseData course in response.result.objectList)
            {
                // Truncate long title and description
                string truncatedTitle = course.title.Length > 10 ? course.title.Substring(0, 10) + "..." : course.title;
                string truncatedDescription = course.description.Length > 10
                    ? course.description.Substring(0, 10) + "..."
                    : course.description;

                // Assign truncated values to course data
                course.title = truncatedTitle;
                course.description = truncatedDescription;
                nocourseText.SetActive(false);
                // Create UI panel for each course
                CreateCoursePanel(course);
            }
        }
        else
        {
            nocourseText.SetActive(true);

            Debug.LogError("Response data is null or invalid!");
        }
    }

    void CreateCoursePanel(CourseData course)
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

}




[System.Serializable]
public class CourseResponse1
{
    public int code;
    public ResultData1 result;
}
[System.Serializable]
public class CourseResponse2
{
    public int code;
    public ResultData2 result;
}

[System.Serializable]


public class ResultData1
{
    public int page;
    public int size;
    public int totalItems;
    public int totalPages;
    public List<CourseData> objectList; 
}
[System.Serializable]
public class ResultData2
{
    public int page;
    public int size;
    public int totalItems;
    public int totalPages;
    public List<EnrollmentData> objectList; // Keep objectList as CourseData
}

[System.Serializable]
public class EnrollmentData
{
    public string id;
    public CourseData courseResponse; // ✅ This should exist in the JSON!
    public string userId;
    public string enrollmentDate;
    public string deadline;
    public bool isCompleted;
    public string completionDate;
}
[System.Serializable]
public class CourseData
{
    public string id;
    public string companyId;
    public string title;
    public string description;
    public int duration;
    public bool isMandatory;
    public string imageUrl;
    public int? numberOfLessons;
    public int? numberOfParticipants;
    public string status;
    public string type;
}

