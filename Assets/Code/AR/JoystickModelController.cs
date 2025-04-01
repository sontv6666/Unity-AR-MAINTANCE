using System.Collections.Generic;
using UnityEngine;

public class JoystickModelController : MonoBehaviour
{
    public Joystick joystick; // Assign in Inspector
    public float moveSpeed = 0.5f; // Adjust movement speed
    public float rotationSpeed = 100f; // Adjust rotation speed
    public bool enableRotation = true; // Toggle rotation

    private Vector3 targetPosition; // Smooth movement target
    private Quaternion targetRotation; // Smooth rotation target
    private bool isRotating = false; // Track if the model is being rotated

    void Start()
    {
        targetPosition = transform.position; // Set initial position
        targetRotation = transform.rotation; // Set initial rotation

        // Initialize latest transforms for the model's child meshes using QRCodeScanner
        if (QRCodeScanner.Instance != null)
        {
            StoreLatestMeshTransforms(transform);
        }
    }

    void Update()
    {
        if (QRCodeScanner.Instance != null && QRCodeScanner.Instance.isAnimationPlaying) 
            return; // Stop movement during animations

        // Get joystick movement
        Vector3 moveDirection = new Vector3(joystick.Horizontal, 0, joystick.Vertical);

        if (moveDirection.sqrMagnitude > 0.01f) // Prevent tiny movements
        {
            // Move the model smoothly
            Vector3 movement = moveDirection.normalized * moveSpeed * Time.deltaTime;
            transform.position += movement;

            // Store the latest transform when the model moves
            StoreLatestMeshTransforms(transform);
            Debug.Log($"Model moved to: {transform.position}");
        }

        // Enable rotation if needed
        if (enableRotation && moveDirection != Vector3.zero)
        {
            isRotating = true;
            targetRotation = Quaternion.LookRotation(moveDirection);
        }
        else
        {
            isRotating = false;
        }

        // Smooth rotation if the model is being rotated
        if (isRotating)
        {
            transform.rotation = Quaternion.RotateTowards(transform.rotation, targetRotation, rotationSpeed * Time.deltaTime);
            StoreLatestMeshTransforms(transform); // Store rotation updates
            Debug.Log($"Model rotated to: {transform.rotation.eulerAngles}");
        }
    }

    // Method to store the latest mesh transforms using QRCodeScanner's dictionary
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

        Debug.Log($"✅ Updated latestPosition: {QRCodeScanner.Instance.latestPosition}, latestRotation: {QRCodeScanner.Instance.latestRotation}");
    }

    // Method to reset the model and its child meshes to the latest stored transforms from QRCodeScanner
    void ResetModelToLatestTransforms(Transform model)
    {
        if (QRCodeScanner.Instance == null) return;

        var latestMeshTransforms = QRCodeScanner.Instance.latestMeshTransforms;
        
        if (latestMeshTransforms.ContainsKey(model))
        {
            var (position, rotation, scale) = latestMeshTransforms[model];
            model.localPosition = position;
            model.localRotation = rotation;
            model.localScale = scale;
        }

        // Reset child meshes
        foreach (Transform child in model)
        {
            if (latestMeshTransforms.ContainsKey(child))
            {
                var (position, rotation, scale) = latestMeshTransforms[child];
                child.localPosition = position;
                child.localRotation = rotation;
                child.localScale = scale;
            }
        }
    }
}
