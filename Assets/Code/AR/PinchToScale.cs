using UnityEngine;

public class PinchToScale : MonoBehaviour
{
    private float initialDistance;
    private Vector3 initialScale;
    private bool isScaling = false;

    public float minScale = 0.05f;  // 🔹 Prevent model from getting too small
    public float maxScale = 3.0f;  // 🔹 Prevent model from getting too big

    void Update()
    {
        if (Input.touchCount == 2)
        {
            Touch touch0 = Input.GetTouch(0);
            Touch touch1 = Input.GetTouch(1);

            if (touch0.phase == TouchPhase.Began || touch1.phase == TouchPhase.Began)
            {
                initialDistance = Vector2.Distance(touch0.position, touch1.position);
                initialScale = transform.localScale;
                isScaling = true;
            }
            else if (touch0.phase == TouchPhase.Moved && touch1.phase == TouchPhase.Moved && isScaling)
            {
                float currentDistance = Vector2.Distance(touch0.position, touch1.position);
                float scaleFactor = currentDistance / initialDistance;

                // 🔹 Apply clamped scaling
                Vector3 newScale = initialScale * scaleFactor;
                newScale.x = Mathf.Clamp(newScale.x, minScale, maxScale);
                newScale.y = Mathf.Clamp(newScale.y, minScale, maxScale);
                newScale.z = Mathf.Clamp(newScale.z, minScale, maxScale);

                transform.localScale = newScale;
            }
        }
        else
        {
            isScaling = false;
        }
    }
}