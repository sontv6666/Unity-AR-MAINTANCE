using UnityEngine;
using UnityEngine.XR.ARFoundation;
using UnityEngine.XR.ARSubsystems;
using ZXing;
using TMPro;
using System.IO;
using System.Linq;

using UnityEngine.Networking;
using System.Collections;
using System.Collections.Generic;

public class ARQRCodeScanner : MonoBehaviour
{
    public ARCameraManager arCameraManager;
    
    // UI Elements
    public GameObject scanUIPanel;        
    public GameObject loadingUIPanel;     
    public GameObject courseUIPanel;      
    public TMP_Text uiMessageText;        
    public TMP_Text courseTitleText;      
    public TMP_Text courseDescriptionText;
    public TMP_Text instructionsText;
    public TMP_Text debugLogText;  // For debugging

    private bool isScanning = true;
    private Vector3 qrCodePosition = Vector3.zero;
    public GameObject modelContainer;

    private ARSessionOrigin arSessionOrigin;
    private ARTrackedImageManager trackedImageManager;
    
    public GameObject instructionTemplateContent; // Parent container for instruction steps
    public GameObject instructionDetailStepPrefab; // Prefab for each step
    public GameObject instructionDetailPanel; // Panel to show instruction details
    public TMP_Text instructionDetailTitleText;
    public TMP_Text instructionDetailDescriptionText;
    public GameObject instructionDetailModelForEachStep;
    public UnityEngine.UI.Image instructionDetailImage;
    public UnityEngine.UI.Button instructionDetailPreviousButton;
    public UnityEngine.UI.Button instructionDetailNextStepButton;
    
    public TMP_Text instructionDetailShowStepText; 


    private int currentStepIndex = 0;
    private List<InstructionDetail> instructionSteps = new List<InstructionDetail>();

    void Start()
    {
        arSessionOrigin = FindObjectOfType<ARSessionOrigin>();
        trackedImageManager = FindObjectOfType<ARTrackedImageManager>();

        if (trackedImageManager != null)
        {
            trackedImageManager.trackedImagesChanged += OnTrackedImagesChanged;
        }

        ShowScanUI();
    }


    void Update()
    {
        if (isScanning)
        {
            TryScanQRCode();
        }
    }

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
                StartCoroutine(CheckQRCode(result.Text));
            }
        }
    }



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


    void ProcessResponse(string jsonResponse, string scannedQR)
    {
        var response = JsonUtility.FromJson<ApiResponse>(jsonResponse);
    
        if (response.code == 1000 && response.result.courseCode == scannedQR)
        {
            UpdateUIText("QR Validated! Loading UI...", "Course: " + response.result.courseCode);
            StartCoroutine(DownloadAndLoadUI(response.result));
        }
        else
        {
            UpdateUIText("Invalid QR Code.", "Scanned: " + scannedQR);
            Invoke(nameof(ResetScanning), 2f);
        }
    }

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

        // ✅ Load all 3D models
        foreach (string path in modelPaths)
        {
            StartCoroutine(Load3DModel(path));
        }
    }
    void LoadUI(CourseResult course, Dictionary<string, string> imagePaths)
    {
        ShowCourseUI(course); // This method sets course title, description, and instructions

        // ✅ Load the course image if available
        if (!string.IsNullOrEmpty(course.imageUrl) && imagePaths.ContainsKey(course.imageUrl))
        {
            StartCoroutine(LoadImageFromLocal(imagePaths[course.imageUrl], courseUIPanel.transform.Find("CourseImage").GetComponent<UnityEngine.UI.Image>()));
        }
    }




    IEnumerator DownloadFile(string fileUrl, string fileName)
    {
        string savePath = Path.Combine(Application.persistentDataPath, fileName);

        if (File.Exists(savePath))
        {
            Debug.Log("File already exists: " + fileName);
            yield break;
        }

        string fullUrl = ApiConfig.GetBaseUrl() + fileUrl;
        UnityWebRequest request = ApiConfig.CreateRequest(fullUrl);

        yield return request.SendWebRequest();

        if (request.result == UnityWebRequest.Result.Success)
        {
            File.WriteAllBytes(savePath, request.downloadHandler.data);
            Debug.Log("File downloaded and saved: " + savePath);
        }
        else
        {
            Debug.LogError("Failed to download file: " + fullUrl + " Error: " + request.error);
            UpdateUIText("File Download Error", request.error);
        }
    }


    IEnumerator Load3DModel(string modelPath)
    {
        Debug.Log("Attempting to load 3D model from: " + modelPath);

        if (!File.Exists(modelPath))
        {
            Debug.LogError("Model file does not exist: " + modelPath);
            yield break;
        }

        if (modelContainer == null)
        {
            modelContainer = new GameObject("ModelContainer");
        }

        GameObject gltfObject = new GameObject("LoadedModel");
        gltfObject.transform.SetParent(modelContainer.transform);
        gltfObject.transform.localScale = Vector3.one * 0.1f;

        string fileUrl = "file://" + modelPath;
        Debug.Log("Loading GLB file from: " + fileUrl);

        var gltfComponent = gltfObject.AddComponent<GLTFast.GltfAsset>();
    
        yield return gltfComponent.Load(fileUrl);

        if (gltfComponent.transform.childCount > 0)
        {
            Debug.Log("3D Model successfully loaded.");

            var anchorManager = FindObjectOfType<ARAnchorManager>();
            if (anchorManager != null)
            {
                var anchor = anchorManager.AddAnchor(new Pose(qrCodePosition, Quaternion.identity));
                if (anchor != null)
                {
                    gltfObject.transform.SetParent(anchor.transform);
                    Debug.Log("Model anchored to AR space.");
                }
                else
                {
                    Debug.LogWarning("Failed to create anchor.");
                }
            }
        }
        else
        {
            Debug.LogError("Failed to load model: " + modelPath);
        }
    }



    void ShowScanUI(string message = "Scan a QR Code")
    {
        scanUIPanel.SetActive(true);
        loadingUIPanel.SetActive(false);
        courseUIPanel.SetActive(false);
        uiMessageText.text = message;
    }

    void ShowLoadingUI(string message = "Loading...")
    {
        scanUIPanel.SetActive(false);
        loadingUIPanel.SetActive(true);
        courseUIPanel.SetActive(false);
        uiMessageText.text = message;
    }

 void ShowCourseUI(CourseResult course)
{
    scanUIPanel.SetActive(false);
    loadingUIPanel.SetActive(false);
    courseUIPanel.SetActive(true);

    courseTitleText.text = course.title;
    courseDescriptionText.text = course.description;

    // ✅ Clear previous instruction steps
    foreach (Transform child in instructionTemplateContent.transform)
    {
        Destroy(child.gameObject);
    }

    instructionSteps.Clear(); // ✅ Clear the instruction steps list
    currentStepIndex = 0; // ✅ Reset step index

    // ✅ Populate Instructions
    foreach (var instruction in course.instructions)
    {
        GameObject stepItem = Instantiate(instructionDetailStepPrefab, instructionTemplateContent.transform);

        TMP_Text stepNameText = stepItem.transform.Find("instructionNameText").GetComponent<TMP_Text>();
        TMP_Text stepDescriptionText = stepItem.transform.Find("instructionDetailDescriptionText").GetComponent<TMP_Text>();

        stepNameText.text = instruction.name;
        stepDescriptionText.text = instruction.description;

        instructionSteps.Add(new InstructionDetail
        {
            id = instruction.id,
            name = instruction.name,
            description = instruction.description,
            fileString = instruction.instructionDetailResponse.Length > 0 ? instruction.instructionDetailResponse[0].fileString : "",
            imgString = instruction.instructionDetailResponse.Length > 0 ? instruction.instructionDetailResponse[0].imgString : ""
        });

        // ✅ Load images & models from local storage
        foreach (var detail in instruction.instructionDetailResponse)
        {
            // ✅ Load Images into UnityEngine.UI.Image
            if (!string.IsNullOrEmpty(detail.imgString))
            {
                string imagePath = Path.Combine(Application.persistentDataPath, detail.imgString);
                UnityEngine.UI.Image stepImage = stepItem.transform.Find("instructionDetailImage").GetComponent<UnityEngine.UI.Image>();
                StartCoroutine(LoadImageFromLocal(imagePath, stepImage));
            }

            // ✅ Load 3D Models
            if (!string.IsNullOrEmpty(detail.fileString) && (detail.fileString.EndsWith(".glb") || detail.fileString.EndsWith(".gltf")))
            {
                string modelPath = Path.Combine(Application.persistentDataPath, detail.fileString);
                GameObject modelContainer = stepItem.transform.Find("instructionDetailModelForEachStep")?.gameObject;

                if (modelContainer != null)
                {
                    StartCoroutine(Load3DModelForStep(modelPath, modelContainer));
                }
                else
                {
                    Debug.LogWarning("Model container not found in step item.");
                }
            }
        }
    }

    // ✅ Update step UI to show the first step
    UpdateStepUI();
}

void UpdateStepUI()
{
    if (instructionSteps.Count > 0 && instructionDetailShowStepText != null)
    {
        instructionDetailShowStepText.text = $"Step {currentStepIndex + 1} of {instructionSteps.Count}";
    }
}


IEnumerator Load3DModelForStep(string modelPath, GameObject modelContainer)
{
    if (!File.Exists(modelPath))
    {
        Debug.LogError("Model file does not exist: " + modelPath);
        yield break;
    }

    if (modelContainer == null)
    {
        Debug.LogError("Model container is null. Cannot load model.");
        yield break;
    }

    GameObject gltfObject = new GameObject("StepModel");
    gltfObject.transform.SetParent(modelContainer.transform);
    gltfObject.transform.localScale = Vector3.one * 0.1f;

    string fileUrl = "file://" + modelPath;
    var gltfComponent = gltfObject.AddComponent<GLTFast.GltfAsset>();
    yield return gltfComponent.Load(fileUrl);

    if (gltfComponent.transform.childCount > 0)
    {
        Debug.Log("3D Model successfully loaded for step.");
    }
    else
    {
        Debug.LogError("Failed to load 3D model: " + modelPath);
    }
}

IEnumerator LoadImageFromLocal(string filePath, UnityEngine.UI.Image targetImage)
{
    if (!File.Exists(filePath))
    {
        Debug.LogError("Image file does not exist: " + filePath);
        yield break;
    }

    byte[] fileData = File.ReadAllBytes(filePath);
    Texture2D texture = new Texture2D(2, 2);
    texture.LoadImage(fileData);

    // ✅ Convert Texture2D to Sprite
    Sprite sprite = Sprite.Create(texture, new Rect(0, 0, texture.width, texture.height), new Vector2(0.5f, 0.5f));

    if (targetImage != null)
    {
        targetImage.sprite = sprite;
        targetImage.preserveAspect = true; // ✅ Keeps image ratio correct
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
            if (trackedImage.trackingState == TrackingState.Limited)
            {
                Debug.LogWarning("Tracking lost! Hiding model.");
                if (modelContainer != null)
                {
                    modelContainer.SetActive(false);
                }
            }
            else if (trackedImage.trackingState == TrackingState.Tracking)
            {
                if (modelContainer != null)
                {
                    modelContainer.SetActive(true);
                }
            }
        }
    }
     
 

     
    void UpdateUIText(string message, string debugMessage)
    {
        if (uiMessageText != null)
            uiMessageText.text = message;

        if (debugLogText != null)
            debugLogText.text = debugMessage;
    }

    
    public void ShowPreviousStep()
    {
        if (currentStepIndex > 0)
        {
            currentStepIndex--;
            ShowStep(currentStepIndex);
        }
    }

    public void ShowNextStep()
    {
        if (currentStepIndex < instructionSteps.Count - 1)
        {
            currentStepIndex++;
            ShowStep(currentStepIndex);
        }
    }

    void ShowStep(int stepIndex)
    {
        if (instructionSteps == null || instructionSteps.Count == 0) return;

        InstructionDetail currentStep = instructionSteps[stepIndex];

        instructionDetailTitleText.text = currentStep.name;
        instructionDetailDescriptionText.text = currentStep.description;

        instructionDetailShowStepText.text = $"Step {stepIndex + 1} of {instructionSteps.Count}"; // ✅ Update step count
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
    public string title;
    public string description;
    public string imageUrl;
    public Instruction[] instructions;
}

[System.Serializable]
public class Instruction
{
    public string id;
    public string name;
    public string description;
    public InstructionDetail[] instructionDetailResponse;
}

[System.Serializable]
public class InstructionDetail
{
    public string id;
    public string name;
    public string description;
    public string fileString;
    public string imgString;
}
