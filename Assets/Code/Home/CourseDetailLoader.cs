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
        [Header("UI References")] 
        public TMP_Text courseTitleText;
        public TMP_InputField courseDescriptionText;
        public TMP_Text courseDurationText;
        public TMP_Text courseParticipantsText;
        public TMP_Text courseTypeText;
        public TMP_Text shortDescriptionText;
        public TMP_Text targetAudienceText;
        public TMP_Text statusText;
        public TMP_Text mandatoryText;
        public TMP_Text numberOfLessonsText;
        public TMP_Text companyIdText;
        
        // Thêm Circular Progress Spinner và Overlay
        public GameObject loadingSpinner;  // Spinner Circular
        public GameObject overlay;         // Màn hình mờ

        
        [Header("Machine Type UI")]
        public TMP_Text machineTypeNameText; // ✅ UI for Machine Type Name
        public Transform machineAttributesContainer; // ✅ Parent for attributes
        public GameObject machineAttributePrefab; // ✅ Prefab for attributes (TMP_Text)

        
        
        public Button ARButton;
        public Image courseImage;
        public TMP_Text progressText;
        public Slider progressBar;
        public GameObject homePage, loadingUIPanel, detailPage;



        [Header("API Settings")]
        private const string CourseApiEndpoint = "/course/";
        private const string ModelApiEndpoint = "/model/";
        private const string FileDownloadBaseUrl = "/files/";
        

        private string selectedCourseId;
        private bool isDownloading = false;
        private CourseResult cachedCourseData;
        
        public void LoadCourseDetails(string courseId)
        {
            if (string.IsNullOrEmpty(courseId))
            {
                Debug.LogError("❌ No Course ID provided!");
                return;
            }
            loadingSpinner.SetActive(true);
            overlay.SetActive(true);
            selectedCourseId = courseId;
            StartCoroutine(FetchCourseData(courseId));
        }

        private IEnumerator FetchCourseData(string courseId)
        {
            string endpoint = CourseApiEndpoint + courseId;
            using (UnityWebRequest request = ApiConfig.CreateRequest(endpoint))
            {
                yield return request.SendWebRequest();

                if (request.result != UnityWebRequest.Result.Success)
                {
                    Debug.LogError($"❌ API Request Failed: {request.error}");
                    yield break;
                }

                var response = JsonUtility.FromJson<ApiResponse<CourseResult>>(request.downloadHandler.text);
                if (response?.result != null)
                {
                    cachedCourseData = response.result; // Cache course data
                    UpdateUI(cachedCourseData);
                }
            }
            loadingSpinner.SetActive(false);
            overlay.SetActive(false);
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

            
            if (!string.IsNullOrEmpty(course.imageUrl))
            {
                StartCoroutine(DownloadAndLoadCourseImage(course.imageUrl));
            }
            
            if (!string.IsNullOrEmpty(course.machineTypeId))
            {
                StartCoroutine(FetchMachineTypeDetails(course.machineTypeId));
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

        private IEnumerator DownloadAndLoadCourseImage(string imageUrl)
        {
            string filename = Path.GetFileName(imageUrl);
            string localPath = Path.Combine(Application.persistentDataPath, filename);

            if (File.Exists(localPath))
            {
                yield return LoadImageFromLocal(localPath);
                yield break;
            }

            using (UnityWebRequest request = UnityWebRequestTexture.GetTexture(imageUrl))
            {
                yield return request.SendWebRequest();

                if (request.result == UnityWebRequest.Result.Success)
                {
                    Texture2D texture = DownloadHandlerTexture.GetContent(request);
                    File.WriteAllBytes(localPath, texture.EncodeToPNG());
                    ApplyTextureToImage(texture);
                }
                else
                {
                    Debug.LogError($"❌ Image Download Error: {request.error}");
                }
            }
        }


        private IEnumerator FetchMachineTypeDetails(string machineTypeId)
        {
            if (string.IsNullOrEmpty(machineTypeId))
            {
                Debug.LogWarning("⚠️ No Machine Type ID found!");
                yield break;
            }

            string endpoint = "/machine-type/" + machineTypeId;
            using (UnityWebRequest request = ApiConfig.CreateRequest(endpoint))
            {
                yield return request.SendWebRequest();

                if (request.result != UnityWebRequest.Result.Success)
                {
                    Debug.LogError($"❌ Failed to fetch Machine Type: {request.error}");
                    yield break;
                }

                var response = JsonUtility.FromJson<ApiResponse<MachineTypeResponse>>(request.downloadHandler.text);
                if (response?.result != null)
                {
                    DisplayMachineType(response.result);
                }
            }
        }

        
        private void DisplayMachineType(MachineTypeResponse machineType)
        {
            if (machineTypeNameText != null)
            {
                machineTypeNameText.text = machineType.machineTypeName;
            }

            // ✅ Xóa thuộc tính cũ trước khi thêm mới
            foreach (Transform child in machineAttributesContainer)
            {
                Destroy(child.gameObject);
            }

            // ✅ Hiển thị danh sách thuộc tính của máy
            foreach (var attribute in machineType.machineTypeAttributeResponses)
            {
                GameObject newAttribute = Instantiate(machineAttributePrefab, machineAttributesContainer);

                // 🔹 Tìm TMP_Text trong "MachineType"
                Transform machineTypeTransform = newAttribute.transform.Find("MachineType");
                if (machineTypeTransform != null)
                {
                    TMP_Text attributeText = machineTypeTransform.GetComponent<TMP_Text>();
                    if (attributeText != null)
                    {
                        attributeText.text = $"<b>{attribute.attributeName}</b>: {attribute.valueAttribute}";
                    }
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

        private IEnumerator LoadImageFromLocal(string path)
        {
            byte[] imageData = File.ReadAllBytes(path);
            Texture2D texture = new Texture2D(2, 2);
            texture.LoadImage(imageData);
            ApplyTextureToImage(texture);
            yield return null;
        }

        private void ApplyTextureToImage(Texture2D texture)
        {
            courseImage.sprite = Sprite.Create(texture, new Rect(0, 0, texture.width, texture.height), Vector2.one * 0.5f);
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
            overlay.SetActive(true);
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
            string endpoint = CourseApiEndpoint  + courseId; // Fetch course details
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


        private IEnumerator DownloadModelFile(string modelId)
        {
            string endpoint = ModelApiEndpoint + modelId;
            using (UnityWebRequest request = ApiConfig.CreateRequest(endpoint))
            {
                yield return request.SendWebRequest();

                if (request.result != UnityWebRequest.Result.Success)
                {
                    Debug.LogError($"❌ Failed to fetch model data: {request.error}");
                    loadingUIPanel.SetActive(false);
                    yield break;
                }

                var response = JsonUtility.FromJson<ApiResponse<ModelDataResult>>(request.downloadHandler.text);
                if (response?.result == null || string.IsNullOrEmpty(response.result.file))
                {
                    Debug.LogError("❌ Model file is null or API response is invalid!");
                    yield break;
                }

                string modelFilePath = Path.Combine(Application.persistentDataPath, response.result.file);
                if (!File.Exists(modelFilePath))
                {
                    yield return StartCoroutine(DownloadFile(FileDownloadBaseUrl + response.result.file, response.result.file));
                }

                SceneManager.LoadScene("ARVRScanner");
            }
        }


public void BackToHomePage()
{
    Debug.Log("🔙 Hiding Course Detail Page and returning to Home...");

    if (detailPage != null)
    {
        detailPage.SetActive(false); // ✅ Hide Detail Page
    }

    if (homePage != null)
    {
        homePage.SetActive(true); // ✅ Show Home Page even if it was inactive
    }
    else
    {
        Debug.LogWarning("⚠️ HomePage reference is missing! Make sure to assign it.");
    }
}






    }
    

}