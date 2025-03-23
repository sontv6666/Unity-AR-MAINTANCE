using System.Collections;
using System.IO;
using TMPro;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.Networking;
using UnityEngine.UI;
using UnityEngine.SceneManagement;
using Models;

namespace Code
{
    public class CourseDetailLoader : MonoBehaviour
    {
        [Header("UI References")] public TMP_Text courseTitleText;
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

        public GameObject loadingUIPanel; // ✅ Show loading UI
        public Slider progressBar;
        public TMP_Text progressText;


        [Header("API Settings")] private string courseApiEndpoint = "/course/";
        private string modelApiEndpoint = "/model/";
        private string fileDownloadBaseUrl = "/files/";


        public Image courseImage; // UI Image component for the course image
        public GameObject detailPage; // Assign in Inspector (DetailPage)


        private string selectedCourseId;
        private bool isDownloading = false;

        public void LoadCourseDetails(string courseId)
        {
            if (!string.IsNullOrEmpty(courseId))
            {
                selectedCourseId = courseId;
                StartCoroutine(FetchCourseData(courseId));
            }
            else
            {
                Debug.LogError("❌ No Course ID provided!");
            }
        }

        private IEnumerator FetchCourseData(string courseId)
        {
            string endpoint = courseApiEndpoint + courseId;
            UnityWebRequest request = ApiConfig.CreateRequest(endpoint);

            yield return request.SendWebRequest();

            if (request.result == UnityWebRequest.Result.Success)
            {
                string jsonResponse = request.downloadHandler.text;

                // ✅ Use the correct class
                var response = JsonUtility.FromJson<ApiResponse<CourseResult>>(jsonResponse);

                if (response != null && response.result != null)
                {
                    UpdateUI(response.result);
                }
            }
            else
            {
                Debug.LogError("❌ API Request Failed: " + request.error);
            }
        }




        private void UpdateUI(Models.CourseResult  course)
        {
            if (courseTitleText != null) courseTitleText.text = course.title;
            if (courseDescriptionText != null) courseDescriptionText.text = course.description;
            if (courseDurationText != null) courseDurationText.text = $"Duration: {course.duration} minutes";
            if (courseParticipantsText != null)
                courseParticipantsText.text = $"Participants: {course.numberOfParticipants}";
            if (courseTypeText != null) courseTypeText.text = $"Type: {course.type}";

            if (shortDescriptionText != null)
                shortDescriptionText.text = course.shortDescription ?? "No Short Description Available";
            if (targetAudienceText != null)
                targetAudienceText.text = course.targetAudience ?? "No Target Audience Specified";
            if (statusText != null) statusText.text = $"Status: {course.status}";
            if (mandatoryText != null) mandatoryText.text = course.isMandatory ? "Mandatory Course" : "Optional Course";
            if (numberOfLessonsText != null) numberOfLessonsText.text = $"Lessons: {course.numberOfLessons}";
            if (companyIdText != null) companyIdText.text = $"Company ID: {course.companyId}";

            if (!string.IsNullOrEmpty(course.imageUrl) && courseImage != null)
            {
                StartCoroutine(DownloadAndLoadCourseImage(course.imageUrl, courseImage));
            }

            if (detailPage != null)
            {
                detailPage.SetActive(true);
            }

            if (ARButton != null)
            {
                Debug.Log("✅ AR Button found!");
                ARButton.onClick.RemoveAllListeners(); // ✅ Remove old listeners
                ARButton.onClick.AddListener(() => OnClickLoadARScene(course.id));
            }
            else
            {
                Debug.LogError("❌ AR Button not found!");
            }
        }

        private IEnumerator DownloadAndLoadCourseImage(string imageUrl, Image imageComponent)
        {
            string filename = Path.GetFileName(imageUrl);
            string localPath = Path.Combine(Application.persistentDataPath, filename);

            if (File.Exists(localPath))
            {
                Debug.Log($"📂 Loading cached image: {localPath}");
                yield return LoadImageFromLocal(localPath, imageComponent);
                yield break;
            }

            using (UnityWebRequest request = UnityWebRequestTexture.GetTexture(imageUrl))
            {
                yield return request.SendWebRequest();

                if (request.result == UnityWebRequest.Result.Success)
                {
                    Texture2D texture = DownloadHandlerTexture.GetContent(request);
                    if (texture != null)
                    {
                        File.WriteAllBytes(localPath, texture.EncodeToPNG());
                        imageComponent.sprite = Sprite.Create(texture, new Rect(0, 0, texture.width, texture.height),
                            Vector2.one * 0.5f);
                    }
                }
                else
                {
                    Debug.LogError($"Image Download Error: {request.error}");
                }
            }
        }






        IEnumerator DownloadFile(string fileUrl, string fileName)
        {
            string savePath = Path.Combine(Application.persistentDataPath, fileName);

            if (File.Exists(savePath))
            {
                Debug.Log($"📌 File already exists: {fileName}");
                yield break;
            }

            UnityWebRequest request = ApiConfig.CreateRequest(fileUrl);
            request.downloadHandler = new DownloadHandlerFile(savePath);
            UnityWebRequestAsyncOperation operation = request.SendWebRequest();

            while (!operation.isDone)
            {
                float progress = operation.progress;
                UpdateDownloadProgress(progress, $"Downloading: {fileName}");
                yield return null;
            }

            if (request.result == UnityWebRequest.Result.Success)
            {
                Debug.Log($"✅ File downloaded: {savePath}");
            }
            else
            {
                Debug.LogError($"❌ Download failed: {fileUrl} Error: {request.error}");
                UpdateUIText("Download Error", request.error);
                isDownloading = false;
                loadingUIPanel.SetActive(false);
            }
        }

        void UpdateDownloadProgress(float progress, string message)
        {
            if (progressBar != null) progressBar.value = progress;
            if (progressText != null) progressText.text = $"{message} ({(progress * 100):F0}%)";
        }

        void UpdateUIText(string title, string message)
        {
            Debug.Log($"{title}: {message}");
        }

        IEnumerator LoadImageFromLocal(string path, Image imageComponent)
        {
            byte[] imageData = File.ReadAllBytes(path);
            Texture2D texture = new Texture2D(2, 2);
            texture.LoadImage(imageData);
            imageComponent.sprite =
                Sprite.Create(texture, new Rect(0, 0, texture.width, texture.height), Vector2.one * 0.5f);
            yield return null;
        }

        public void OnClickLoadARScene(string courseId)
        {
            if (string.IsNullOrEmpty(courseId))
            {
                Debug.LogError("❌ Course ID is null or empty!");
                return;
            }

            // ✅ Show loading UI
            loadingUIPanel.SetActive(true);
            progressBar.value = 0;
            progressText.text = "Preparing to download...";

            Debug.Log($"🔄 Loading AR Scene for Course ID: {courseId}");


            // ✅ Save current Course ID and UI state
            PlayerPrefs.SetString("SelectedCourseID", courseId);
            PlayerPrefs.SetString("LastPage", "DetailPage");
            PlayerPrefs.SetInt("ShowHomePage", 1);
            PlayerPrefs.SetInt("ShowDetailPage", 1);
            PlayerPrefs.Save();
            // ✅ Start model download
            StartCoroutine(FetchAndDownloadModel(courseId));
        }


        IEnumerator FetchAndDownloadModel(string courseId)
        {
            string endpoint = courseApiEndpoint + courseId; // Fetch course details
            UnityWebRequest request = ApiConfig.CreateRequest(endpoint);

            yield return request.SendWebRequest();

            if (request.result == UnityWebRequest.Result.Success)
            {
                // ✅ Correctly parse the response
                var response = JsonUtility.FromJson<ApiResponse<CourseResult>>(request.downloadHandler.text);

                if (response != null && response.result != null)
                {
                    CourseResult courseData = response.result;

                    if (!string.IsNullOrEmpty(courseData.modelId))
                    {
                        Debug.Log($"✅ Found Model ID: {courseData.modelId}, starting download...");
                        yield return StartCoroutine(DownloadModelFile(courseData.modelId));
                    }
                    else
                    {
                        Debug.LogError("❌ No Model ID found for this course.");
                        loadingUIPanel.SetActive(false);
                    }
                }
            }
            else
            {
                Debug.LogError("❌ Failed to fetch course data: " + request.error);
                loadingUIPanel.SetActive(false);
            }
        }


       IEnumerator DownloadModelFile(string modelId)
{
    string endpoint = modelApiEndpoint + modelId;
    UnityWebRequest request = ApiConfig.CreateRequest(endpoint);

    yield return request.SendWebRequest();

    if (request.result == UnityWebRequest.Result.Success)
    {
        string jsonResponse = request.downloadHandler.text;
        Debug.Log("📡 API Response: " + jsonResponse);

        ApiResponse<ModelDataResult> response = JsonUtility.FromJson<ApiResponse<ModelDataResult>>(jsonResponse);

        if (response == null || response.result == null || string.IsNullOrEmpty(response.result.file))
        {
            Debug.LogError("❌ modelData.file is null or API response is invalid!");
            yield break; // Stop execution
        }

        string modelFilePath = Path.Combine(Application.persistentDataPath, response.result.file);

        if (!File.Exists(modelFilePath))
        {
            yield return StartCoroutine(DownloadFile(fileDownloadBaseUrl + response.result.file, response.result.file));
        }

        Debug.Log("✅ Model downloaded successfully. Loading AR Scene...");
        SceneManager.LoadScene("ARVRScanner");
    }
    else
    {
        Debug.LogError("❌ Failed to fetch model data: " + request.error);
        loadingUIPanel.SetActive(false);
    }
}

        
        public void BackToHomePage()
        {
            Debug.Log("🔙 Hiding Course Detail Page and returning to Home...");

            // ✅ Hide the Detail Page
            if (detailPage != null)
            {
                detailPage.SetActive(false);
            }

            // ✅ Show Home Page (if needed)
            GameObject homePage = GameObject.Find("HomePage"); // Change this to your actual Home Page GameObject name
            if (homePage != null)
            {
                homePage.SetActive(true);
            }
        }




    }

}



