using UnityEngine;
using UnityEngine.UI;
using TMPro;
using System.IO;
using System.Collections.Generic;

public class DownloadManager : MonoBehaviour
{
    public GameObject filePrefab; // Prefab with filename + size + delete button
    public Transform fileListContainer; // UI layout for file list
    public TMP_Text totalSizeText; // Text to show total size
    public Button deleteAllButton; // Button to delete all files

    
    public GameObject confirmationPanel; // Confirmation panel UI
    public TMP_Text confirmationMessage; // Message text in confirmation panel
    public Button confirmYesButton; // Yes button
    public Button confirmNoButton; // No button

    private string fileToDelete; // Store the file to be deleted
    private bool deleteAll; // Track whether to delete all files
    
    public GameObject profilePage;
    public GameObject downloadManagerPage;

    private List<string> downloadedFiles = new List<string>();

    private void Start()
    {
        LoadDownloadedFiles();
    }

    public void OpenDownloadManager()
    {
        Debug.Log("📂 Opening Download Manager...");
        
        profilePage.SetActive(false);
        downloadManagerPage.SetActive(true);
        LoadDownloadedFiles();
    }

    public void GoBackToProfile()
    {
        Debug.Log("🔙 Returning to Profile Page...");
        
        downloadManagerPage.SetActive(false);
        profilePage.SetActive(true);
    }

    private void LoadDownloadedFiles()
    {
        // Clear previous list
        foreach (Transform child in fileListContainer)
        {
            Destroy(child.gameObject);
        }
        downloadedFiles.Clear();

        // Get all files
        string path = Application.persistentDataPath;
        if (!Directory.Exists(path)) return;

        string[] files = Directory.GetFiles(path);
        long totalSize = 0;

        foreach (string file in files)
        {
            downloadedFiles.Add(file);
            CreateFileUI(file);
            totalSize += new FileInfo(file).Length; // Add file size
        }

        // Update total download size
        totalSizeText.text = $"Total Size {FormatFileSize(totalSize)}";

        // Show or hide "Delete All" button
        deleteAllButton.gameObject.SetActive(downloadedFiles.Count > 0);
    }

    private void CreateFileUI(string filePath)
    {
        GameObject fileEntry = Instantiate(filePrefab, fileListContainer);
        fileEntry.transform.localScale = Vector3.one;

        TMP_Text fileNameText = fileEntry.transform.Find("FileNameText")?.GetComponent<TMP_Text>();
        TMP_Text fileSizeText = fileEntry.transform.Find("FileSizeText")?.GetComponent<TMP_Text>(); // New for size
        Button deleteButton = fileEntry.transform.Find("DeleteButton")?.GetComponent<Button>();

        string fileName = Path.GetFileName(filePath);
        long fileSize = new FileInfo(filePath).Length;
        string formattedSize = FormatFileSize(fileSize);

        if (fileNameText != null) fileNameText.text = fileName;
        if (fileSizeText != null) fileSizeText.text = formattedSize; // Show size

        if (deleteButton != null)
        {
            deleteButton.onClick.AddListener(() => ShowDeleteConfirmation(filePath, fileEntry));
        }
    }


    private string FormatFileSize(long bytes)
    {
        if (bytes < 1024) return $"{bytes} B";
        if (bytes < 1024 * 1024) return $"{(bytes / 1024f):F2} KB";
        if (bytes < 1024 * 1024 * 1024) return $"{(bytes / (1024f * 1024f)):F2} MB";
        return $"{(bytes / (1024f * 1024f * 1024f)):F2} GB";
    }
    
    private void ShowDeleteConfirmation(string filePath, GameObject fileEntry)
    {
        confirmationPanel.SetActive(true);
        confirmationMessage.text = $"Are you sure to delete this?</b>?";

        fileToDelete = filePath;
        deleteAll = false; // Single file delete

        confirmYesButton.onClick.RemoveAllListeners();
        confirmYesButton.onClick.AddListener(() => DeleteFile(fileEntry));
        confirmNoButton.onClick.RemoveAllListeners();
        confirmNoButton.onClick.AddListener(() => confirmationPanel.SetActive(false));
    }

    private void ShowDeleteAllConfirmation()
    {
        confirmationPanel.SetActive(true);
        confirmationMessage.text = "Are you sure you want to delete <b>ALL</b> downloaded files?";

        deleteAll = true; // Delete all files

        confirmYesButton.onClick.RemoveAllListeners();
        confirmYesButton.onClick.AddListener(DeleteAllFilesConfirmed);
        confirmNoButton.onClick.RemoveAllListeners();
        confirmNoButton.onClick.AddListener(() => confirmationPanel.SetActive(false));
    }

    private void DeleteFile(GameObject fileEntry)
    {
        if (File.Exists(fileToDelete))
        {
            File.Delete(fileToDelete);
            Destroy(fileEntry);
            downloadedFiles.Remove(fileToDelete);
            Debug.Log($"🗑️ Deleted: {fileToDelete}");

            // Refresh the total download size
            LoadDownloadedFiles();
        }

        confirmationPanel.SetActive(false);
    }

    public void DeleteAllFiles()
    {
        ShowDeleteAllConfirmation();
    }

    private void DeleteAllFilesConfirmed()
    {
        foreach (string filePath in downloadedFiles)
        {
            if (File.Exists(filePath))
            {
                File.Delete(filePath);
                Debug.Log($"🗑️ Deleted: {filePath}");
            }
        }
        
        // Clear UI
        foreach (Transform child in fileListContainer)
        {
            Destroy(child.gameObject);
        }

        downloadedFiles.Clear();
        deleteAllButton.gameObject.SetActive(false);
        totalSizeText.text = "Total Size 0 B"; // Reset size
        Debug.Log("🧹 All files deleted!");

        confirmationPanel.SetActive(false);
    }
}
