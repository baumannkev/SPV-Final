using System;
using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// Represents a list of Firebase task data objects for JSON parsing.
/// </summary>
[Serializable]
public class FirebaseTaskDataList
{
    /// <summary>
    /// List of tasks fetched from Firebase Firestore.
    /// </summary>
    public List<FirebaseTaskData> tasks = new List<FirebaseTaskData>(); // 🔹 Ensures correct JSON parsing
}