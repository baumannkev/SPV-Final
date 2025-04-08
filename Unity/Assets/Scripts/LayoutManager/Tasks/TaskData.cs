using System;
using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// Represents the data structure for a task, including its properties and relationships.
/// </summary>
public class TaskData
{
    public string Critical { get; set; } // Indicates if the task is critical
    public string UID { get; set; } // Unique identifier for the task
    public string Name { get; set; } // Name of the task
    public string WBS { get; set; } // Work Breakdown Structure (WBS) code
    public DateTime Start { get; set; } // Start date of the task
    public DateTime Finish { get; set; } // Finish date of the task
    public double Duration { get; set; } // Duration of the task in days
    public int OutlineLevel { get; set; } // Outline level in the task hierarchy
    public int ComputedColumn { get; set; } // Computed column position in the grid
    public int ComputedRow { get; set; } // Computed row position in the grid
    public int row { get; set; } // Row position in the layout
    public TaskData Parent { get; set; } // Reference to the parent task
    public List<TaskData> Predecessors { get; set; } = new List<TaskData>(); // List of predecessor tasks
    public List<TaskData> Successors { get; set; } = new List<TaskData>(); // List of successor tasks
    public List<TaskData> Children { get; set; } = new List<TaskData>(); // List of child tasks
    public List<string> PredecessorUIDs { get; set; } = new List<string>(); // List of predecessor task UIDs

    public TaskItem TaskItem { get; set; } // Reference to the associated TaskItem
}