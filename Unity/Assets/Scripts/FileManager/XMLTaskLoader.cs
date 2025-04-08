using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using System.Xml;

public class XMLTaskLoader : MonoBehaviour
{
    public TaskManager taskManager;
    public TaskVisualizer taskVisualizer;

    private Dictionary<string, TaskData> taskDictionary = new Dictionary<string, TaskData>();
    private List<TaskData> _tasks = new List<TaskData>();
    private Dictionary<string, int> taskColumn = new Dictionary<string, int>();
    private Dictionary<string, int> taskRow = new Dictionary<string, int>();

    /// <summary>
    /// Loads tasks from an XML string, parses the data, and sets up the task hierarchy and dependencies.
    /// </summary>
    public void LoadTasksFromXML(string xmlContent)
    {
        ClearOldData();

        XmlDocument xmlDoc = new XmlDocument();
        xmlDoc.LoadXml(xmlContent);

        XmlNamespaceManager namespaceManager = new XmlNamespaceManager(xmlDoc.NameTable);
        namespaceManager.AddNamespace("ns", "http://schemas.microsoft.com/project");

        XmlNodeList taskNodes = xmlDoc.SelectNodes("//ns:Task", namespaceManager);
    
        if (taskNodes != null && taskNodes.Count > 0)
        {
            foreach (XmlNode taskNode in taskNodes)
            {
                string outlineLevelText = taskNode.SelectSingleNode("ns:OutlineLevel", namespaceManager)?.InnerText;

                if (!int.TryParse(outlineLevelText, out int outlineLevel) || outlineLevel == 0)
                {
                    continue; // Skip invalid or project name tasks
                }

                string wbs = taskNode.SelectSingleNode("ns:WBS", namespaceManager)?.InnerText ?? "";

                XmlNodeList predecessorNodes = taskNode.SelectNodes("ns:PredecessorLink/ns:PredecessorUID", namespaceManager);
                TaskData task = new TaskData
                {
                    UID = taskNode.SelectSingleNode("ns:UID", namespaceManager)?.InnerText,
                    Name = taskNode.SelectSingleNode("ns:Name", namespaceManager)?.InnerText,
                    Start = DateTime.TryParse(taskNode.SelectSingleNode("ns:Start", namespaceManager)?.InnerText, out var parsedStart) ? parsedStart : DateTime.MinValue,
                    Finish = DateTime.TryParse(taskNode.SelectSingleNode("ns:Finish", namespaceManager)?.InnerText, out var parsedFinish) ? parsedFinish : DateTime.MinValue,
                    Critical = taskNode.SelectSingleNode("ns:Critical", namespaceManager)?.InnerText,
                    Duration = XmlConvert.ToTimeSpan(taskNode.SelectSingleNode("ns:Duration", namespaceManager)?.InnerText ?? "PT0H0M0S").TotalHours / 8,
                    OutlineLevel = outlineLevel,
                    WBS = wbs
                };
                // Debug.Log(task.Duration);
                foreach (XmlNode predecessorNode in predecessorNodes)
                {
                    if (!string.IsNullOrEmpty(predecessorNode.InnerText))
                    {
                        task.PredecessorUIDs.Add(predecessorNode.InnerText);
                    }
                }

                if (!string.IsNullOrEmpty(task.Name))
                {
                    _tasks.Add(task);
                    taskDictionary[task.UID] = task;
                }
            }

            BuildParentChildHierarchy(_tasks);

            if (_tasks.Count > 0)
            {
                // After setting up _tasks and taskDictionary
                taskManager.SetTasks(_tasks);
                //taskManager.CreateTaskPanels(_tasks);
                //taskVisualizer.CalculateTaskPositions(_tasks, taskDictionary);

                StartCoroutine(DrawDependencyLinesWithDelay(0.5f));
            }
        }
        else
        {
            Debug.LogWarning("No task nodes found in XML.");
        }
    }

    /// <summary>
    /// Builds the parent-child hierarchy and predecessor-successor relationships for the tasks.
    /// </summary>
    private void BuildParentChildHierarchy(List<TaskData> tasks)
    {
        tasks = tasks.OrderBy(t => t.WBS).ToList();

        foreach (var task in tasks)
        {
            // Assign parent based on WBS and OutlineLevel
            if (task.OutlineLevel > 1)
            {
                TaskData parentTask = tasks
                    .Where(t => t.OutlineLevel == task.OutlineLevel - 1 && string.Compare(t.WBS, task.WBS) < 0)
                    .LastOrDefault();

                if (parentTask != null)
                {
                    task.Parent = parentTask;
                    parentTask.Children.Add(task);
                    // Debug.Log($"Task '{task.Name}' assigned parent '{parentTask.Name}' based on OutlineLevel and WBS.");
                }
            }

            foreach (var predecessorUID in task.PredecessorUIDs)
            {
                TaskData predecessorTask = tasks.FirstOrDefault(t => t.UID == predecessorUID);
                if (predecessorTask != null)
                {
                    if (!predecessorTask.Successors.Contains(task))
                    {
                        predecessorTask.Successors.Add(task);
                        task.Predecessors.Add(predecessorTask);
                        // Debug.Log($"Task '{task.Name}' added to successors of '{predecessorTask.Name}' based on PredecessorLink.");
                    }
                }
            }
        }
    }

    /// <summary>
    /// Coroutine to delay the drawing of dependency lines to ensure all tasks are properly initialized.
    /// </summary>
    private System.Collections.IEnumerator DrawDependencyLinesWithDelay(float delay)
    {
        yield return new WaitForSeconds(delay);
        //taskVisualizer.DrawDependencyLines(taskDictionary, taskColumn, taskRow);
        taskVisualizer.DrawDependencyLines(taskDictionary);
    }

    /// <summary>
    /// Clears all previously loaded task data and visuals.
    /// </summary>
    private void ClearOldData()
    {
        taskManager.ClearAllTasks();
        taskVisualizer.ClearAllVisuals();
        _tasks.Clear();
        taskDictionary.Clear();
        taskColumn.Clear();
        taskRow.Clear();
    }
}
