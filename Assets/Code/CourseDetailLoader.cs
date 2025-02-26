using System.Collections;
using UnityEngine;
using UnityEngine.Networking;
using UnityEngine.UI;
using TMPro;

public class CourseDetailLoader : MonoBehaviour
{
    [Header("UI References")]
    public TMP_Text courseTitleText;
    public TMP_Text courseDescriptionText;
    public TMP_Text courseDurationText;
    public TMP_Text courseParticipantsText;
    public TMP_Text courseTypeText;
    
    public TMP_Text shortDescriptionText;     // New
    public TMP_Text targetAudienceText;       // New
    public TMP_Text statusText;               // New
    public TMP_Text mandatoryText;            // New
    public TMP_Text numberOfLessonsText;      // New
    public TMP_Text companyIdText;  
    public Image courseImage; // Add this for image loading
    public GameObject detailPage; // Assign in Inspector (DetailPage)

    [Header("API Settings")]
    private string endpointTemplate = "course/{0}"; 
        
    public void LoadCourseDetails(string courseId)
    {
        if (!string.IsNullOrEmpty(courseId))
        {
            string endpoint = string.Format(endpointTemplate, courseId);
            StartCoroutine(FetchCourseDetails(endpoint));
        }
        else
        {
            Debug.LogError("CourseDetailLoader: No course ID provided!");
        }
    }

    IEnumerator FetchCourseDetails(string endpoint)
    {
        using (UnityWebRequest request = ApiConfig.CreateRequest(endpoint))
        {
            yield return request.SendWebRequest();

            if (request.result == UnityWebRequest.Result.ConnectionError || request.result == UnityWebRequest.Result.ProtocolError)
            {
                Debug.LogError($"CourseDetailLoader: API Error: {request.error}");
            }
            else
            {
                string jsonData = request.downloadHandler.text;
                CourseDetailResponse response = JsonUtility.FromJson<CourseDetailResponse>(jsonData);

                if (response != null && response.result != null)
                {
                    UpdateUI(response.result);
                }
                else
                {
                    Debug.LogError("CourseDetailLoader: Invalid API response!");
                }
            }
        }
    }

    void UpdateUI(CourseDetailData course)
    {
        if (courseTitleText != null) courseTitleText.text = course.title;
        if (courseDescriptionText != null) courseDescriptionText.text = course.description;
        if (courseDurationText != null) courseDurationText.text = $"Duration: {course.duration} minutes";
        if (courseParticipantsText != null) courseParticipantsText.text = $"Participants: {course.numberOfParticipants}";
        if (courseTypeText != null) courseTypeText.text = $"Type: {course.type}";

        // New fields
        if (shortDescriptionText != null) shortDescriptionText.text = course.shortDescription ?? "No Short Description Available";
        if (targetAudienceText != null) targetAudienceText.text = course.targetAudience ?? "No Target Audience Specified";
        if (statusText != null) statusText.text = $"Status: {course.status}";
        if (mandatoryText != null) mandatoryText.text = course.isMandatory ? "Mandatory Course" : "Optional Course";
        if (numberOfLessonsText != null) numberOfLessonsText.text = $"Lessons: {course.numberOfLessons}";
        if (companyIdText != null) companyIdText.text = $"Company ID: {course.companyId}";

        // Load image if available
        if (!string.IsNullOrEmpty(course.imageUrl) && courseImage != null)
        {
            StartCoroutine(LoadCourseImage(course.imageUrl, courseImage));
        }

        if (detailPage != null)
        {
            detailPage.SetActive(true);
        }
    }


    IEnumerator LoadCourseImage(string url, Image imageComponent)
    {
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
                }
            }
        }
    }
}


    


[System.Serializable]
public class CourseDetailResponse
{
    public int code;
    public CourseDetailData result;
}

[System.Serializable]
public class CourseDetailData
{
    public string id;
    public string companyId; // Added
    public string title;
    public string description;
    public string shortDescription; // Added
    public string targetAudience; // Added
    public int duration;
    public bool isMandatory; // Added
    public string imageUrl;
    public int numberOfLessons; // Added
    public int numberOfParticipants;
    public string status; // Added
    public string type;
}
