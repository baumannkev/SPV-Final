using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// Visualizes tasks in a grid layout and manages dependency lines between tasks.
/// </summary>
public class TaskVisualizer : MonoBehaviour
{
    public TaskManager taskManager; // Reference to the TaskManager
    public ItemGrid itemGrid; // Reference to the grid layout for tasks
    public TaskLineDrawer taskLineDrawer; // Handles drawing dependency lines between tasks
    public ScrollRect scrollRect; // ScrollRect for panning and zooming the task view

    private Dictionary<TaskData, Vector2> originalPositions = new Dictionary<TaskData, Vector2>(); // Stores original positions of tasks
    public static TaskVisualizer Instance; // Singleton instance of TaskVisualizer

    /// <summary>
    /// Initializes the singleton instance.
    /// </summary>
    void Awake()
    {
        Instance = this;
    }

    /// <summary>
    /// Toggles highlighting of critical tasks.
    /// </summary>
    public void ToggleCriticalPathHighlighting(bool showCritical)
    {
        List<TaskData> allTasks = taskManager.GetAllTasks();
        foreach (var task in allTasks)
        {
            if (task.TaskItem != null)
            {
                Image taskImage = task.TaskItem.GetComponent<Image>();
                if (taskImage != null)
                {
                    if (showCritical && (task.Critical == "1"))
                    {
                        taskImage.color = new Color32(255, 107, 111, 190);
                    }
                    else
                    {
                        taskImage.color = Color.white;
                    }
                }
            }
        }
    }

    /// <summary>
    /// Initializes task items with default settings.
    /// </summary>
    public void InitializeTaskItems()
    {
        foreach (var task in taskManager.GetAllTasks())
        {
            if (task.TaskItem != null)
            {
                task.TaskItem.InitializeAlpha();
            }
        }
    }

    /// <summary>
    /// Destroys all visual elements related to tasks.
    /// </summary>
    public void DestroyVisuals()
    {
        if (taskLineDrawer != null)
        {
            taskLineDrawer.DestroyLines();
        }
        if (itemGrid == null)
        {
            Debug.LogError("ItemGrid is not assigned to TaskVisualizer.");
            return;
        }
        foreach (Transform child in itemGrid.transform)
        {
            Destroy(child.gameObject);
        }
        originalPositions.Clear();
    }

    /// <summary>
    /// Clears all task visuals without destroying them.
    /// </summary>
    public void ClearAllVisuals()
    {
        if (taskLineDrawer != null)
        {
            taskLineDrawer.ClearLines();
        }
        if (itemGrid == null)
        {
            Debug.LogError("ItemGrid is not assigned to TaskVisualizer.");
            return;
        }
        foreach (Transform child in itemGrid.transform)
        {
            child.gameObject.SetActive(false);
        }
        originalPositions.Clear();
    }

    /// <summary>
    /// Moves a task to a specified position over a duration.
    /// </summary>
    public IEnumerator MoveTaskToPosition(TaskItem taskItem, Vector2 targetPosition, float duration, TaskData task)
    {
        RectTransform rectTransform = taskItem.GetComponent<RectTransform>();
        Vector2 initialPosition = rectTransform.anchoredPosition;
        float time = 0f;
        while (time < duration)
        {
            time += Time.deltaTime;
            float t = time / duration;
            rectTransform.anchoredPosition = Vector2.Lerp(initialPosition, targetPosition, t);
            yield return null;
        }
        rectTransform.anchoredPosition = targetPosition;
    }

    /// <summary>
    /// Scrolls to a specific task or position over a duration.
    /// </summary>
    public IEnumerator ScrollToTask(TaskData task, float duration, bool useAveragePosition = false, HashSet<TaskData> visibleTasks = null, Vector2? overridePosition = null)
    {
        if (scrollRect == null)
        {
            Debug.LogError("ScrollRect is not assigned to TaskVisualizer");
            yield break;
        }

        RectTransform contentRect = scrollRect.content;
        RectTransform viewportRect = scrollRect.viewport;

        // Calculate target position
        Vector2 targetCenter;
        
        if (overridePosition.HasValue)
        {
            targetCenter = overridePosition.Value;
        }
        else if (useAveragePosition && visibleTasks != null && visibleTasks.Count > 0)
        {
            // Calculate average position of all visible tasks
            Vector2 sum = Vector2.zero;
            int count = 0;
            foreach (var visibleTask in visibleTasks)
            {
                if (visibleTask.TaskItem != null)
                {
                    RectTransform taskRect = visibleTask.TaskItem.GetComponent<RectTransform>();
                    Vector2 taskCenter = taskRect.anchoredPosition;
                    sum += taskCenter;
                    count++;
                }
            }
            targetCenter = count > 0 ? sum / count : Vector2.zero;
        }
        else if (task != null && task.TaskItem != null)
        {
            // Use single task position
            RectTransform taskRect = task.TaskItem.GetComponent<RectTransform>();
            targetCenter = taskRect.anchoredPosition;
        }
        else
        {
            Debug.LogError("No valid target position for scrolling");
            yield break;
        }

        // Calculate the normalized target position
        float viewportWidth = viewportRect.rect.width;
        float viewportHeight = viewportRect.rect.height;
        float contentWidth = contentRect.rect.width;
        float contentHeight = contentRect.rect.height;

        // Calculate target normalized position (0-1 range)
        float targetNormalizedX = ((-targetCenter.x + viewportWidth * 0.5f) / (contentWidth - viewportWidth));
        float targetNormalizedY = ((-targetCenter.y + viewportHeight * 0.5f) / (contentHeight - viewportHeight));

        // Clamp values to valid range
        targetNormalizedX = Mathf.Clamp01(targetNormalizedX);
        targetNormalizedY = Mathf.Clamp01(targetNormalizedY);

        Vector2 startPosition = scrollRect.normalizedPosition;
        Vector2 targetPosition = new Vector2(targetNormalizedX, targetNormalizedY);
        
        // Smooth scrolling animation
        float elapsed = 0f;
        while (elapsed < duration)
        {
            elapsed += Time.deltaTime;
            float t = elapsed / duration;
            
            // Smooth step easing
            t = t * t * (3f - 2f * t);
            
            scrollRect.normalizedPosition = Vector2.Lerp(startPosition, targetPosition, t);
            yield return null;
        }

        // Ensure we reach the exact target position
        scrollRect.normalizedPosition = targetPosition;
    }

    /// <summary>
    /// Restores the original layout of tasks.
    /// </summary>
    public void RestoreOriginalLayout(float duration)
    {
        if (originalPositions.Count == 0)
            return;
        // Start the coroutine that moves tasks back and then redraws lines.
        StartCoroutine(RestoreOriginalLayoutRoutine(duration));
    }

    /// <summary>
    /// Helper coroutine to restore the original layout and redraw dependency lines.
    /// </summary>
    private IEnumerator RestoreOriginalLayoutRoutine(float duration)
    {
        // Ensure all tasks are visible (in case some were hidden)
        ShowAllTasks();

        // Animate tasks back to original positions.
        foreach (var task in taskManager.GetAllTasks())
        {
            if (task.TaskItem != null && originalPositions.ContainsKey(task))
            {
                Vector2 originalPos = originalPositions[task];
                StartCoroutine(MoveTaskToPosition(task.TaskItem, originalPos, duration, task));
                task.TaskItem.gameObject.SetActive(true);
                task.TaskItem.InitializeAlpha();
            }
        }
        // Wait for all animations to complete.
        yield return new WaitForSeconds(duration);
        yield return new WaitForEndOfFrame();

        // Clear existing dependency lines.
        taskLineDrawer.ClearLines();

        // Redraw dependency lines using the tasks' current (original) positions.
        HashSet<TaskData> allTasksSet = new HashSet<TaskData>(taskManager.GetAllTasks());
        foreach (var t in allTasksSet)
        {
            if (t.Successors == null) continue;
            foreach (var s in t.Successors)
            {
                if (s.TaskItem == null)
                    continue;
                RectTransform startRect = t.TaskItem.GetComponent<RectTransform>();
                RectTransform endRect = s.TaskItem.GetComponent<RectTransform>();
                Vector3 startWorldPos = startRect.position + new Vector3(startRect.rect.width / 2f, 0f, 0f);
                Vector3 endWorldPos = endRect.position - new Vector3(endRect.rect.width / 2f, 0f, 0f);
                Vector3 localStart = taskLineDrawer.contentParent.InverseTransformPoint(startWorldPos);
                Vector3 localEnd = taskLineDrawer.contentParent.InverseTransformPoint(endWorldPos);
                taskLineDrawer.AddLine(t, s, localStart, localEnd);
            }
        }
        taskLineDrawer.UpdateLineVisibility(allTasksSet);
        // Debug.Log("RestoreOriginalLayout complete: tasks and lines are back to original positions.");
    }

    /// <summary>
    /// Ensures all task items are active and visible.
    /// </summary>
    private void ShowAllTasks()
    {
        foreach (var task in taskManager.GetAllTasks())
        {
            if (task.TaskItem != null)
            {
                task.TaskItem.gameObject.SetActive(true);
                CanvasGroup cg = task.TaskItem.GetComponent<CanvasGroup>();
                if (cg != null)
                {
                    cg.alpha = 1f;
                }
            }
        }
    }

    /// <summary>
    /// Compacts the visible positions of tasks to avoid overlaps.
    /// </summary>
    private Dictionary<string, (int column, int row)> CompactVisiblePositions(
        Dictionary<string, (int column, int row)> computedPositions,
        List<TaskData> visibleTasks,
        Dictionary<string, TaskData> visibleLookup)
    {
        // Sort visible tasks by their computed row, then by column.
        var sorted = visibleTasks
            .Where(t => computedPositions.ContainsKey(t.UID))
            .OrderBy(t => computedPositions[t.UID].row)
            .ThenBy(t => computedPositions[t.UID].column)
            .ToList();

        // candidateRows stores the intermediate candidate row for each task.
        Dictionary<string, int> candidateRows = new Dictionary<string, int>();
        int nextRow = 0;
        foreach (var task in sorted)
        {
            // Start candidate with the sorted order index.
            int candidate = nextRow;
            if (task.PredecessorUIDs != null)
            {
                foreach (var predId in task.PredecessorUIDs)
                {
                    // Only consider visible predecessors that have already been processed.
                    if (visibleLookup.ContainsKey(predId) && candidateRows.ContainsKey(predId))
                    {
                        int predCandidate = candidateRows[predId];
                        // If the task's computed row equals its predecessor's computed row,
                        // then force the candidate to be one row below that predecessor.
                        if (computedPositions[task.UID].row == computedPositions[predId].row)
                        {
                            candidate = Mathf.Max(candidate, predCandidate + 1);
                            // Debug.Log($"[Compaction] Task {task.UID} candidate updated to {candidate} (equal computed row) due to predecessor {predId} (candidate {predCandidate})");
                        }
                        else
                        {
                            // Otherwise, just ensure candidate is at least as high as the predecessor's candidate.
                            candidate = Mathf.Max(candidate, predCandidate);
                            // Debug.Log($"[Compaction] Task {task.UID} candidate updated to {candidate} due to predecessor {predId} (candidate {predCandidate})");
                        }
                    }
                }
            }
            candidateRows[task.UID] = candidate;
            // Debug.Log($"[Compaction] Final candidate for Task {task.UID} ({task.Name}): {candidate}");
            nextRow = candidate + 1;
        }

        // Remap the candidate rows to consecutive integers (compact the layout).
        var distinctCandidates = candidateRows.Values.Distinct().OrderBy(x => x).ToList();
        Dictionary<int, int> mapping = new Dictionary<int, int>();
        for (int i = 0; i < distinctCandidates.Count; i++)
        {
            mapping[distinctCandidates[i]] = i;
            // Debug.Log($"[Compaction] Mapping original row {distinctCandidates[i]} -> new row {i}");
        }

        Dictionary<string, (int column, int row)> newPositions = new Dictionary<string, (int column, int row)>();
        foreach (var task in sorted)
        {
            int newRow = mapping[candidateRows[task.UID]];
            newPositions[task.UID] = (computedPositions[task.UID].column, newRow);
            // Debug.Log($"[Compaction] Task {task.UID} ({task.Name}): Column {computedPositions[task.UID].column}, Final New Row {newRow}");
        }
        
        return newPositions;
    }

    /// <summary>
    /// Rearranges visible tasks into a compact hierarchy.
    /// </summary>
    public void RearrangeVisibleTasksWithCompactHierarchy(HashSet<TaskData> visibleTasks, float duration)
    {
        if (itemGrid == null || taskLineDrawer == null)
        {
            Debug.LogError("ItemGrid or TaskLineDrawer is not assigned in TaskVisualizer.");
            return;
        }
        
        if (originalPositions.Count == 0)
        {
            foreach (var task in taskManager.GetAllTasks())
            {
                if (task.TaskItem != null)
                {
                    originalPositions[task] = task.TaskItem.GetComponent<RectTransform>().anchoredPosition;
                }
            }
        }
        
        List<TaskData> visibleList = visibleTasks.ToList();
        
        LayoutGrid tempLayout = new LayoutGrid();
        tempLayout.ComputeFinalLayout(visibleList);
        Dictionary<string, (int column, int row)> computedPositions = tempLayout.GetAllPositions();
        
        Dictionary<string, TaskData> visibleLookup = visibleList
            .Where(t => computedPositions.ContainsKey(t.UID))
            .GroupBy(t => t.UID)
            .ToDictionary(g => g.Key, g => g.First());
        
        Dictionary<string, (int column, int row)> compactedPositions = CompactVisiblePositions(computedPositions, visibleList, visibleLookup);

        // Find the currently highlighted task's final position
        Vector2? targetScrollPosition = null;
        
        // Start all animations
        foreach (var task in visibleList)
        {
            if (task.TaskItem == null || !compactedPositions.ContainsKey(task.UID))
                continue;
            var pos = compactedPositions[task.UID];
            float targetX = pos.column * (itemGrid.tileSizeWidth + itemGrid.spacingX) + itemGrid.tileSizeWidth / 2;
            float targetY = -(pos.row * (itemGrid.tileSizeHeight + itemGrid.spacingY) + itemGrid.tileSizeHeight / 2);
            Vector2 targetPosition = new Vector2(targetX, targetY);

            // Store the target position if this is the highlighted task
            if (visibleTasks.Count == 1 || (taskManager != null && taskManager.IsTaskHighlighted(task)))
            {
                targetScrollPosition = targetPosition;
            }

            if (!task.TaskItem.gameObject.activeSelf)
                task.TaskItem.gameObject.SetActive(true);
            StartCoroutine(MoveTaskToPosition(task.TaskItem, targetPosition, duration, task));
        }
        
        // Start fade-out animations for non-visible tasks
        foreach (var task in taskManager.GetAllTasks())
        {
            if (task.TaskItem != null && !visibleTasks.Contains(task))
            {
                StartCoroutine(HideTaskWithDelay(task, duration));
            }
        }
        
        // Update dependency lines
        StartCoroutine(UpdateDependencyLinesAfterMovement(visibleTasks, duration));
        
        // Scroll to either the highlighted task's final position or the center of visible tasks
        if (targetScrollPosition.HasValue)
        {
            StartCoroutine(ScrollToTask(null, duration, false, null, targetScrollPosition));
        }
        else
        {
            StartCoroutine(ScrollToTask(null, duration, true, visibleTasks));
        }
    }

    /// <summary>
    /// Updates dependency lines after tasks have been moved.
    /// </summary>
    private IEnumerator UpdateDependencyLinesAfterMovement(HashSet<TaskData> visibleTasks, float duration)
    {
        yield return new WaitForSeconds(duration);
        taskLineDrawer.ClearLines();

        foreach (var task in visibleTasks)
        {
            foreach (var successor in task.Successors)
            {
                if (successor.TaskItem == null)
                    continue;
                if (!visibleTasks.Contains(successor))
                    continue;

                RectTransform startRect = task.TaskItem.GetComponent<RectTransform>();
                RectTransform endRect = successor.TaskItem.GetComponent<RectTransform>();
                if (startRect != null && endRect != null)
                {
                    Vector3 startWorldPos = startRect.position + new Vector3(startRect.rect.width / 2, 0, 0);
                    Vector3 endWorldPos = endRect.position - new Vector3(endRect.rect.width / 2, 0, 0);
                    Vector3 localStart = taskLineDrawer.contentParent.InverseTransformPoint(startWorldPos);
                    Vector3 localEnd = taskLineDrawer.contentParent.InverseTransformPoint(endWorldPos);

                    // Debug.Log($"[Dependency Line] {task.UID} ({task.Name}) -> {successor.UID} ({successor.Name}) | Local Start: {localStart}, Local End: {localEnd}");
                    taskLineDrawer.AddLine(task, successor, localStart, localEnd);
                }
            }
        }

        taskLineDrawer.UpdateLineVisibility(visibleTasks);
    }

    /// <summary>
    /// Hides a task with a fade-out animation and removes its dependency lines.
    /// </summary>
    private IEnumerator HideTaskWithDelay(TaskData task, float duration)
    {
        // Start fading immediately but take longer
        if (task.TaskItem != null)
        {
            StartCoroutine(FadeOutTask(task.TaskItem, duration * 1.2f));
        }
        
        // Remove lines after the fade completes
        yield return new WaitForSeconds(duration * 1.2f);
        foreach (var successor in task.Successors)
        {
            taskLineDrawer.RemoveLine(task, successor);
        }
        foreach (var predecessor in task.Predecessors)
        {
            taskLineDrawer.RemoveLine(predecessor, task);
        }
    }
    
    /// <summary>
    /// Fades out a task item over a duration.
    /// </summary>
    public IEnumerator FadeOutTask(TaskItem taskItem, float duration)
    {
        CanvasGroup cg = taskItem.GetComponent<CanvasGroup>();
        if (cg == null)
        {
            cg = taskItem.gameObject.AddComponent<CanvasGroup>();
            cg.alpha = 1;
        }

        RectTransform rectTransform = taskItem.GetComponent<RectTransform>();
        Vector2 originalScale = rectTransform.localScale;
        Vector2 originalPosition = rectTransform.anchoredPosition;
        float startAlpha = cg.alpha;
        float time = 0;

        // Initial slight move back in Z (scale down) before other movements
        float initialDelay = duration * 0.2f; // Use first 20% of time for initial scale
        while (time < initialDelay)
        {
            time += Time.deltaTime;
            float t = time / initialDelay;
            float easeT = 1 - Mathf.Pow(1 - t, 3); // Cubic ease-out
            
            // Initial scale down and slight fade
            float scaleValue = Mathf.Lerp(1f, 0.9f, easeT);
            rectTransform.localScale = new Vector3(scaleValue, scaleValue, 1);
            cg.alpha = Mathf.Lerp(1f, 0.9f, easeT);
            
            yield return null;
        }

        // Main fade out animation
        float mainAnimationTime = 0;
        Vector2 startPos = rectTransform.anchoredPosition;
        Vector2 controlPoint1 = startPos + new Vector2(-20f, 30f); // First control point for bezier
        Vector2 controlPoint2 = startPos + new Vector2(20f, -40f); // Second control point for bezier
        Vector2 endPos = startPos + new Vector2(0f, -60f); // End slightly below and behind

        while (mainAnimationTime < duration * 0.8f) // Use remaining 80% of time for main animation
        {
            mainAnimationTime += Time.deltaTime;
            float t = mainAnimationTime / (duration * 0.8f);
            float easeT = t * t * (3f - 2f * t); // Smooth step easing
            
            // Cubic bezier curve for position
            Vector2 p1 = Vector2.Lerp(Vector2.Lerp(startPos, controlPoint1, easeT), 
                                    Vector2.Lerp(controlPoint1, controlPoint2, easeT), easeT);
            Vector2 p2 = Vector2.Lerp(Vector2.Lerp(controlPoint1, controlPoint2, easeT), 
                                    Vector2.Lerp(controlPoint2, endPos, easeT), easeT);
            Vector2 finalPos = Vector2.Lerp(p1, p2, easeT);
            
            // Apply position
            rectTransform.anchoredPosition = finalPos;
            
            // Continue scaling down
            float scaleValue = Mathf.Lerp(0.9f, 0.7f, easeT);
            rectTransform.localScale = new Vector3(scaleValue, scaleValue, 1);
            
            // Fade out
            cg.alpha = Mathf.Lerp(0.9f, 0, Mathf.Pow(easeT, 2));
            
            yield return null;
        }

        // Ensure final state
        cg.alpha = 0;
        taskItem.gameObject.SetActive(false);
        
        // Reset transform for future use
        rectTransform.localScale = originalScale;
        rectTransform.anchoredPosition = originalPosition;
    }

    /// <summary>
    /// Calculates positions for tasks in the grid layout.
    /// </summary>
    public void CalculateTaskPositions(List<TaskData> tasks, Dictionary<string, TaskData> taskDictionary)
    {
        ClearAllVisuals();
        List<TaskData> sortedTasks = TopologicalSort(tasks);
        LayoutGrid layoutGrid = new LayoutGrid();
        layoutGrid.ComputeFinalLayout(sortedTasks);
        System.Text.StringBuilder sb = new System.Text.StringBuilder();
        sb.AppendLine("=== Computed Grid Positions ===");
        foreach (var task in tasks)
        {
            var pos = layoutGrid.GetPosition(task.UID);
            if (pos.HasValue)
                sb.AppendLine($"Task {task.UID} ({task.Name}): Column {pos.Value.column}, Row {pos.Value.row}");
            else
                sb.AppendLine($"Task {task.UID} ({task.Name}): No position computed");

            if (task.Predecessors != null && task.Predecessors.Count > 0)
                sb.AppendLine("    Predecessors: " + string.Join(", ", task.Predecessors.Select(p => p.UID)));
            else
                sb.AppendLine("    Predecessors: None");
        }
        
        sb.AppendLine();
        sb.AppendLine("=== Overlap Detection ===");
        
        Dictionary<(int column, int row), List<TaskData>> positionMap = new Dictionary<(int column, int row), List<TaskData>>();
        
        foreach (var task in tasks)
        {
            var pos = layoutGrid.GetPosition(task.UID);
            if (pos.HasValue)
            {
                var position = (pos.Value.column, pos.Value.row);
                if (!positionMap.ContainsKey(position))
                {
                    positionMap[position] = new List<TaskData>();
                }
                positionMap[position].Add(task);
            }
        }
        
        bool overlapsFound = false;
        foreach (var entry in positionMap)
        {
            if (entry.Value.Count > 1)
            {
                overlapsFound = true;
                sb.AppendLine($"Overlap at Column {entry.Key.column}, Row {entry.Key.row} with tasks:");
                foreach (var task in entry.Value)
                {
                    sb.AppendLine($"  - Task {task.UID} ({task.Name})");
                }
            }
        }
        
        if (!overlapsFound)
        {
            sb.AppendLine("No overlaps detected.");
        }
        
        Debug.Log(sb.ToString());
        foreach (var task in tasks)
        {
            var pos = layoutGrid.GetPosition(task.UID);
            if (pos.HasValue)
            {
                itemGrid.CreateVisualizedTasks(task, pos.Value.column, pos.Value.row, new Vector2Int(pos.Value.column + 1, pos.Value.row + 1));
            }
        }
    }

    /// <summary>
    /// Draws a dependency line between two tasks.
    /// </summary>
    public void DrawDependencyLine(TaskData task, TaskData successor)
    {
        if (task.TaskItem == null || successor.TaskItem == null)
        {
            Debug.LogError($"❌ Missing TaskItem. Task: {task.Name}, Successor: {successor.Name}");
            return;
        }
        RectTransform startRect = task.TaskItem.GetComponent<RectTransform>();
        RectTransform endRect = successor.TaskItem.GetComponent<RectTransform>();
        if (startRect == null || endRect == null)
        {
            Debug.LogError($"❌ RectTransform is NULL. Task: {task.Name}, Successor: {successor.Name}");
            return;
        }
        Vector3 startWorldPosition = GetRightEdgeWorldPosition(startRect);
        Vector3 endWorldPosition = GetLeftEdgeWorldPosition(endRect);
        Vector3 localStart = taskLineDrawer.contentParent.InverseTransformPoint(startWorldPosition);
        Vector3 localEnd = taskLineDrawer.contentParent.InverseTransformPoint(endWorldPosition);
        // Debug.Log($"📍 Start Position: {localStart}, End Position: {localEnd} for {task.Name} -> {successor.Name}");
        taskLineDrawer.AddLine(task, successor, localStart, localEnd);
    }

    /// <summary>
    /// Draws dependency lines for all tasks in the dictionary.
    /// </summary>
    public void DrawDependencyLines(Dictionary<string, TaskData> taskDictionary)
    {
        if (taskLineDrawer == null)
        {
            Debug.LogError("❌ TaskLineDrawer is not assigned. Cannot draw dependency lines.");
            return;
        }
        taskLineDrawer.ClearLines();
        // Debug.Log("📌 Drawing Dependency Lines...");
        foreach (var task in taskDictionary.Values)
        {
            if (task.Successors == null || task.Successors.Count == 0)
            {
                continue;
            }
            foreach (var successor in task.Successors)
            {
                if (successor.TaskItem == null)
                {
                    Debug.LogError($"❌ TaskItem for successor '{successor.Name}' is NULL. Skipping line.");
                    continue;
                }
                // Debug.Log($"✅ Drawing line from '{task.Name}' to '{successor.Name}'...");
                DrawDependencyLine(task, successor);
            }
        }
    }

    /// <summary>
    /// Performs a topological sort of tasks based on dependencies.
    /// </summary>
    public List<TaskData> TopologicalSort(List<TaskData> tasks)
    {
        Dictionary<string, TaskData> lookup = tasks.ToDictionary(t => t.UID);
        Dictionary<string, int> inDegree = tasks.ToDictionary(t => t.UID, t => 0);
        Dictionary<string, List<string>> graph = tasks.ToDictionary(t => t.UID, t => new List<string>());
        foreach (var task in tasks)
        {
            if (task.PredecessorUIDs != null)
            {
                foreach (var predUID in task.PredecessorUIDs)
                {
                    if (lookup.ContainsKey(predUID))
                    {
                        graph[predUID].Add(task.UID);
                        inDegree[task.UID]++;
                    }
                }
            }
        }
        Queue<string> q = new Queue<string>();
        foreach (var kvp in inDegree)
        {
            if (kvp.Value == 0)
                q.Enqueue(kvp.Key);
        }
        List<TaskData> sorted = new List<TaskData>();
        while (q.Count > 0)
        {
            string uid = q.Dequeue();
            sorted.Add(lookup[uid]);
            foreach (var neighbor in graph[uid])
            {
                inDegree[neighbor]--;
                if (inDegree[neighbor] == 0)
                    q.Enqueue(neighbor);
            }
        }
        if (sorted.Count != tasks.Count)
            Debug.LogError("Cycle detected in task dependencies!");
        return sorted;
    }
    
    /// <summary>
    /// Gets the world position of the right edge of a RectTransform.
    /// </summary>
    private Vector3 GetRightEdgeWorldPosition(RectTransform rect)
    {
        Vector3 offset = new Vector3((1 - rect.pivot.x) * rect.rect.width, (0.5f - rect.pivot.y) * rect.rect.height, 0);
        return rect.TransformPoint(offset);
    }

    /// <summary>
    /// Gets the world position of the left edge of a RectTransform.
    /// </summary>
    private Vector3 GetLeftEdgeWorldPosition(RectTransform rect)
    {
        Vector3 offset = new Vector3(-rect.pivot.x * rect.rect.width, (0.5f - rect.pivot.y) * rect.rect.height, 0);
        return rect.TransformPoint(offset);
    }
}
