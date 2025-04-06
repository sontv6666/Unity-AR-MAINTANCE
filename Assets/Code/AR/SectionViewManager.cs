using UnityEngine;

public class TransparentOutlineToggle : MonoBehaviour
{
    public GameObject modelContainer;
    public Shader outlineShader;

    private Material[] originalMaterials;
    private Renderer[] renderers;
    private bool isOutlineActive = false;

    void Start()
    {
        if (outlineShader == null)
        {
            Debug.LogError("❌ Outline Shader is not assigned!");
        }
    }

    public void ToggleOutline()
    {
        if (modelContainer == null)
        {
            Debug.LogError("❌ Model container is NULL! Cannot apply shader.");
            return;
        }

        renderers = modelContainer.GetComponentsInChildren<Renderer>();

        if (renderers.Length == 0)
        {
            Debug.LogError("❌ No Renderer found on the model or its children!");
            return;
        }

        if (!isOutlineActive)
        {
            originalMaterials = new Material[renderers.Length];

            for (int i = 0; i < renderers.Length; i++)
            {
                originalMaterials[i] = renderers[i].material;

                Material outlineMaterial = new Material(outlineShader);
                outlineMaterial.SetColor("_Color", new Color(1f, 1f, 1f, 0.3f)); // transparent white
                outlineMaterial.SetColor("_OutlineColor", Color.yellow);
                outlineMaterial.SetFloat("_OutlineWidth", 0.02f);

                renderers[i].material = outlineMaterial;
            }

            Debug.Log("✅ Transparent Outline Enabled");
        }
        else
        {
            for (int i = 0; i < renderers.Length; i++)
            {
                renderers[i].material = originalMaterials[i];
            }

            Debug.Log("🔄 Outline Disabled");
        }

        isOutlineActive = !isOutlineActive;
    }
}