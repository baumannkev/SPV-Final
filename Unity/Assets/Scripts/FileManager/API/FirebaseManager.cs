using System;
using UnityEngine;
using System.Collections.Generic;
using System.Linq;
using System.Xml; // Add this for XmlConvert

/// <summary>
/// Manages interactions with Firebase Firestore, including fetching and converting task data.
/// </summary>
public class FirebaseManager : MonoBehaviour
{
    public static FirebaseManager Instance;
    public TaskManager taskManager;
    public TaskVisualizer taskVisualizer;

    // IMPORTANT: Make sure this project id matches the one used when exporting tasks.
    // Either hard-code it, or update it dynamically from your export process.
    public static string ProjectId = "project_20250131_230154"; // <-- Update as needed

    // Instance dictionary for tasks
    private Dictionary<string, TaskData> taskDictionary = new Dictionary<string, TaskData>();

    /// <summary>
    /// Initializes the FirebaseManager singleton instance.
    /// </summary>
    void Awake()
    {
        Instance = this;
    }

    /// <summary>
    /// Fetches tasks from Firestore when the script starts.
    /// </summary>
    void Start()
    {
        // Alternatively, update ProjectId here dynamically if needed.
        // For example: ProjectId = GlobalProjectSettings.CurrentProjectId;
        FetchTasksFromFirestore();
    }

    /// <summary>
    /// Fetches tasks from Firestore. Only works in WebGL builds.
    /// </summary>
    public void FetchTasksFromFirestore()
    {
    #if UNITY_WEBGL && !UNITY_EDITOR
        // Do not override window.currentProjectId; let the index.html manage it.
        Application.ExternalEval("fetchTasks();");
    #else
        Debug.Log("⚠ Firestore fetch only works in WebGL!");
    #endif
    }

    /// <summary>
    /// Callback for receiving tasks from Firestore in JSON format.
    /// </summary>
    /// <param name="json">JSON string containing task data.</param>
    public void OnFirestoreTasksReceived(string json)
    {
        // Debug.Log("📡 Received Tasks from Firestore...");
        Debug.Log("Received JSON: " + json);

        try
        {
            FirebaseTaskDataList firebaseTaskList = JsonUtility.FromJson<FirebaseTaskDataList>(json);

            if (firebaseTaskList == null || firebaseTaskList.tasks == null || firebaseTaskList.tasks.Count == 0)
            {
                Debug.LogError("❌ FirebaseTaskDataList is null or contains no tasks.");
                return;
            }

            // Debug.Log($"✅ Loaded {firebaseTaskList.tasks.Count} Firestore tasks.");

            // Log each task's merged predecessor values.
            // foreach (var task in firebaseTaskList.tasks)
            // {
            //     Debug.Log($"📄 Task '{task.Name}' (UID: {task.UID}) Predecessors: {string.Join(", ", MergePredecessorIDs(task))}");
            // }

            ConvertFirebaseTasksToTaskData(firebaseTaskList.tasks);
        }
        catch (System.Exception ex)
        {
            Debug.LogError($"❌ JSON Parsing Error: {ex.Message}");
        }
    }

    /// <summary>
    /// Merges the two predecessor fields (PredecessorUIDs and Predecessors) into a single list.
    /// </summary>
    /// <param name="firebaseTask">The Firebase task data.</param>
    /// <returns>A merged list of predecessor IDs.</returns>
    private List<string> MergePredecessorIDs(FirebaseTaskData firebaseTask)
    {
        List<string> merged = new List<string>();

        if (firebaseTask.PredecessorUIDs != null)
            merged.AddRange(firebaseTask.PredecessorUIDs.Where(id => !string.IsNullOrEmpty(id)));

        if (firebaseTask.Predecessors != null)
            merged.AddRange(firebaseTask.Predecessors.Where(id => !string.IsNullOrEmpty(id)));

        return merged;
    }

    /// <summary>
    /// Converts Firebase task data into TaskData objects and sets up relationships.
    /// </summary>
    /// <param name="firebaseTasks">List of Firebase task data.</param>
    public void ConvertFirebaseTasksToTaskData(List<FirebaseTaskData> firebaseTasks)
    {
        // Clear any existing tasks and visuals.
        ClearOldData();

        List<TaskData> convertedTasks = new List<TaskData>();
        taskDictionary = new Dictionary<string, TaskData>();

        // Create TaskData objects for each Firebase task.
        foreach (var firebaseTask in firebaseTasks)
        {
            List<string> mergedPredecessors = MergePredecessorIDs(firebaseTask);
            // Debug.Log($"📌 Processing Task '{firebaseTask.Name}' (UID: {firebaseTask.UID}) Merged Predecessors: {string.Join(", ", mergedPredecessors)}");

            // Parse the Start and Finish values.
            DateTime startTime = DateTime.MinValue;
            DateTime finishTime = DateTime.MinValue;
            try
            {
                // Parse using DateTime.Parse (adjust format if needed)
                startTime = DateTime.Parse(firebaseTask.Start);
            }
            catch (System.Exception ex)
            {
                Debug.LogError($"Error parsing Start for task '{firebaseTask.Name}': {ex.Message}");
            }
            try
            {
                finishTime = DateTime.Parse(firebaseTask.Finish);
            }
            catch (System.Exception ex)
            {
                Debug.LogError($"Error parsing Finish for task '{firebaseTask.Name}': {ex.Message}");
            }

            TaskData task = new TaskData
            {
                UID = firebaseTask.UID,
                Name = firebaseTask.Name,
                Critical = firebaseTask.Critical,
                // Set Start and Finish as UTC (adjust if needed based on your project requirements)
                Start = DateTime.SpecifyKind(startTime, DateTimeKind.Utc),
                Finish = DateTime.SpecifyKind(finishTime, DateTimeKind.Utc),
                // Convert hours back to days (8 hours per day)
                Duration = firebaseTask.Duration,
                OutlineLevel = firebaseTask.OutlineLevel,
                WBS = firebaseTask.WBS,
                PredecessorUIDs = mergedPredecessors,
                Predecessors = new List<TaskData>(),
                Successors = new List<TaskData>(),
                Children = new List<TaskData>()
            };

            convertedTasks.Add(task);
            taskDictionary[task.UID] = task;
        }

        // Debug.Log("✅ All Firebase tasks converted. Now linking predecessors...");

        // Link each task to its predecessors and vice versa.
        foreach (var task in convertedTasks)
        {
            foreach (var predecessorUID in task.PredecessorUIDs)
            {
                if (taskDictionary.TryGetValue(predecessorUID, out TaskData predecessorTask))
                {
                    if (!task.Predecessors.Contains(predecessorTask))
                        task.Predecessors.Add(predecessorTask);
                    if (!predecessorTask.Successors.Contains(task))
                        predecessorTask.Successors.Add(task);

                    // Debug.Log($"✅ Linked '{task.Name}' to predecessor '{predecessorTask.Name}'.");
                }
                else
                {
                    Debug.LogWarning($"⚠ Task '{task.Name}' has a missing predecessor '{predecessorUID}'.");
                }
            }
        }

        // Debug.Log("✅ Finished linking predecessors. Now building parent-child hierarchy...");

        // Build the parent-child hierarchy.
        BuildParentChildHierarchy(convertedTasks);

        // Debug.Log("✅ Finished building parent-child hierarchy. Now sending tasks to TaskManager.");

        if (convertedTasks.Count > 0)
        {
            // After setting up _tasks and taskDictionary
            taskManager.SetTasks(convertedTasks);
            //taskVisualizer.CalculateTaskPositions(convertedTasks, taskDictionary);
            StartCoroutine(DrawDependencyLinesWithDelay(0.5f));
        }
    }

    /// <summary>
    /// Draws dependency lines between tasks after a delay.
    /// </summary>
    /// <param name="delay">Delay in seconds before drawing lines.</param>
    private System.Collections.IEnumerator DrawDependencyLinesWithDelay(float delay)
    {
        yield return new WaitForSeconds(delay);
        //taskVisualizer.DrawDependencyLines(taskDictionary, taskColumn, taskRow);
        taskVisualizer.DrawDependencyLines(taskDictionary);
    }

    /// <summary>
    /// Builds the parent-child hierarchy for tasks based on their WBS and OutlineLevel.
    /// </summary>
    /// <param name="tasks">List of tasks to process.</param>
    private void BuildParentChildHierarchy(List<TaskData> tasks)
    {
        // Order tasks by WBS.
        tasks = tasks.OrderBy(t => t.WBS).ToList();

        foreach (var task in tasks)
        {
            // If the task is not a root, assign a parent based on OutlineLevel and WBS.
            if (task.OutlineLevel > 1)
            {
                TaskData parentTask = tasks
                    .Where(t => t.OutlineLevel == task.OutlineLevel - 1 && string.Compare(t.WBS, task.WBS) < 0)
                    .LastOrDefault();

                if (parentTask != null)
                {
                    task.Parent = parentTask;
                    if (!parentTask.Children.Contains(task))
                        parentTask.Children.Add(task);
                    // Debug.Log($"✅ Task '{task.Name}' assigned parent '{parentTask.Name}' using WBS: {task.WBS}.");
                }
                else
                {
                    Debug.LogWarning($"⚠ Task '{task.Name}' has no valid parent.");
                }
            }
        }
    }

    /// <summary>
    /// Clears all existing task data and visuals.
    /// </summary>
    private void ClearOldData()
    {
        taskManager.ClearAllTasks();
        taskVisualizer.ClearAllVisuals();
        taskDictionary.Clear();
    }

    /// <summary>
    /// Converts a TaskData object into a FirebaseTaskData object for Firestore storage.
    /// </summary>
    /// <param name="task">The TaskData object to convert.</param>
    /// <returns>A FirebaseTaskData object.</returns>
    private FirebaseTaskData ConvertTaskDataToFirebaseTask(TaskData task)
    {
        return new FirebaseTaskData
        {
            Critical = task.Critical,
            UID = task.UID,
            Name = task.Name,
            Start = task.Start.ToString("yyyy-MM-ddTHH:mm:ss"),
            Finish = task.Finish.ToString("yyyy-MM-ddTHH:mm:ss"),
            Duration = task.Duration * 8, // Store duration in hours (8 hours per day)
            OutlineLevel = task.OutlineLevel,
            WBS = task.WBS,
            Predecessors = task.PredecessorUIDs,
            PredecessorUIDs = task.PredecessorUIDs
        };
    }
}
