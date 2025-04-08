using UnityEngine;
using TMPro;
using UnityEngine.UI;
using UnityEngine.EventSystems;

public class ItemGrid : MonoBehaviour, IPointerClickHandler
{
    public TaskManager taskManager;
    private RectTransform rectTransform;

    public float tileSizeWidth = 180;
    public float tileSizeHeight = 90;

    public float spacingX = 80;
    public float spacingY = 40;

    [SerializeField] private GameObject taskItemPrefab;

    void Awake()
    {
        rectTransform = GetComponent<RectTransform>();
    }

    /// <summary>
    /// Handles pointer click events on the grid background. Does nothing to prevent unhighlighting tasks.
    /// </summary>
    public void OnPointerClick(PointerEventData eventData)
    {
        // Do nothing when clicking the background
        // This effectively prevents background clicks from unhighlighting tasks
    }

    /// <summary>
    /// Calculates and returns the row index for a given task based on its Y position.
    /// </summary>
    public int GetRow(TaskData task)
    {
        if (task.TaskItem == null)
        {
            Debug.LogWarning($"TaskItem is null for task '{task.Name}'.");
            return -1;
        }

        RectTransform itemRect = task.TaskItem.GetComponent<RectTransform>();
        if (itemRect == null)
        {
            Debug.LogWarning($"RectTransform not found for TaskItem of task '{task.Name}'.");
            return -1;
        }

        // Calculate row based on the Y position
        float yPosition = -itemRect.anchoredPosition.y;
        int row = Mathf.RoundToInt(yPosition / (tileSizeHeight + spacingY));
        return row;
    }

    /// <summary>
    /// Calculates and returns the column index for a given task based on its X position.
    /// </summary>
    public int GetColumn(TaskData task)
    {
        if (task.TaskItem == null)
        {
            Debug.LogWarning($"TaskItem is null for task '{task.Name}'.");
            return -1;
        }

        RectTransform itemRect = task.TaskItem.GetComponent<RectTransform>();
        if (itemRect == null)
        {
            Debug.LogWarning($"RectTransform not found for TaskItem of task '{task.Name}'.");
            return -1;
        }

        // Calculate column based on the X position
        float xPosition = itemRect.anchoredPosition.x;
        int column = Mathf.RoundToInt(xPosition / (tileSizeWidth + spacingX));
        return column;
    }

    /// <summary>
    /// Creates and visualizes a task in the grid at the specified column and row.
    /// </summary>
    public void CreateVisualizedTasks(TaskData task, int column, int row, Vector2Int maxColAndRow)
    {
        if (rectTransform == null)
        {
            rectTransform = GetComponent<RectTransform>();
        }

        if (taskItemPrefab == null)
        {
            Debug.LogError("TaskItem prefab is not assigned in the ItemGrid component.");
            return;
        }

        // Check if the item already exists (reuse if hidden)
        if (task.TaskItem == null)
        {
            // Instantiate the TaskItem if it doesn't exist
            TaskItem taskItem = Instantiate(taskItemPrefab, rectTransform).GetComponent<TaskItem>();
            if (taskItem == null)
            {
                Debug.LogError($"Failed to instantiate TaskItem for task '{task.Name}'.");
                return;
            }

            taskItem.taskData = task; // Link TaskData to TaskItem
            task.TaskItem = taskItem; // Link TaskItem to TaskData
        }
        else
        {
            // Reactivate hidden TaskItem
            task.TaskItem.gameObject.SetActive(true);
        }

        // Debug.Log($"TaskItem linked for task: {task.Name}");

        // Clear any existing listeners to prevent duplicate calls
        Button button = task.TaskItem.GetComponent<Button>();
        if (button != null)
        {
            button.onClick.RemoveAllListeners(); // Remove any previously assigned listeners
            button.onClick.AddListener(() => taskManager.OnTaskSelected(task)); // Add the new listener
        }
        else
        {
            Debug.LogWarning($"Button component not found on TaskItem for task '{task.Name}'.");
        }

        // Populate Task Name (with WBS)
        TextMeshProUGUI nameText = task.TaskItem.transform.Find("TaskNameText")?.GetComponent<TextMeshProUGUI>();
        if (nameText != null)
        {
            // Display both task name and its WBS on separate lines.
            nameText.text = $"{task.Name}";
            nameText.enableWordWrapping = true;       // Enable word wrapping
            nameText.overflowMode = TextOverflowModes.Ellipsis; // Use ellipsis if text overflows
            nameText.maxVisibleLines = 2;            // Limit to two lines
        }
        else
        {
            Debug.LogWarning($"TaskNameText not found for task '{task.Name}' in the prefab.");
        }

        // Populate Task Date
        TextMeshProUGUI dateText = task.TaskItem.transform.Find("TaskDateText")?.GetComponent<TextMeshProUGUI>();
        if (dateText != null)
        {
            string startDate = task.Start.ToString("MMM d");
            string endDate = task.Finish.ToString("MMM d");
            string duration = $"{task.Duration:0.##} days";
            dateText.text = $"{duration}\n{startDate} - {endDate}\nWBS: {task.WBS}";
        }
        else
        {
            Debug.LogWarning($"TaskDateText not found for task '{task.Name}' in the prefab.");
        }

        // Set position of the task in the grid
        RectTransform itemRect = task.TaskItem.GetComponent<RectTransform>();
        if (itemRect != null)
        {
            Vector2 position = new Vector2(
                column * (tileSizeWidth + spacingX) + tileSizeWidth / 2,
                -(row * (tileSizeHeight + spacingY) + tileSizeHeight / 2)
            );

            itemRect.anchoredPosition = position;
            itemRect.localScale = Vector3.one;
        }

        // Debug.Log($"Task '{task.Name}' added to grid at column {column}, row {row}");
    }
}
