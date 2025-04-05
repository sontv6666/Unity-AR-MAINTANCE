using UnityEngine;

public class SectionViewManager : MonoBehaviour
{
    public GameObject modelContainer;
    public Shader sectionShader;
    private Material[] originalMaterials;
    private Renderer[] renderers;
    private bool isSectionViewActive = false;
    public Transform clipPlane; // This will be the plane that defines where to clip the model

    void Start()
    {
        if (sectionShader == null)
        {
            Debug.LogError("❌ Section Shader is not assigned!");
            return;
        }
    }

    public void ToggleSectionView()
    {
        if (modelContainer == null)
        {
            Debug.LogError("❌ Model container is NULL! Cannot apply shader.");
            return;
        }

        // Find all renderers within the model container, including its children
        renderers = modelContainer.GetComponentsInChildren<Renderer>();

        if (renderers.Length == 0)
        {
            Debug.LogError("❌ No Renderer found on the model or its children!");
            return;
        }

        // Store the original materials and apply the section shader
        if (!isSectionViewActive)
        {
            originalMaterials = new Material[renderers.Length];
            for (int i = 0; i < renderers.Length; i++)
            {
                // Store the original material for later restoration
                originalMaterials[i] = renderers[i].material;

                // Create a new material for the outline effect
                Material outlineMaterial = new Material(sectionShader);
                outlineMaterial.SetColor("_OutlineColor", Color.yellow); // Set your desired outline color
                outlineMaterial.SetFloat("_OutlineWidth", 0.02f); // Set your desired outline width
                renderers[i].material = outlineMaterial; // Apply outline material to the renderer
            }
            Debug.Log("✅ Section View Enabled");
        }
        else
        {
            // Restore the original materials when toggling off
            for (int i = 0; i < renderers.Length; i++)
            {
                renderers[i].material = originalMaterials[i];
            }
            Debug.Log("🔄 Section View Disabled");
        }

        // Update _ClipPlane value in all materials
        Vector4 clipPlaneVector = new Vector4(
            clipPlane.up.x,
            clipPlane.up.y,
            clipPlane.up.z,
            -Vector3.Dot(clipPlane.position, clipPlane.up)
        );

        foreach (Renderer rend in renderers)
        {
            // Set the clip plane for all materials
            rend.material.SetVector("_ClipPlane", clipPlaneVector);
        }

        // Toggle the section view state
        isSectionViewActive = !isSectionViewActive;
    }
}
