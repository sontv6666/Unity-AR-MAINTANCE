using UnityEngine;
using UnityEngine.Networking;
using System.Collections;
using System.Collections.Generic;
using TMPro;
using Newtonsoft.Json.Linq;
using UnityEngine.UI;

public class APIRealTime : MonoBehaviour
{
    public Transform dataLayoutGroup;
    public GameObject dataRowPrefab;
    public GameObject categoryHeaderPrefab;
    private string previousJson = "";

    void Start()
    {
        InvokeRepeating(nameof(FetchAPIData), 0f, 3f);
    }

    void FetchAPIData()
    {
        StartCoroutine(GetDataFromAPI());
    }

    IEnumerator GetDataFromAPI()
    {
        string url = "https://demoapicallgetlaptopinfo.onrender.com/cpu";
        UnityWebRequest request = UnityWebRequest.Get(url);
        request.SetRequestHeader("API_KEY", "my_secure_token_123");

        yield return request.SendWebRequest();

        if (request.result == UnityWebRequest.Result.Success)
        {
            string jsonResponse = request.downloadHandler.text;

            if (jsonResponse != previousJson) // Chỉ cập nhật nếu dữ liệu thay đổi
            {
                JObject parsedData = JObject.Parse(jsonResponse);
                DisplayData(parsedData);
                previousJson = jsonResponse;
            }
        }
        else
        {
            Debug.LogError("API Error: " + request.error);
        }
    }

    void DisplayData(JObject data)
    {
        // Xóa dữ liệu cũ trước khi cập nhật
        foreach (Transform child in dataLayoutGroup)
        {
            Destroy(child.gameObject);
        }

        foreach (var category in data)
        {
            string categoryName = category.Key;
            JToken categoryValues = category.Value;

            if (categoryValues.Type == JTokenType.Object) // 🔹 Nếu là nhóm dữ liệu
            {
                AddCategoryHeader(categoryName);
                foreach (var item in categoryValues.Children<JProperty>())
                {
                    AddDataRow(item.Name, FormatValue(item.Value));
                }
            }
            else
            {
                AddDataRow(categoryName, FormatValue(categoryValues));
            }
        }
    }

    // 🔹 Chuẩn hóa giá trị hiển thị
    string FormatValue(JToken value)
    {
        if (value.Type == JTokenType.Null || (value.Type == JTokenType.String && string.IsNullOrWhiteSpace(value.ToString())))
        {
            return "Unknown"; // Nếu rỗng, hiển thị "Unknown"
        }

        if (value.Type == JTokenType.Float || value.Type == JTokenType.Integer)
        {
            return $"{value:F2}"; // Format số thực (2 chữ số thập phân)
        }

        return value.ToString(); // Các kiểu khác giữ nguyên
    }

    // 🔹 Thêm hàng key-value
    void AddDataRow(string key, string value)
    {
        GameObject newRow = Instantiate(dataRowPrefab, dataLayoutGroup);
        TMP_Text keyText = newRow.transform.Find("KeyText").GetComponent<TMP_Text>();
        TMP_Text valueText = newRow.transform.Find("ValueText").GetComponent<TMP_Text>();

        keyText.text = key + ":";
        keyText.fontStyle = FontStyles.Bold;
        keyText.alignment = TextAlignmentOptions.Right;
        keyText.fontSize = 28;

        valueText.text = value;
        valueText.fontStyle = FontStyles.Normal;
        valueText.alignment = TextAlignmentOptions.Left;
        valueText.fontSize = 28;

        // 🎨 Đổi màu chữ nếu giá trị là phần trăm
        if (key.ToLower().Contains("percent") || key.ToLower().Contains("usage"))
        {
            if (float.TryParse(value.Replace("%", ""), out float percentage))
            {
                valueText.color = percentage > 80 ? Color.red :
                                  percentage > 50 ? new Color(1f, 0.5f, 0f) :
                                  Color.green;
            }
        }

        // 🎬 Hiệu ứng Fade-in
        CanvasGroup canvasGroup = newRow.AddComponent<CanvasGroup>();
        canvasGroup.alpha = 0;
        StartCoroutine(FadeIn(canvasGroup, 0.5f));
    }

    // 🔹 Thêm tiêu đề nhóm
    void AddCategoryHeader(string title)
    {
        GameObject newHeader = Instantiate(categoryHeaderPrefab, dataLayoutGroup);
        TMP_Text headerText = newHeader.GetComponentInChildren<TMP_Text>();

        headerText.text = title.ToUpper(); // Viết hoa tiêu đề
        headerText.fontSize = 34;
        headerText.fontStyle = FontStyles.Bold;
        headerText.alignment = TextAlignmentOptions.Center;
    }

    // 🎬 Hiệu ứng Fade-in
    IEnumerator FadeIn(CanvasGroup canvasGroup, float duration)
    {
        float time = 0;
        while (time < duration)
        {
            time += Time.deltaTime;
            canvasGroup.alpha = Mathf.Lerp(0, 1, time / duration);
            yield return null;
        }
        canvasGroup.alpha = 1;
    }
}
