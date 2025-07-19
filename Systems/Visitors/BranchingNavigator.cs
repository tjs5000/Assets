using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using PlexiPark.Core.SharedEnums;
using PlexiPark.Managers;

public class BranchingNavigator
{
    const int LOOKAHEAD = 3;
    const int HISTORY_SIZE = 3;

    private readonly GridManager _grid = GridManager.Instance;
    private readonly TrailPreferenceData _prefs;
    private readonly Queue<Vector2Int> _history = new Queue<Vector2Int>();

    public BranchingNavigator(TrailPreferenceData prefs)
    {
        _prefs = prefs;
    }

    public Vector2Int GetNextCell(Vector2Int current)
    {
        var branches = _grid.GetAdjacentTrailCells(current).ToList();
        if (branches.Count == 0) return current;

        // --- NEW: drop branches that were visited in the last N steps
        var freshBranches = branches
            .Where(b => !_history.Contains(b))
            .ToList();

        // if we removed everything (dead-end), fall back to originals
        if (freshBranches.Count == 0)
            freshBranches = branches;

        branches = freshBranches;

        // compute weight for each branch
        var weights = new float[branches.Count];
        for (int i = 0; i < branches.Count; i++)
        {
            var b = branches[i];
            float cost = ComputeCost(current, b);
            float baseW = _prefs.GetWeight(_grid.GetCell(b).Trail);
            float w = baseW + 1f / (cost + 0.1f);
            // if (_history.Contains(b)) w *= 0.5f; // penalty for recent
            weights[i] = w;
        }

        // weighted random pick
        float sum = weights.Sum();
        float r = Random.value * sum;
        for (int i = 0; i < branches.Count; i++)
        {
            r -= weights[i];
            if (r <= 0f)
            {
                Remember(branches[i]);
                return branches[i];
            }
        }

        // fallback
        Remember(branches.Last());
        return branches.Last();
    }

    private float ComputeCost(Vector2Int start, Vector2Int branch)
    {
        var dir = branch - start;
        var pos = branch;
        float c = 0f;
        for (int i = 0; i < LOOKAHEAD; i++)
        {
            var cell = _grid.GetCell(pos);
            c += SlopeCost(cell.slope) * _grid.CellSize;
            pos += dir;
            if (_grid.GetCell(pos).Trail == TrailType.None) break;
        }
        return c;
    }

    private float SlopeCost(SlopeType s) => s switch
    {
        SlopeType.Flat => 1f,
        SlopeType.Gentle => 2f,
        SlopeType.Steep => 4f,
        SlopeType.Cliff => 8f,
        _ => 1f
    };

    private void Remember(Vector2Int cell)
    {
        _history.Enqueue(cell);
        if (_history.Count > HISTORY_SIZE)
            _history.Dequeue();
    }

    public bool WasRecentlyVisited(Vector2Int cell) => _history.Contains(cell);
}
