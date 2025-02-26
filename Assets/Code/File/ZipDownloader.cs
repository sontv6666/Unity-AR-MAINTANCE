using UnityEngine;
using UnityEngine.Networking;
using System.IO;
using System.IO.Compression;
using System.Threading.Tasks;

public class ZipDownloader : MonoBehaviour
{
    private string apiUrl = "https://joey-lenient-ostrich.ngrok-free.app/api/v1/files/db5bbeaf-75d0-46ef-9e98-41cbfb159573.zip";
    private string localPath;
    private string vuforiaPath;

    void Start()
    {
        localPath = Path.Combine(Application.persistentDataPath, "DownloadedFiles");
        vuforiaPath = Path.Combine(Application.streamingAssetsPath, "Vuforia");

        Directory.CreateDirectory(localPath);
        Directory.CreateDirectory(vuforiaPath);

        Debug.Log($"Download folder: {localPath}");
        Debug.Log($"Vuforia Model Target Path: {vuforiaPath}");

        DownloadAndExtractZip(apiUrl, localPath);
    }

    async void DownloadAndExtractZip(string url, string outputPath)
    {
        string zipFilePath = Path.Combine(outputPath, "model.zip");

        Debug.Log($"Downloading from: {url}");
        Debug.Log($"ZIP will be saved to: {zipFilePath}");

        using (UnityWebRequest request = UnityWebRequest.Get(url))
        {
            request.downloadHandler = new DownloadHandlerFile(zipFilePath);
            var operation = request.SendWebRequest();

            while (!operation.isDone)
            {
                Debug.Log($"Download Progress: {request.downloadProgress * 100}%");
                await Task.Yield();
            }

            if (request.result != UnityWebRequest.Result.Success)
            {
                Debug.LogError($"Error downloading file: {request.error}");
                return;
            }

            Debug.Log("Download complete.");
        }

        // Extract ZIP
        try
        {
            Debug.Log("Extracting ZIP...");
            ZipFile.ExtractToDirectory(zipFilePath, outputPath, true);
            Debug.Log($"Extraction complete. Files extracted to: {outputPath}");
        }
        catch (System.Exception ex)
        {
            Debug.LogError($"Extraction failed: {ex.Message}");
        }

        // Move extracted Vuforia model files to the correct folder
        MoveModelFiles(outputPath, vuforiaPath);

        // Delete ZIP file
        if (File.Exists(zipFilePath))
        {
            File.Delete(zipFilePath);
            Debug.Log("Deleted ZIP file after extraction.");
        }
    }

    void MoveModelFiles(string sourcePath, string destinationPath)
    {
        
        Debug.Log("Checking extracted files in: " + sourcePath);
        string[] extractedFiles = Directory.GetFiles(sourcePath);
        foreach (var file in extractedFiles)
        {
            Debug.Log("Extracted file: " + file);
        }

        string[] filesToMove = { "MTDataset.dat", "MTDataset.xml" };

        foreach (string fileName in filesToMove)
        {
            string sourceFile = Path.Combine(sourcePath, fileName);
            string destinationFile = Path.Combine(destinationPath, fileName);

            Debug.Log($"Looking for {sourceFile}");

            if (File.Exists(sourceFile))
            {
                File.Copy(sourceFile, destinationFile, true);
                Debug.Log($"✅ Moved {fileName} to {destinationPath}");
            }
            else
            {
                Debug.LogWarning($"❌ {fileName} not found in extracted files!");
            }
        }

        Debug.Log("✅ Vuforia Model Target files updated.");
    }


    
}
