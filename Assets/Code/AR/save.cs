// using UnityEngine;
// using UnityEngine.XR.ARFoundation;
// using UnityEngine.XR.ARSubsystems;
// using ZXing;
// using TMPro;
// using UnityEngine.Networking;
// using System.Collections;
// using System.Collections.Generic;
//
// public class ARQRCodeScanner : MonoBehaviour
// {
//     public ARCameraManager arCameraManager;  
//     public TMP_Text uiText; 
//     public TMP_Text uilog; 
//     private bool isScanning = true;
//     
//     public GameObject modelContainer;
//
//     
//     Vector3 qrCodePosition = Vector3.zero; // Store position of QR Code
//     
//     
//     ARSessionOrigin arSessionOrigin;
//     ARTrackedImageManager trackedImageManager;
//
//     void Start()
//     {
//         arSessionOrigin = FindObjectOfType<ARSessionOrigin>();
//         trackedImageManager = FindObjectOfType<ARTrackedImageManager>();
//
//         if (trackedImageManager != null)
//         {
//             trackedImageManager.trackedImagesChanged += OnTrackedImagesChanged;
//         }
//     }
//
//
//     void Update()
//     {
//         if (isScanning)
//         {
//             TryScanQRCode();
//         }
//     }
//
//     void TryScanQRCode()
//     {
//         if (!isScanning) return;
//
//         if (arCameraManager.TryAcquireLatestCpuImage(out XRCpuImage image))
//         {
//             var conversionParams = new XRCpuImage.ConversionParams
//             {
//                 inputRect = new RectInt(0, 0, image.width, image.height),
//                 outputDimensions = new Vector2Int(image.width, image.height),
//                 outputFormat = TextureFormat.RGBA32,
//                 transformation = XRCpuImage.Transformation.None
//             };
//
//             var textureData = new Texture2D(image.width, image.height, TextureFormat.RGBA32, false);
//             image.Convert(conversionParams, textureData.GetRawTextureData<byte>());
//             image.Dispose();
//             textureData.Apply();
//
//             IBarcodeReader barcodeReader = new BarcodeReader();
//             var result = barcodeReader.Decode(textureData.GetPixels32(), textureData.width, textureData.height);
//         
//             // 🛑 Prevent memory leaks
//             Destroy(textureData);  
//
//             if (result != null)
//             {
//                 isScanning = false;  
//                 Debug.Log("QR Code Scanned: " + result.Text);
//                 UpdateUIText("Scanning...", "QR: " + result.Text);
//
//                 qrCodePosition = arCameraManager.transform.position + arCameraManager.transform.forward * 0.5f;
//                 StartCoroutine(CheckQRCode(result.Text));
//             }
//         }
//     }
//
//
//
//     IEnumerator CheckQRCode(string qrValue)
//     {
//         string endpoint = "/course/3494239c-709c-4ec0-8bc2-a7a33cbaf2ef";
//         UnityWebRequest request = ApiConfig.CreateRequest(endpoint);
//
//         yield return request.SendWebRequest();
//
//         if (request.result == UnityWebRequest.Result.Success)
//         {
//             string jsonResponse = request.downloadHandler.text;
//             Debug.Log("API Response: " + jsonResponse);
//             ProcessResponse(jsonResponse, qrValue);
//         }
//         else
//         {
//             Debug.LogError("API Request Failed: " + request.error);
//             UpdateUIText("Scan failed. Try again.", "");
//             Invoke(nameof(ResetScanning), 2f); // 🔥 Added to reset scanning after failure
//         }
//     }
//
//
//     void ProcessResponse(string jsonResponse, string scannedQR)
//     {
//         var response = JsonUtility.FromJson<ApiResponse>(jsonResponse);
//     
//         if (response.code == 1000 && response.result.courseCode == scannedQR)
//         {
//             UpdateUIText("QR Validated! Loading UI...", "Course: " + response.result.courseCode);
//             StartCoroutine(DownloadAndLoadUI(response.result));
//         }
//         else
//         {
//             UpdateUIText("Invalid QR Code.", "Scanned: " + scannedQR);
//             Invoke(nameof(ResetScanning), 2f);
//         }
//     }
//
//     IEnumerator DownloadAndLoadUI(CourseResult course)
//     {
//         string fileEndpoint = "/files/";
//
//         // Download course image (if available)
//         if (!string.IsNullOrEmpty(course.imageUrl))
//         {
//             yield return StartCoroutine(DownloadFile(fileEndpoint + course.imageUrl));
//         }
//
//         List<string> modelPaths = new List<string>(); // ✅ List to store all model paths
//
//         // Download each file in the instructions
//         foreach (var instruction in course.instructions)
//         {
//             foreach (var detail in instruction.instructionDetailResponse)
//             {
//                 if (!string.IsNullOrEmpty(detail.imgString))
//                 {
//                     yield return StartCoroutine(DownloadFile(fileEndpoint + detail.imgString));
//                 }
//                 if (!string.IsNullOrEmpty(detail.fileString))
//                 {
//                     yield return StartCoroutine(DownloadFile(fileEndpoint + detail.fileString));
//
//                     // ✅ Store multiple model paths
//                     if (detail.fileString.EndsWith(".glb") || detail.fileString.EndsWith(".gltf"))
//                     {
//                         string fullPath = Application.persistentDataPath + "/" + detail.fileString;
//                         modelPaths.Add(fullPath);
//                     }
//                 }
//             }
//         }
//
//         LoadUI(course);
//
//         // ✅ Load all 3D models
//         foreach (string path in modelPaths)
//         {
//             StartCoroutine(Load3DModel(path));
//         }
//     }
//
//
//     IEnumerator DownloadFile(string fileUrl)
//     {
//         UnityWebRequest request = ApiConfig.CreateRequest(fileUrl);
//
//         yield return request.SendWebRequest();
//
//         if (request.result == UnityWebRequest.Result.Success)
//         {
//             string filePath = Application.persistentDataPath + "/" + System.IO.Path.GetFileName(fileUrl);
//             System.IO.File.WriteAllBytes(filePath, request.downloadHandler.data);
//             Debug.Log("Downloaded: " + filePath);
//         }
//         else
//         {
//             Debug.LogError("Download failed: " + request.error);
//         }
//     }
//
//     IEnumerator Load3DModel(string modelPath)
//     {
//         Debug.Log("Attempting to load 3D model from: " + modelPath);
//
//         if (!System.IO.File.Exists(modelPath))
//         {
//             Debug.LogError("Model file does not exist: " + modelPath);
//             yield break;
//         }
//
//         if (modelContainer == null)
//         {
//             Debug.LogWarning("ModelContainer not assigned. Creating one dynamically.");
//             modelContainer = new GameObject("ModelContainer");
//         }
//
//         // Create a new GameObject for the model
//         GameObject gltfObject = new GameObject("LoadedModel");
//         gltfObject.transform.SetParent(modelContainer.transform);
//         gltfObject.transform.localScale = Vector3.one * 0.1f;
//
//         // ✅ Use "file://" prefix for Android and WebGL compatibility
//         string fileUrl = "file://" + modelPath;
//         Debug.Log("Loading GLB file from: " + fileUrl);
//
//         var gltfComponent = gltfObject.AddComponent<GLTFast.GltfAsset>();
//     
//         // ✅ Call Load() with the correct argument
//         yield return gltfComponent.Load(fileUrl);
//
//         if (gltfComponent.transform.childCount > 0)
//         {
//             Debug.Log("3D Model successfully loaded.");
//
//             // Attach to AR Anchor to fix it in space
//             var anchorManager = FindObjectOfType<ARAnchorManager>();
//             if (anchorManager != null)
//             {
//                 var anchor = anchorManager.AddAnchor(new Pose(qrCodePosition, Quaternion.identity));
//                 if (anchor != null)
//                 {
//                     gltfObject.transform.SetParent(anchor.transform);
//                     Debug.Log("Model anchored to AR space.");
//                 }
//                 else
//                 {
//                     Debug.LogWarning("Failed to create anchor.");
//                 }
//             }
//             else
//             {
//                 Debug.LogWarning("ARAnchorManager not found.");
//             }
//         }
//         else
//         {
//             Debug.LogError("Failed to load model: " + modelPath);
//         }
//     }
//
//
//
//
//
//
//
//     IEnumerator DownloadFile(string fileUrl, string fileName)
//     {
//         string savePath = Application.persistentDataPath + "/" + fileName;
//
//         if (System.IO.File.Exists(savePath))
//         {
//             Debug.Log("File already exists: " + fileName);
//             yield break;
//         }
//
//         string fullUrl = ApiConfig.GetBaseUrl() + "/files/" + fileName; // Corrected API URL
//         UnityWebRequest request = ApiConfig.CreateRequest(fullUrl);
//
//         yield return request.SendWebRequest();
//
//         if (request.result == UnityWebRequest.Result.Success)
//         {
//             System.IO.File.WriteAllBytes(savePath, request.downloadHandler.data);
//             Debug.Log("File downloaded and saved: " + savePath);
//         }
//         else
//         {
//             Debug.LogError("Failed to download file: " + fullUrl + " Error: " + request.error);
//             UpdateUIText("File Download Error", request.error);
//         }
//     }
//
//
//
//     void LoadUI(CourseResult course)
//     {
//         Debug.Log("Loading UI for: " + course.title);
//         UpdateUIText("Course Loaded!", "Title: " + course.title);
//         // TODO: Hiển thị giao diện hướng dẫn từ course
//     }
//
//     void UpdateUIText(string text, string text1)
//     {
//         if (uiText != null)
//         {
//             uiText.text = text;
//         }
//         if (uilog != null)
//         {
//             uilog.text = text1;
//         }
//     }
//
//     void ResetScanning()
//     {
//         isScanning = true;
//         UpdateUIText("Scan a QR Code", "");
//     }
//     
//     
//     void OnTrackedImagesChanged(ARTrackedImagesChangedEventArgs eventArgs)
//     {
//         foreach (var trackedImage in eventArgs.updated)
//         {
//             if (trackedImage.trackingState == TrackingState.Limited)
//             {
//                 Debug.LogWarning("Tracking lost! Hiding model.");
//                 if (modelContainer != null)
//                 {
//                     modelContainer.SetActive(false);
//                 }
//             }
//             else if (trackedImage.trackingState == TrackingState.Tracking)
//             {
//                 if (modelContainer != null)
//                 {
//                     modelContainer.SetActive(true);
//                 }
//             }
//         }
//     }
//
//
// }
//
// [System.Serializable]
// public class ApiResponse
// {
//     public int code;
//     public CourseResult result;
// }
//
// [System.Serializable]
// public class CourseResult
// {
//     public string id;
//     public string courseCode;
//     public string title;
//     public string description;
//     public string imageUrl;
//     public Instruction[] instructions;
// }
//
// [System.Serializable]
// public class Instruction
// {
//     public string id;
//     public string name;
//     public string description;
//     public InstructionDetail[] instructionDetailResponse;
// }
//
// [System.Serializable]
// public class InstructionDetail
// {
//     public string id;
//     public string name;
//     public string description;
//     public string fileString;
//     public string imgString;
// }
