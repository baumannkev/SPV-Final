using System.Collections;
using System.Collections.Generic;
using System.Linq;
using TMPro;
using UnityEngine;

public class TaskManager : MonoBehaviour
{
    public TaskVisualizer taskVisualizer;
    private List<TaskData> allTasks = new List<TaskData>();
    private TaskData currentlyHighlightedTask = null;
    public TMP_Text taskDetailsText;    
    
    /// <summary>
    /// Sets the list of tasks and calculates their positions using the TaskVisualizer.
    /// </summary>
    public void SetTasks(List<TaskData> tasks)
    {
        allTasks = tasks;
        if (taskVisualizer != null)
        {
            taskVisualizer.CalculateTaskPositions(tasks, GetTaskDictionary());
        }
    }

    /// <summary>
    /// Handles task selection, toggles highlighting, and updates the task details.
    /// </summary>
    public void OnTaskSelected(TaskData task)
    {
        if (task == null)
        {
            Debug.LogError("Task is null.");
            return;
        }

        // Debug.Log($"Task clicked: {task.Name}");

        // Toggle selection: if already selected, restore full layout then scroll to the task.
        if (currentlyHighlightedTask == task)
        {
            // Debug.Log("Deselecting task and restoring full layout, then scrolling to the selected task.");
            currentlyHighlightedTask = null;
            if (taskVisualizer != null)
            {
                taskVisualizer.RestoreOriginalLayout(0.5f);
            }
            ClearTaskDetails();
            return;
        }

        // Otherwise, select the new task.
        currentlyHighlightedTask = task;
        HashSet<TaskData> visibleChain = new HashSet<TaskData>();
        CollectVisibleChain(task, visibleChain, true);

        // Debug.Log($"Highlighting task: {task.Name}. Visible chain count: {visibleChain.Count}");

        if (taskVisualizer != null)
        {
            // Debug.Log("Applying compact effect to visible chain tasks.");
            taskVisualizer.RearrangeVisibleTasksWithCompactHierarchy(visibleChain, 0.5f);
            UpdateTaskDetails(task);
            taskVisualizer.StartCoroutine(taskVisualizer.ScrollToTask(task, 1.5f));
        }
        else
        {
            Debug.LogError("TaskVisualizer is not assigned.");
        }
    }

    /// <summary>
    /// Coroutine to restore the full layout and then scroll to the selected task.
    /// </summary>
    private IEnumerator RestoreAndScroll(TaskData task, float restoreDuration, float scrollDuration)
    {
        if(taskVisualizer != null)
        {
            taskVisualizer.RestoreOriginalLayout(restoreDuration);
            yield return new WaitForSeconds(restoreDuration);
            // Wait an extra frame to ensure layout is fully updated
            yield return new WaitForEndOfFrame();
            yield return taskVisualizer.StartCoroutine(taskVisualizer.ScrollToTask(task, scrollDuration));
        }
    }

    /// <summary>
    /// Retrieves the root task of the given task by traversing its parent chain.
    /// </summary>
    private TaskData GetRootTask(TaskData task)
    {
        if (task == null) return null;
        TaskData current = task;
        while (current.Parent != null)
        {
            current = current.Parent;
        }
        return current;
    }
    
    /// <summary>
    /// Recursively collects the visible chain for the given task, including its ancestors, predecessors, successors, and optionally its subtree.
    /// </summary>
    private void CollectVisibleChain(TaskData task, HashSet<TaskData> collected, bool includeSubtree)
    {
        if (task == null || collected.Contains(task))
            return;

        // Always add the task itself.
        collected.Add(task);

        // Always include the task's full parent chain.
        if (task.Parent != null)
        {
            CollectParentChain(task.Parent, collected);
        }

        // Always include direct predecessors and their parent chains.
        if (task.Predecessors != null)
        {
            foreach (var pred in task.Predecessors)
            {
                // For predecessors, do not force full subtree inclusion.
                CollectVisibleChain(pred, collected, false);
            }
        }

        // Always include direct successors recursively.
        if (task.Successors != null)
        {
            foreach (var succ in task.Successors)
            {
                // For successors, do not force full subtree inclusion.
                CollectVisibleChain(succ, collected, false);
            }
        }

        // If includeSubtree is true (for the selected task or when propagating down the chosen branch),
        // include all children recursively.
        if (includeSubtree && task.Children != null)
        {
            foreach (var child in task.Children)
            {
                CollectVisibleChain(child, collected, true);
            }
        }
    }

    /// <summary>
    /// Recursively collects the ancestors (parent chain) of the given task.
    /// </summary>
    private void CollectParentChain(TaskData task, HashSet<TaskData> collected)
    {
        if (task == null || collected.Contains(task))
            return;
        collected.Add(task);
        if (task.Parent != null)
            CollectParentChain(task.Parent, collected);
    }

    /// <summary>
    /// Updates the task details text with information about the selected task.
    /// </summary>
    private void UpdateTaskDetails(TaskData task)
    {
        if (task == null)
        {
            taskDetailsText.text = "<b><size=24><color=#4F46E5>Select a task to view details</color></size></b>";
            return;
        }

        string predecessors = (task.Predecessors != null && task.Predecessors.Count > 0)
            ? string.Join(", ", task.Predecessors.Select(p => p.Name))
            : "None";
        string successors = (task.Successors != null && task.Successors.Count > 0)
            ? string.Join(", ", task.Successors.Select(s => s.Name))
            : "None";

        // Create the completion status tag
        string statusTag = "<color=#4CAF50><b>Completed</b></color>";
        
        // Format duration as days with one decimal place
        string durationStr = $"{task.Duration:0.0} day(s)";

        taskDetailsText.text =
            $"<size=28><color=#4F46E5>Task Details</color></size>\n\n" +
            $"<size=24><b>{task.Name}</b></size>\n" +
            $"{statusTag}\n\n" +
            $"<color=#000000>ID:</color> {task.UID}\n" +
            $"<color=#000000>WBS:</color> {task.WBS}\n\n" +
            $"<color=#000000>Start:</color> {task.Start:MMM d, yyyy}\n" +
            $"<color=#000000>Finish:</color> {task.Finish:MMM d, yyyy}\n" +
            $"<color=#000000>Duration:</color> {durationStr}\n\n" +
            $"<b>Predecessors</b>\n" +
            $"<color=#000000>{predecessors}</color>\n\n" +
            $"<b>Successors</b>\n" +
            $"<color=#000000>{successors}</color>";
    }

    /// <summary>
    /// Clears the task details text.
    /// </summary>
    public void ClearTaskDetails()
    {
        UpdateTaskDetails(null);
    }

    /// <summary>
    /// Returns a dictionary of tasks with their UIDs as keys.
    /// </summary>
    public Dictionary<string, TaskData> GetTaskDictionary()
    {
        Dictionary<string, TaskData> taskDict = new Dictionary<string, TaskData>();
        foreach (var task in allTasks)
        {
            taskDict[task.UID] = task;
        }
        return taskDict;
    }

    /// <summary>
    /// Returns the list of all tasks.
    /// </summary>
    public List<TaskData> GetAllTasks()
    {
        return allTasks;
    }

    /// <summary>
    /// Clears all tasks and their associated visuals.
    /// </summary>
    public void ClearAllTasks()
    {
        // Destroy all TaskItems.
        foreach (var task in allTasks)
        {
            if (task.TaskItem != null)
            {
                Destroy(task.TaskItem.gameObject);
                task.TaskItem = null;
            }
        }
        // Debug.Log("ClearAllTasks called. Clearing all tasks...");
        allTasks.Clear();
        currentlyHighlightedTask = null;
        ClearTaskDetails();

        // Clear visuals.
        if (taskVisualizer != null)
        {
            taskVisualizer.DestroyVisuals();
        }
    }

    /// <summary>
    /// Checks if the given task is currently highlighted.
    /// </summary>
    public bool IsTaskHighlighted(TaskData task)
    {
        return currentlyHighlightedTask == task;
    }
}