using UnityEngine;
using UnityEngine.Networking;
using UnityEngine.UI;
using TMPro;
using System.Collections;
using System.Collections.Generic;
using Models;
using Newtonsoft.Json;

public class TransactionLoader : MonoBehaviour
{
    [Header("UI References")]
    public GameObject transactionPrefab; // Prefab for transaction items
    public Transform transactionLayoutGroup; // Layout for transactions

    public GameObject transactionListPage; // Transaction history page
    public GameObject profilePage; // Profile page

    private GameObject previousPage;

    [Header("Empty State")]
    public GameObject emptyStatePanel; // Empty state when no transactions exist

    [Header("Pagination")]
    public Button nextPageButton;
    public Button prevPageButton;
    public TMP_Text pageIndicatorText;
    public int currentPage = 1;
    public int pageSize = 10;
    public int totalPages = 1;

    public Button backButton;

    [Header("API Settings")]
    private string getTransactionEndpoint = "/wallets/history/user/"; // API Endpoint
    
    void Start()
    {
        // Set up button listeners
        backButton.onClick.AddListener(GoBack);
        
        // Set up pagination buttons
        if (nextPageButton != null)
            nextPageButton.onClick.AddListener(NextPage);
        
        if (prevPageButton != null)
            prevPageButton.onClick.AddListener(PreviousPage);
            
        // Initial load
        LoadTransactions();
    }

    public void OpenTransactionListFromProfile()
    {
        currentPage = 1; // Reset to first page when opening
        OpenTransactionListPage();
        LoadTransactions();
        Debug.Log("📌 Opening Transaction List from Profile...");
        previousPage = profilePage; // Remember previous page
      
    }

    private void OpenTransactionListPage()
    {
        if (transactionListPage != null)
        {
            transactionListPage.SetActive(true);
            profilePage.SetActive(false);
        }
        else
        {
            Debug.LogWarning("⚠️ transactionListPage is not assigned!");
        }
    }

    public void GoBack()
    {
        Debug.Log("🔙 Returning to Previous Page...");

        if (transactionListPage != null)
        {
            transactionListPage.SetActive(false);
        }

        if (previousPage != null)
        {
            previousPage.SetActive(true);
        }
        else
        {
            Debug.LogWarning("⚠️ No previous page! Defaulting to Profile Page.");
            if (profilePage != null)
            {
                profilePage.SetActive(true);
            }
        }
    }

    public void LoadTransactions()
    {
        string userId = PlayerPrefs.GetString("UserId", "");
        string token = PlayerPrefs.GetString("AuthToken", "");

        if (string.IsNullOrEmpty(userId) || string.IsNullOrEmpty(token))
        {
            Debug.LogError("⚠️ Missing User ID or authentication token.");
            return;
        }

        StartCoroutine(FetchTransactions(userId, token));
    }

    // Pagination methods
    public void NextPage()
    {
        if (currentPage < totalPages)
        {
            currentPage++;
            LoadTransactions();
        }
    }

    public void PreviousPage()
    {
        if (currentPage > 1)
        {
            currentPage--;
            LoadTransactions();
        }
    }

    private void UpdatePaginationUI()
    {
        // Update page indicator text
        if (pageIndicatorText != null)
        {
            pageIndicatorText.text = $"Page {currentPage} of {totalPages}";
        }

        // Enable/disable pagination buttons based on current page
        if (prevPageButton != null)
        {
            prevPageButton.interactable = (currentPage > 1);
        }

        if (nextPageButton != null)
        {
            nextPageButton.interactable = (currentPage < totalPages);
        }
    }

    private IEnumerator FetchTransactions(string userId, string token)
    {
        Debug.Log("🔄 Fetching transactions for User ID: " + userId);
        string endpoint = $"/wallets/history/user/{userId}?page={currentPage}&size={pageSize}";
        UnityWebRequest request = ApiConfig.CreateRequest(endpoint, "GET");

        yield return request.SendWebRequest();

        if (request.result != UnityWebRequest.Result.Success)
        {
            Debug.LogError($"❌ API Error: {request.error}");
        }
        else
        {
            string responseText = request.downloadHandler.text;
            Debug.Log($"✅ API Response: {responseText}");

            try
            {
                // Use the generic ApiResponse with PaginationResult for properly parsing
                ApiResponse<PaginationResult<WalletTransaction>> response = 
                    JsonConvert.DeserializeObject<ApiResponse<PaginationResult<WalletTransaction>>>(responseText);
                    
                if (response != null && response.code == 1000 && response.result != null)
                {
                    // Update pagination info
                    totalPages = response.result.totalPages;
                    currentPage = response.result.page;
                    
                    // Update pagination UI
                    UpdatePaginationUI();
                    
                    // Display transactions from objectList
                    DisplayTransactions(response.result.objectList);
                }
                else
                {
                    Debug.LogError("❌ API Response format error or empty result");
                    ShowEmptyState(true);
                }
            }
            catch (System.Exception e)
            {
                Debug.LogError($"❌ JSON Parsing Error: {e.Message}");
                ShowEmptyState(true);
            }
        }
    }

    private void DisplayTransactions(List<WalletTransaction> transactions)
    {
        // Clear old transactions before loading new ones
        foreach (Transform child in transactionLayoutGroup)
        {
            Destroy(child.gameObject);
        }

        Debug.Log($"🔄 Displaying {transactions.Count} transactions...");

        // Show empty state if there are no transactions
        if (transactions == null || transactions.Count == 0)
        {
            ShowEmptyState(true);
            return;
        }

        ShowEmptyState(false);

        foreach (var transaction in transactions)
        {
            GameObject newTransaction = Instantiate(transactionPrefab, transactionLayoutGroup);
            newTransaction.transform.localScale = Vector3.one; // Ensure proper scaling

            TMP_Text serviceName = newTransaction.transform.Find("guidelineName")?.GetComponent<TMP_Text>();
            TMP_Text createdDate = newTransaction.transform.Find("createdDate")?.GetComponent<TMP_Text>();
            TMP_Text amount = newTransaction.transform.Find("usageAmount")?.GetComponent<TMP_Text>();
            TMP_Text balance = newTransaction.transform.Find("remainAmount")?.GetComponent<TMP_Text>();
           
            if (serviceName == null || createdDate == null || amount == null || balance == null)
            {
                Debug.LogError("⚠️ Missing UI Elements! Check prefab structure.");
                continue;
            }

            // For credit transactions, use serviceName instead of guidelineName
            if (transaction.type == "CREDIT")
            {
                serviceName.text = transaction.serviceName != null ? transaction.serviceName : "Points Added";
                amount.text = $"+{transaction.amount}";
                amount.color = Color.green;  // Green for credit/income
            }
            else // DEBIT
            {
                serviceName.text = transaction.guidelineName;
                amount.text = $"-{transaction.amount}";  
                amount.color = Color.red;  // Red for debit/usage
            }
            
            createdDate.text = $"Date: {FormatDate(transaction.createdDate)}";
            balance.text = $"Points: {transaction.balance}";
        }
    }

    private string FormatDate(string dateTime)
    {
        if (System.DateTime.TryParse(dateTime, out System.DateTime parsedDate))
        {
            return parsedDate.ToString("yyyy-MM-dd HH:mm");
        }
        return dateTime; // Return original if parsing fails
    }
    
    private void ShowEmptyState(bool show)
    {
        if (emptyStatePanel)
        {
            emptyStatePanel.SetActive(show);
        }
        
        if (transactionLayoutGroup != null && transactionLayoutGroup.gameObject != null)
        {
            transactionLayoutGroup.gameObject.SetActive(!show);
        }
        
        // Hide pagination if there are no transactions
        if (nextPageButton != null && nextPageButton.gameObject != null)
        {
            nextPageButton.gameObject.SetActive(!show);
        }
        
        if (prevPageButton != null && prevPageButton.gameObject != null)
        {
            prevPageButton.gameObject.SetActive(!show);
        }
        
        if (pageIndicatorText != null && pageIndicatorText.gameObject != null)
        {
            pageIndicatorText.gameObject.SetActive(!show);
        }
    }
}

[System.Serializable]
public class WalletResponse
{
    public int code;
    public string message;
    public List<WalletTransaction> result;
}

[System.Serializable]
public class WalletTransaction
{
    public string id;
    public string type;
    public string serviceName;
    public string guidelineName;
    public int amount;
    public int balance;
    public string createdDate;
}