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

using System.Collections;
using System.Collections.Generic;




public class ARQRCodeScanner : MonoBehaviour
{
    
    // AR Elementss
    public ARCameraManager arCameraManager;
    private bool isScanning = true;
    private Vector3 qrCodePosition = Vector3.zero;
    private Vector3 qrCodeRotation = Vector3.zero;
    public GameObject modelContainer;



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

   

    
    public GameObject instructionDetailStepPrefab; // Prefab for each step


    public TMP_Text courseTitleText;


    private List<InstructionDetail> instructionSteps = new List<InstructionDetail>();

    private List<InstructionDetail> currentInstructionDetails = new List<InstructionDetail>();
    private int currentStepIndex = 0;
    private GameObject currentLoadedModel = null;

    private List<GameObject> instructionStepInstances = new List<GameObject>();

    void Start()
    {
        arSessionOrigin = FindObjectOfType<ARSessionOrigin>();
        trackedImageManager = FindObjectOfType<ARTrackedImageManager>();

        // ✅ Show the Scan UI
        //ShowScanUI();
        StartCoroutine(FetchCourseData());


    }

    void Update()
    {
        if (isScanning)
        {
            TryScanQRCode();
        }
        
        if (Input.touchCount == 2)
            {
                Touch touch0 = Input.GetTouch(0);
                Touch touch1 = Input.GetTouch(1);

                float prevDistance = (touch0.position - touch0.deltaPosition - (touch1.position - touch1.deltaPosition)).magnitude;
                float currentDistance = (touch0.position - touch1.position).magnitude;
                float scaleFactor = currentDistance / prevDistance;

                modelContainer.transform.localScale *= scaleFactor;
            }
        
            if (Input.touchCount == 1)
            {
                Touch touch = Input.GetTouch(0);
                if (touch.phase == TouchPhase.Moved)
                {
                    Vector2 touchPos = touch.position;
                    Ray ray = Camera.main.ScreenPointToRay(touchPos);
                    if (Physics.Raycast(ray, out RaycastHit hit))
                    {
                        modelContainer.transform.position = hit.point;
                    }
                }
            }
        
            

    }

    //cach 1.1
    IEnumerator FetchCourseData()
    {
        string endpoint = "/course/3494239c-709c-4ec0-8bc2-a7a33cbaf2ef";
        UnityWebRequest request = ApiConfig.CreateRequest(endpoint);

        yield return request.SendWebRequest(); // ✅ Wait until the request is done

        if (request.result == UnityWebRequest.Result.Success)
        {
            string jsonResponse = request.downloadHandler.text;
            var response = JsonUtility.FromJson<ApiResponse>(jsonResponse);

            // ✅ Start fetching model data (position, rotation, file)
            StartCoroutine(FetchModelData(response.result));
            
            StartCoroutine(DownloadAndLoadUI(response.result)); // ✅ Load UI after getting data
        }
        else
        {
            Debug.LogError("API Request Failed: " + request.error);
            Debug.LogError("API Request result: " + request.result);
        }
    }




    //cach 2.1
    void TryScanQRCode()
    {
        if (!isScanning) return;

        if (arCameraManager.TryAcquireLatestCpuImage(out XRCpuImage image))
        {
            var conversionParams = new XRCpuImage.ConversionParams
            {
                inputRect = new RectInt(0, 0, image.width, image.height),
                outputDimensions = new Vector2Int(image.width, image.height),
                outputFormat = TextureFormat.RGBA32,
                transformation = XRCpuImage.Transformation.None
            };

            var textureData = new Texture2D(image.width, image.height, TextureFormat.RGBA32, false);
            image.Convert(conversionParams, textureData.GetRawTextureData<byte>());
            image.Dispose();
            textureData.Apply();

            IBarcodeReader barcodeReader = new BarcodeReader();
            var result = barcodeReader.Decode(textureData.GetPixels32(), textureData.width, textureData.height);

            Destroy(textureData); // 🛑 Prevent memory leaks

            if (result != null)
            {
                isScanning = false;
                Debug.Log("QR Code Scanned: " + result.Text);
                ShowLoadingUI("Processing QR Code...");
                UpdateUIText("Scanning...", "QR: " + result.Text);

                // 🔹 Store QR Code Position & Rotation
                qrCodePosition = arCameraManager.transform.position + arCameraManager.transform.forward * 0.5f;
                qrCodeRotation = arCameraManager.transform.rotation.eulerAngles;

                StartCoroutine(CheckQRCode(result.Text));
            }
        }
    }



    //cach 2.2
    IEnumerator CheckQRCode(string qrValue)
    {
        string endpoint = "/course/3494239c-709c-4ec0-8bc2-a7a33cbaf2ef";
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
            StartCoroutine(DownloadAndLoadModel(modelData));
        }
        else
        {
            Debug.LogError("Failed to fetch model data: " + request.error);
            UpdateUIText("Error", "Failed to fetch model.");
        }
    }
 
    // DOWNLOAD MODEL FROM FETCH CALL LOAD MODEL
    IEnumerator DownloadAndLoadModel(ModelResponse modelData)
    {
        string fileEndpoint = "/files/";
        string modelFilePath = Path.Combine(Application.persistentDataPath, modelData.result.file);

        yield return StartCoroutine(DownloadFile(fileEndpoint + modelData.result.file, modelData.result.file));

        // ✅ Apply position & rotation
        Vector3 position = new Vector3(modelData.result.position[0], modelData.result.position[1], modelData.result.position[2]);
        Vector3 rotation = new Vector3(modelData.result.rotation[0], modelData.result.rotation[1], modelData.result.rotation[2]);

        // ✅ Load the model
    StartCoroutine(Load3DModel(modelFilePath, modelContainer,position , rotation));

    }
    
    //LOAD MODEL FROM FETCH, DOWNLOAD MODEL

public IEnumerator Load3DModel(string modelPath, GameObject modelContainer, Vector3 position, Vector3 rotation)
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
    
    // ✅ Attach to QR code
    loadedModel.transform.position = qrCodePosition;  // Use QR Code position
    loadedModel.transform.eulerAngles = qrCodeRotation;  // Use QR Code rotation
    loadedModel.transform.localScale = Vector3.one * 0.1f;

    Debug.Log("✅ Model correctly anchored to QR Code.");
}



    // AFTER LOAD FIRST 3D MODEL.IT WILL CALL THIS FOR DOWNLOAD AND LOAD UI
    IEnumerator DownloadAndLoadUI(CourseResult course)
    {
        string fileEndpoint = "/files/";

        // ✅ Store file paths for models and images
        List<string> modelPaths = new List<string>();
        Dictionary<string, string> imagePaths = new Dictionary<string, string>();

        // ✅ Download course image if available
        if (!string.IsNullOrEmpty(course.imageUrl))
        {
            string imageFilePath = Path.Combine(Application.persistentDataPath, course.imageUrl);
            yield return StartCoroutine(DownloadFile(fileEndpoint + course.imageUrl, course.imageUrl));
            imagePaths[course.imageUrl] = imageFilePath;
        }

        // ✅ Download instruction files (images & 3D models)
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
                }

                // ✅ Download models
                if (!string.IsNullOrEmpty(detail.fileString))
                {
                    yield return StartCoroutine(DownloadFile(fileEndpoint + detail.fileString, detail.fileString));

                    if (detail.fileString.EndsWith(".glb") || detail.fileString.EndsWith(".gltf"))
                    {
                        string modelPath = Path.Combine(Application.persistentDataPath, detail.fileString);
                        modelPaths.Add(modelPath);
                    }
                }
            }
        }

        // ✅ Load UI with local images
        LoadUI(course, imagePaths);
        
    }
    
    
    // DOWNLOAD FILE
    IEnumerator DownloadFile(string fileUrl, string fileName)
    {
        string savePath = Path.Combine(Application.persistentDataPath, fileName);

        if (File.Exists(savePath))
        {
            Debug.Log("File already exists: " + fileName);
            yield break;
        }

        UnityWebRequest request = ApiConfig.CreateRequest(fileUrl);

        yield return request.SendWebRequest();

        if (request.result == UnityWebRequest.Result.Success)
        {
            File.WriteAllBytes(savePath, request.downloadHandler.data);
            Debug.Log("File downloaded and saved: " + savePath);
        }
        else
        {
            Debug.LogError("Failed to download file: " + fileUrl + " Error: " + request.error);
            UpdateUIText("File Download Error", request.error);
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
    
    void ShowCourseUI(CourseResult course)
    {
        // Hide other panels & show course UI
        scanUIPanel.SetActive(false);
        loadingUIPanel.SetActive(false);
        courseUIPanel.SetActive(true);
        instructionDetailPanel.SetActive(false);

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
            TMP_Text instructionNameText = instructionItem.transform.Find("instructionName").GetComponent<TMP_Text>();
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
}
    void UpdateInstructionStepUI(Instruction instruction)
{
    if (currentInstructionDetails == null || currentInstructionDetails.Count == 0)
    {
        Debug.LogError("No instruction details to display!");
        return;
    }

    // ✅ Hide "FirstModelAfterScan" when entering step-by-step mode
    GameObject firstModel = modelContainer.transform.Find("FirstModelAfterScan")?.gameObject;
    if (firstModel != null)
    {
        firstModel.SetActive(false);
    }

    // ✅ Clear previous step instances
    foreach (GameObject step in instructionStepInstances)
    {
        Destroy(step);
    }
    instructionStepInstances.Clear();

    List<GameObject> loadedStepModels = new List<GameObject>(); // Store all models to disable them later

    
    
    for (int i = 0; i < currentInstructionDetails.Count; i++)
    {
        InstructionDetail detail = currentInstructionDetails[i];

        GameObject stepItem = Instantiate(instructionDetailStepPrefab, instructionDetailPanel.transform);
        stepItem.SetActive(i == 0); 

        TMP_Text nameText = stepItem.transform.Find("instructionNameText")?.GetComponent<TMP_Text>();
        TMP_Text descriptionText = stepItem.transform.Find("instructionDetailDescriptionText")?.GetComponent<TMP_Text>();
        TMP_Text stepCountText = stepItem.transform.Find("instructionDetailShowStepText")?.GetComponent<TMP_Text>();

        if (nameText) nameText.text = instruction.name;
        if (descriptionText) descriptionText.text = detail.description;
        if (stepCountText) stepCountText.text = $"{i + 1}/{currentInstructionDetails.Count}";

        if (!string.IsNullOrEmpty(detail.imgString))
        {
            string imagePath = Path.Combine(Application.persistentDataPath, detail.imgString);
            Image stepImage = stepItem.transform.Find("instructionDetailImageShow")?.GetComponent<Image>();
            if (stepImage) StartCoroutine(LoadImageFromLocal(imagePath, stepImage));
        }

        // ✅ Remove old models from previous steps
        foreach (Transform child in modelContainer.transform)
        {
            if (child.name.StartsWith("ModelStep"))
            {
                Destroy(child.gameObject);
            }
        }

        GameObject modelContainerForStep = modelContainer;
        GameObject currentStepModel = null;

        // ✅ Load step-specific model if available
        if (!string.IsNullOrEmpty(detail.fileString) &&
            (detail.fileString.EndsWith(".glb") || detail.fileString.EndsWith(".gltf")))
        {
            string modelPath = Path.Combine(Application.persistentDataPath, detail.fileString);
            if (modelContainerForStep != null)
            {
                StartCoroutine(LoadModelAndWait(modelPath, modelContainerForStep, $"Step{i}", (loadedModel) =>
                {
                    if (loadedModel != null)
                    {
                        currentStepModel = loadedModel;
                        currentStepModel.SetActive(i == 0);
                    }
                }));
            }
        }

        Debug.Log($"🔍 Checking model for Step {i}: {currentStepModel?.name ?? "NULL"}");

        // ✅ Play/Stop animation button logic
        Button playAnimationButton = stepItem.transform.Find("playanimationButton")?.GetComponent<Button>();
        if (playAnimationButton)
        {
            playAnimationButton.onClick.RemoveAllListeners();
            playAnimationButton.onClick.AddListener(() =>
            {
                if (currentStepModel != null)
                {
                    currentStepModel.SetActive(true);
                    Animation animation = currentStepModel.GetComponentInChildren<Animation>(true);

                    if (animation != null)
                    {
                        string animationName = detail.name;
                        if (animation.IsPlaying(animationName))
                        {
                            animation.Stop();
                            Debug.Log($"⏹ Stopped animation: {animationName}");
                        }
                        else
                        {
                            animation.Play(animationName);
                            Debug.Log($"▶️ Playing animation: {animationName}");
                        }
                    }
                    else
                    {
                        Debug.LogWarning("⚠️ No Animation component found on the current step model.");
                    }
                }
                else
                {
                    Debug.LogWarning($"⚠️ No model found for Step {i}");
                }
            });
        }

        Button backButton = stepItem.transform.Find("backInstructionPanel")?.GetComponent<Button>();
        if (backButton)
        {
            backButton.onClick.RemoveAllListeners();
            backButton.onClick.AddListener(() => BackToCourseUI());
        }

        Button prevButton = stepItem.transform.Find("instructionDetailPreviousButton")?.GetComponent<Button>();
        Button nextButton = stepItem.transform.Find("instructionDetailNextStepButton")?.GetComponent<Button>();

        if (prevButton) prevButton.onClick.AddListener(() => ChangeInstructionStep(-1));
        if (nextButton) nextButton.onClick.AddListener(() => ChangeInstructionStep(1));

        instructionStepInstances.Add(stepItem);
    }

    UpdateStepNavigationButtons();
}

// ✅ Coroutine to Load Model & Wait Until It's Ready
IEnumerator LoadModelAndWait(string modelPath, GameObject modelContainerForStep, string stepName, System.Action<GameObject> callback)
{
    yield return Load3DModelForStep(modelPath, modelContainerForStep, stepName);
    GameObject loadedModel = modelContainerForStep.transform.Find($"Model{stepName}")?.gameObject;
    callback?.Invoke(loadedModel);
}

// 🔄 Change step logic
void ChangeInstructionStep(int direction)
{
    if (instructionStepInstances.Count == 0) return;

    // ✅ Deactivate current step UI
    instructionStepInstances[currentStepIndex].SetActive(false);

    // ✅ Move to next/previous step
    currentStepIndex += direction;
    currentStepIndex = Mathf.Clamp(currentStepIndex, 0, instructionStepInstances.Count - 1);

    // ✅ Activate new step UI
    instructionStepInstances[currentStepIndex].SetActive(true);

    // ✅ Remove models not in the current step
    foreach (Transform child in modelContainer.transform)
    {
        if (child.name.StartsWith("ModelStep"))
        {
            child.gameObject.SetActive(false);
        }
    }

    // ✅ Show the model for the current step if it exists
    GameObject currentStepModel = modelContainer.transform.Find($"ModelStep{currentStepIndex}")?.gameObject;
    if (currentStepModel != null)
    {
        currentStepModel.SetActive(true);
        currentLoadedModel = currentStepModel;
    }

    // ✅ Hide "FirstModelAfterScan" when entering step-by-step mode
    GameObject firstModel = modelContainer.transform.Find("FirstModelAfterScan")?.gameObject;
    if (firstModel != null)
    {
        firstModel.SetActive(false);
    }

    // ✅ Update navigation buttons
    UpdateStepNavigationButtons();
}

// 🟢 Enable/Disable Previous & Next buttons dynamically
void UpdateStepNavigationButtons()
{
    if (instructionStepInstances.Count == 0) return;

    GameObject currentStep = instructionStepInstances[currentStepIndex];

    Button prevButton = currentStep.transform.Find("instructionDetailPreviousButton")?.GetComponent<Button>();
    Button nextButton = currentStep.transform.Find("instructionDetailNextStepButton")?.GetComponent<Button>();

    if (prevButton) prevButton.interactable = (currentStepIndex > 0);
    if (nextButton) nextButton.interactable = (currentStepIndex < instructionStepInstances.Count - 1);
}

    
//   void UpdateInstructionStepUI(Instruction instruction)
// {
//     if (currentInstructionDetails == null || currentInstructionDetails.Count == 0)
//     {
//         Debug.LogError("No instruction details to display!");
//         return;
//     }
//     
//     // ✅ Hide "FirstModelAfterScan" when entering step-by-step mode
//     GameObject firstModel = modelContainer.transform.Find("FirstModelAfterScan")?.gameObject;
//     if (firstModel != null)
//     {
//         firstModel.SetActive(false);
//     }
//
//     instructionStepInstances.Clear();
//
//     for (int i = 0; i < currentInstructionDetails.Count; i++)
//     {
//         InstructionDetail detail = currentInstructionDetails[i];
//
//         GameObject stepItem = Instantiate(instructionDetailStepPrefab, instructionDetailPanel.transform);
//         stepItem.SetActive(i == 0); 
//
//         TMP_Text nameText = stepItem.transform.Find("instructionNameText")?.GetComponent<TMP_Text>();
//         TMP_Text descriptionText = stepItem.transform.Find("instructionDetailDescriptionText")?.GetComponent<TMP_Text>();
//         TMP_Text stepCountText = stepItem.transform.Find("instructionDetailShowStepText")?.GetComponent<TMP_Text>();
//
//         if (nameText) nameText.text = instruction.name;
//         if (descriptionText) descriptionText.text = detail.description;
//         if (stepCountText) stepCountText.text = $"{i + 1}/{currentInstructionDetails.Count}";
//
//         if (!string.IsNullOrEmpty(detail.imgString))
//         {
//             string imagePath = Path.Combine(Application.persistentDataPath, detail.imgString);
//             Image stepImage = stepItem.transform.Find("instructionDetailImageShow")?.GetComponent<Image>();
//             if (stepImage) StartCoroutine(LoadImageFromLocal(imagePath, stepImage));
//         }
//
//         // ⚡️ Use modelContainer to load models
//         GameObject modelContainerForStep = modelContainer;
//
//         if (!string.IsNullOrEmpty(detail.fileString) &&
//                  (detail.fileString.EndsWith(".glb") || detail.fileString.EndsWith(".gltf")))
//         {
//             // 🏗 Load step-specific models
//             string modelPath = Path.Combine(Application.persistentDataPath, detail.fileString);
//
//             if (modelContainerForStep != null)
//             {
//                 StartCoroutine(Load3DModelForStep(modelPath, modelContainerForStep, $"Step{i}"));
//             }
//         }
//         
//         // 🔹 Hide all step models except the current step
//         foreach (Transform child in modelContainer.transform)
//         {
//             if (child.name.StartsWith("ModelStep"))
//             {
//                 child.gameObject.SetActive(false);
//             }
//         }
//
//         // ✅ Show the model for the current step if it exists
//         GameObject currentStepModel = modelContainer.transform.Find($"ModelStep{currentStepIndex}")?.gameObject;
//         if (currentStepModel != null)
//         {
//             currentStepModel.SetActive(true);
//             currentLoadedModel = currentStepModel;
//         }
//
//         Debug.Log($"🔍 Checking model for Step {i}: {currentStepModel?.name ?? "NULL"}");
//
//         // ✅ Play animation button logic
//         // ✅ Play/Stop animation button logic
//         Button playAnimationButton = stepItem.transform.Find("playanimationButton")?.GetComponent<Button>();
//
//         if (playAnimationButton)
//         {
//             playAnimationButton.onClick.RemoveAllListeners();
//             playAnimationButton.onClick.AddListener(() =>
//             {
//                 if (currentStepModel != null)
//                 {
//                     currentStepModel.SetActive(true); // Ensure model is active
//                     Animation animation = currentStepModel.GetComponentInChildren<Animation>(true);
//
//                     if (animation != null)
//                     {
//                         string animationName = detail.name; // Get the animation name
//
//                         if (animation.IsPlaying(animationName))
//                         {
//                             animation.Stop(); // Stop animation if playing
//                             Debug.Log($"⏹ Stopped animation: {animationName}");
//                         }
//                         else
//                         {
//                             animation.Play(animationName); // Play animation if stopped
//                             Debug.Log($"▶️ Playing animation: {animationName}");
//                         }
//                     }
//                     else
//                     {
//                         Debug.LogWarning("⚠️ No Animation component found on the current step model.");
//                     }
//                 }
//                 else
//                 {
//                     Debug.LogWarning($"⚠️ No model found for Step {i}");
//                 }
//             });
//         }
//
//
//
//         Button backButton = stepItem.transform.Find("backInstructionPanel")?.GetComponent<Button>();
//
//         if (backButton)
//         {
//             backButton.onClick.RemoveAllListeners();
//             backButton.onClick.AddListener(() => BackToCourseUI());
//         }
//
//         Button prevButton = stepItem.transform.Find("instructionDetailPreviousButton")?.GetComponent<Button>();
//         Button nextButton = stepItem.transform.Find("instructionDetailNextStepButton")?.GetComponent<Button>();
//
//         if (prevButton) prevButton.onClick.AddListener(() => ChangeInstructionStep(-1));
//         if (nextButton) nextButton.onClick.AddListener(() => ChangeInstructionStep(1));
//
//         instructionStepInstances.Add(stepItem);
//     }
//
//     UpdateStepNavigationButtons();
// }  
//    
//
// // 🔄 Change step by activating/deactivating instead of destroying
//     void ChangeInstructionStep(int direction)
//     {
//         if (instructionStepInstances.Count == 0) return;
//
//         // ✅ Deactivate current step UI
//         instructionStepInstances[currentStepIndex].SetActive(false);
//
//         // ✅ Move to next/previous step
//         currentStepIndex += direction;
//         currentStepIndex = Mathf.Clamp(currentStepIndex, 0, instructionStepInstances.Count - 1);
//
//         // ✅ Activate new step UI
//         instructionStepInstances[currentStepIndex].SetActive(true);
//
//         
//      
//         
//         // 🔹 Hide all step models except current step
//         foreach (Transform child in modelContainer.transform)
//         {
//             if (child.name.StartsWith("ModelStep"))
//             {
//                 child.gameObject.SetActive(false);
//             }
//         }
//
//         // ✅ Show the model for the current step if it exists
//         GameObject currentStepModel = modelContainer.transform.Find($"ModelStep{currentStepIndex}")?.gameObject;
//         if (currentStepModel != null)
//         {
//             currentStepModel.SetActive(true);
//             currentLoadedModel = currentStepModel;
//         }
//
//         // ✅ Hide "FirstModelAfterScan" when entering step-by-step mode
//         GameObject firstModel = modelContainer.transform.Find("FirstModelAfterScan")?.gameObject;
//         if (firstModel != null)
//         {
//             firstModel.SetActive(false);
//         }
//
//         // ✅ Update navigation buttons
//         UpdateStepNavigationButtons();
//     }
//
//
// // 🟢 Enable/Disable Previous & Next buttons dynamically
// void UpdateStepNavigationButtons()
// {
//     if (instructionStepInstances.Count == 0) return;
//
//     GameObject currentStep = instructionStepInstances[currentStepIndex];
//
//     Button prevButton = currentStep.transform.Find("instructionDetailPreviousButton")?.GetComponent<Button>();
//     Button nextButton = currentStep.transform.Find("instructionDetailNextStepButton")?.GetComponent<Button>();
//
//     if (prevButton) prevButton.interactable = (currentStepIndex > 0);
//     if (nextButton) nextButton.interactable = (currentStepIndex < instructionStepInstances.Count - 1);
// }


   


public IEnumerator Load3DModelForStep(string modelPath, GameObject modelContainerForStep, string stepName)
{
    Debug.Log($"📌 Attempting to load model for step from: {modelPath}");

    string formattedPath = modelPath.Replace("\\", "/");

    if (modelContainerForStep == null)
    {
        Debug.LogError("❌ Model container is null. Cannot load model.");
        yield break;
    }

    string fullPath;
    #if UNITY_ANDROID
        fullPath = "file://" + Path.Combine(Application.persistentDataPath, Path.GetFileName(formattedPath));
    #else
        fullPath = "file:///" + formattedPath;
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

    // 🔹 Hide all previously loaded step models
    foreach (Transform child in modelContainerForStep.transform)
    {
        if (child.name.StartsWith("ModelStep"))
        {
            child.gameObject.SetActive(false);
        }
    }

    Debug.Log($"🔗 Loading GLB from: {fullPath}");

    var gltf = new GltfImport();
    var loadTask = gltf.Load(fullPath);
    while (!loadTask.IsCompleted) yield return null;

    if (!loadTask.Result)
    {
        Debug.LogError("❌ Failed to load GLB model.");
        yield break;
    }

    var instantiateTask = gltf.InstantiateMainSceneAsync(modelContainerForStep.transform);
    while (!instantiateTask.IsCompleted) yield return null;

    if (!instantiateTask.Result)
    {
        Debug.LogError("❌ Failed to instantiate GLB model.");
        yield break;
    }

    Debug.Log("✅ 3D Model successfully loaded for step.");

    GameObject loadedModel = modelContainerForStep.transform.childCount > 0
        ? modelContainerForStep.transform.GetChild(modelContainerForStep.transform.childCount - 1).gameObject
        : null;

    if (loadedModel == null)
    {
        Debug.LogError("❌ Failed to find the loaded model.");
        yield break;
    }
    
    // ✅ Stop auto-playing animation after loading
    Animation animation = loadedModel.GetComponentInChildren<Animation>(true);
    if (animation != null)
    {
        animation.Stop(); // Stop the default animation from playing automatically
        animation.playAutomatically = false; // Prevent it from playing on start
        Debug.Log("⏹ Stopped auto-playing animation.");
    }
    else
    {
        Debug.LogWarning("⚠️ No Animation component found on the loaded model.");
    }


    // 🔹 Set step model name (ModelStep1, ModelStep2, ...)
    loadedModel.name = $"Model{stepName}";

    // 🔹 Set the loaded model as the current active model
    currentLoadedModel = loadedModel;
    currentLoadedModel.SetActive(true);

    // 🔹 Set model parent correctly
    currentLoadedModel.transform.SetParent(modelContainerForStep.transform, false);
    currentLoadedModel.transform.position = qrCodePosition;  
    currentLoadedModel.transform.eulerAngles = qrCodeRotation;
    currentLoadedModel.transform.localScale = Vector3.one * 0.1f; 

    Debug.Log($"✅ Model correctly anchored for step: {stepName}");

 
}







   




    void PlayModelAnimation(GameObject modelContainerForStep)
    {
        if (modelContainerForStep == null)
        {
            Debug.LogWarning("⚠️ Model container is null.");
            return;
        }

        Animator animator = modelContainerForStep.GetComponentInChildren<Animator>();
        if (animator is not null)
        {
            if (animator.runtimeAnimatorController != null &&
                animator.runtimeAnimatorController.animationClips.Length > 0)
            {
                string firstAnimation = animator.runtimeAnimatorController.animationClips[0].name;
                animator.Play(firstAnimation);
                Debug.Log($"▶️ Playing animation: {firstAnimation}");
            }
            else
            {
                Debug.LogWarning("⚠️ No animations found in the Animator.");
            }
        }
        else
        {
            Debug.LogWarning("⚠️ No Animator component found on the loaded model.");
        }
    }
       

    

    void BackToCourseUI()
    {
        instructionDetailPanel.SetActive(false);
        courseUIPanel.SetActive(true);
    
        Debug.Log("🔙 Returning to Course UI");

        // ✅ Hide all step models
        foreach (Transform child in modelContainer.transform)
        {
            if (child.name.StartsWith("ModelStep"))
            {
                child.gameObject.SetActive(false);
            }
        }

        // ✅ Hide all step UI elements
        foreach (GameObject stepItem in instructionStepInstances)
        {
            stepItem.SetActive(false);
        }

        // ✅ Reset to first step (so it starts fresh when reopened)
        currentStepIndex = 0;

        // ✅ Reactivate "FirstModelAfterScan"
        GameObject firstModel = modelContainer.transform.Find("FirstModelAfterScan")?.gameObject;
        if (firstModel != null)
        {
            firstModel.SetActive(true);
        }
    }






    // ✅ Updated PlayAnimation function (For Animator only)
    void PlayAnimation(Animator animator, string animationName)
    {
        if (animator == null || animator.runtimeAnimatorController == null)
        {
            Debug.LogWarning("⚠️ Animator or Controller is missing!");
            return;
        }

        if (AnimationExists(animator, animationName))
        {
            animator.Play(animationName);
            Debug.Log($"▶️ Playing animation: {animationName}");
        }
        else
        {
            Debug.LogWarning($"⚠️ Animation '{animationName}' not found in Animator.");
        }
    }

// ✅ Helper function to check if animation exists in the Animator
    bool AnimationExists(Animator animator, string animationName)
    {
        foreach (AnimationClip clip in animator.runtimeAnimatorController.animationClips)
        {
            if (clip.name == animationName)
            {
                return true;
            }
        }
        return false;
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
    public float[] position;  // Position as [x, y, z]
    public float[] rotation;  // Rotation as [x, y, z]
    public string file;
    public string courseName;
}
