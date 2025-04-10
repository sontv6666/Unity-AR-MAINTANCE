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
        
        // Loading UI Elements
        public GameObject loadingSpinner;
        public GameObject overlay;
        
        [Header("Machine Type UI")]
        public TMP_Text machineTypeNameText;
        public Transform machineAttributesContainer;
        public GameObject machineAttributePrefab;
        
        [Header("Action Buttons")]
        public Button ARButton;
        public Button backButton;
        
        [Header("Course Visual Elements")]
        public Image courseImage;
        public Image backgroundPanel;
        public Transform courseInfoPanel;
        
        [Header("Download Progress UI")]
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
        
        private void Start()
        {
            SetupUI();
            
            // Setup back button
            if (backButton != null)
            {
                backButton.onClick.AddListener(BackToHomePage);
            }
        }
        
        private void SetupUI()
        {
            // Apply beautiful styles to the UI
            if (detailPage != null)
            {
                // Style the background panel
                if (backgroundPanel != null)
                {
                    backgroundPanel.color = new Color(0.95f, 0.97f, 1f); // Light blue background
                }
                
                // Style course info panel
                if (courseInfoPanel != null)
                {
                    // Add shadow and rounded corners via components if needed
                    VerticalLayoutGroup layout = courseInfoPanel.GetComponent<VerticalLayoutGroup>();
                    if (layout == null)
                    {
                        layout = courseInfoPanel.gameObject.AddComponent<VerticalLayoutGroup>();
                    }
                    layout.padding = new RectOffset(20, 20, 20, 20);
                    layout.spacing = 15;
                    layout.childAlignment = TextAnchor.UpperLeft;
                    layout.childControlWidth = true;
                    layout.childForceExpandWidth = true;
                }
                
                // Style title text
                if (courseTitleText != null)
                {
                    courseTitleText.fontSize = 28;
                    courseTitleText.fontStyle = FontStyles.Bold;
                    courseTitleText.color = new Color(0.1f, 0.2f, 0.6f); // Dark blue
                    courseTitleText.alignment = TextAlignmentOptions.Center;
                }
                
                // Style description input field
                if (courseDescriptionText != null)
                {
                    courseDescriptionText.textComponent.fontSize = 16;
                    courseDescriptionText.textComponent.color = new Color(0.2f, 0.2f, 0.2f);
                }
                
                // Style info texts with consistent formatting
                StyleInfoText(courseDurationText);
                StyleInfoText(courseParticipantsText);
                StyleInfoText(courseTypeText);
                StyleInfoText(shortDescriptionText);
                StyleInfoText(targetAudienceText);
                StyleInfoText(statusText);
                StyleInfoText(mandatoryText);
                StyleInfoText(numberOfLessonsText);
                StyleInfoText(companyIdText);
                
                // Style machine type section
                if (machineTypeNameText != null)
                {
                    machineTypeNameText.fontSize = 20;
                    machineTypeNameText.fontStyle = FontStyles.Bold;
                    machineTypeNameText.color = new Color(0.1f, 0.5f, 0.3f); // Dark green
                }
                
                // Style AR Button
                if (ARButton != null)
                {
                    Button btn = ARButton.GetComponent<Button>();
                    ColorBlock colors = btn.colors;
                    colors.normalColor = new Color(0.2f, 0.6f, 1f); // Light blue
                    colors.highlightedColor = new Color(0.3f, 0.7f, 1f);
                    colors.pressedColor = new Color(0.1f, 0.5f, 0.9f);
                    btn.colors = colors;
                    
                    // Style the button text if it exists
                    TMP_Text btnText = ARButton.GetComponentInChildren<TMP_Text>();
                    if (btnText != null)
                    {
                        btnText.fontSize = 18;
                        btnText.fontStyle = FontStyles.Bold;
                        btnText.color = Color.white;
                    }
                }
                
                // Style back button
                if (backButton != null)
                {
                    Button btn = backButton.GetComponent<Button>();
                    ColorBlock colors = btn.colors;
                    colors.normalColor = new Color(0.7f, 0.7f, 0.7f); // Gray
                    colors.highlightedColor = new Color(0.8f, 0.8f, 0.8f);
                    colors.pressedColor = new Color(0.6f, 0.6f, 0.6f);
                    btn.colors = colors;
                    
                    // Style the button text if it exists
                    TMP_Text btnText = backButton.GetComponentInChildren<TMP_Text>();
                    if (btnText != null)
                    {
                        btnText.fontSize = 16;
                        btnText.fontStyle = FontStyles.Bold;
                        btnText.color = Color.white;
                    }
                }
                
                // Style progress bar
                if (progressBar != null)
                {
                    Image fillImage = progressBar.fillRect.GetComponent<Image>();
                    if (fillImage != null)
                    {
                        fillImage.color = new Color(0.2f, 0.7f, 0.3f); // Green progress
                    }
                }
                
                if (progressText != null)
                {
                    progressText.fontSize = 16;
                    progressText.color = new Color(0.2f, 0.2f, 0.2f);
                }
                
                // Style loading spinner (make it more visible)
                if (loadingSpinner != null)
                {
                    Image spinnerImage = loadingSpinner.GetComponent<Image>();
                    if (spinnerImage != null)
                    {
                        spinnerImage.color = new Color(0.2f, 0.6f, 1f); // Blue spinner
                    }
                }
                
                // Style overlay for better contrast
                if (overlay != null)
                {
                    Image overlayImage = overlay.GetComponent<Image>();
                    if (overlayImage != null)
                    {
                        overlayImage.color = new Color(0f, 0f, 0f, 0.7f); // Semi-transparent black
                    }
                }
            }
        }
        
        private void StyleInfoText(TMP_Text textElement)
        {
            if (textElement != null)
            {
                textElement.fontSize = 16;
                textElement.color = new Color(0.2f, 0.2f, 0.2f); // Dark gray
                textElement.alignment = TextAlignmentOptions.Left;
                
                // Add visual separation with underlines for section headers
                if (textElement == courseTypeText || textElement == targetAudienceText || 
                    textElement == statusText || textElement == numberOfLessonsText)
                {
                    textElement.fontStyle = FontStyles.Bold;
                }
            }
        }
        
        public void LoadCourseDetails(string courseId)
        {
            if (string.IsNullOrEmpty(courseId))
            {
                Debug.LogError("❌ No Course ID provided!");
                return;
            }
            
            // Show loading indicators
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

        private void UpdateUI(Models.CourseResult course)
        {
            // Main course details
            if (courseTitleText != null) courseTitleText.text = course.title;
            if (courseDescriptionText != null) courseDescriptionText.text = course.description;
            
            // Format duration to be more readable
            if (courseDurationText != null)
            {
                int? hours = course.duration / 60;
                int? minutes = course.duration % 60;
                
                if (hours > 0)
                {
                    courseDurationText.text = $"<b>Duration:</b> {hours}h {minutes}m";
                }
                else
                {
                    courseDurationText.text = $"<b>Duration:</b> {minutes} minutes";
                }
            }
            
            // Enhanced info formatting with bold labels
            if (courseParticipantsText != null)
                courseParticipantsText.text = $"<b>Participants:</b> {course.numberOfParticipants}";
            if (courseTypeText != null) 
                courseTypeText.text = $"<b>Type:</b> {course.type}";
            if (shortDescriptionText != null)
                shortDescriptionText.text = $"<b>Overview:</b> {course.shortDescription ?? "No Short Description Available"}";
            if (targetAudienceText != null)
                targetAudienceText.text = $"<b>Target Audience:</b> {course.targetAudience ?? "No Target Audience Specified"}";
            if (statusText != null) 
                statusText.text = $"<b>Status:</b> {course.status}";
            
            // Highlight mandatory courses
            if (mandatoryText != null)
            {
                mandatoryText.text = course.isMandatory ? "⚠️ Mandatory Course" : "Optional Course";
                mandatoryText.color = course.isMandatory ? new Color(0.8f, 0.2f, 0.2f) : new Color(0.2f, 0.7f, 0.3f);
                mandatoryText.fontStyle = FontStyles.Bold;
            }
            
            if (numberOfLessonsText != null) 
                numberOfLessonsText.text = $"<b>Lessons:</b> {course.numberOfLessons}";
            if (companyIdText != null) 
                companyIdText.text = $"<b>Company ID:</b> {course.companyId}";
            
            // Load course image with fade-in effect
            if (!string.IsNullOrEmpty(course.imageUrl))
            {
                StartCoroutine(DownloadAndLoadCourseImage(course.imageUrl));
            }
            
            // Load machine type details if available
            if (!string.IsNullOrEmpty(course.machineTypeId))
            {
                StartCoroutine(FetchMachineTypeDetails(course.machineTypeId));
            }
            
            // Show the detail page
            if (detailPage != null)
            {
                detailPage.SetActive(true);
                
                // Apply animation effect: fade in
                CanvasGroup canvasGroup = detailPage.GetComponent<CanvasGroup>();
                if (canvasGroup == null)
                {
                    canvasGroup = detailPage.AddComponent<CanvasGroup>();
                }
                StartCoroutine(FadeIn(canvasGroup, 0.5f));
            }

            // Setup AR button
            if (ARButton != null)
            {
                Debug.Log("✅ AR Button found!");
                ARButton.onClick.RemoveAllListeners();
                ARButton.onClick.AddListener(() => OnClickLoadARScene(course.id));
                
                // Disable AR button if no model is available
                ARButton.interactable = !string.IsNullOrEmpty(course.modelId);
            }
            else
            {
                Debug.LogError("❌ AR Button not found!");
            }
        }

        private IEnumerator FadeIn(CanvasGroup canvasGroup, float duration)
        {
            canvasGroup.alpha = 0f;
            
            float startTime = Time.time;
            while (Time.time < startTime + duration)
            {
                float t = (Time.time - startTime) / duration;
                canvasGroup.alpha = Mathf.Lerp(0f, 1f, t);
                yield return null;
            }
            
            canvasGroup.alpha = 1f;
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
                machineTypeNameText.text = $"Machine Type: {machineType.machineTypeName}";
            }

            // Clear existing attributes
            foreach (Transform child in machineAttributesContainer)
            {
                Destroy(child.gameObject);
            }

            // Display machine attributes with styled appearance
            foreach (var attribute in machineType.machineTypeAttributeResponses)
            {
                GameObject newAttribute = Instantiate(machineAttributePrefab, machineAttributesContainer);

                Transform machineTypeTransform = newAttribute.transform.Find("MachineType");
                if (machineTypeTransform != null)
                {
                    TMP_Text attributeText = machineTypeTransform.GetComponent<TMP_Text>();
                    if (attributeText != null)
                    {
                        // Apply styling to attributes
                        attributeText.text = $"<b>{attribute.attributeName}</b>: {attribute.valueAttribute}";
                        attributeText.fontSize = 14;
                        attributeText.color = new Color(0.2f, 0.2f, 0.2f);
                        
                        // Add visual indicator for important attributes (optional)
                        if (attribute.attributeName.ToLower().Contains("important") || 
                            attribute.attributeName.ToLower().Contains("critical"))
                        {
                            attributeText.color = new Color(0.8f, 0.2f, 0.2f); // Red for important attributes
                        }
                    }
                }
            }
        }

        IEnumerator DownloadFile(string fileUrl, string fileName)
        {
            string savePath = Path.Combine(Application.persistentDataPath, fileName);

            // First check the file size on the server
            yield return StartCoroutine(CheckFileSize(fileUrl, savePath));

            if (!isDownloading)
            {
                Debug.Log($"📌 File already exists and is up to date: {fileName}");
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

        IEnumerator CheckFileSize(string fileUrl, string localFilePath)
        {
            // Check if file exists locally
            if (File.Exists(localFilePath))
            {
                // Get the size endpoint from the URL
                string sizeEndpoint = fileUrl + "/size";
                using (UnityWebRequest sizeRequest = ApiConfig.CreateRequest(sizeEndpoint))
                {
                    yield return sizeRequest.SendWebRequest();

                    if (sizeRequest.result == UnityWebRequest.Result.Success)
                    {
                        // Parse the size response
                        long serverFileSize;
                        if (long.TryParse(sizeRequest.downloadHandler.text, out serverFileSize))
                        {
                            // Get local file size
                            FileInfo fileInfo = new FileInfo(localFilePath);
                            long localFileSize = fileInfo.Length;

                            // Compare sizes
                            if (localFileSize == serverFileSize)
                            {
                                // File is complete and up to date
                                isDownloading = false;
                                UpdateDownloadProgress(1f, $"File up to date: {Path.GetFileName(localFilePath)}");
                                yield break;
                            }
                            else
                            {
                                Debug.Log($"⚠️ File size mismatch - Local: {localFileSize}, Server: {serverFileSize}. Re-downloading...");
                                
                                // Delete the outdated file before re-downloading
                                try
                                {
                                    File.Delete(localFilePath);
                                    Debug.Log($"🗑️ Deleted outdated file: {localFilePath}");
                                }
                                catch (System.Exception e)
                                {
                                    Debug.LogError($"❌ Failed to delete outdated file: {e.Message}");
                                    
                                    // If we can't delete it, try renaming it as a backup
                                    try
                                    {
                                        string backupPath = localFilePath + ".bak";
                                        if (File.Exists(backupPath))
                                            File.Delete(backupPath);
                                            
                                        File.Move(localFilePath, backupPath);
                                        Debug.Log($"📝 Renamed outdated file to: {backupPath}");
                                    }
                                    catch (System.Exception ex)
                                    {
                                        Debug.LogError($"❌ Failed to backup outdated file: {ex.Message}");
                                    }
                                }
                                
                                isDownloading = true;
                            }
                        }
                        else
                        {
                            Debug.LogError($"❌ Failed to parse file size response: {sizeRequest.downloadHandler.text}");
                            isDownloading = true; // Download anyway to be safe
                        }
                    }
                    else
                    {
                        Debug.LogWarning($"⚠️ Couldn't check file size: {sizeRequest.error}. Will download anyway.");
                        isDownloading = true;
                    }
                }
            }
            else
            {
                // File doesn't exist locally, need to download
                isDownloading = true;
            }
        }

        void UpdateDownloadProgress(float progress, string message)
        {
            if (progressBar != null) 
            {
                progressBar.value = progress;
                // Add visual feedback with color change
                Image fillImage = progressBar.fillRect.GetComponent<Image>();
                if (fillImage != null)
                {
                    // Change color based on progress - red to yellow to green
                    fillImage.color = Color.Lerp(
                        Color.Lerp(new Color(0.8f, 0.2f, 0.2f), new Color(0.9f, 0.9f, 0.2f), progress * 2f), 
                        new Color(0.2f, 0.8f, 0.2f), 
                        progress > 0.5f ? (progress - 0.5f) * 2f : 0f);
                }
            }
            
            if (progressText != null)
            {
                progressText.text = $"{message} ({(progress * 100):F0}%)";
                
                // Add visual emphasis for completion
                if (progress >= 0.99f)
                {
                    progressText.fontStyle = FontStyles.Bold;
                    progressText.color = new Color(0.2f, 0.7f, 0.3f); // Green for completed
                }
                else
                {
                    progressText.fontStyle = FontStyles.Normal;
                    progressText.color = new Color(0.2f, 0.2f, 0.2f); // Normal color
                }
            }
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
            if (courseImage != null)
            {
                // Create sprite with proper settings
                courseImage.sprite = Sprite.Create(
                    texture, 
                    new Rect(0, 0, texture.width, texture.height), 
                    Vector2.one * 0.5f
                );
                
                // Apply image aspect ratio settings
                AspectRatioFitter fitter = courseImage.GetComponent<AspectRatioFitter>();
                if (fitter == null)
                {
                    fitter = courseImage.gameObject.AddComponent<AspectRatioFitter>();
                }
                fitter.aspectMode = AspectRatioFitter.AspectMode.FitInParent;
                fitter.aspectRatio = (float)texture.width / texture.height;
                
                // Add fade-in effect
                StartCoroutine(FadeInImage(courseImage, 0.5f));
            }
        }
        
        private IEnumerator FadeInImage(Image image, float duration)
        {
            image.color = new Color(1f, 1f, 1f, 0f);
            
            float startTime = Time.time;
            while (Time.time < startTime + duration)
            {
                float t = (Time.time - startTime) / duration;
                image.color = new Color(1f, 1f, 1f, Mathf.Lerp(0f, 1f, t));
                yield return null;
            }
            
            image.color = new Color(1f, 1f, 1f, 1f);
        }
        
        public void OnClickLoadARScene(string courseId)
        {
            if (string.IsNullOrEmpty(courseId))
            {
                Debug.LogError("❌ Course ID is null or empty!");
                return;
            }

            // Show loading UI with enhanced styling
            loadingUIPanel.SetActive(true);
            overlay.SetActive(true);
            progressBar.value = 0;
            progressText.text = "Preparing AR experience...";

            Debug.Log($"🔄 Loading AR Scene for Course ID: {courseId}");

            // Save current Course ID and UI state
            PlayerPrefs.SetString("SelectedCourseID", courseId);
            PlayerPrefs.SetString("LastPage", "DetailPage");
            PlayerPrefs.SetInt("ShowHomePage", 1);
            PlayerPrefs.SetInt("ShowDetailPage", 1);
            PlayerPrefs.Save();
            
            // Start model download
            StartCoroutine(FetchAndDownloadModel(courseId));
        }

        IEnumerator FetchAndDownloadModel(string courseId)
        {
            string endpoint = CourseApiEndpoint + courseId;
            UnityWebRequest request = ApiConfig.CreateRequest(endpoint);

            yield return request.SendWebRequest();

            if (request.result == UnityWebRequest.Result.Success)
            {
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
                        DisplayErrorMessage("No AR model available for this course");
                        loadingUIPanel.SetActive(false);
                    }
                }
            }
            else
            {
                Debug.LogError("❌ Failed to fetch course data: " + request.error);
                DisplayErrorMessage("Failed to load course data");
                loadingUIPanel.SetActive(false);
            }
        }
        
        private void DisplayErrorMessage(string message)
        {
            // Create and show an error message to the user
            if (progressText != null)
            {
                progressText.text = "Error: " + message;
                progressText.color = new Color(0.8f, 0.2f, 0.2f); // Red for error
                
                // Auto-hide after a few seconds
                StartCoroutine(AutoHideError(3f)); 
            }
        }
        
        private IEnumerator AutoHideError(float delay)
        {
            yield return new WaitForSeconds(delay);
            overlay.SetActive(false);
            loadingUIPanel.SetActive(false);
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
                    DisplayErrorMessage("Failed to fetch AR model");
                    loadingUIPanel.SetActive(false);
                    yield break;
                }

                var response = JsonUtility.FromJson<ApiResponse<ModelDataResult>>(request.downloadHandler.text);
                if (response?.result == null || string.IsNullOrEmpty(response.result.file))
                {
                    Debug.LogError("❌ Model file is null or API response is invalid!");
                    DisplayErrorMessage("Invalid AR model data");
                    loadingUIPanel.SetActive(false);
                    yield break;
                }

                isDownloading = false; // Reset the download flag
                string fileUrl = FileDownloadBaseUrl + response.result.file;
                yield return StartCoroutine(DownloadFile(fileUrl, response.result.file));

                // Show a completion message before transitioning
                UpdateDownloadProgress(1f, "Download complete! Starting AR mode...");
                yield return new WaitForSeconds(0.8f); // Brief pause to show completion
                
                SceneManager.LoadScene("ARVRScanner");
                Debug.Log("🔄 Loading AR Scene...");
            }
        }

        public void BackToHomePage()
        {
            Debug.Log("🔙 Hiding Course Detail Page and returning to Home...");

            // Add animation for smoother transition
            if (detailPage != null)
            {
                StartCoroutine(FadeOutAndHide(detailPage, 0.3f));
            }

            if (homePage != null)
            {
                homePage.SetActive(true);
                
                // Fade in the home page
                CanvasGroup homeCanvasGroup = homePage.GetComponent<CanvasGroup>();
                if (homeCanvasGroup == null)
                {
                    homeCanvasGroup = homePage.AddComponent<CanvasGroup>();
                }
                StartCoroutine(FadeIn(homeCanvasGroup, 0.3f));
            }
            else
            {
                Debug.LogWarning("⚠️ HomePage reference is missing! Make sure to assign it.");
            }
        }
        
        private IEnumerator FadeOutAndHide(GameObject obj, float duration)
        {
            CanvasGroup canvasGroup = obj.GetComponent<CanvasGroup>();
            if (canvasGroup == null)
            {
                canvasGroup = obj.AddComponent<CanvasGroup>();
            }
            
            float startTime = Time.time;
            while (Time.time < startTime + duration)
            {
                float t = (Time.time - startTime) / duration;
                canvasGroup.alpha = Mathf.Lerp(1f, 0f, t);
                yield return null;
            }
            
            canvasGroup.alpha = 0f;
            obj.SetActive(false);
        }
    }
}