using System.Collections;
using System.IO;
using TMPro;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.Networking;
using UnityEngine.UI;
using UnityEngine.SceneManagement;
namespace Code
{
    public class CourseDetailLoader : MonoBehaviour
    {
        [Header("UI References")]
        public TMP_Text courseTitleText;
        public TMP_Text courseDescriptionText;
        public TMP_Text courseDurationText;
        public TMP_Text courseParticipantsText;
        public TMP_Text courseTypeText;
        public Button ARButton;
        public TMP_Text shortDescriptionText;    
        public TMP_Text targetAudienceText;      
        public TMP_Text statusText;              
        public TMP_Text mandatoryText;           
        public TMP_Text numberOfLessonsText;     
        public TMP_Text companyIdText;  
    
        public Image courseImage; // UI Image component for the course image
        public GameObject detailPage; // Assign in Inspector (DetailPage)

        [Header("API Settings")]
        private string endpointTemplate = "/course/{0}"; 
        
        
        // void Start()
        // {
        //     RestoreLastPage();
        // }
        //
        // void RestoreLastPage()
        // {
        //     bool showHome = PlayerPrefs.GetInt("ShowHomePage", 0) == 1;
        //     bool showDetail = PlayerPrefs.GetInt("ShowDetailPage", 0) == 1;
        //     string lastCourseId = PlayerPrefs.GetString("SelectedCourseID", "");
        //
        //     if (showHome)
        //     {
        //         ShowHomePage();
        //     }
        //
        //     if (showDetail)
        //     {
        //         ShowDetailPage();
        //
        //         // ✅ Reload the last course if an ID is stored
        //         if (!string.IsNullOrEmpty(lastCourseId))
        //         {
        //             LoadCourseDetails(lastCourseId);
        //         }
        //     }
        //
        //     // ✅ Clear stored values after restoring
        //     PlayerPrefs.SetInt("ShowHomePage", 0);
        //     PlayerPrefs.SetInt("ShowDetailPage", 0);
        //     PlayerPrefs.SetString("SelectedCourseID", ""); 
        //     PlayerPrefs.Save();
        // }
        //
        //
        // void ShowHomePage()
        // {
        //     GameObject homePage = GameObject.Find("HomePage");
        //     if (homePage != null)
        //     {
        //         homePage.SetActive(true);
        //     }
        // }
        //
        // void ShowDetailPage()
        // {
        //     GameObject detailPage = GameObject.Find("DetailPage");
        //     if (detailPage != null)
        //     {
        //         detailPage.SetActive(true);
        //     }
        // }

        
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

            // ✅ Download and load image
            if (!string.IsNullOrEmpty(course.imageUrl) && courseImage != null)
            {
                StartCoroutine(DownloadAndLoadCourseImage(course.imageUrl, courseImage));
            }

            if (detailPage != null)
            {
                detailPage.SetActive(true);
            }
            
            // ✅ Add AR button click event
            if (ARButton != null)
            {
                Debug.Log("✅ arButton found!");
                ARButton.onClick.AddListener(() => OnClickLoadARScene(course.id));

            }
            else
            {
                Debug.LogError("❌ arButton not found inside detailPage!");
            }

        }

        IEnumerator DownloadAndLoadCourseImage(string imageUrl, Image imageComponent)
        {
            string filename = Path.GetFileName(imageUrl);
            string localPath = Path.Combine(Application.persistentDataPath, filename);

            // ✅ Load from local storage if available
            if (File.Exists(localPath))
            {
                Debug.Log($"📂 Loading cached image: {localPath}");
                yield return LoadImageFromLocal(localPath, imageComponent);
                yield break;
            }

            // ✅ Download the image
            using (UnityWebRequest request = UnityWebRequestTexture.GetTexture(imageUrl))
            {
                yield return request.SendWebRequest();

                if (request.result == UnityWebRequest.Result.ConnectionError || request.result == UnityWebRequest.Result.ProtocolError)
                {
                    Debug.LogError($"Image Download Error: {request.error}");
                }
                else
                {
                    Texture2D texture = DownloadHandlerTexture.GetContent(request);
                    if (texture != null)
                    {
                        // ✅ Save locally
                        File.WriteAllBytes(localPath, texture.EncodeToPNG());

                        // ✅ Apply to UI
                        imageComponent.sprite = Sprite.Create(texture, new Rect(0, 0, texture.width, texture.height), Vector2.one * 0.5f);
                    }
                }
            }
        }

        IEnumerator LoadImageFromLocal(string path, Image imageComponent)
        {
            byte[] imageData = File.ReadAllBytes(path);
            Texture2D texture = new Texture2D(2, 2);
            texture.LoadImage(imageData);
            imageComponent.sprite = Sprite.Create(texture, new Rect(0, 0, texture.width, texture.height), Vector2.one * 0.5f);
            yield return null;
        }
        
        
        public void OnClickLoadARScene(string courseId)
        {
            if (string.IsNullOrEmpty(courseId))
            {
                Debug.LogError("❌ Course ID is null or empty!");
                return;
            }

            // ✅ Save current Course ID and UI state
            PlayerPrefs.SetString("SelectedCourseID", courseId);
            PlayerPrefs.SetString("LastPage", "DetailPage"); 
            PlayerPrefs.SetInt("ShowHomePage", 1);
            PlayerPrefs.SetInt("ShowDetailPage", 1);
            PlayerPrefs.Save();

            Debug.Log($"✅ Saved Course ID: {courseId}");

            SceneManager.LoadScene("QRScanner");
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
}