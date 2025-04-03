using UnityEngine;
using UnityEngine.UI;

public class SectionViewManager : MonoBehaviour
{
    public GameObject modelContainer;  // ✅ Assign your model container
    public Button sectionViewButton;   // ✅ Assign your UI Button
    public Shader sectionShader;       // ✅ Assign your section shader
    private Material sectionMaterial;

    private bool isSectionViewActive = false;

    void Start()
    {
        if (sectionViewButton != null)
        {
            sectionViewButton.onClick.AddListener(ToggleSectionView);
        }
    }

    void ToggleSectionView()
    {
        if (modelContainer == null)
        {
            Debug.LogError("❌ Model container is NULL! Cannot apply shader.");
            return;
        }

        Transform model = modelContainer.transform.Find("FirstModelAfterScan");
        if (model == null)
        {
            Debug.LogError("❌ No model named 'FirstModelAfterScan' found!");
            return;
        }

        Renderer modelRenderer = model.GetComponent<Renderer>();
        if (modelRenderer == null)
        {
            Debug.LogError("❌ No Renderer found on the model!");
            return;
        }

        if (sectionShader == null)
        {
            Debug.LogError("❌ Section Shader is not assigned!");
            return;
        }

        if (isSectionViewActive)
        {
            // 🔄 Revert to original material
            modelRenderer.material = sectionMaterial;
            Debug.Log("🔄 Section View Disabled");
        }
        else
        {
            // 🆕 Store original material before applying the shader
            sectionMaterial = modelRenderer.material;

            // 🆕 Create a new material with the section shader
            Material newMaterial = new Material(sectionShader);
            modelRenderer.material = newMaterial;

            Debug.Log("✅ Section View Enabled");
        }

        isSectionViewActive = !isSectionViewActive;
    }
}