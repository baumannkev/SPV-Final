using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

/// <summary>
/// Handles the computation of task positions in a grid layout based on dependencies and hierarchy.
/// </summary>
public class LayoutGrid
{
    private Dictionary<string, (int column, int row)> taskPositions; // Stores final positions for tasks by UID
    private Dictionary<string, TaskData> taskLookup; // Global lookup for tasks by UID

    public LayoutGrid()
    {
        taskPositions = new Dictionary<string, (int column, int row)>();
        taskLookup = new Dictionary<string, TaskData>();
    }

    #region Column Computation

    /// <summary>
    /// Computes each task’s column based on its dependencies and hierarchy.
    /// </summary>
    public void ComputeColumns(List<TaskData> rootTasks)
    {
        List<TaskData> allTasks = FlattenTasks(rootTasks);
        taskLookup = allTasks.GroupBy(t => t.UID)
                             .ToDictionary(g => g.Key, g => g.First());
        foreach (var task in allTasks)
        {
            if (task.Parent == null)
                task.ComputedColumn = 0;
            else if (task.PredecessorUIDs == null || task.PredecessorUIDs.Count == 0)
                task.ComputedColumn = task.Parent.ComputedColumn + 1;
            else
            {
                int maxPredCol = -1;
                foreach (var predId in task.PredecessorUIDs)
                {
                    if (taskLookup.ContainsKey(predId))
                        maxPredCol = Math.Max(maxPredCol, taskLookup[predId].ComputedColumn);
                }
                task.ComputedColumn = maxPredCol + 1;
            }
        }
    }

    #endregion

    #region Row Assignment

    /// <summary>
    /// Recursively lays out the subtree for a parent task and assigns rows.
    /// </summary>
    private int LayoutSubtree(TaskData parent, int parentRow)
    {
        if (!taskPositions.ContainsKey(parent.UID))
        {
            parent.ComputedRow = parentRow;
            taskPositions[parent.UID] = (parent.ComputedColumn, parentRow);
        }
        int subtreeMax = parent.ComputedRow;
        int lastUsedRow = parent.ComputedRow;

        if (parent.Children == null || parent.Children.Count == 0)
            return parent.ComputedRow;

        // Assume children are in desired order; if not, sort by WBS then UID.
        List<TaskData> children = parent.Children.OrderBy(c => c.WBS)
                                                    .ThenBy(c => c.UID)
                                                    .ToList();

        for (int i = 0; i < children.Count; i++)
        {
            TaskData child = children[i];
            int candidateRow = 0;

            if (i == 0)
            {
                // First child: align with parent's row.
                candidateRow = parentRow;
            }
            else
            {
                // Always find min row of all predecessors first
                int minPredRow = lastUsedRow;
                if(child.PredecessorUIDs != null && child.PredecessorUIDs.Count > 0){
                    var placedDeps = child.PredecessorUIDs
                                    .Where(id => taskPositions.ContainsKey(id))
                                    .Select(id => taskPositions[id].row);
                    
                    if(placedDeps.Any())
                        minPredRow = placedDeps.Min();
                }

                TaskData prevSibling = children[i - 1];
                bool dependsOnPrev = child.PredecessorUIDs.Contains(prevSibling.UID);
                if(dependsOnPrev && child.PredecessorUIDs.Count == 1){
                        candidateRow = taskPositions[prevSibling.UID].row;
                }
                else
                candidateRow = minPredRow + 1;

            }
                // Force candidate to be below parent's row only if it is less than parent's row.
            if (candidateRow < parentRow)
                candidateRow = parentRow + 1;
            

            child.ComputedRow = candidateRow;
            taskPositions[child.UID] = (child.ComputedColumn, candidateRow);
            lastUsedRow = Math.Max(lastUsedRow, candidateRow);
            subtreeMax = Math.Max(subtreeMax, candidateRow);

            int childSubtreeMax = LayoutSubtree(child, candidateRow);
            lastUsedRow = Math.Max(lastUsedRow, childSubtreeMax);
            subtreeMax = Math.Max(subtreeMax, childSubtreeMax);
        }
        return subtreeMax;
    }

    #endregion

    #region Final Layout and Compaction

    /// <summary>
    /// Flattens the hierarchical list of tasks into a single list.
    /// </summary>
    private List<TaskData> FlattenTasks(List<TaskData> tasks)
    {
        List<TaskData> flat = new List<TaskData>();
        foreach (var t in tasks)
        {
            flat.Add(t);
            if (t.Children != null && t.Children.Count > 0)
                flat.AddRange(FlattenTasks(t.Children));
        }
        return flat;
    }

    /// <summary>
    /// Compacts row numbers to ensure they are consecutive starting at 0.
    /// </summary>
    private void CompactRows()
    {
        var usedRows = taskPositions.Values.Select(pos => pos.row).Distinct().OrderBy(r => r).ToList();
        if (usedRows.Count == 0)
            return;
        Dictionary<int, int> mapping = new Dictionary<int, int>();
        for (int i = 0; i < usedRows.Count; i++)
            mapping[usedRows[i]] = i;
        foreach (var key in taskPositions.Keys.ToList())
        {
            var pos = taskPositions[key];
            taskPositions[key] = (pos.column, mapping[pos.row]);
        }
    }

    /// <summary>
    /// Computes the final layout of tasks, including columns and rows.
    /// </summary>
    public void ComputeFinalLayout(List<TaskData> rootTasks)
    {
        ComputeColumns(rootTasks);
        taskPositions.Clear();

        rootTasks.Sort((a, b) => string.Compare(a.UID, b.UID, StringComparison.Ordinal));
        int globalRow = 0;
        foreach (var root in rootTasks.Where(t => t.Parent == null))
        {
            root.ComputedRow = globalRow;
            taskPositions[root.UID] = (root.ComputedColumn, globalRow);
            int subtreeMax = LayoutSubtree(root, globalRow);
            globalRow = subtreeMax + 1;
        }
        CompactRows();
    }

    /// <summary>
    /// Retrieves the computed (column, row) position for a given task UID.
    /// </summary>
    public (int column, int row)? GetPosition(string uid)
    {
        return taskPositions.ContainsKey(uid) ? taskPositions[uid] : null;
    }

    /// <summary>
    /// Returns all computed positions for tasks.
    /// </summary>
    public Dictionary<string, (int column, int row)> GetAllPositions()
    {
        return taskPositions;
    }

    #endregion
}