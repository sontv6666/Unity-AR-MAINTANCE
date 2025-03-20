using UnityEngine;
using UnityEngine.Networking;
using System.Collections;
using System.Collections.Generic;
using TMPro;
using Newtonsoft.Json;
using UnityEngine.UI;

public class APIRealTime : MonoBehaviour
{
    public Transform dataLayoutGroup;
    public GameObject dataRowPrefab;
    private Dictionary<string, string> previousData = new Dictionary<string, string>();

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
        string url = "https://demoapicallgetlaptopinfo.onrender.com/ram";
        UnityWebRequest request = UnityWebRequest.Get(url);
        
        request.SetRequestHeader("API_KEY", "my_secure_token_123");

        yield return request.SendWebRequest();

        if (request.result == UnityWebRequest.Result.Success)
        {
            Dictionary<string, string> newData = JsonConvert.DeserializeObject<Dictionary<string, string>>(request.downloadHandler.text);
            
            if (!IsDataSame(previousData, newData))
            {
                DisplayData(newData);
                previousData = new Dictionary<string, string>(newData);
            }
        }
        else
        {
            Debug.LogError("API Error: " + request.error);
        }
    }

    void DisplayData(Dictionary<string, string> data)
    {
        // Xóa dữ liệu cũ trước khi cập nhật
        foreach (Transform child in dataLayoutGroup)
        {
            Destroy(child.gameObject);
        }

        foreach (var entry in data)
        {
            GameObject newRow = Instantiate(dataRowPrefab, dataLayoutGroup);

            TMP_Text keyText = newRow.transform.Find("KeyText").GetComponent<TMP_Text>();
            TMP_Text valueText = newRow.transform.Find("ValueText").GetComponent<TMP_Text>();

            // 🔹 Kiểm tra xem có Image không trước khi truy cập
            Image background = newRow.GetComponent<Image>();
            if (background == null)
            {
                background = newRow.AddComponent<Image>(); // Nếu không có, thêm mới
            }

            // 🔥 Thiết lập UI đẹp hơn
            keyText.text = entry.Key + ":";
            keyText.fontStyle = FontStyles.Bold;
            keyText.alignment = TextAlignmentOptions.Right;
            keyText.fontSize = 30;

            valueText.text = entry.Value;
            valueText.fontStyle = FontStyles.Normal;
            valueText.alignment = TextAlignmentOptions.Left;
            valueText.fontSize = 30;

            // 🎨 Đổi màu valueText theo giá trị
            if (entry.Key.ToLower().Contains("percent") || entry.Key.ToLower().Contains("usage"))
            {
                float value;
                if (float.TryParse(entry.Value.Replace("%", ""), out value))
                {
                    if (value > 80) valueText.color = Color.red; // 🔴 Cảnh báo
                    else if (value > 50) valueText.color = new Color(1f, 0.5f, 0f); // 🟠 Màu cam
                    else valueText.color = Color.green; // 🟢 Bình thường
                }
            }

            // 🎨 Bo góc và đổi màu nền
            background.color = new Color(1f, 1f, 1f, 0.85f); // Màu nền trắng nhạt
            background.GetComponent<RectTransform>().sizeDelta = new Vector2(500, 70);
            background.maskable = true;

            // 🎬 Hiệu ứng Fade-in
            CanvasGroup canvasGroup = newRow.AddComponent<CanvasGroup>();
            canvasGroup.alpha = 0;
            StartCoroutine(FadeIn(canvasGroup, 0.5f));
        }
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

    bool IsDataSame(Dictionary<string, string> oldData, Dictionary<string, string> newData)
    {
        if (oldData.Count != newData.Count) return false;
        foreach (var key in newData.Keys)
        {
            if (!oldData.ContainsKey(key) || oldData[key] != newData[key])
                return false;
        }
        return true;
    }
}
