using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class InstructionPanelManager : MonoBehaviour
{
    public List<GameObject> instructionPrefabs; // Drag prefabs here in Inspector
    public Transform contentPanel; // Assign the ScrollRect Content Panel
    public float snapSpeed = 10f;
    
    private List<GameObject> spawnedPanels = new List<GameObject>();
    private int currentPanelIndex = 0;

    void Start()
    {
        LoadInstructionPanels();
    }

    void LoadInstructionPanels()
    {
        foreach (GameObject prefab in instructionPrefabs)
        {
            GameObject panel = Instantiate(prefab, contentPanel);
            spawnedPanels.Add(panel);
        }

        if (spawnedPanels.Count == 0)
        {
            Debug.LogError("❌ No instruction panels loaded!");
        }
    }

    public void NextPanel()
    {
        if (currentPanelIndex < spawnedPanels.Count - 1)
        {
            currentPanelIndex++;
            SnapToPanel(currentPanelIndex);
        }
    }

    public void PreviousPanel()
    {
        if (currentPanelIndex > 0)
        {
            currentPanelIndex--;
            SnapToPanel(currentPanelIndex);
        }
    }

    private void SnapToPanel(int panelIndex)
    {
        Vector3 targetPos = new Vector3(-spawnedPanels[panelIndex].GetComponent<RectTransform>().anchoredPosition.x, 0, 0);
        StartCoroutine(SmoothMove(targetPos));
    }

    private System.Collections.IEnumerator SmoothMove(Vector3 targetPosition)
    {
        float elapsedTime = 0f;
        Vector3 startPosition = contentPanel.localPosition;
        while (elapsedTime < 0.3f) // Adjust time as needed
        {
            contentPanel.localPosition = Vector3.Lerp(startPosition, targetPosition, elapsedTime * snapSpeed);
            elapsedTime += Time.deltaTime;
            yield return null;
        }
        contentPanel.localPosition = targetPosition;
    }
}