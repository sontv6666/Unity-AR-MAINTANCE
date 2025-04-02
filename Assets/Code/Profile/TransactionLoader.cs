using UnityEngine;
using UnityEngine.Networking;
using UnityEngine.UI;
using TMPro;
using System.Collections;
using System.Collections.Generic;
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

    public Button backButton;

    [Header("API Settings")]
    private string getTransactionEndpoint = "/wallets/history/user/"; // API Endpoint
    void Start()
    {
        LoadTransactions();
        backButton.onClick.AddListener(GoBack);
    }

    public void OpenTransactionListFromProfile()
    {
        LoadTransactions();
        Debug.Log("📌 Opening Transaction List from Profile...");
        previousPage = profilePage; // Remember previous page
        OpenTransactionListPage();
    }

    private void OpenTransactionListPage()
    {
        LoadTransactions();
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

    private IEnumerator FetchTransactions(string userId, string token)
    {
        string endpoint = $"/wallets/history/user/{userId}";
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
                WalletResponse response = JsonConvert.DeserializeObject<WalletResponse>(responseText);
                if (response != null && response.code == 1000)
                {
                    DisplayTransactions(response.result);
                }
            }
            catch (System.Exception e)
            {
                Debug.LogError($"❌ JSON Parsing Error: {e.Message}");
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

            serviceName.text = transaction.guidelineName;
            createdDate.text = $"Date: {FormatDate(transaction.createdDate)}";
            amount.text = $"Usage Points: {transaction.amount}";
            balance.text = $"Remain Points: {transaction.balance}";
            
        }

        // Show empty state if there are no transactions
        ShowEmptyState(transactions.Count == 0);
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
        transactionLayoutGroup.gameObject.SetActive(!show);
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
