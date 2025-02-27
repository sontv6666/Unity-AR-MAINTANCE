// using UnityEngine;
// using Vuforia;
// using GLTFast;
// using System.Collections;
// using System.Collections.Generic;
// using System.IO;
// using System.Threading.Tasks;
// using TMPro;
// using UnityEngine.Networking;
// using UnityEngine.UI; 
//
// using System.Linq;
//
//
// public class StepLoader : MonoBehaviour
// {
//     public string apiBaseUrl = "https://capital-earwig-vertically.ngrok-free.app/api/v1";
//     public string modelId = "644fd623-9c01-40cc-96ba-59582761482e";
//     public Transform modelTargetTransform;
//     public Transform instructionListParent;
//
//     public GameObject instructionPanel;
//     public GameObject instructionDetailPanel;
//     public GameObject instructionButtonPrefab;
//     public Button previousButton, nextButton;
//
//     private int currentStep = 0;
//     private GameObject currentModel;
//     private Animator currentAnimator;
//     private ObserverBehaviour modelTargetObserver;
//     
//     public TMP_Text instructionText;
//     public TMP_Text descriptionText;
//     public TMP_Text showStepText;
//
// public GameObject stepPrefab;
//     
//     public UnityEngine.UI.Image stepImage;
//
//     private string modelFolder;
//     private List<StepData> stepList = new List<StepData>();
//
//     void Start()
//     {
//         modelFolder = Path.Combine(Application.persistentDataPath, "Models");
//         Directory.CreateDirectory(modelFolder);
//
//         modelTargetObserver = modelTargetTransform.GetComponent<ObserverBehaviour>();
//         if (modelTargetObserver)
//             modelTargetObserver.OnTargetStatusChanged += OnTargetStatusChanged;
//
//         instructionPanel.SetActive(true);
//         instructionDetailPanel.SetActive(false);
//
//         StartCoroutine(LoadInstructionsFromAPI());
//     }
//
//     IEnumerator LoadInstructionsFromAPI()
//     {
//         string url = $"{apiBaseUrl}/model/{modelId}";
//         Debug.LogError(url);
//         UnityWebRequest request = UnityWebRequest.Get(url);
//         yield return request.SendWebRequest();
//
//         if (request.result != UnityWebRequest.Result.Success)
//         {
//             Debug.LogError("Failed to load instructions: " + request.error);
//             yield break;
//         }
//
//         string jsonData = request.downloadHandler.text;
//         stepList = JsonUtility.FromJson<ApiResponse>(jsonData).result.ToStepList(apiBaseUrl);
//
//         PopulateInstructionList();
//     }
//
//     void PopulateInstructionList()
//     {
//         foreach (Transform child in instructionListParent)
//             Destroy(child.gameObject);
//
//         var groupedSteps = stepList.GroupBy(s => s.instructionId).ToList();
//
//         foreach (var instructionGroup in groupedSteps)
//         {
//             // Create a header using the prefab
//             GameObject headerObj = Instantiate(instructionButtonPrefab, instructionListParent);
//             TMP_Text headerText = headerObj.GetComponentInChildren<TMP_Text>();
//
//             if (headerText != null)
//             {
//                 headerText.text = instructionGroup.First().instructionTitle;
//                 headerText.fontSize = 24;
//                 headerText.alignment = TextAlignmentOptions.Center;
//             }
//
//             // Add click event to show steps inside instructionDetailPanel
//             Button headerButton = headerObj.GetComponent<Button>();
//             if (headerButton != null)
//             {
//                 headerButton.onClick.AddListener(() => ShowStepsInDetailPanel(instructionGroup.ToList()));
//             }
//         }
//     }
//
//   void ShowStepsInDetailPanel(List<StepData> steps)
// {
//     if (steps == null || steps.Count == 0) return;
//
//     // Clear previous steps
//     foreach (Transform child in instructionDetailPanel.transform)
//         Destroy(child.gameObject);
//
//     // Store the steps for navigation
//     stepList = steps;
//     currentStep = 0;
//
//     foreach (var step in steps)
//     {
//         if (stepPrefab == null)
//         {
//             Debug.LogError("stepPrefab is not assigned!");
//             return;
//         }
//
//         // ✅ Use stepPrefab (correct prefab) instead of instructionDetailPanel
//         GameObject stepObj = Instantiate(stepPrefab, instructionDetailPanel.transform);
//         
//         // ✅ Get child UI elements safely
//         TMP_Text stepText = stepObj.transform.Find("IntructionText")?.GetComponent<TMP_Text>();
//         TMP_Text descText = stepObj.transform.Find("descriptionText")?.GetComponent<TMP_Text>();
//         UnityEngine.UI.Image imageShow = stepObj.transform.Find("imageShow")?.GetComponent<UnityEngine.UI.Image>();
//         TMP_Text stepCounterText = stepObj.transform.Find("showStepText")?.GetComponent<TMP_Text>();
//         Button prevButton = stepObj.transform.Find("previousButton")?.GetComponent<Button>();
//         Button nextButton = stepObj.transform.Find("nextStepButton")?.GetComponent<Button>();
//
//         // ✅ Ensure elements are found
//         if (stepText == null || descText == null || imageShow == null || stepCounterText == null || prevButton == null || nextButton == null)
//         {
//             Debug.LogError("One or more UI elements are missing in stepPrefab!");
//             return;
//         }
//
//    instructionDetailPanel.SetActive(true);
// instructionDetailPanel.SetActive(false);
//
//
//         // ✅ Assign step data
//         stepText.text = step.title;
//         descText.text = step.description;
//         stepCounterText.text = $"Step {step.step_number}/{stepList.Count}";
//
//         StartCoroutine(LoadStepImage(step.image_file, imageShow));
//
//         // ✅ Set button navigation
//         prevButton.onClick.RemoveAllListeners();
//         prevButton.onClick.AddListener(() => PreviousStep());
//         prevButton.interactable = (currentStep > 0);
//
//         nextButton.onClick.RemoveAllListeners();
//         nextButton.onClick.AddListener(() => NextStep());
//         nextButton.interactable = (currentStep < stepList.Count - 1);
//
//         // ✅ Assign button click to load detailed step
//         stepObj.GetComponent<Button>().onClick.RemoveAllListeners();
//         stepObj.GetComponent<Button>().onClick.AddListener(() => LoadStepDetail(stepList.IndexOf(step)));
//     }
//
//     // ✅ Show the first step
//     instructionDetailPanel.SetActive(true);
//
//     instructionPanel.SetActive(false);
//     LoadStepDetail(0);
// }
//
//
//
//     void LoadStepDetail(int stepIndex)
//     {
//         if (stepIndex < 0 || stepIndex >= stepList.Count) return;
//
//
//         currentStep = stepIndex;
//         StepData step = stepList[stepIndex];
//
//         // Update UI Elements
//         instructionText.text = step.title;
//         descriptionText.text = step.description;
//         showStepText.text = $"Step {step.step_number}/{stepList.Count}";
//
//         StartCoroutine(LoadStepImage(step.image_file, stepImage));
//         StartCoroutine(LoadModel(step));
//
//         // Update button states
//         previousButton.interactable = (currentStep > 0);
//         nextButton.interactable = (currentStep < stepList.Count - 1);
//     }
//
//     public void NextStep()
//     {
//         if (currentStep < stepList.Count - 1)
//         {
//             LoadStepDetail(currentStep + 1);
//         }
//     }
//
//     public void PreviousStep()
//     {
//         if (currentStep > 0)
//         {
//             LoadStepDetail(currentStep - 1);
//         }
//     }
//
//     IEnumerator LoadModel(StepData step)
//     {
//         if (currentModel != null)
//         {
//             Destroy(currentModel);
//             currentModel = null;
//             currentAnimator = null;
//         }
//
//         string modelPath = Path.Combine(modelFolder, Path.GetFileName(step.model_file));
//
//         if (!File.Exists(modelPath))
//             yield return StartCoroutine(DownloadFile(step.model_file, modelPath));
//
//         GameObject newModel = new GameObject("Model_" + step.step_number);
//         newModel.transform.SetParent(modelTargetTransform, false);
//
//         var gltf = new GLTFast.GltfImport();
//         yield return gltf.Load("file://" + modelPath);
//
//         if (gltf.InstantiateMainScene(newModel.transform))
//         {
//             currentModel = newModel;
//             currentAnimator = currentModel.GetComponentInChildren<Animator>();
//             PlayAnimation(currentAnimator, step.animation_name);
//         }
//         else
//         {
//             Debug.LogError("Failed to load GLB model from: " + modelPath);
//         }
//     }
//
//     IEnumerator DownloadFile(string url, string savePath)
//     {
//         UnityWebRequest request = UnityWebRequest.Get(url);
//         yield return request.SendWebRequest();
//
//         if (request.result == UnityWebRequest.Result.Success)
//         {
//             File.WriteAllBytes(savePath, request.downloadHandler.data);
//             Debug.Log("File downloaded: " + savePath);
//         }
//         else
//         {
//             Debug.LogError("Failed to download file: " + request.error);
//         }
//     }
//
// IEnumerator LoadStepImage(string imageFileName, UnityEngine.UI.Image targetImage)
// {
//     if (string.IsNullOrEmpty(imageFileName) || targetImage == null)
//     {
//         Debug.LogError("Invalid image file or target image component is missing.");
//         yield break;
//     }
//
//     // Construct the full URL to fetch the image
//     string imageUrl = $"{imageFileName}";
//     Debug.Log($"Loading image from: {imageUrl}");
//
//     UnityWebRequest request = UnityWebRequestTexture.GetTexture(imageUrl);
//     yield return request.SendWebRequest();
//
//     if (request.result == UnityWebRequest.Result.Success)
//     {
//         Texture2D texture = DownloadHandlerTexture.GetContent(request);
//         if (texture != null)
//         {
//             targetImage.sprite = Sprite.Create(texture, new Rect(0, 0, texture.width, texture.height), new Vector2(0.5f, 0.5f));
//         }
//     }
//     else
//     {
//         Debug.LogError($"Failed to load image: {request.error}");
//     }
// }
//
//
//
//     void PlayAnimation(Animator animator, string animationName)
//     {
//         if (animator != null && animator.runtimeAnimatorController != null)
//         {
//             if (!string.IsNullOrEmpty(animationName))
//             {
//                 animator.Play(animationName);
//                 Debug.Log($"Playing animation: {animationName}");
//                 return;
//             }
//
//             foreach (AnimationClip clip in animator.runtimeAnimatorController.animationClips)
//             {
//                 animator.Play(clip.name);
//                 Debug.Log($"Playing clip: {clip.name}");
//                 return;
//             }
//
//             Debug.LogWarning("No animations found in Animator.");
//         }
//     }
//
//     private void OnTargetStatusChanged(ObserverBehaviour behaviour, TargetStatus status)
//     {
//         if (status.Status == Status.TRACKED || status.Status == Status.EXTENDED_TRACKED)
//         {
//             LoadStepDetail(currentStep);
//         }
//     }
// }
//
//
//
// [System.Serializable]
// public class ApiInstruction
// {
//     public string id;
//     public string modelId;
//     public int orderNumber;
//     public string name;
//     public string description;
//     public string position;
//     public string rotation;
//     public List<ApiInstructionDetail> instructionDetailResponse;
// }
//
// [System.Serializable]
// public class ApiInstructionDetail
// {
//     public string id;
//     public string instructionId;
//     public string name;
//     public int orderNumber;
//     public string description;
//     public string fileString;
//     public string imgString;
// }
//
// [System.Serializable]
// public class StepData
// {
// public string instructionId;  // This should be set from API
// public string instructionTitle; // Store instructionResponse title
//
//     public int step_number;
//     public string title;
//     public string description;
//     public string image_file;
//     public string model_file;
//     public Vector3 attach_rotation;
//     public string animation_name;
// }
//
// [System.Serializable]
// public class ApiResponse
// {
//     public int code;
//     public ApiResult result;
// }
//
// [System.Serializable]
// public class ApiResult
// {
//     public string id;
//     public string name;
//     public string file;
//     public List<ApiInstruction> instructionResponses;
//
//   public List<StepData> ToStepList(string apiBaseUrl)
// {
//     List<StepData> steps = new List<StepData>();
//
//     foreach (var instruction in instructionResponses)
//     {
//         foreach (var detail in instruction.instructionDetailResponse)
//         {
//             steps.Add(new StepData
//             {
//                 instructionId = instruction.id,  // ✅ Correctly sets instruction ID
//                 instructionTitle = instruction.name,  // ✅ Gets name from instructionResponses
//
//                 step_number = detail.orderNumber,
//                 title = detail.name,  // This is fine since it's the step title
//                 description = detail.description,
//                 image_file = $"{apiBaseUrl}/files/{detail.imgString}",
//                 model_file = $"{apiBaseUrl}/files/{detail.fileString}",
//                 attach_rotation = ParseVector3(instruction.rotation),
//                 animation_name = detail.name
//             });
//         }
//     }
//     return steps;
// }
//
//
//
//     private Vector3 ParseVector3(string value)
//     {
//         string[] parts = value.Split(',');
//         if (parts.Length == 3 && 
//             float.TryParse(parts[0], out float x) && 
//             float.TryParse(parts[1], out float y) && 
//             float.TryParse(parts[2], out float z))
//         {
//             return new Vector3(x, y, z);
//         }
//         return Vector3.zero;
//     }
// }
//
//
