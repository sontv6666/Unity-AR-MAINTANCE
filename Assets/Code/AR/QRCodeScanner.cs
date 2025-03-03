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
    
    private List<GameObject> instructionStepInstances = new List<GameObject>();

    void Start()
    {
        arSessionOrigin = FindObjectOfType<ARSessionOrigin>();
        trackedImageManager = FindObjectOfType<ARTrackedImageManager>();

        if (trackedImageManager != null)
        {
            trackedImageManager.trackedImagesChanged += OnTrackedImagesChanged;
        }

        ShowScanUI();
        //StartCoroutine(FetchCourseData());


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

            // 🛑 Prevent memory leaks
            Destroy(textureData);

            if (result != null)
            {
                isScanning = false;
                Debug.Log("QR Code Scanned: " + result.Text);
                ShowLoadingUI("Processing QR Code...");
                UpdateUIText("Scanning...", "QR: " + result.Text);
                
                qrCodePosition = arCameraManager.transform.position + arCameraManager.transform.forward * 0.5f;
                Quaternion qrCodeRotation = arCameraManager.transform.rotation;

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

    
    
    // all call this
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

public IEnumerator Load3DModel(string modelPath, GameObject modelContainer, Vector3 position, Vector3 rotation)
{
    Debug.Log($"📌 Attempting to load model from: {modelPath}");

    // 🔹 Normalize file path format
    string formattedPath = modelPath.Replace("\\", "/");

    // 🔹 Construct full file path
    string fullPath;
    #if UNITY_ANDROID
        fullPath = "file://" + Path.Combine(Application.persistentDataPath, Path.GetFileName(formattedPath));
    #else
        fullPath = formattedPath; // Windows/Mac
    #endif

    Debug.Log($"🔗 Full path: {fullPath}");

    // 🔹 Check if the file exists
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

    // 🔹 Ensure the model container exists
    if (modelContainer == null)
    {
        Debug.LogError("❌ Model container is null. Cannot load model.");
        yield break;
    }

    // 🔹 Remove previously loaded model
    foreach (Transform child in modelContainer.transform)
    {
        Destroy(child.gameObject);
    }

    Debug.Log($"🔗 Preparing to load GLB from: {fullPath}");

    // 🔹 Create GLTFast loader
    var gltf = new GltfImport();
    var loadTask = gltf.Load(fullPath);
    
    while (!loadTask.IsCompleted) // Wait for loading to finish
    {
        yield return null;
    }

    if (!loadTask.Result) // Check if load was successful
    {
        Debug.LogError("❌ Failed to load GLB model.");
        yield break;
    }

    // 🔹 Instantiate the model
    var instantiateTask = gltf.InstantiateMainSceneAsync(modelContainer.transform);
    
    while (!instantiateTask.IsCompleted) // Wait for instantiation
    {
        yield return null;
    }

    if (!instantiateTask.Result)
    {
        Debug.LogError("❌ Failed to instantiate GLB model.");
        yield break;
    }

    Debug.Log("✅ 3D Model successfully loaded.");

    // 🔹 Find loaded model in the scene
    GameObject loadedModel = modelContainer.transform.childCount > 0
        ? modelContainer.transform.GetChild(modelContainer.transform.childCount - 1).gameObject
        : null;

    if (loadedModel == null)
    {
        Debug.LogError("❌ Failed to find the loaded model.");
        yield break;
    }

    // 🔹 Apply position and rotation
    loadedModel.transform.SetParent(modelContainer.transform, false);
    loadedModel.transform.position = position;
    loadedModel.transform.eulerAngles = rotation;
    loadedModel.transform.localScale = Vector3.one * 0.1f;

    Debug.Log("✅ Model correctly parented and transformed.");
}



 
 
 
 
 
    
    
    

    // all call   
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

    
void UpdateInstructionStepUI(Instruction instruction)
{
    if (currentInstructionDetails == null || currentInstructionDetails.Count == 0)
    {
        Debug.LogError("No instruction details to display!");
        return;
    }

    instructionStepInstances.Clear();

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

        // ⚡️ Chỉ dùng modelContainer chung để tải mô hình
        GameObject modelContainerForStep = modelContainer;

        if (i == 0) 
        {
            // 🚀 Load mô hình đầu tiên từ StreamingAssets
            string firstModelPath = Path.Combine(Application.persistentDataPath, "Models/051a5414-0e1a-4fb5-ae8b-b23b76f4e011.glb");

            if (modelContainerForStep != null)
            {
                StartCoroutine(Load3DModelForStep(firstModelPath, modelContainerForStep, "First Sample Model"));
            }
        }
        else if (!string.IsNullOrEmpty(detail.fileString) &&
                 (detail.fileString.EndsWith(".glb") || detail.fileString.EndsWith(".gltf")))
        {
            // 🏗 Load các mô hình còn lại từ dữ liệu API
            string modelPath = Path.Combine(Application.persistentDataPath, detail.fileString);

            if (modelContainerForStep != null)
            {
                StartCoroutine(Load3DModelForStep(modelPath, modelContainerForStep, detail.name));
            }
        }

        Button playAnimationButton = stepItem.transform.Find("playanimationButton")?.GetComponent<Button>();

        if (playAnimationButton && modelContainerForStep)
        {
            playAnimationButton.onClick.RemoveAllListeners();
            playAnimationButton.onClick.AddListener(() => PlayModelAnimation(modelContainerForStep));
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
   

// 🔄 Change step by activating/deactivating instead of destroying
void ChangeInstructionStep(int direction)
{
    if (instructionStepInstances.Count == 0) return;

    // ✅ Deactivate current step
    instructionStepInstances[currentStepIndex].SetActive(false);

    // ✅ Move step index
    currentStepIndex += direction;
    currentStepIndex = Mathf.Clamp(currentStepIndex, 0, instructionStepInstances.Count - 1);

    // ✅ Activate new step
    instructionStepInstances[currentStepIndex].SetActive(true);

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


   



//
// IEnumerator Load3DModelForStep(string modelPath, GameObject modelContainerForStep, string animationName)
// {
//     Debug.Log($"📌 Attempting to load model from: {modelPath}");
//
//     // 🔹 Normalize path format
//     string formattedPath = modelPath.Replace("\\", "/");
//
//     // 🔹 Check if file exists (special handling for StreamingAssets on Android)
//     bool fileExists = false;
//     #if UNITY_ANDROID
//         string androidPath = Path.Combine(Application.persistentDataPath, formattedPath);
//         using (UnityWebRequest request = UnityWebRequest.Get(androidPath))
//         {
//             yield return request.SendWebRequest();
//             fileExists = !request.isHttpError && !request.isNetworkError;
//         }
//     #else
//         fileExists = File.Exists(formattedPath);
//     #endif
//
//     if (!fileExists)
//     {
//         Debug.LogError($"❌ Model file not found: {formattedPath}");
//         yield break;
//     }
//
//     Debug.Log($"✅ Model file exists: {formattedPath}");
//
//     // 🔹 Ensure the model container exists
//     if (modelContainerForStep == null)
//     {
//         Debug.LogError("❌ Model container is null. Cannot load model.");
//         yield break;
//     }
//
//     // 🔹 Remove previous model safely
//     foreach (Transform child in modelContainerForStep.transform)
//     {
//         Destroy(child.gameObject);
//     }
//
//     // 🔹 Create a new GameObject to hold the model
//     GameObject newModel = new GameObject("LoadedModel");
//     newModel.transform.SetParent(modelContainerForStep.transform, false);
//     newModel.transform.localScale = Vector3.one * 0.1f;
//
//     // 🔹 Construct correct URI for UnityWebRequest
//     string uri;
//     #if UNITY_ANDROID
//         uri = androidPath; // StreamingAssets access on Android
//     #else
//         uri = "file:///" + formattedPath; // Windows/Mac
//     #endif
//
//     Debug.Log($"🔗 Loading GLB from: {uri}");
//
//     // 🔹 Create a UnityWebRequestLoader instead of DefaultLoader
//     var loader = new UnityWebRequestLoader(uri);
//
//     // 🔹 Create ImportOptions instance
//     ImportOptions importOptions = new ImportOptions()
//     {
//         DataLoader = loader
//     };
//
//     // 🔹 Load the model asynchronously using UnityGLTF
//     GLTFSceneImporter gltfImporter = new GLTFSceneImporter(uri, importOptions);
//     yield return gltfImporter.LoadSceneAsync(-1); // ✅ FIX: Pass -1 instead of Transform
//
//     Debug.Log("✅ 3D Model successfully loaded.");
//
//     // 🔹 Try to find Animator & Play Animation
//     Animator animator = newModel.GetComponentInChildren<Animator>();
//     if (animator != null)
//     {
//         Debug.Log("🎬 Animator found! Playing animation...");
//         PlayAnimation(animator, animationName);
//     }
//     else
//     {
//         Debug.LogWarning("⚠️ No Animator found on the model.");
//     }
// }
 public IEnumerator Load3DModelForStep(string modelPath, GameObject modelContainerForStep, string animationName)
{
    Debug.Log($"📌 Attempting to load model from: {modelPath}");

    // 🔹 Normalize file path format
    string formattedPath = modelPath.Replace("\\", "/");

    // 🔹 Ensure the model container exists
    if (modelContainerForStep == null)
    {
        Debug.LogError("❌ Model container is null. Cannot load model.");
        yield break;
    }

    // 🔹 Construct full file path
    string fullPath;
    #if UNITY_ANDROID
        fullPath = "file://" + Path.Combine(Application.persistentDataPath, Path.GetFileName(formattedPath));
    #else
        fullPath = "file:///" + formattedPath;
    #endif

    Debug.Log($"🔗 Full path: {fullPath}");

    // 🔹 Check if the file exists
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

    // 🔹 Remove previous model safely
    foreach (Transform child in modelContainerForStep.transform)
    {
        Destroy(child.gameObject);
    }

    Debug.Log($"🔗 Loading GLB from: {fullPath}");

    // 🔹 Use GLTFast to load model
    var gltf = new GltfImport();
    var loadTask = gltf.Load(fullPath);
    while (!loadTask.IsCompleted) yield return null; // Wait until loading is done

    if (!loadTask.Result)
    {
        Debug.LogError("❌ Failed to load GLB model.");
        yield break;
    }

    // 🔹 Instantiate the model
    var instantiateTask = gltf.InstantiateMainSceneAsync(modelContainerForStep.transform);
    while (!instantiateTask.IsCompleted) yield return null; // Wait until instantiation is done

    if (!instantiateTask.Result)
    {
        Debug.LogError("❌ Failed to instantiate GLB model.");
        yield break;
    }

    Debug.Log("✅ 3D Model successfully loaded.");

    // 🔹 Find the newly loaded model
    GameObject loadedModel = modelContainerForStep.transform.childCount > 0
        ? modelContainerForStep.transform.GetChild(modelContainerForStep.transform.childCount - 1).gameObject
        : null;

    if (loadedModel == null)
    {
        Debug.LogError("❌ Failed to find the loaded model.");
        yield break;
    }

    // 🔹 Set model parent correctly
    loadedModel.transform.SetParent(modelContainerForStep.transform, false);
    loadedModel.transform.localScale = Vector3.one * 0.1f;

    Debug.Log("✅ Model correctly parented and scaled.");

    // 🔹 Try to find Animator & Play Animation
    Animator animator = loadedModel.GetComponentInChildren<Animator>();
    if (animator != null)
    {
        Debug.Log("🎬 Animator found! Playing animation...");
        PlayAnimation(animator, animationName);
    }
    else
    {
        Debug.LogWarning("⚠️ No Animator found on the model.");
    }
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
        courseUIPanel.SetActive(true); // Show the course UI again
        Debug.Log("🔙 Returning to Course UI");
    }




    void PlayAnimation(Animator animator, string animationName)
    {
        if (animator != null && animator.runtimeAnimatorController != null)
        {
            if (!string.IsNullOrEmpty(animationName) && AnimationExists(animator, animationName))
            {
                animator.Play(animationName);
                Debug.Log($"▶️ Playing animation: {animationName}");
                return;
            }

            // ✅ Play first animation if no name is provided
            foreach (AnimationClip clip in animator.runtimeAnimatorController.animationClips)
            {
                animator.Play(clip.name);
                Debug.Log($"▶️ Playing default clip: {clip.name}");
                return;
            }

            Debug.LogWarning("⚠️ No animations found in Animator.");
        }
    }

// ✅ Function to check if an animation exists
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
    

    
    void OnTrackedImagesChanged(ARTrackedImagesChangedEventArgs eventArgs)
    {
        foreach (var trackedImage in eventArgs.updated)
        {
            if (modelContainer == null)
            {
                Debug.LogWarning("Model container is null, skipping update.");
                return;
            }

            if (trackedImage.trackingState == TrackingState.Limited || trackedImage.trackingState == TrackingState.None)
            {
                Debug.LogWarning("Tracking lost! Hiding model.");
                modelContainer.SetActive(false);
            }
            else if (trackedImage.trackingState == TrackingState.Tracking)
            {
                modelContainer.SetActive(true);

                // Update model position & rotation to follow the QR code
                modelContainer.transform.position = trackedImage.transform.position;
                modelContainer.transform.rotation = trackedImage.transform.rotation;
            }
        }
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
