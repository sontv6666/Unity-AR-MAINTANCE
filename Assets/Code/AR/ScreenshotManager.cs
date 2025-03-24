// using System.Collections;
// using UnityEngine;
// using System.IO;
// using UnityEngine.UI;
// using UnityEngine.Android; // For Android permissions
//
// public class ScreenshotManager : MonoBehaviour
// {
//     public Button screenshotButton;  // Button to capture screenshot
//     public GameObject previewPanel;  // Panel to show the screenshot preview
//     public Image previewImage;       // UI Image to display the captured screenshot
//     public Button yesButton;         // Button to confirm saving the image
//     public Button noButton;          // Button to discard the image
//     public GameObject uiContainer;   // Parent object containing all UI elements
//
//     private string fileName;
//     private string filePath;
//
//     void Start()
//     {
//         screenshotButton.onClick.AddListener(TakeScreenshot);
//         yesButton.onClick.AddListener(SaveToGallery);
//         noButton.onClick.AddListener(ClosePreview);
//
//         previewPanel.SetActive(false); // Hide the preview panel at the start
//
//         // 🟢 Request storage permissions on Android
//         if (Application.platform == RuntimePlatform.Android)
//         {
//             if (!Permission.HasUserAuthorizedPermission(Permission.ExternalStorageWrite))
//             {
//                 Permission.RequestUserPermission(Permission.ExternalStorageWrite);
//             }
//         }
//     }
//
//     public void TakeScreenshot()
//     {
//         StartCoroutine(CaptureScreenshot());
//     }
//
//     private IEnumerator CaptureScreenshot()
//     {
//         // 🔹 Hide the UI before taking a screenshot
//         uiContainer.SetActive(false);
//
//         // 📌 Generate a unique filename using timestamp
//         fileName = "QR_Screenshot_" + System.DateTime.Now.ToString("yyyyMMdd_HHmmss") + ".png";
//         filePath = Path.Combine(Application.persistentDataPath, fileName);
//
//         // 📸 Capture the screenshot
//         ScreenCapture.CaptureScreenshot(fileName);
//         yield return new WaitForSeconds(1f); // Wait for the screenshot to be saved
//
//         // 🔹 Show UI again after capturing the screenshot
//         uiContainer.SetActive(true);
//
//         // 📌 Load the saved image into a Texture2D
//         yield return new WaitUntil(() => File.Exists(filePath));
//         byte[] imageData = File.ReadAllBytes(filePath);
//         Texture2D texture = new Texture2D(2, 2);
//         texture.LoadImage(imageData);
//
//         // 🎨 Display the captured screenshot in the preview UI
//         previewImage.sprite = Sprite.Create(texture, new Rect(0, 0, texture.width, texture.height), Vector2.one * 0.5f);
//         previewPanel.SetActive(true);
//     }
//
//     private void SaveToGallery()
//     {
//         // 📌 Save the screenshot to the gallery
//         NativeGallery.Permission permission = NativeGallery.SaveImageToGallery(filePath, "MyScreenshots", fileName);
//         Debug.Log("✅ Saved to Gallery: " + permission);
//         
//         // 🎉 Show success message and hide preview panel
//         previewPanel.SetActive(false);
//         Debug.Log("🎉 Screenshot saved successfully!");
//     }
//
//     private void ClosePreview()
//     {
//         // 🚫 Discard the screenshot (not saved)
//         previewPanel.SetActive(false);
//         Debug.Log("❌ Screenshot was not saved.");
//     }
// }
