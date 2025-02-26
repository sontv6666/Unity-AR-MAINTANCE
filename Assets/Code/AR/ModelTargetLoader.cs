// using UnityEngine;
// using Vuforia;
//
// public class ModelTargetLoader : MonoBehaviour
// {
//     private string modelTargetPath;
//
//     void Start()
//     {
//         modelTargetPath = Path.Combine(Application.persistentDataPath, "DownloadedFiles", "YourModelTargetDatabase");
//         LoadModelTarget(modelTargetPath, "YourTargetName");
//     }
//
//     void LoadModelTarget(string databasePath, string targetName)
//     {
//         var modelTarget = VuforiaBehaviour.Instance.ObserverFactory.CreateModelTarget(
//             databasePath: databasePath,
//             targetName: targetName,
//             hasOcclusion: false,
//             hasCollision: false,
//             trackingOptimization: TrackingOptimization.DEFAULT,
//             enhanceRuntimeDetection: false
//         );
//
//         if (modelTarget != null)
//         {
//             Debug.Log("Model Target loaded successfully.");
//             // Additional setup if needed
//         }
//         else
//         {
//             Debug.LogError("Failed to load Model Target.");
//         }
//     }
// }