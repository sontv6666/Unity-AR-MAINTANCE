using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Scale : MonoBehaviour
{
    public GameObject loginScreen;

    public RectTransform scrollViewRect;
    public float phoneHeight = 1920f; // Chiều cao thiết kế cho điện thoại
    public float tabletHeight = 1024f; // Chiều cao thiết kế cho tablet

    // Start is called before the first frame update
    void Start()
    {
        print("Screen width: " + Screen.width);
        print("Screen height: " + Screen.height);
        if (loginScreen != null)
        {
            LoginFormResize();
        }


        if (scrollViewRect != null)
        {

            AdjustScrollViewSize();
        }


    }

    // Update is called once per frame
    void Update()
    {


    }

    void LoginFormResize()
    {
        float screenHeight = Screen.height;
        float aspectRatio = (float)Screen.width / Screen.height;

        if (aspectRatio >= 0.7f && aspectRatio <= 0.8f) // Thêm điều kiện kiểm tra
        {
            Vector3 position = loginScreen.transform.position;
            position.y += 625;
            loginScreen.transform.position = position;
        }

    }

    void AdjustScrollViewSize()
    {
        float screenHeight = Screen.height;
        float aspectRatio = (float)Screen.width / Screen.height;

        // Nếu tỉ lệ màn hình gần với tablet (ví dụ: 4:3)
        if (aspectRatio >= 0.7f && aspectRatio <= 0.8f)
        {
            float scaleFactor = tabletHeight / phoneHeight;
            scrollViewRect.sizeDelta = new Vector2(
                scrollViewRect.sizeDelta.x,
                scrollViewRect.sizeDelta.y * scaleFactor
            );
            // Cập nhật vị trí Y của scrollViewRect
            scrollViewRect.anchoredPosition = new Vector2(
                scrollViewRect.anchoredPosition.x,
                -scrollViewRect.sizeDelta.y
            );


        }
    }
}