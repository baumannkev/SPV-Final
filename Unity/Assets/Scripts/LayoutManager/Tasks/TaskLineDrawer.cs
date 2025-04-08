using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// Handles the creation, management, and visualization of dependency lines between tasks.
/// </summary>
public class TaskLineDrawer : MonoBehaviour
{
    public GameObject linePrefab; // Prefab for the line segments
    public GameObject arrowPrefab; // Prefab for the arrow at the end of a line
    public Transform contentParent; // Parent transform for all line objects

    public float lineThickness = 3f; // Thickness of the line segments
    public float arrowSize = 30f; // Size of the arrow object

    private List<LineData> lines = new List<LineData>(); // List of all active lines

    [System.Serializable]
    public class LineData
    {
        public TaskData StartTask; // Starting task of the line
        public TaskData EndTask; // Ending task of the line
        public List<GameObject> lineSegments = new List<GameObject>(); // Line segments making up the line
        public GameObject arrowObject; // Arrow object at the end of the line
    }

    public enum GSide
    {
        RIGHT, // Right side of the task
        LEFT, // Left side of the task
        BOTTOM_RIGHT, // Bottom-right corner of the task
        BOTTOM_LEFT // Bottom-left corner of the task
    }

    /// <summary>
    /// Adds a new dependency line between two tasks.
    /// </summary>
    public void AddLine(TaskData startTask, TaskData endTask, Vector3 startLocalPos, Vector3 endLocalPos)
    {
        // Force a layout rebuild on the contentParent to ensure up-to-date positions.
        LayoutRebuilder.ForceRebuildLayoutImmediate(contentParent as RectTransform);

        if (linePrefab == null || arrowPrefab == null)
        {
            Debug.LogError("LinePrefab or ArrowPrefab is not assigned.");
            return;
        }

        LineData existingLine = lines.Find(l => l.StartTask == startTask && l.EndTask == endTask);
        if (existingLine != null)
        {
            foreach (var segment in existingLine.lineSegments)
            {
                segment.SetActive(true);
            }
            if (existingLine.arrowObject != null)
            {
                existingLine.arrowObject.SetActive(true);
            }
            // Debug.Log($"Reusing existing line between '{startTask.Name}' and '{endTask.Name}'.");
            return;
        }

        LineData lineData = new LineData
        {
            StartTask = startTask,
            EndTask = endTask,
            lineSegments = new List<GameObject>()
        };

        // Get the RectTransforms for both tasks
        RectTransform startRect = startTask.TaskItem.GetComponent<RectTransform>();
        RectTransform endRect = endTask.TaskItem.GetComponent<RectTransform>();

        // Get world positions of the centers
        Vector3 startWorldCenter = startRect.TransformPoint(startRect.rect.center);
        Vector3 endWorldCenter = endRect.TransformPoint(endRect.rect.center);

        // Convert to local space of content parent
        Vector3 startLocal = contentParent.InverseTransformPoint(startWorldCenter);
        Vector3 endLocal = contentParent.InverseTransformPoint(endWorldCenter);

        // Calculate the edges considering the width
        float startX = startLocal.x + (startRect.rect.width / 2);
        float endX = endLocal.x - (endRect.rect.width / 2);

        // Create waypoints using local positions
        List<Vector3> waypoints = new List<Vector3>();
        waypoints.Add(new Vector3(startX, startLocal.y, 0));
        waypoints.Add(new Vector3(startX + 20f, startLocal.y, 0)); // Add offset for right side
        waypoints.Add(new Vector3(startX + 20f, endLocal.y, 0)); // Vertical segment
        waypoints.Add(new Vector3(endX - 20f, endLocal.y, 0)); // Last horizontal segment before arrow

        // Create line segments
        for (int i = 0; i < waypoints.Count - 1; i++)
        {
            AddSingleSegment(lineData, waypoints[i], waypoints[i + 1]);
        }

        // Add arrow at the end of the last line segment
        if (waypoints.Count > 1)
        {
            AddArrow(lineData, waypoints[waypoints.Count - 1]);
        }

        lines.Add(lineData);
        // Debug.Log($"Created new line between '{startTask.Name}' and '{endTask.Name}' with {waypoints.Count} waypoints. Start: ({startX}, {startLocal.y}), End: ({endX}, {endLocal.y})");
    }

    /// <summary>
    /// Calculates the route for a dependency line between two tasks.
    /// </summary>
    private List<Vector3> CalculateRoute(TaskData startTask, TaskData endTask, Vector3 startLocalPos, Vector3 endLocalPos)
    {
        List<Vector3> waypoints = new List<Vector3> { startLocalPos };

        // Define entry/exit sides for start and end tasks
        GSide atStart = DetermineSide(startTask, endTask, true);
        GSide atEnd = DetermineSide(startTask, endTask, false);

        // Start and end positions adjusted for task sides
        Vector3 startAdjusted = AdjustForSide(startLocalPos, atStart);
        Vector3 endAdjusted = AdjustForSide(endLocalPos, atEnd);

        // Route logic to avoid tasks (a simple L-shaped route)
        if (atStart == GSide.RIGHT && atEnd == GSide.LEFT)
        {
            waypoints.Add(new Vector3(startAdjusted.x, startAdjusted.y, 0));
            waypoints.Add(new Vector3(startAdjusted.x, endAdjusted.y, 0));
        }
        else if (atStart == GSide.BOTTOM_RIGHT || atStart == GSide.BOTTOM_LEFT)
        {
            waypoints.Add(new Vector3(startAdjusted.x, startAdjusted.y + 20f, 0));
            waypoints.Add(new Vector3(endAdjusted.x, startAdjusted.y + 20f, 0));
        }
        else
        {
            waypoints.Add(new Vector3(endAdjusted.x, startAdjusted.y, 0));
        }

        waypoints.Add(endAdjusted);
        return waypoints;
    }

    /// <summary>
    /// Determines the side of a task (entry/exit point) for a dependency line.
    /// </summary>
    private GSide DetermineSide(TaskData startTask, TaskData endTask, bool isStart)
    {
        // Hardcoded: start always exits on the right, end enters on the left.
        return isStart ? GSide.RIGHT : GSide.LEFT;
    }

    /// <summary>
    /// Adjusts a position based on the specified side of a task.
    /// </summary>
    private Vector3 AdjustForSide(Vector3 position, GSide side)
    {
        switch (side)
        {
            case GSide.RIGHT:
                return new Vector3(position.x + 20f, position.y, 0);
            case GSide.LEFT:
                return new Vector3(position.x - 20f, position.y, 0);
            case GSide.BOTTOM_RIGHT:
                return new Vector3(position.x + 20f, position.y - 10f, 0);
            case GSide.BOTTOM_LEFT:
                return new Vector3(position.x - 20f, position.y - 10f, 0);
            default:
                return position;
        }
    }

    /// <summary>
    /// Adds a single line segment between two points.
    /// </summary>
    private void AddSingleSegment(LineData lineData, Vector3 startPos, Vector3 endPos)
    {
        Vector3 diff = endPos - startPos;
        float length = Mathf.Max(diff.magnitude, 5f); // Prevent lines from becoming too short
        float angle = Mathf.Atan2(diff.y, diff.x) * Mathf.Rad2Deg;

        GameObject lineObject = Instantiate(linePrefab, contentParent);
        RectTransform rectTransform = lineObject.GetComponent<RectTransform>();
        rectTransform.anchoredPosition = startPos;
        rectTransform.localRotation = Quaternion.Euler(0, 0, angle);
        rectTransform.sizeDelta = new Vector2(length, lineThickness);

        lineData.lineSegments.Add(lineObject);
    }

    /// <summary>
    /// Adds an arrow object at the end of a line.
    /// </summary>
    private void AddArrow(LineData lineData, Vector3 endPos)
    {
        GameObject arrowObject = Instantiate(arrowPrefab, contentParent);
        RectTransform rectTransform = arrowObject.GetComponent<RectTransform>();
        rectTransform.anchoredPosition = endPos;
        rectTransform.sizeDelta = new Vector2(arrowSize, arrowSize);

        // Ensure arrow is always rendered in front of the line.
        arrowObject.transform.SetAsLastSibling();

        lineData.arrowObject = arrowObject;
    }

    /// <summary>
    /// Updates the visibility of lines based on the visibility of tasks.
    /// </summary>
    public void UpdateLineVisibility(HashSet<TaskData> visibleTasks)
    {
        List<LineData> linesToRemove = new List<LineData>();

        foreach (var lineData in lines)
        {
            bool isStartVisible = visibleTasks.Contains(lineData.StartTask);
            bool isEndVisible = visibleTasks.Contains(lineData.EndTask);

            if (!isStartVisible || !isEndVisible)
            {
                linesToRemove.Add(lineData);
            }
        }

        foreach (var lineData in linesToRemove)
        {
            RemoveLine(lineData.StartTask, lineData.EndTask);
        }
    }

    /// <summary>
    /// Removes a dependency line between two tasks.
    /// </summary>
    public void RemoveLine(TaskData startTask, TaskData endTask)
    {
        LineData existingLine = lines.Find(l => l.StartTask == startTask && l.EndTask == endTask);
        if (existingLine != null)
        {
            foreach (var segment in existingLine.lineSegments)
            {
                Destroy(segment);
            }
            if (existingLine.arrowObject != null)
            {
                Destroy(existingLine.arrowObject);
            }
            lines.Remove(existingLine);
        }
    }

    /// <summary>
    /// Checks if a dependency line exists between two tasks.
    /// </summary>
    public bool DoesLineExist(TaskData startTask, TaskData endTask)
    {
        return lines.Any(l => l.StartTask == startTask && l.EndTask == endTask);
    }

    /// <summary>
    /// Clears all dependency lines.
    /// </summary>
    public void ClearLines()
    {
        foreach (var lineData in lines)
        {
            foreach (var segment in lineData.lineSegments)
            {
                if (segment != null)
                {
                    Destroy(segment);
                }
            }
            if (lineData.arrowObject != null)
            {
                Destroy(lineData.arrowObject);
            }
        }
        lines.Clear(); // Ensure all line data is removed.
    }

    /// <summary>
    /// Destroys all dependency lines and clears the line data.
    /// </summary>
    public void DestroyLines()
    {
        foreach (var lineData in lines)
        {
            foreach (var segment in lineData.lineSegments)
            {
                Destroy(segment);
            }
            if (lineData.arrowObject != null)
            {
                Destroy(lineData.arrowObject);
            }
        }
        lines.Clear();
    }

    /// <summary>
    /// Calculates the horizontal distance from the right edge of a task to the start of a line.
    /// </summary>
    private float CalculateDistanceFromTaskToLine(RectTransform taskRect, Vector3 lineStartLocal)
    {
        // Compute task's right edge in world space:
        float taskRightWorld = taskRect.position.x + (1 - taskRect.pivot.x) * taskRect.rect.width;
        // Convert the task's right edge to local space of contentParent.
        Vector3 taskRightLocal = contentParent.InverseTransformPoint(new Vector3(taskRightWorld, taskRect.position.y, taskRect.position.z));
        // Distance is the difference in x coordinates.
        return lineStartLocal.x - taskRightLocal.x;
    }
}

public enum GSide
{
    RIGHT, // Right side of the task
    LEFT, // Left side of the task
    BOTTOM_RIGHT, // Bottom-right corner of the task
    BOTTOM_LEFT // Bottom-left corner of the task
}
