using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Scale : MonoBehaviour
{
    public RectTransform loginScreen;
    public RectTransform scrollView;
    public RectTransform quizScreen1;
    public RectTransform quizScreen2;
    private float firstHeight = 2532f / 1170f;
    private float changeHeight;
    public float heightScaleDeviated;
    private Vector2 lastScreenSize;
    private bool isTablet;

    // Lưu trữ kích thước và vị trí ban đầu
    private Vector2 loginScreenOriginalPosition;
    private Vector2 scrollViewOriginalSize;
    private Vector2 quizScreen1OriginalPosition;
    private Vector2 quizScreen2OriginalPosition;

    // Start is called before the first frame update
    void Start()
    {
        changeHeight = (float)Screen.height / Screen.width;
        lastScreenSize = new Vector2(Screen.width, Screen.height);

        // Lưu trữ kích thước và vị trí ban đầu
        if (loginScreen != null)
        {
            loginScreenOriginalPosition = loginScreen.anchoredPosition;
            LoginFormResize();
        }

        if (scrollView != null)
        {
            scrollViewOriginalSize = scrollView.sizeDelta;
            ScaleScrollView();
        }

        if (quizScreen1 != null && quizScreen2 != null)
        {
            quizScreen1OriginalPosition = quizScreen1.anchoredPosition;
            quizScreen2OriginalPosition = quizScreen2.anchoredPosition;
            ScaleQuizScreen();
        }
    }

    // Update is called once per frame
    void Update()
    {
        if (Screen.width != lastScreenSize.x || Screen.height != lastScreenSize.y)
        {
            changeHeight = (float)Screen.height / Screen.width;
            lastScreenSize = new Vector2(Screen.width, Screen.height);

            if (loginScreen != null)
            {
                LoginFormResize();
            }

            if (scrollView != null)
            {
                ScaleScrollView();
            }

            if (quizScreen1 != null && quizScreen2 != null)
            {
                ScaleQuizScreen();
            }
        }
    }

    void LoginFormResize()
    {
        float screenHeight = Screen.height;
        float aspectRatio = (float)Screen.width / Screen.height;
        bool currentIsTablet = aspectRatio >= 0.65f && aspectRatio <= 0.85f;

        if (currentIsTablet != isTablet)
        {
            isTablet = currentIsTablet;

            if (isTablet)
            {
                loginScreen.anchoredPosition = new Vector2(
                    loginScreen.anchoredPosition.x,
                    400
                );
            }
            else
            {
                // Đặt lại vị trí ban đầu khi không phải tablet
                loginScreen.anchoredPosition = loginScreenOriginalPosition;
            }
        }
    }

    void ScaleScrollView()
    {
        float aspectRatio = (float)Screen.width / Screen.height;
        bool currentIsTablet = aspectRatio >= 0.65f && aspectRatio <= 0.85f;

        if (currentIsTablet != isTablet)
        {
            isTablet = currentIsTablet;

            if (isTablet)
            {
                float scaleFactor = changeHeight / firstHeight;
                scrollView.sizeDelta = new Vector2(
                    scrollView.sizeDelta.x,
                    (scrollView.sizeDelta.y * scaleFactor) - heightScaleDeviated
                );

                scrollView.anchoredPosition = new Vector2(
                    scrollView.anchoredPosition.x,
                    0
                );
            }
            else
            {
                // Đặt lại kích thước ban đầu khi không phải tablet
                scrollView.sizeDelta = scrollViewOriginalSize;
            }
        }
    }

    void ScaleQuizScreen()
    {
        float aspectRatio = (float)Screen.width / Screen.height;
        bool currentIsTablet = aspectRatio >= 0.65f && aspectRatio <= 0.85f;

        if (currentIsTablet != isTablet)
        {
            isTablet = currentIsTablet;

            if (isTablet)
            {
                quizScreen1.anchoredPosition = new Vector2(
                    quizScreen1.anchoredPosition.x,
                    quizScreen1.anchoredPosition.y + 200
                );
                quizScreen2.anchoredPosition = new Vector2(
                    quizScreen2.anchoredPosition.x,
                    quizScreen2.anchoredPosition.y + 375
                );
            }
            else
            {
                // Đặt lại vị trí ban đầu khi không phải tablet
                quizScreen1.anchoredPosition = quizScreen1OriginalPosition;
                quizScreen2.anchoredPosition = quizScreen2OriginalPosition;
            }
        }
    }
}