using System.Collections;
using System.Collections.Generic;
using Code;
using UnityEngine;
using UnityEngine.Networking;
using UnityEngine.UI;
using TMPro;
using Newtonsoft.Json; 
public class CourseLoader1 : MonoBehaviour
{
    [Header("UI References")]
    public GameObject coursePanelPrefab; // Prefab for each course item
    public Transform contentParent; // Parent object to hold all course panels
    public GameObject nocourseText;
    public GameObject detailPage; 
    public GameObject homePage; 

    [Header("API Settings")]
    private string endpointTemplate = "/enrollment/user/{0}?isRequiredCourse=false&page=1&size=10";

    void Start()
    {
        if (!string.IsNullOrEmpty(UserManager.UserId))
        {
            string endpoint = string.Format(endpointTemplate, UserManager.UserId);
            Debug.Log($"CourseLoader1: Fetching enrolled courses for userId: {UserManager.UserId}");
            StartCoroutine(FetchCourseData(endpoint));
        }
        else
        {
            Debug.LogError("CourseLoader1: UserId is not set. Unable to fetch courses!");
        }
    }

    IEnumerator FetchCourseData(string endpoint)
    {
        Debug.Log("CourseLoader1: Sending request to API.");
        using (UnityWebRequest request = ApiConfig.CreateRequest(endpoint))
        {
            yield return request.SendWebRequest();

            if (request.result == UnityWebRequest.Result.ConnectionError || request.result == UnityWebRequest.Result.ProtocolError)
            {
                Debug.LogError($"API Error: {request.error}");
            }
            else
            {
                string jsonData = request.downloadHandler.text;
                Debug.Log($"OK OK : {jsonData}");
                Debug.Log($"API Response: {jsonData}");

                ProcessCourseData(jsonData);
            }
        }
    }

   

    void ProcessCourseData(string jsonData)
    {
        Debug.Log("CourseLoader: Processing course data.");

        CourseResponse2 response = JsonConvert.DeserializeObject<CourseResponse2>(jsonData); // ✅ Use Newtonsoft.Json

        if (response != null && response.result != null && response.result.objectList != null)
        {
            Debug.Log($"CourseLoader: Found {response.result.objectList.Count} enrollments.");

            foreach (EnrollmentData enrollment in response.result.objectList) 
            {
                Debug.Log($"GET enrollment: {JsonConvert.SerializeObject(enrollment, Formatting.Indented)}"); // ✅ Better JSON debugging

                if (enrollment.courseResponse != null)
                {
                    CourseData course = enrollment.courseResponse;
                    Debug.Log($"OK OK : {JsonConvert.SerializeObject(course, Formatting.Indented)}");

                    // Truncate long title and description
                    course.title = TruncateString(course.title, 20);
                    course.description = TruncateString(course.description, 50);
                    nocourseText.SetActive(false);
                    // Create UI panel for each course
                    CreateCoursePanel(course);
                }
                else
                {
                    nocourseText.SetActive(true);
                    Debug.LogWarning("CourseLoader: Enrollment has no courseResponse!");
                }
            }
        }
        else
        {
            Debug.LogError("CourseLoader: Response data is null or invalid!");
            nocourseText.SetActive(true);

        }
    }

    void CreateCoursePanel(CourseData course)
    {
        Debug.Log($"CourseLoader: Creating panel for course: {course.title}");
        GameObject panel = Instantiate(coursePanelPrefab, contentParent);

        TMP_Text titleText = panel.transform.Find("course_titleText")?.GetComponent<TMP_Text>();
        TMP_Text descriptionText = panel.transform.Find("course_descriptionText")?.GetComponent<TMP_Text>();
        TMP_Text scoreText = panel.transform.Find("course_scoreText")?.GetComponent<TMP_Text>();
        TMP_Text statusText = panel.transform.Find("course_statusText")?.GetComponent<TMP_Text>();

        if (titleText != null) titleText.text = course.title;
        if (descriptionText != null) descriptionText.text = course.description;
        if (scoreText != null) scoreText.text = $"Lessons: {(course.numberOfLessons.HasValue ? course.numberOfLessons.Value.ToString() : "N/A")}";
        Button courseButton = panel.GetComponent<Button>();
        if (courseButton != null)
        {
            courseButton.onClick.AddListener(() => OnCourseClicked(course.id));
        }

        if (!string.IsNullOrEmpty(course.imageUrl))
        {
            Image imageComponent = panel.transform.Find("courseImage_background/course_image")?.GetComponent<Image>();
            if (imageComponent != null)
            {
                StartCoroutine(LoadCourseImage(course.imageUrl, imageComponent));
            }
        }
    }

    IEnumerator LoadCourseImage(string url, Image imageComponent)
    {
        Debug.Log($"CourseLoader: Loading image from {url}");
        using (UnityWebRequest textureRequest = UnityWebRequestTexture.GetTexture(url))
        {
            yield return textureRequest.SendWebRequest();

            if (textureRequest.result == UnityWebRequest.Result.ConnectionError || textureRequest.result == UnityWebRequest.Result.ProtocolError)
            {
                Debug.LogError($"Image Load Error: {textureRequest.error}");
            }
            else
            {
                Texture2D texture = DownloadHandlerTexture.GetContent(textureRequest);
                if (texture != null)
                {
                    imageComponent.sprite = Sprite.Create(texture, new Rect(0, 0, texture.width, texture.height), Vector2.one * 0.5f);
                    Debug.Log("CourseLoader: Image loaded successfully.");
                }
            }
        }
    }

    string TruncateString(string input, int maxLength)
    {
        if (!string.IsNullOrEmpty(input) && input.Length > maxLength)
        {
            return input.Substring(0, maxLength) + "...";
        }
        return input;
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

