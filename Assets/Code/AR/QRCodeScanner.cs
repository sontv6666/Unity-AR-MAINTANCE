using UnityEngine;
using UnityEngine.XR.ARFoundation;
using UnityEngine.XR.ARSubsystems;
using UnityEngine.Networking;
using ZXing;
using TMPro;
using System.IO;
using System.Linq;
using UnityEngine.UI;
using GLTFast;
using GLTFast.Loading;
using System.Threading.Tasks;
using System;

using System.Collections;
using System.Collections.Generic;
using UnityEngine.SceneManagement;
using UnityEngine.XR.Interaction.Toolkit.AR;
using UnityEngine.XR.Interaction.Toolkit.Interactables;

public class ARQRCodeScanner : MonoBehaviour
{

    // AR Elementss
    public ARCameraManager arCameraManager;
    private bool isScanning = true;
    private Vector3 qrCodePosition = Vector3.zero;
    private Vector3 qrCodeRotation = Vector3.zero;
    public GameObject modelContainer;


    private ARPlaneManager arPlaneManager;
    private ARSessionOrigin arSessionOrigin;
    private ARTrackedImageManager trackedImageManager;

    // UI Elements
    public GameObject scanUIPanel; //first panel
    public GameObject loadingUIPanel; //second panel
    public TMP_Text uiMessageText; // in second panel

    public TMP_Text debugLogText; // in second panel For debugging


    public GameObject courseUIPanel; //third panel

    public GameObject instructionLayoutGroup; //in third panel, container for instructionTemplateContent

    public GameObject instructionTemplateContent; // template for each instruction 



    public GameObject instructionDetailPanel; //four panel

    public Button backButton;

    public Button centerModelButton;
    public float modelDistanceFromCamera = 0.5f;

    public GameObject instructionDetailStepPrefab; // Prefab for each step


    public TMP_Text courseTitleText;




    private List<InstructionDetail> instructionSteps = new List<InstructionDetail>();

    private List<InstructionDetail> currentInstructionDetails = new List<InstructionDetail>();
    private int currentStepIndex = 0;
    private GameObject currentLoadedModel = null;

    private List<GameObject> instructionStepInstances = new List<GameObject>();
    private ModelResponse cachedModelData;

    private string courseID;
    private string testID = "c886f9f1-68f8-4596-b625-f14c5ef8addc";
    private string testqrCode = "42be6340-c590-4477-8508-f6250717cd7b";
    private string testqrCode2 = "97794f13-1146-4b1a-83c2-0c9b693a346e";

    public GameObject scanBoxUI;
    
    // Progress UI Elements
    public Slider progressBar;
    public TMP_Text progressText;
    
    
    // Speed values to cycle through
    float[] speedOptions = { 0.25f, 0.5f, 1f, 2f, 3f };
    int currentSpeedIndex = 2; // Default speed index (1x)
    private float lastAnimationSpeed = 1f; // Default speed is 1x


    void Start()
    {
        if (scanBoxUI != null)
        {
            scanBoxUI.SetActive(false); // Hide scan box initially
        }
        
        if (progressBar != null)  // ✅ Reset progress bar
        {
            progressBar.value = 0f; 
        }

        if (loadingUIPanel != null)
        {
            loadingUIPanel.SetActive(true); // Show loading screen
        }

        if (centerModelButton != null)
        {
            centerModelButton.onClick.AddListener(CenterModel);
        }

        if (backButton != null)
        {
            backButton.onClick.AddListener(GoBackToMainApp);
        }

        arSessionOrigin = FindObjectOfType<ARSessionOrigin>();
        trackedImageManager = FindObjectOfType<ARTrackedImageManager>();
        arPlaneManager = FindObjectOfType<ARPlaneManager>();

        // Use test ID for debugging
        courseID = PlayerPrefs.GetString("SelectedCourseID", "");
       // courseID = testID;

        if (string.IsNullOrEmpty(courseID))
        {
            Debug.LogError("❌ No Course ID found!");
            scanUIPanel.SetActive(true);
        }
        else
        {
            Debug.Log($"✅ Retrieved Course ID: {courseID}");
            StartCoroutine(DownloadCourseBeforeScanning(courseID));
        }
    }



    void Update()
    {
        if (isScanning)
        {
            //TryScanQRCode();
        }



    }

    IEnumerator DownloadCourseBeforeScanning(string courseId)
    {
        courseUIPanel.SetActive(false);
        instructionDetailPanel.SetActive(false);
        scanUIPanel.SetActive(false);
        scanBoxUI.SetActive(false);
        centerModelButton.gameObject.SetActive(false);
        Debug.Log($"📥 Downloading course data for ID: {courseId}");

        // Fetch course data before scanning
        yield return StartCoroutine(FetchCourseData(courseId));

        Debug.Log("✅ All downloads completed!");

        yield return new WaitForSeconds(1f);

        // ✅ Hide loading and show scan UI
        loadingUIPanel.SetActive(false);
        scanUIPanel.SetActive(false);
        scanBoxUI.SetActive(false);
        courseUIPanel.SetActive(true); 
        centerModelButton.gameObject.SetActive(true);
        instructionDetailPanel.SetActive(false);
       
     //   StartScanning();
     
     
     
    }


    void StartScanning()
    {
        Debug.Log("🔍 Starting QR Code Scanning...");

        isScanning = true;
    }


    IEnumerator FetchCourseData(string courseId)
    {
        if (string.IsNullOrEmpty(courseId))
        {
            Debug.LogError("❌ No Course ID found in FetchCourseData!");
            yield break;
        }

        string endpoint = "/course/" + courseId;
        UnityWebRequest request = ApiConfig.CreateRequest(endpoint);

        yield return request.SendWebRequest();

        if (request.result == UnityWebRequest.Result.Success)
        {
            string jsonResponse = request.downloadHandler.text;
            var response = JsonUtility.FromJson<ApiResponse>(jsonResponse);

            List<IEnumerator> downloadTasks = new List<IEnumerator>();

            // ✅ Collect all download tasks
            downloadTasks.Add(FetchModelData(response.result));
            downloadTasks.Add(DownloadAndLoadUI(response.result));

            yield return StartCoroutine(DownloadAllFiles(downloadTasks));
        }
        else
        {
            Debug.LogError("❌ API Request Failed: " + request.error);
        }
    }
    void UpdateProgress(float completed, float total)
    {
        if (progressBar != null)
        {
            progressBar.value = completed / total;  // ✅ Update the progress bar
        }

        if (progressText != null)
        {
            int percent = Mathf.RoundToInt((completed / total) * 100);
            progressText.text = $"Downloading... {percent}%";  // ✅ Update UI text
        }
    }

    IEnumerator DownloadAllFiles(List<IEnumerator> tasks)
    {
        float totalFiles = tasks.Count;
        float completedFiles = 0;

        foreach (var task in tasks)
        {
            yield return StartCoroutine(task);
            completedFiles++;

            // ✅ Update Progress Bar
            if (progressBar != null)
            {
                progressBar.value = completedFiles / totalFiles;
            }

            // ✅ Update Progress Text
            if (progressText != null)
            {
                int percent = Mathf.RoundToInt((completedFiles / totalFiles) * 100);
                progressText.text = $"Downloading... {percent}%";
            }
        }

        // ✅ Hide Loading and Show Scan UI
        loadingUIPanel.SetActive(false);
        scanUIPanel.SetActive(true);
    }


    
    
        //cach 2.1
        void TryScanQRCode()
        {
            // 🛑 Ensure Course UI is hidden before scanning
            if (courseUIPanel != null)
            {
                courseUIPanel.SetActive(false);
            }

            if (!isScanning) return;

            if (arCameraManager.TryAcquireLatestCpuImage(out XRCpuImage image))
            {
                // 📌 Define scan area (smaller portion of the screen)
                float scanZoneFactor = 0.3f; // Adjust for smaller area (30% of screen width/height)
                int scanZoneWidth = (int)(image.width * scanZoneFactor);
                int scanZoneHeight = (int)(image.height * scanZoneFactor);
                int scanZoneX = (image.width - scanZoneWidth) / 2;
                int scanZoneY = (image.height - scanZoneHeight) / 2;

                Debug.Log($"📍 Scan Zone: X={scanZoneX}, Y={scanZoneY}, W={scanZoneWidth}, H={scanZoneHeight}");

                // 🔹 Update ScanBox UI size & position
                if (scanBoxUI != null)
                {
                    RectTransform scanBoxRect = scanBoxUI.GetComponent<RectTransform>();

                    // 🛑 Set size relative to screen size
                    float scanBoxSizeFactor = 0.2f; // 30% of screen
                    scanBoxRect.sizeDelta =
                        new Vector2(Screen.width * scanBoxSizeFactor, Screen.height * scanBoxSizeFactor);

                    // 🛑 Center the scan box in the screen
                    scanBoxRect.anchoredPosition = Vector2.zero;

                    scanBoxUI.SetActive(true);
                }


                var conversionParams = new XRCpuImage.ConversionParams
                {
                    inputRect = new RectInt(scanZoneX, scanZoneY, scanZoneWidth, scanZoneHeight),
                    outputDimensions = new Vector2Int(scanZoneWidth, scanZoneHeight),
                    outputFormat = TextureFormat.RGBA32,
                    transformation = XRCpuImage.Transformation.None
                };

                var textureData = new Texture2D(scanZoneWidth, scanZoneHeight, TextureFormat.RGBA32, false);
                image.Convert(conversionParams, textureData.GetRawTextureData<byte>());
                image.Dispose();
                textureData.Apply();

                IBarcodeReader barcodeReader = new BarcodeReader();
                var result = barcodeReader.Decode(textureData.GetPixels32(), textureData.width, textureData.height);

                Destroy(textureData); // 🛑 Prevent memory leaks

                if (result != null)
                {
                    isScanning = false;

                    // 🛑 Hide Scan Box after successful scan
                    if (scanBoxUI != null)
                    {
                        scanBoxUI.SetActive(false);
                    }

                    Debug.Log($"✅ QR Code Scanned: {result.Text}");
                    ShowLoadingUI("Processing QR Code...");
                    UpdateUIText("Scanning...", "QR: " + result.Text);

                    qrCodePosition = arCameraManager.transform.position + arCameraManager.transform.forward * 0.5f;
                    qrCodeRotation = arCameraManager.transform.rotation.eulerAngles;



                    StartCoroutine(CheckQRCode(result.Text, courseID));
                }
            }
        }
    






    //cach 2.2
        IEnumerator CheckQRCode(string qrValue, string courseId)
        {
            string endpoint = "/course/" + courseId;
            UnityWebRequest request = ApiConfig.CreateRequest(endpoint);

            yield return request.SendWebRequest();

            if (request.result == UnityWebRequest.Result.Success)
            {
                string jsonResponse = request.downloadHandler.text;
                Debug.Log("API Response: " + jsonResponse);
                ProcessResponse(jsonResponse, qrValue);
            }
            else
            {
                Debug.LogError("API Request Failed: " + request.error);
                UpdateUIText("Scan failed. Try again.", "");
                Invoke(nameof(ResetScanning), 2f); // 🔥 Added to reset scanning after failure
            }
        }

        //cach 2.3
        void ProcessResponse(string jsonResponse, string scannedQR)
        {
            var response = JsonUtility.FromJson<ApiResponse>(jsonResponse);

            if (response.code == 1000 && response.result.courseCode == scannedQR)
            {
                UpdateUIText("QR Validated! Loading UI...", "Course: " + response.result.courseCode);
                StartCoroutine(FetchModelData(response.result));

                StartCoroutine(DownloadAndLoadUI(response.result));

                // 🔹 Ensure Model Stays Upright
                qrCodeRotation.x = 0; // Reset X rotation (prevents laying down)
                qrCodeRotation.z = 0; // Reset Z rotation (prevents tilting)
            }
            else
            {
                UpdateUIText("Invalid QR Code.", "Scanned: " + scannedQR);
                Invoke(nameof(ResetScanning), 2f);
            }
        }





        // FETCH MODEL AND CALL DOWNLOAD MODEL
        IEnumerator FetchModelData(CourseResult course)
        {
            string modelApiUrl = "/model/" + course.modelId;
            UnityWebRequest request = ApiConfig.CreateRequest(modelApiUrl);

            yield return request.SendWebRequest();

            if (request.result == UnityWebRequest.Result.Success)
            {
                ModelResponse modelData = JsonUtility.FromJson<ModelResponse>(request.downloadHandler.text);
                Debug.Log("📌 Model Rotation Y: " + modelData.result.rotation[1]);
        
                // ✅ Add to progress tracking
                yield return StartCoroutine(DownloadAndLoadModel(modelData));
            }
            else
            {
                Debug.LogError("❌ Failed to fetch model data: " + request.error);
                UpdateUIText("Error", "Failed to fetch model.");
            }
        }

        // DOWNLOAD MODEL FROM FETCH CALL LOAD MODEL
        IEnumerator DownloadAndLoadModel(ModelResponse modelData)
        {
            string fileEndpoint = "/files/";
            string modelFilePath = Path.Combine(Application.persistentDataPath, modelData.result.file);

            // ✅ Download model file
            yield return StartCoroutine(DownloadFile(fileEndpoint + modelData.result.file, modelData.result.file));

            // ✅ Apply position & rotation
            Vector3 position = new Vector3(modelData.result.position[0], modelData.result.position[1], modelData.result.position[2]);
            Vector3 rotation = new Vector3(modelData.result.rotation[0], modelData.result.rotation[1], modelData.result.rotation[2]);
            Vector3 scale = modelData.result.GetScale();  
            
                
            // ✅ Load the model
            yield return StartCoroutine(Load3DModel(modelFilePath, modelContainer, position, rotation, scale));
        }
        


        //LOAD MODEL FROM FETCH, DOWNLOAD MODEL

        public IEnumerator Load3DModel(string modelPath, GameObject modelContainer, Vector3 position, Vector3 rotation,  Vector3 scale)
        {
            Debug.Log($"📌 Attempting to load model from: {modelPath}");

            string formattedPath = modelPath.Replace("\\", "/");

            string fullPath;
#if UNITY_ANDROID
            fullPath = "file://" + Path.Combine(Application.persistentDataPath, Path.GetFileName(formattedPath));
#else
            fullPath = formattedPath;
#endif

            Debug.Log($"🔗 Full path: {fullPath}");

            bool fileExists = File.Exists(fullPath);

#if UNITY_ANDROID
         using (UnityWebRequest request = UnityWebRequest.Get(fullPath))
        {
            yield return request.SendWebRequest();
            fileExists = !request.isHttpError && !request.isNetworkError;
        }
#endif

            if (!fileExists)
            {
                Debug.LogError($"❌ Model file not found: {fullPath}");
                yield break;
            }

            Debug.Log($"✅ Model file exists: {fullPath}");

            if (modelContainer == null)
            {
                Debug.LogError("❌ Model container is null. Cannot load model.");
                yield break;
            }

            foreach (Transform child in modelContainer.transform)
            {
                Destroy(child.gameObject);
            }

            Debug.Log($"🔗 Preparing to load GLB from: {fullPath}");

            var gltf = new GltfImport();
            var loadTask = gltf.Load(fullPath);

            while (!loadTask.IsCompleted)
            {
                yield return null;
            }

            if (!loadTask.Result)
            {
                Debug.LogError("❌ Failed to load GLB model.");
                yield break;
            }

            var instantiateTask = gltf.InstantiateMainSceneAsync(modelContainer.transform);

            while (!instantiateTask.IsCompleted)
            {
                yield return null;
            }

            if (!instantiateTask.Result)
            {
                Debug.LogError("❌ Failed to instantiate GLB model.");
                yield break;
            }

            Debug.Log("✅ 3D Model successfully loaded.");

            GameObject loadedModel = modelContainer.transform.childCount > 0
                ? modelContainer.transform.GetChild(modelContainer.transform.childCount - 1).gameObject
                : null;

            if (loadedModel == null)
            {
                Debug.LogError("❌ Failed to find the loaded model.");
                yield break;
            }

            // 🔹 Set the name of the first model after scanning
            loadedModel.name = "FirstModelAfterScan";

            // 🔹 Apply QR Code position and rotation
            loadedModel.transform.SetParent(modelContainer.transform, false);
            SetupModelInteractions(loadedModel);

            // ✅ Attach to QR code
            // loadedModel.transform.position = qrCodePosition;  // Use QR Code position
            // loadedModel.transform.eulerAngles = qrCodeRotation;  // Use QR Code rotation
            // loadedModel.transform.localScale = Vector3.one * 0.1f;
            //
            // Debug.Log("✅ Model correctly anchored to QR Code.");


            Debug.Log(
                $"✅ Model info {position}, Rotation: {rotation}");
          
            // ✅ Apply correct position
            loadedModel.transform.position = qrCodePosition + position;;

      
            // ✅ Apply correct rotation
            loadedModel.transform.rotation = Quaternion.Euler(rotation);

            // ✅ Apply correct scale
            loadedModel.transform.localScale = scale;
            Debug.Log(
                $"✅ Model anchored to QR Code at {loadedModel.transform.position}, Rotation: {loadedModel.transform.rotation.eulerAngles}");

        }



        // AFTER LOAD FIRST 3D MODEL.IT WILL CALL THIS FOR DOWNLOAD AND LOAD UI
        IEnumerator DownloadAndLoadUI(CourseResult course)
        {
            string fileEndpoint = "/files/";

            List<string> modelPaths = new List<string>();
            Dictionary<string, string> imagePaths = new Dictionary<string, string>();

            // ✅ Total files to download (course image + instruction assets)
            int totalFiles = 1 + course.instructions.Sum(i => i.instructionDetailResponse.Count * 2);
            int completedFiles = 0;

            // ✅ Download course image
            if (!string.IsNullOrEmpty(course.imageUrl))
            {
                string imageFilePath = Path.Combine(Application.persistentDataPath, course.imageUrl);
                yield return StartCoroutine(DownloadFile(fileEndpoint + course.imageUrl, course.imageUrl));
                imagePaths[course.imageUrl] = imageFilePath;
                completedFiles++;
                UpdateProgress(completedFiles, totalFiles);
            }

            // ✅ Download instruction images & 3D models
            foreach (var instruction in course.instructions)
            {
                foreach (var detail in instruction.instructionDetailResponse)
                {
                    // ✅ Download images
                    if (!string.IsNullOrEmpty(detail.imgString))
                    {
                        string imageFilePath = Path.Combine(Application.persistentDataPath, detail.imgString);
                        yield return StartCoroutine(DownloadFile(fileEndpoint + detail.imgString, detail.imgString));
                        imagePaths[detail.imgString] = imageFilePath;
                        completedFiles++;
                        UpdateProgress(completedFiles, totalFiles);
                    }

                    // ✅ Download 3D models
                    if (!string.IsNullOrEmpty(detail.fileString))
                    {
                        yield return StartCoroutine(DownloadFile(fileEndpoint + detail.fileString, detail.fileString));
                        completedFiles++;
                        UpdateProgress(completedFiles, totalFiles);

                        if (detail.fileString.EndsWith(".glb") || detail.fileString.EndsWith(".gltf"))
                        {
                            string modelPath = Path.Combine(Application.persistentDataPath, detail.fileString);
                            modelPaths.Add(modelPath);
                        }
                    }
                }
            }

            // ✅ Load UI with local images
            Debug.Log($"Load UI: {course.title} with {imagePaths.Count} images");
            LoadUI(course, imagePaths);
        }
        

        void LoadUI(CourseResult course, Dictionary<string, string> imagePaths)
        {
            ShowCourseUI(course); // This method sets course title, description, and instructions
            Debug.Log($"📂 Application Persistent Data Path: {Application.persistentDataPath}");

            // ✅ Load the course image if available
            if (!string.IsNullOrEmpty(course.imageUrl) && imagePaths.ContainsKey(course.imageUrl))
            {
                StartCoroutine(LoadImageFromLocal(imagePaths[course.imageUrl],
                    courseUIPanel.transform.Find("CourseImage").GetComponent<UnityEngine.UI.Image>()));
            }
        }

        // DOWNLOAD FILE
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
            }
        }
        
        // ✅ Start File Download
    public void StartFileDownload(string fileUrl, string fileName)
    {
        StartCoroutine(DownloadFileWithSizeCheck(fileUrl, fileName));
    }

    // ✅ Download with File Size Check
    IEnumerator DownloadFileWithSizeCheck(string fileUrl, string fileName)
    {
        long expectedFileSize = -1;

        // 🔹 Step 1: Get file size first
        yield return StartCoroutine(GetFileSize(fileUrl, size => expectedFileSize = size));

        if (expectedFileSize <= 0)
        {
            Debug.LogError("❌ Failed to get valid file size. Cancelling download.");
            yield break;
        }

        string savePath = Path.Combine(Application.persistentDataPath, fileName);

        // 🔹 Step 2: Check if file already exists
        if (File.Exists(savePath))
        {
            long existingFileSize = new FileInfo(savePath).Length;

            if (existingFileSize == expectedFileSize)
            {
                Debug.Log($"📌 File already fully downloaded: {fileName} (Size: {existingFileSize} bytes)");
                yield break;
            }
            else
            {
                Debug.LogWarning($"⚠️ Incomplete file detected! {fileName} (Size: {existingFileSize}/{expectedFileSize})");
                File.Delete(savePath); // ❌ Delete corrupted/incomplete file
            }
        }

        // 🔹 Step 3: Start Download
        yield return StartCoroutine(DownloadFile(fileUrl, fileName, expectedFileSize));
    }

    // ✅ Download File
    IEnumerator DownloadFile(string fileUrl, string fileName, long expectedFileSize)
    {
        string savePath = Path.Combine(Application.persistentDataPath, fileName);
        UnityWebRequest request = new UnityWebRequest(fileUrl, UnityWebRequest.kHttpVerbGET);
        request.downloadHandler = new DownloadHandlerFile(savePath);

        UnityWebRequestAsyncOperation operation = request.SendWebRequest();

        while (!operation.isDone)
        {
            float progress = operation.progress;
            UpdateDownloadProgress(progress, $"Downloading: {fileName}");
            yield return null;
        }

        // 🔹 Step 4: Verify Download Success
        if (request.result == UnityWebRequest.Result.Success)
        {
            long downloadedFileSize = new FileInfo(savePath).Length;

            if (downloadedFileSize == expectedFileSize)
            {
                Debug.Log($"✅ File successfully downloaded: {savePath} (Size: {downloadedFileSize} bytes)");
            }
            else
            {
                Debug.LogError($"❌ File size mismatch! Expected: {expectedFileSize}, Got: {downloadedFileSize}");
                File.Delete(savePath); // ❌ Delete corrupted file
            }
        }
        else
        {
            Debug.LogError($"❌ Download failed: {fileUrl} Error: {request.error}");
            if (File.Exists(savePath))
            {
                File.Delete(savePath); // ❌ Delete incomplete file
            }
        }
    }

    // ✅ Get File Size
    IEnumerator GetFileSize(string fileUrl, Action<long> onFileSizeReceived, int retries = 3)
    {
        for (int attempt = 1; attempt <= retries; attempt++)
        {
            UnityWebRequest request = UnityWebRequest.Head(fileUrl);
            yield return request.SendWebRequest();

            if (request.result == UnityWebRequest.Result.Success)
            {
                long fileSize = -1;
                
                // ✅ Try Content-Range header first
                string contentRange = request.GetResponseHeader("Content-Range");
                if (!string.IsNullOrEmpty(contentRange))
                {
                    string[] parts = contentRange.Split('/');
                    if (parts.Length == 2 && long.TryParse(parts[1], out fileSize))
                    {
                        Debug.Log($"📏 File Size (Content-Range): {fileSize} bytes");
                        onFileSizeReceived?.Invoke(fileSize);
                        yield break;
                    }
                }

                // ✅ Fallback to Content-Length if Content-Range is missing
                string contentLength = request.GetResponseHeader("Content-Length");
                if (!string.IsNullOrEmpty(contentLength) && long.TryParse(contentLength, out fileSize))
                {
                    Debug.Log($"📏 File Size (Content-Length): {fileSize} bytes");
                    onFileSizeReceived?.Invoke(fileSize);
                    yield break;
                }
            }

            Debug.LogError($"❌ Attempt {attempt}/{retries} - Failed to get file size: {request.error}");
            if (attempt == retries) onFileSizeReceived?.Invoke(-1);
            yield return new WaitForSeconds(2); // Wait before retrying
        }
    }


        void UpdateDownloadProgress(float progress, string message)
        {
            if (progressBar != null)
            {
                progressBar.value = progress;
            }

            if (progressText != null)
            {
                progressText.text = $"{message} ({(progress * 100):F0}%)";
            }
        }




        void ShowScanUI(string message = "Scan a QR Code")
        {
            scanUIPanel.SetActive(true);
            loadingUIPanel.SetActive(false);
            courseUIPanel.SetActive(false);
            instructionDetailPanel.SetActive(false);
            uiMessageText.text = message;
        }



        void ShowLoadingUI(string message = "Loading...")
        {
            scanUIPanel.SetActive(false);
            loadingUIPanel.SetActive(true);
            courseUIPanel.SetActive(false);
            instructionDetailPanel.SetActive(false);
            uiMessageText.text = message;

        }



        void ShowCourseUI(CourseResult course)
        {
            // Hide other panels & show course UI
            scanUIPanel.SetActive(false);
            loadingUIPanel.SetActive(false);
            courseUIPanel.SetActive(true);
            instructionDetailPanel.SetActive(false);
            backButton.gameObject.SetActive(true);
            centerModelButton.gameObject.SetActive(true);

            // Show title
            courseTitleText.text = course.title;

            // ✅ Clear previous instructions
            foreach (Transform child in instructionLayoutGroup.transform)
            {
                Destroy(child.gameObject);
            }

            // ✅ Sort instructions by `orderNumber`
            var sortedInstructions = course.instructions.OrderBy(i => i.orderNumber).ToList();

            // ✅ Populate Instructions in the correct order
            foreach (var instruction in sortedInstructions)
            {
                // Instantiate instruction UI element
                GameObject instructionItem = Instantiate(instructionTemplateContent, instructionLayoutGroup.transform);
                instructionItem.SetActive(true); // Ensure it's visible

                // Assign instruction name
                TMP_Text instructionNameText =
                    instructionItem.transform.Find("instructionName").GetComponent<TMP_Text>();
                instructionNameText.text = instruction.name;

                // ✅ Add button click event to move to instructionDetailPanel
                Button instructionButton = instructionItem.GetComponent<Button>();
                instructionButton.onClick.AddListener(() => ShowInstructionDetails(instruction));
            }
        }







        void ShowInstructionDetails(Instruction instruction)
        {
            currentInstructionDetails = instruction.instructionDetailResponse;
            currentStepIndex = 0;

            // ✅ Update UI with instruction details
            UpdateInstructionStepUI(instruction);




            // ✅ Show instruction detail panel
            scanUIPanel.SetActive(false);
            loadingUIPanel.SetActive(false);
            courseUIPanel.SetActive(false);
            instructionDetailPanel.SetActive(true);
            backButton.gameObject.SetActive(false);

        }

        void UpdateInstructionStepUI(Instruction instruction)
        {
            if (currentInstructionDetails == null || currentInstructionDetails.Count == 0)
            {
                Debug.LogError("No instruction details to display!");
                return;
            }

            // ✅ Ensure "FirstModelAfterScan" exists
            GameObject firstModel = modelContainer.transform.Find("FirstModelAfterScan")?.gameObject;
            if (firstModel == null)
            {
                Debug.LogError("FirstModelAfterScan not found!");
                return;
            }

            // ✅ Hide "FirstModelAfterScan" initially
            firstModel.SetActive(false);

            instructionDetailStepPrefab.SetActive(false);

            // ✅ Clear previous step UI instances
            foreach (GameObject step in instructionStepInstances)
            {
                Destroy(step);
            }

            instructionStepInstances.Clear();


            for (int i = 0; i < currentInstructionDetails.Count; i++)
            {
                InstructionDetail detail = currentInstructionDetails[i];

                GameObject stepItem = Instantiate(instructionDetailStepPrefab, instructionDetailPanel.transform);
                stepItem.SetActive(i == 0); // Show only the first step initially

                TMP_Text nameText = stepItem.transform.Find("instructionNameText")?.GetComponent<TMP_Text>();
                TMP_Text descriptionText = stepItem.transform.Find("instructionDetailDescriptionText")
                    ?.GetComponent<TMP_Text>();
                TMP_Text stepCountText =
                    stepItem.transform.Find("instructionDetailShowStepText")?.GetComponent<TMP_Text>();

                if (nameText) nameText.text = instruction.name;
                if (descriptionText) descriptionText.text = detail.description;
                if (stepCountText) stepCountText.text = $"{i + 1}/{currentInstructionDetails.Count}";

                if (!string.IsNullOrEmpty(detail.imgString))
                {
                    string imagePath = Path.Combine(Application.persistentDataPath, detail.imgString);
                    Image stepImage = stepItem.transform.Find("instructionDetailImageShow")?.GetComponent<Image>();
                    if (stepImage) StartCoroutine(LoadImageFromLocal(imagePath, stepImage));
                }

                // ✅ Play/Stop animation button logic
                Button replayAnimationButton = stepItem.transform.Find("replayanimationButton")?.GetComponent<Button>();

                if (replayAnimationButton)
                {
                    replayAnimationButton.onClick.RemoveAllListeners();
                    replayAnimationButton.onClick.AddListener(() => { PlayStepAnimation(firstModel, detail); });
                }

                Button playAndStopAnimationButton =
                    stepItem.transform.Find("playandstopanimationButton")?.GetComponent<Button>();
                if (playAndStopAnimationButton)
                {
                    playAndStopAnimationButton.onClick.RemoveAllListeners();
                    playAndStopAnimationButton.onClick.AddListener(() => { TogglePlayPauseAnimation(firstModel); });
                }

                // ✅ Speed control buttons
                SetupSpeedControls(stepItem, firstModel, detail);

                // ✅ Close buttons logic
                Button closeButtonFirst = stepItem.transform.Find("closeButtonFirst")?.GetComponent<Button>();
                Button closeButtonSecond = stepItem.transform.Find("closeButtonSecond")?.GetComponent<Button>();

                if (closeButtonFirst) closeButtonFirst.onClick.AddListener(() => ShowInstructionUI(stepItem));
                if (closeButtonSecond) closeButtonSecond.onClick.AddListener(() => HideInstructionUI(stepItem));

                // ✅ Navigation buttons
                Button backButton = stepItem.transform.Find("backInstructionPanel")?.GetComponent<Button>();
                Button prevButton = stepItem.transform.Find("instructionDetailPreviousButton")?.GetComponent<Button>();
                Button nextButton = stepItem.transform.Find("instructionDetailNextStepButton")?.GetComponent<Button>();

                if (backButton) backButton.onClick.AddListener(() => BackToCourseUI());
                if (prevButton) prevButton.onClick.AddListener(() => ChangeInstructionStep(-1));
                if (nextButton) nextButton.onClick.AddListener(() => ChangeInstructionStep(1));

                instructionStepInstances.Add(stepItem);
            }



            // ✅ Show first step model & animation
            firstModel.SetActive(true);
            PlayStepAnimation(firstModel, currentInstructionDetails[0]);

            UpdateStepNavigationButtons();
        }


        /// ✅ Hide UI elements (except navigation & closeButtonFirst) and set image transparency to 0
        void HideInstructionUI(GameObject stepItem)
        {
            Debug.Log("🔻 Hiding instruction UI elements (except navigation & closeButtonFirst)...");

            foreach (Transform child in stepItem.transform)
            {
                if (child.name != "closeButtonFirst" &&
                    child.name != "instructionDetailShowStepText" &&
                    child.name != "instructionDetailPreviousButton" &&
                    child.name != "instructionDetailNextStepButton")
                {
                    child.gameObject.SetActive(false);
                }
            }

            // ✅ Hide images by setting transparency to 0
            SetInstructionImageTransparency(stepItem, 0f);

            // ✅ Ensure closeButtonFirst is ACTIVE
            Transform closeButtonFirst = stepItem.transform.Find("closeButtonFirst");
            if (closeButtonFirst != null) closeButtonFirst.gameObject.SetActive(true);
        }

        // ✅ Show all UI elements and restore image transparency to 170/255
        void ShowInstructionUI(GameObject stepItem)
        {
            Debug.Log("🔹 Showing all instruction UI elements...");

            foreach (Transform child in stepItem.transform)
            {
                child.gameObject.SetActive(true);
            }

            // ✅ Restore image transparency
            SetInstructionImageTransparency(stepItem, 170f / 255f);

            // ✅ Ensure closeButtonFirst is HIDDEN
            Transform closeButtonFirst = stepItem.transform.Find("closeButtonFirst");
            if (closeButtonFirst != null) closeButtonFirst.gameObject.SetActive(false);
        }

        // ✅ Helper function to change image transparency
        void SetInstructionImageTransparency(GameObject stepItem, float alpha)
        {
            Image stepImage = stepItem.GetComponent<Image>(); // Get Image component of the prefab itself
            if (stepImage != null)
            {
                Color color = stepImage.color;
                color.a = alpha; // Set alpha transparency
                stepImage.color = color;
            }
        }


     
    
        // ✅ Create buttons for animation speed options
        void SetupSpeedControls(GameObject stepItem, GameObject firstModel, InstructionDetail detail)
        {
            // Find Buttons
            Button speedButtonX025 = stepItem.transform.Find("speedButtonX025")?.GetComponent<Button>();
            Button speedButtonX05 = stepItem.transform.Find("speedButtonX05")?.GetComponent<Button>();
            Button speedButtonX1 = stepItem.transform.Find("speedButtonX1")?.GetComponent<Button>();
            Button speedButtonX2 = stepItem.transform.Find("speedButtonX2")?.GetComponent<Button>();
            Button speedButtonX3 = stepItem.transform.Find("speedButtonX3")?.GetComponent<Button>();

            // Find the Slider
            Slider speedSlider = stepItem.transform.Find("animationSpeedSlider")?.GetComponent<Slider>();

            // Assign Button Clicks
            if (speedButtonX025)
                speedButtonX025.onClick.AddListener(() => ChangeAnimationSpeed(firstModel, detail, 0.25f));
            if (speedButtonX05)
                speedButtonX05.onClick.AddListener(() => ChangeAnimationSpeed(firstModel, detail, 0.5f));
            if (speedButtonX1) speedButtonX1.onClick.AddListener(() => ChangeAnimationSpeed(firstModel, detail, 1f));
            if (speedButtonX2) speedButtonX2.onClick.AddListener(() => ChangeAnimationSpeed(firstModel, detail, 2f));
            if (speedButtonX3) speedButtonX3.onClick.AddListener(() => ChangeAnimationSpeed(firstModel, detail, 3f));

            // Assign Slider Change Listener
            if (speedSlider)
            {
                speedSlider.minValue = 0.25f;
                speedSlider.maxValue = 3f;
                speedSlider.value = 1f; // Default speed
                speedSlider.onValueChanged.AddListener((value) => ChangeAnimationSpeed(firstModel, detail, value));
            }
        }


    // ✅ Function to change animation speed
    void ChangeAnimationSpeed(GameObject firstModel, InstructionDetail detail, float newSpeed)
    {
        if (firstModel == null) return;

        Animation animation = firstModel.GetComponentInChildren<Animation>(true);
        if (animation != null)
        {
            if (animation.GetClip(detail.animationName) != null)
            {
                AnimationState state = animation[detail.animationName];
                state.speed = newSpeed;
                lastAnimationSpeed = newSpeed; // ✅ Save the new speed globally
                Debug.Log($"⚡ Changed animation speed to: {newSpeed}");
            }
        }
    }
    
    
    void UpdateSpeedSliderUI(GameObject stepItem)
    {
        Slider speedSlider = stepItem.transform.Find("animationSpeedSlider")?.GetComponent<Slider>();

        if (speedSlider)
        {
            speedSlider.value = 1f; // ✅ Reset speed slider to default 1x
        }
    }





        void PlayStepAnimation(GameObject firstModel, InstructionDetail detail, float speed = 1f)
        {
            if (!firstModel) return;

            // ✅ Disable buttons at the start (force this so they don’t get overridden)
            SetNavigationButtonsInteractable(false);
            isAnimationPlaying = true;
            
            SetReplayButtonInteractable(false); 
            // ✅ Get the Animation component
            Animation animation = firstModel.GetComponentInChildren<Animation>(true);
     
            
            if (animation != null)
            {
                
                // ✅ Disable Animator to avoid conflicts
                Animator animator = firstModel.GetComponentInChildren<Animator>();
                if (animator) animator.enabled = false;

                animation.Stop();
                animation.Rewind();


                animation.Stop();
                animation.Rewind(); // ✅ Force rewind to first frame
                Debug.Log("🔄 Stopped and rewound all animations.");
                
                // ✅ Ensure all animations reset before playing a new on
                foreach (AnimationState state in animation)
                {
                    if (state != null)
                    {
                        state.time = 0f; // Rewind to first frame
                        state.enabled = true; // Ensure it's enabled so Sample() works
                        animation.Play(state.name);
                        animation.Stop(); // Stop immediately after playing
                        animation.Sample(); // Apply first frame
                        state.enabled = false; // Disable after sampling
                    }
                }
                
                
                // ✅ Disable looping for ALL animations
                foreach (AnimationState state in animation)
                {
                    if (state != null)
                    {
                        state.wrapMode = WrapMode.Once; // 🔴 Prevent looping
                    }
                }
                

                Debug.Log("🔄 Reset all previous animations.");

                // ✅ Reset the last played animation if available
                if (!string.IsNullOrEmpty(lastPlayedAnimationName) &&
                    animation.GetClip(lastPlayedAnimationName) != null)
                {
                    AnimationState lastState = animation[lastPlayedAnimationName];
                    lastState.time = 0f;
                    lastState.enabled = true;
                    animation.Play(lastPlayedAnimationName);
                    animation.Stop();
                    animation.Sample();
                    lastState.enabled = false;
                    Debug.Log($"🔄 Reset last played animation: {lastPlayedAnimationName}");
                }
                
                // ✅ Play the new animation with speed control
                if (animation.GetClip(detail.animationName) != null)
                {
                    AnimationState newState = animation[detail.animationName];
                    lastPlayedAnimationName = detail.animationName; // ✅ Store last played animation
                    newState.speed = lastAnimationSpeed;
                    newState.wrapMode = WrapMode.Once; // 🔴 Ensure it only runs once
                    animation.Play(detail.animationName);
                    Debug.Log($"▶️ Playing animation: {detail.animationName} at speed {speed}x");
                    StartCoroutine(WaitForAnimationToEnd(animation, newState));
                }
                else
                {
                    Debug.LogWarning($"⚠️ Animation '{detail.animationName}' not found!");
                    isAnimationPlaying = false;
                    SetNavigationButtonsInteractable(true);
                    SetReplayButtonInteractable(true);
                }
            }
            else
            {
                Debug.LogWarning("⚠️ No Animation component found!");
                isAnimationPlaying = false;
                SetNavigationButtonsInteractable(true);
            }

            // ✅ Reset all meshes to active before hiding specific ones
            foreach (Transform child in firstModel.transform)
            {
                child.gameObject.SetActive(true);
            }

            // ✅ Hide only specific meshes if needed
            HashSet<string> meshesToHide = detail.meshes != null ? new HashSet<string>(detail.meshes) : null;
            if (meshesToHide != null)
            {
                foreach (Transform child in firstModel.transform)
                {
                    if (meshesToHide.Contains(child.name))
                    {
                        child.gameObject.SetActive(false);
                        Debug.Log($"🚫 Hiding mesh: {child.name}");
                    }
                }
            }
        }
        
        bool isAnimationPlaying = false;
        private bool isAnimationPaused = false; // 🔴 Track if animation was paused manually
        // IEnumerator WaitForAnimationToEnd(Animation animation, AnimationState state)
        // {
        //     Debug.Log("⏳ Waiting for animation to finish...");
        //
        //     while (state.enabled && animation.IsPlaying(state.name) && !isAnimationPaused)
        //     {
        //         yield return null; // Wait until animation finishes or is manually paused/stopped
        //     }
        //
        //
        //     Debug.Log("✅ Animation finished. Re-enabling navigation buttons.");
        //
        //     isAnimationPlaying = false;
        //     SetNavigationButtonsInteractable(true);
        // }
        

        IEnumerator WaitForAnimationToEnd(Animation animation, AnimationState state)
        {
            Debug.Log("⏳ Waiting for animation to finish...");

            SetNavigationButtonsInteractable(false); // 🚫 Disable buttons immediately

            while (state.enabled && animation.IsPlaying(state.name))
            {
                if (isAnimationPaused) // 🛑 If paused, keep waiting and keep buttons disabled
                {
                    Debug.Log("⏸️ Animation paused, waiting...");
                    yield return null;
                }
                else
                {
                    yield return null; // ✅ Continue waiting normally
                }
            }

            // ✅ Only re-enable buttons if animation finished naturally
            if (!isAnimationPaused)
            {
                Debug.Log("✅ Animation finished. Re-enabling navigation buttons.");
                isAnimationPlaying = false;
                SetNavigationButtonsInteractable(true);
                SetReplayButtonInteractable(true); 
            }
        }

        

      
        // 🟢 Enable/Disable Previous & Next buttons dynamically
        void UpdateStepNavigationButtons()
        {
            if (instructionStepInstances.Count == 0) return;

            GameObject currentStep = instructionStepInstances[currentStepIndex];

            Button prevButton = currentStep.transform.Find("instructionDetailPreviousButton")?.GetComponent<Button>();
            Button nextButton = currentStep.transform.Find("instructionDetailNextStepButton")?.GetComponent<Button>();

            // ✅ Only update buttons if animation is not playing
            if (!isAnimationPlaying)
            {
                if (prevButton) prevButton.interactable = (currentStepIndex > 0);
                if (nextButton) nextButton.interactable = (currentStepIndex < instructionStepInstances.Count - 1);
            }
        }

        
        bool isModelCentered = false; // Add this as a global variable

        void CenterModel()
        {
            if (isModelCentered)
            {
                Debug.Log("⚠️ Model is already centered. Skipping repositioning.");
                return;
            }

            if (modelContainer == null)
            {
                Debug.LogError("❌ Model container is NULL! Cannot center the model.");
                return;
            }

            Transform model = modelContainer.transform.Find("FirstModelAfterScan");
            if (model == null)
            {
                Debug.LogError("❌ No child model named 'FirstModelAfterScan' found inside ModelContainer!");
                return;
            }

            Camera arCamera = Camera.main;
            if (arCamera == null)
            {
                Debug.LogError("❌ No Main Camera found! Make sure your AR camera is tagged as 'MainCamera'.");
                return;
            }

            Vector3 cameraForward = arCamera.transform.forward.normalized;
            Vector3 cameraPosition = arCamera.transform.position;
            float adjustedDistance = Mathf.Clamp(modelDistanceFromCamera, 2f, 4f); // 2m to 4m range
            Vector3 newPosition = cameraPosition + (cameraForward * adjustedDistance);
            Vector3 finalPosition = newPosition;

            // 🔍 Find the closest AR Plane
            float minPlaneDistance = float.MaxValue;
            ARPlane closestPlane = null;

            if (arPlaneManager != null)
            {
                foreach (ARPlane plane in arPlaneManager.trackables)
                {
                    float distance = Vector3.Distance(newPosition, plane.transform.position);
                    if (distance < minPlaneDistance)
                    {
                        minPlaneDistance = distance;
                        closestPlane = plane;
                    }
                }
            }

            // ✅ Adjust position to detected plane
            if (closestPlane != null)
            {
                finalPosition.y = closestPlane.transform.position.y;
            }

            model.position = finalPosition;
            model.rotation = Quaternion.LookRotation(cameraForward);

            Debug.Log($"🎯 Model placed at: {finalPosition}");
            model.gameObject.SetActive(true);


        }


        public void ResetModelPosition()
        {
            isModelCentered = false;
        }




      

    private string lastPlayedAnimationName = null;
   
    private float lastAnimationTime = 0f; // Store last animation time

    public void TogglePlayPauseAnimation(GameObject firstModel)
    {
        if (!firstModel) return;

        Animation animation = firstModel.GetComponentInChildren<Animation>(true);
        if (animation != null)
        {
            foreach (AnimationState state in animation)
            {
                if (state.enabled) // 🔴 If animation is playing, PAUSE it
                {
                    lastPlayedAnimationName = state.name;
                    lastAnimationTime = state.time; // Save current time before pausing
                    state.enabled = false; // Pause animation
                    animation.Sample(); // Keep the last frame
                    isAnimationPaused = true; // ✅ Mark as paused
                    Debug.Log($"⏸️ Paused animation '{state.name}' at frame: {state.time}");
                    return;
                }
            }

            // ▶️ If NO animation is playing, RESUME the last played animation
            if (!string.IsNullOrEmpty(lastPlayedAnimationName) &&
                animation.GetClip(lastPlayedAnimationName) != null)
            {
                AnimationState lastState = animation[lastPlayedAnimationName];
                lastState.time = lastAnimationTime; // Resume from last saved time
                lastState.enabled = true;
                animation.Play(lastPlayedAnimationName);
                isAnimationPaused = false; // ✅ Fix stuck state
                Debug.Log($"▶️ Resumed animation '{lastPlayedAnimationName}' at frame: {lastAnimationTime}");

                if (!isAnimationPlaying) // ✅ Restart waiting coroutine
                {
                    StartCoroutine(WaitForAnimationToEnd(animation, lastState));
                }
            }
            else
            {
                Debug.LogWarning("⚠️ No previous animation found to resume.");
            }
        }
        else
        {
            Debug.LogWarning("⚠️ No Animation component found.");
        }
    }




    public void StopCurrentAnimationAtFrame(GameObject firstModel)
    {
        if (firstModel == null) return;

        Animation animation = firstModel.GetComponentInChildren<Animation>(true);
        if (animation != null)
        {
            foreach (AnimationState state in animation)
            {
                if (state.enabled)
                {
                    lastPlayedAnimationName = state.name;
                    lastAnimationTime = state.time; // Save the current frame before stopping
                    state.enabled = false;
                    animation.Sample(); // Apply the current frame
                    isAnimationPaused = false; // ✅ Fix stuck state
                    isAnimationPlaying = false; // ✅ Ensure correct state
                    SetNavigationButtonsInteractable(true); // ✅ Re-enable buttons
                    Debug.Log($"⏹ Stopped animation '{state.name}' at frame: {state.time}");
                    return;
                }
            }
        }
        else
        {
            Debug.LogWarning("⚠️ No Animation component found!");
        }
    }






        void ResetModelState(GameObject model)
        {


            Debug.Log("🔄 Model reset to default transform state.");
            // ✅ Reset blend shapes (if applicable)
            SkinnedMeshRenderer[] skinnedMeshes = model.GetComponentsInChildren<SkinnedMeshRenderer>(true);
            foreach (var skinnedMesh in skinnedMeshes)
            {
                Mesh mesh = skinnedMesh.sharedMesh;
                if (mesh != null && mesh.blendShapeCount > 0)
                {
                    for (int i = 0; i < mesh.blendShapeCount; i++)
                    {
                        skinnedMesh.SetBlendShapeWeight(i, 0); // Reset all blend shapes
                    }
                }
            }

            Debug.Log("🔄 Model reset to default transform state.");
        }

        void SetReplayButtonInteractable(bool isInteractable)
        {
            if (instructionStepInstances.Count == 0) return;

            // ✅ Get the active step
            GameObject currentStep = instructionStepInstances[currentStepIndex];

            // ✅ Find the replay button ONLY in the current step
            Button replayButton = currentStep.transform.Find("replayanimationButton")?.GetComponent<Button>();

            // 🔴 Ensure the replay button stays disabled if animation is still playing
            bool canInteract = isInteractable && !isAnimationPlaying;

            // ✅ Enable/Disable replay button correctly
            if (replayButton) replayButton.interactable = canInteract;

            Debug.Log($"🔄 Replay button {(canInteract ? "enabled" : "disabled")} for step {currentStepIndex}");
        }

        
    // 🔄 Change step logic
    void ChangeInstructionStep(int direction)
    {
        if (instructionStepInstances.Count == 0 || isAnimationPlaying) return; // 🔴 Prevent step change while animating

        // ✅ Hide current step UI
        instructionStepInstances[currentStepIndex].SetActive(false);

        // ✅ Move to next/previous step
        currentStepIndex += direction;
        currentStepIndex = Mathf.Clamp(currentStepIndex, 0, instructionStepInstances.Count - 1);

        // ✅ Show new step UI
        instructionStepInstances[currentStepIndex].SetActive(true);
        
        // ✅ Reset speed to 1x for each step
        lastAnimationSpeed = 1f;  
        
        // ✅ Update speed UI (slider)
        UpdateSpeedSliderUI(instructionStepInstances[currentStepIndex]);

        // ✅ Play animation & block navigation until it’s done
        GameObject firstModel = modelContainer.transform.Find("FirstModelAfterScan")?.gameObject;
        if (firstModel != null)
        {
            InstructionDetail currentStepDetail = currentInstructionDetails[currentStepIndex];

            // ✅ Block buttons
            isAnimationPlaying = true;
            SetNavigationButtonsInteractable(false);

            PlayStepAnimation(firstModel, currentStepDetail); // Play animation
        }
    }


    // 🟢 Enable/Disable Previous & Next buttons dynamically
    void SetNavigationButtonsInteractable(bool isInteractable)
    {
        if (instructionStepInstances.Count == 0) return;

        // ✅ Get the active step
        GameObject currentStep = instructionStepInstances[currentStepIndex];

        // ✅ Find buttons ONLY in the current step
        Button prevButton = currentStep.transform.Find("instructionDetailPreviousButton")?.GetComponent<Button>();
        Button nextButton = currentStep.transform.Find("instructionDetailNextStepButton")?.GetComponent<Button>();

        // 🔴 Ensure buttons stay disabled if an animation is still playing
        bool canInteract = isInteractable && !isAnimationPlaying;

        // ✅ Enable/Disable buttons correctly
        if (prevButton) prevButton.interactable = canInteract && (currentStepIndex > 0);
        if (nextButton) nextButton.interactable = canInteract && (currentStepIndex < instructionStepInstances.Count - 1);

        Debug.Log($"🔘 Buttons updated: Prev={prevButton?.interactable}, Next={nextButton?.interactable}, AnimationPlaying={isAnimationPlaying}");
    }
    
        public void GoBackToMainApp()
        {
            PlayerPrefs.SetString("LastPage", "DetailPage");
            PlayerPrefs.SetInt("ShowHomePage", 1); // ✅ Indicate that HomePage should be shown
            PlayerPrefs.SetInt("ShowDetailPage", 1); // ✅ Indicate that DetailPage should be shown
            PlayerPrefs.Save();
            SceneManager.LoadScene("MainApp");
        }


        void BackToCourseUI()
        {
            // ✅ Activate UI elements correctly
            instructionDetailStepPrefab.SetActive(true);
            instructionDetailPanel.SetActive(false);
            courseUIPanel.SetActive(true);
            backButton.gameObject.SetActive(true);

            Debug.Log("🔙 Returning to Course UI");

            // ✅ Hide all step UI elements
            foreach (GameObject stepItem in instructionStepInstances)
            {
                stepItem.SetActive(false);
            }

            // ✅ Reset to first step
            currentStepIndex = 0;
            isAnimationPlaying = false; // ✅ Ensure animations are marked as done

            // ✅ Find "FirstModelAfterScan"
            GameObject firstModel = modelContainer.transform.Find("FirstModelAfterScan")?.gameObject;
            if (firstModel != null)
            {
                firstModel.SetActive(true);

                // ✅ Reactivate all hidden meshes
                foreach (Transform child in firstModel.transform)
                {
                    child.gameObject.SetActive(true);
                    Debug.Log($"✅ Reactivating mesh: {child.name}");
                }

                // ✅ Reset Animation Component
                Animation animation = firstModel.GetComponentInChildren<Animation>(true);
                if (animation != null)
                {
                    // 🔴 Disable looping for all animations
                    foreach (AnimationState state in animation)
                    {
                        state.wrapMode = WrapMode.Once; // Prevent looping
                    }

                    // ✅ Stop, rewind, and apply first frame
                    animation.Stop();
                    animation.Rewind();
                    animation.Play(); // Play to apply first frame
                    animation.Stop();
                    animation.Sample(); // Force update to first frame
                    animation.enabled = false; // Ensure it's fully stopped

                    Debug.Log("⏹ Animation stopped and reset.");
                }

                // ✅ Reset Animator (if exists)
                Animator animator = firstModel.GetComponentInChildren<Animator>(true);
                if (animator != null)
                {
                    animator.enabled = false; // 🔴 Temporarily disable to avoid conflicts
                    animator.Rebind(); // ✅ Reset all states
                    animator.Update(0); // Force update to first frame
                    animator.enabled = true; // Reactivate after reset

                    Debug.Log("🎭 Animator force-reset successfully.");
                }
            }
            else
            {
                Debug.LogWarning("⚠ No model found with the name 'FirstModelAfterScan'.");
            }
        }
        
        
        
          void SetupModelInteractions(GameObject model)
        {
            if (model == null)
            {
                Debug.LogError("❌ No model found to setup interactions!");
                return;
            }

            // ✅ Ensure a Collider exists (required for interaction)
            if (model.GetComponent<Collider>() == null)
            {
                BoxCollider collider = model.AddComponent<BoxCollider>();
                collider.size *= 1.2f; // Adjust collider size for better touch interaction
                Debug.Log("📌 Added BoxCollider for interactions.");
            }

            // ✅ Fix Rigidbody Issues (Disable Gravity)
            Rigidbody rb = model.GetComponent<Rigidbody>();
            if (rb == null)
            {
                rb = model.AddComponent<Rigidbody>(); // Add if missing
            }

            rb.useGravity = false; // Prevent gravity from pulling it down
            rb.isKinematic = true; // Prevent unwanted physics interactions

            // ✅ Add XRGrabInteractable (Handles dragging)
            XRGrabInteractable grabInteractable = model.GetComponent<XRGrabInteractable>();
            if (grabInteractable == null)
            {
                grabInteractable = model.AddComponent<XRGrabInteractable>();
                grabInteractable.trackPosition = false; // ❌ Disable by default to prevent unwanted movement
                grabInteractable.trackRotation = false;
                grabInteractable.throwOnDetach = false;
                Debug.Log("📌 XRGrabInteractable added (Drag to move).");
            }

            // ✅ Add Event Listeners to Control Movement
            grabInteractable.selectEntered.AddListener((args) =>
                grabInteractable.trackPosition = true); // Enable on grab
            grabInteractable.selectExited.AddListener((args) =>
                grabInteractable.trackPosition = false); // Disable on release

            // ✅ Add manual pinch scaling script
            if (model.GetComponent<PinchToScale>() == null)
            {
                model.AddComponent<PinchToScale>();
                Debug.Log("📌 PinchToScale script added (Pinch to scale).");
            }
        }
        
        // ReSharper disable Unity.PerformanceAnalysis
        IEnumerator LoadImageFromLocal(string imagePath, UnityEngine.UI.Image targetImage)
        {
            Debug.Log($"🔍 Trying to load image from: {imagePath}");

            if (!File.Exists(imagePath))
            {
                Debug.LogError($"❌ Image file not found at: {imagePath}");
                yield break;
            }

            byte[] imageData = File.ReadAllBytes(imagePath);
            Texture2D texture = new Texture2D(2, 2);
            if (texture.LoadImage(imageData))
            {
                Debug.Log($"✅ Successfully loaded image from: {imagePath}");
                targetImage.sprite = Sprite.Create(texture, new Rect(0, 0, texture.width, texture.height),
                    new Vector2(0.5f, 0.5f));
            }
            else
            {
                Debug.LogError($"❌ Failed to load image from: {imagePath}");
            }
        }




        void ResetScanning()
        {
            isScanning = true;
            ShowScanUI();
        }


        void UpdateUIText(string message, string debugMessage)
        {
            if (uiMessageText is not null)
                uiMessageText.text = message;

            if (debugLogText is not null)
                debugLogText.text = debugMessage;
        }
        
 
}

// API Response Classes
[System.Serializable]
public class ApiResponse
{
    public int code;
    public CourseResult result;
}

[System.Serializable]
public class CourseResult
{
    public string id;
    public string courseCode;
    public string modelId;
    public string title;
    public string description;
    public string imageUrl;
    public Instruction[] instructions;
}

// Instruction Data Model
[System.Serializable]
public class Instruction
{
    public string id;
    public string courseId;
    public int orderNumber;
    public string name;
    public string description;
    public string position;
    public string rotation;
    public List<InstructionDetail> instructionDetailResponse;
}

[System.Serializable]
public class InstructionDetail
{
    public string id;
    public string instructionId;
    public string name;
    public List<string> meshes;
    public string animationName;
    public int orderNumber;
    public string description;
    public string fileString;
    public string imgString;
}
[System.Serializable]
public class ModelResponse
{
    public int code;
    public ModelData result;
}

[System.Serializable]
public class ModelData
{
    public string id;
    public string modelTypeId;
    public string modelTypeName;
    public string modelCode;
    public string status;
    public string name;
    public string companyId;
    public string description;
    public string imageUrl;
    public bool isUsed;
    public string version;
    
    public string scale;
    public Vector3 GetScale()
    {
        if (!string.IsNullOrEmpty(scale))
        {
            string[] scaleValues = scale.Split(',');

            if (scaleValues.Length == 1) // Single scale value (e.g., "1")
            {
                if (float.TryParse(scaleValues[0], out float uniformScale))
                {
                    return Vector3.one * uniformScale; // Apply uniform scale
                }
            }
            else if (scaleValues.Length == 3) // Separate x, y, z values (e.g., "1,2,3")
            {
                if (float.TryParse(scaleValues[0], out float x) &&
                    float.TryParse(scaleValues[1], out float y) &&
                    float.TryParse(scaleValues[2], out float z))
                {
                    return new Vector3(x, y, z);
                }
            }
        }

        return Vector3.one; // Default scale if parsing fails
    }
    public float[] position;  // Position as [x, y, z]
    public float[] rotation;  // Rotation as [x, y, z]
    public string file;
    public string courseName;
}
