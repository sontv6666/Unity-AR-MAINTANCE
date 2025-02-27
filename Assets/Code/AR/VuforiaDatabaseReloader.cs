// using UnityEngine;
// using Vuforia;
//
// public class VuforiaDatabaseReloader : MonoBehaviour
// {
//     public void ReloadVuforiaDatabase()
//     {
//         Debug.Log("🔄 Reloading Vuforia Model Target...");
//
//         // Ensure Vuforia is running
//         if (VuforiaBehaviour.Instance == null)
//         {
//             Debug.LogError("❌ VuforiaBehaviour instance not found!");
//             return;
//         }
//
//         // Reset the Device Pose to ensure tracking reinitialization
//         VuforiaBehaviour.Instance.DevicePoseBehaviour.Reset();
//
//         // Find ModelTargetBehaviour and reload it
//         var modelTarget = FindObjectOfType<ModelTargetBehaviour>();
//         if (modelTarget != null)
//         {
//             Debug.Log("✅ Model Target Found. Reloading...");
//             modelTarget.enabled = false;
//             modelTarget.enabled = true;
//         }
//         else
//         {
//             Debug.LogError("❌ Model Target not found in the scene.");
//         }
//     }
// }