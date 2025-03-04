using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Scale : MonoBehaviour
{
    public GameObject loginScreen;

    // Start is called before the first frame update
    void Start()
    {
        if (SystemInfo.deviceType == DeviceType.Handheld && Screen.width > 1200 && Screen.height > 800) // Thêm điều kiện kiểm tra
        {
            Vector3 position = loginScreen.transform.position;
            position.y += 625;
            loginScreen.transform.position = position;
        }
    }

    // Update is called once per frame
    void Update()
    {

    }
}