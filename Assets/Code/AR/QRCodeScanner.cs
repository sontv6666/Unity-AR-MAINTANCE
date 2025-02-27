using UnityEngine;
using UnityEngine.XR.ARFoundation;
using UnityEngine.XR.ARSubsystems;
using ZXing;
using TMPro;
using UnityEngine.Networking;
using System.Collections;

public class ARQRCodeScanner : MonoBehaviour
{
    public ARCameraManager arCameraManager;  
    public TMP_Text uiText; 
    private bool isScanning = true;

    void Update()
    {
        if (isScanning)
        {
            TryScanQRCode();
        }
    }

    void TryScanQRCode()
    {
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
            if (result != null)
            {
                isScanning = false;
                Debug.Log("QR Code Scanned: " + result.Text);
                UpdateUIText("Scanning...");

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
            UpdateUIText("Scan failed. Try again.");
            Invoke(nameof(ResetScanning), 2f);
        }
    }

    void ProcessResponse(string jsonResponse, string scannedQR)
    {
        var response = JsonUtility.FromJson<ApiResponse>(jsonResponse);
        if (response.code == 1000 && response.result.courseCode == scannedQR)
        {
            UpdateUIText("QR Validated! Loading UI...");
            LoadUI(response.result);
        }
        else
        {
            UpdateUIText("Invalid QR Code.");
            Invoke(nameof(ResetScanning), 2f);
        }
    }

    void LoadUI(CourseResult course)
    {
        Debug.Log("Loading UI for: " + course.title);
        // TODO: Hiển thị giao diện hướng dẫn từ course
    }

    void UpdateUIText(string text)
    {
        if (uiText != null)
        {
            uiText.text = text;
        }
    }

    void ResetScanning()
    {
        isScanning = true;
        UpdateUIText("Scan a QR Code");
    }
}

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
