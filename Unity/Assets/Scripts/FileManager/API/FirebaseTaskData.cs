using System;
using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// Represents a single task fetched from Firebase Firestore.
/// </summary>
[Serializable]
public class FirebaseTaskData
{
    /// <summary>
    /// Indicates whether the task is critical.
    /// </summary>
    public string Critical;

    /// <summary>
    /// Unique identifier for the task.
    /// </summary>
    public string UID;

    /// <summary>
    /// Name of the task.
    /// </summary>
    public string Name;

    /// <summary>
    /// Outline level of the task in the hierarchy.
    /// </summary>
    public int OutlineLevel;

    /// <summary>
    /// Work Breakdown Structure (WBS) code for the task.
    /// </summary>
    public string WBS;

    /// <summary>
    /// Start date and time of the task in string format.
    /// </summary>
    public string Start;

    /// <summary>
    /// Finish date and time of the task in string format.
    /// </summary>
    public string Finish;

    /// <summary>
    /// Duration of the task in hours.
    /// </summary>
    public double Duration;

    /// <summary>
    /// List of predecessor task IDs.
    /// </summary>
    public List<string> Predecessors; 

    /// <summary>
    /// List of predecessor task UIDs.
    /// </summary>
    public List<string> PredecessorUIDs;
}