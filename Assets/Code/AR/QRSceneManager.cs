using UnityEngine;
using UnityEngine.XR.ARFoundation;
using UnityEngine.XR.ARSubsystems;
using ZXing;
using TMPro;
using System.Collections;

public class QRSceneManager : MonoBehaviour
{
    public ARCameraManager arCameraManager;
    public GameObject scanBoxUI;
    public TMP_Text statusText;
    public GameObject loadingPanel;

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
        if (!arCameraManager.TryAcquireLatestCpuImage(out XRCpuImage image)) return;

        var conversionParams = new XRCpuImage.ConversionParams
        {
            inputRect = new RectInt(0, 0, image.width, image.height),
            outputDimensions = new Vector2Int(image.width, image.height),
            outputFormat = TextureFormat.RGBA32,
            transformation = XRCpuImage.Transformation.None
        };

        Texture2D texture = new Texture2D(image.width, image.height, TextureFormat.RGBA32, false);
        image.Convert(conversionParams, texture.GetRawTextureData<byte>());
        image.Dispose();
        texture.Apply();

        var barcodeReader = new BarcodeReader();
        var result = barcodeReader.Decode(texture.GetPixels32(), texture.width, texture.height);
        Destroy(texture);

        if (result != null)
        {
            Debug.Log($"✅ QR Code Scanned: {result.Text}");
            isScanning = false;

            // Assume format is "MACHINECODE@COURSEID"
            string[] parts = result.Text.Split('@');
            if (parts.Length != 2)
            {
                statusText.text = "❌ Invalid QR Format!";
                Invoke(nameof(ResetScanning), 2f);
                return;
            }

            string machineCode = parts[0].Trim();
            string courseId = parts[1].Trim();

            statusText.text = "✅ QR Scanned. Loading...";
            loadingPanel.SetActive(true);

            // 👉 Use APIRealTime system
            APIRealTime.Instance.FetchMachineData(machineCode, parts[1], courseId);
        }
    }

    void ResetScanning()
    {
        isScanning = true;
        statusText.text = "📷 Scan a QR Code...";
    }
}
