using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class ScaleQuizResult : MonoBehaviour
{
    public RectTransform groupPercent;
    public RectTransform groupStatus;
    public RectTransform groupCorrect;
    public RectTransform quizResultButton;
    private bool isTablet;

    // Start is called before the first frame update
    void Start()
    {
        float aspectRatio = (float)Screen.width / Screen.height;
        isTablet = aspectRatio >= 0.65f && aspectRatio <= 0.85f;

        if (isTablet)
        {
            if (groupPercent != null && groupStatus != null && groupCorrect != null && quizResultButton != null)
            {
                AdjustQuizResultButton();
                AdjustGroupPercent();
                AdjustGroupStatus();
                AdjustGroupCorrect();
            }
        }
    }

    void Update()
    {
        float aspectRatio = (float)Screen.width / Screen.height;
        bool currentIsTablet = aspectRatio >= 0.65f && aspectRatio <= 0.85f;

        if (currentIsTablet != isTablet)
        {
            isTablet = currentIsTablet;

            if (isTablet)
            {
                if (groupPercent != null && groupStatus != null && groupCorrect != null && quizResultButton != null)
                {
                    AdjustQuizResultButton();
                    AdjustGroupPercent();
                    AdjustGroupStatus();
                    AdjustGroupCorrect();
                }
            }
            else
            {
                ResetAdjustments();
            }
        }
    }

    void AdjustQuizResultButton()
    {
        // Xóa VerticalLayoutGroup nếu có
        if (quizResultButton.TryGetComponent<VerticalLayoutGroup>(out var verticalLayoutGroup))
        {
            DestroyImmediate(verticalLayoutGroup);
        }

        // Thêm HorizontalLayoutGroup nếu chưa có
        if (!quizResultButton.TryGetComponent<HorizontalLayoutGroup>(out var layoutGroup))
        {
            layoutGroup = quizResultButton.gameObject.AddComponent<HorizontalLayoutGroup>();
        }

        // Thiết lập kích thước và khoảng cách
        quizResultButton.sizeDelta = new Vector2(500, 65);
        layoutGroup.spacing = 25;

        // Điều chỉnh vị trí của quizResultButton
        quizResultButton.anchoredPosition = new Vector2(
            quizResultButton.anchoredPosition.x,
            quizResultButton.anchoredPosition.y + 190
        );
    }

    void AdjustGroupPercent()
    {
        // Điều chỉnh scale của groupPercent
        groupPercent.localScale = new Vector3(0.7f, 0.7f, 1f);
    }

    void AdjustGroupStatus()
    {
        // Điều chỉnh vị trí của groupStatus
        groupStatus.anchoredPosition = new Vector2(
            groupStatus.anchoredPosition.x,
            groupStatus.anchoredPosition.y + 150
        );
    }

    void AdjustGroupCorrect()
    {
        // Điều chỉnh vị trí của groupCorrect
        groupCorrect.anchoredPosition = new Vector2(
            groupCorrect.anchoredPosition.x,
            groupCorrect.anchoredPosition.y + 160
        );
    }

    void ResetAdjustments()
    {
        // Đặt lại các điều chỉnh khi không phải là tablet
        groupPercent.localScale = Vector3.one;
        groupStatus.anchoredPosition = new Vector2(
            groupStatus.anchoredPosition.x,
            groupStatus.anchoredPosition.y - 150
        );
        groupCorrect.anchoredPosition = new Vector2(
            groupCorrect.anchoredPosition.x,
            groupCorrect.anchoredPosition.y - 160
        );
        // Xóa HorizontalLayoutGroup nếu có
        if (quizResultButton.TryGetComponent<HorizontalLayoutGroup>(out var layoutGroup))
        {
            DestroyImmediate(layoutGroup);
        }

        // Thêm lại VerticalLayoutGroup nếu chưa có
        if (!quizResultButton.TryGetComponent<VerticalLayoutGroup>(out var verticalLayoutGroup))
        {
            verticalLayoutGroup = quizResultButton.gameObject.AddComponent<VerticalLayoutGroup>();
        }

        // Thiết lập spacing và alignment
        verticalLayoutGroup.spacing = 10;
        verticalLayoutGroup.childAlignment = TextAnchor.UpperCenter;

        // Đặt lại kích thước ban đầu của quizResultButton
        quizResultButton.sizeDelta = new Vector2(371, 134); // Giả sử kích thước ban đầu là 371x134

        // Đặt lại vị trí của quizResultButton
        quizResultButton.anchoredPosition = new Vector2(
            quizResultButton.anchoredPosition.x,
            quizResultButton.anchoredPosition.y - 190
        );
    }
}