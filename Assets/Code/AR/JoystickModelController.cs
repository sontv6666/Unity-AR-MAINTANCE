using System.Collections.Generic;
using UnityEngine;

public class JoystickModelController : MonoBehaviour
{
    public Joystick joystick; // Assign in Inspector
    public float moveSpeed = 0.5f; // Adjust movement speed
    public float rotationSpeed = 100f; // Adjust rotation speed
    public bool enableRotation = true; // Toggle rotation

    private bool isTouchingModel = false; // Track if the model is being touched
    private Vector2 lastTouchPosition; // Store last touch position

    void Start()
    {
        if (QRCodeScanner.Instance != null)
        {
            StoreLatestMeshTransforms(transform); // ✅ Store initial transforms
        }
    }

    void Update()
    {
        if (QRCodeScanner.Instance != null && QRCodeScanner.Instance.isAnimationPlaying) 
            return; // Stop movement during animations

        HandleMovement();  // ✅ Handle joystick movement
        HandleTouchRotation(); // ✅ Handle touch rotation
    }

    void HandleMovement()
    {
        if (isTouchingModel) return; // Prevent joystick movement while touching the model

        Vector3 moveDirection = new Vector3(joystick.Horizontal, 0, joystick.Vertical);
        if (moveDirection.sqrMagnitude > 0.01f)
        {
            Vector3 movement = moveDirection.normalized * moveSpeed * Time.deltaTime;
            transform.position += movement;
            StoreLatestMeshTransforms(transform); // ✅ Store latest position updates
            Debug.Log($"🚀 Model moved to: {transform.position}");
        }
    }

    void HandleTouchRotation()
    {
        if (Input.touchCount == 1)  // Detect single touch
        {
            Touch touch = Input.GetTouch(0);
            Ray ray = Camera.main.ScreenPointToRay(touch.position);
            RaycastHit hit;

            if (touch.phase == TouchPhase.Began)
            {
                if (Physics.Raycast(ray, out hit) && hit.transform == transform)
                {
                    isTouchingModel = true;
                    lastTouchPosition = touch.position;
                }
            }
            else if (touch.phase == TouchPhase.Moved && isTouchingModel)
            {
                Vector2 delta = touch.position - lastTouchPosition;
                float rotationAmount = delta.x * rotationSpeed * Time.deltaTime; // Rotate based on X movement
                transform.Rotate(Vector3.up, -rotationAmount, Space.World);
                lastTouchPosition = touch.position; // Update last position

                StoreLatestMeshTransforms(transform); // ✅ Store latest rotation updates
                Debug.Log($"🔄 Model rotated to: {transform.rotation.eulerAngles}");
            }
            else if (touch.phase == TouchPhase.Ended)
            {
                isTouchingModel = false;
            }
        }
    }

    // ✅ Store latest mesh transforms using QRCodeScanner's dictionary
    void StoreLatestMeshTransforms(Transform model)
    {
        if (QRCodeScanner.Instance == null) return;

        var latestMeshTransforms = QRCodeScanner.Instance.latestMeshTransforms;

        // ✅ Store latest position & rotation in QRCodeScanner
        QRCodeScanner.Instance.latestPosition = model.position;
        QRCodeScanner.Instance.latestRotation = model.rotation;

        latestMeshTransforms[model] = (model.localPosition, model.localRotation, model.localScale);

        foreach (Transform child in model)
        {
            latestMeshTransforms[child] = (child.localPosition, child.localRotation, child.localScale);
        }

        Debug.Log($"✅ Stored latestPosition: {QRCodeScanner.Instance.latestPosition}, latestRotation: {QRCodeScanner.Instance.latestRotation}");
    }

    // ✅ Reset model & child meshes to the last stored transforms
    public void ResetModelToLatestTransforms()
    {
        if (QRCodeScanner.Instance == null) return;

        var latestMeshTransforms = QRCodeScanner.Instance.latestMeshTransforms;
        
        if (latestMeshTransforms.ContainsKey(transform))
        {
            var (position, rotation, scale) = latestMeshTransforms[transform];
            transform.localPosition = position;
            transform.localRotation = rotation;
            transform.localScale = scale;
        }

        // Reset child meshes
        foreach (Transform child in transform)
        {
            if (latestMeshTransforms.ContainsKey(child))
            {
                var (position, rotation, scale) = latestMeshTransforms[child];
                child.localPosition = position;
                child.localRotation = rotation;
                child.localScale = scale;
            }
        }

        Debug.Log($"🔄 Model reset to latest stored position & rotation!");
    }
}
