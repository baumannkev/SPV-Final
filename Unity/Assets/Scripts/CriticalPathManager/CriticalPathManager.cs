using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class CriticalPathManager : MonoBehaviour
{
    // Reference to the button in the nav bar.
    public Button criticalPathButton;

    // List of all task data. This list will be refreshed on toggle.
    public List<TaskData> tasks;

    // Flag to track whether critical path is currently being shown.
    private bool isCriticalPathVisible = true;

    private TaskManager taskManager;

    // Use a coroutine to ensure tasks are loaded before applying the critical path highlighting.
    IEnumerator Start()
    {
        // Find the TaskManager in the scene.
        taskManager = FindObjectOfType<TaskManager>();

        if (criticalPathButton != null)
        {
            // Attach the toggle functionality to the button's click event.
            criticalPathButton.onClick.AddListener(ToggleCriticalPath);
        }

        // Wait until tasks are loaded (you might need to adjust this condition based on your loading logic).
        yield return new WaitUntil(() => taskManager.GetAllTasks().Count > 0);

        tasks = taskManager.GetAllTasks();
        TaskVisualizer.Instance.ToggleCriticalPathHighlighting(isCriticalPathVisible);
    }

    // Toggle the critical path view on or off.
    void ToggleCriticalPath()
    {
        // Debug.Log("clicked critical path");
        tasks = taskManager.GetAllTasks();
        isCriticalPathVisible = !isCriticalPathVisible;
        TaskVisualizer.Instance.ToggleCriticalPathHighlighting(isCriticalPathVisible);
    }
}