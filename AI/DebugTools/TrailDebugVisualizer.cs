// Assets/Debug/TrailDebugVisualizer.cs
// Draws gizmos for all trail paths stored in TrailGenerator results

using System.Collections.Generic;
using UnityEngine;
using PlexiPark.Data.Trail;
using PlexiPark.Core.SharedEnums;
using PlexiPark.AI.TrailGeneration;
using PlexiPark.Systems.Trail;

[ExecuteAlways]
public class TrailDebugVisualizer : MonoBehaviour
{
    [Header("Visualization Settings")]
    public Color defaultTrailColor = Color.red;
    public float nodeSize = 2f;
    public bool drawAnchors = true;

    [HideInInspector] public List<TrailPlan> Trails = new();

    private Dictionary<TrailType, Color> trailTypeColors = new()
    {
        { TrailType.HikingTrail, Color.green },
        { TrailType.MountainTrail, new Color(0.5f, 0.25f, 0.1f) }, // brown
        { TrailType.BikePath, Color.blue },
        { TrailType.WalkPath, Color.yellow }
    };

private void OnDrawGizmos()
{
    if (!Application.isPlaying) return;
    if (Trails == null || Trails.Count == 0) return;

    // half-cell offset so your cube/sphere sits in the centre of each cell
    float halfCell = nodeSize ;

    foreach (var plan in Trails)
    {
        // pick a color per trail type
        Gizmos.color = trailTypeColors.TryGetValue(plan.Type, out var c) 
                           ? c 
                           : defaultTrailColor;

        // draw each cell in the path
        foreach (var cell in plan.Path)
        {
            // if cell is local to this chunk, you need to add chunkOrigin:
            // Vector2Int worldCell = cell + chunkOrigin;
            // float cellX = worldCell.x;
            // float cellY = worldCell.y;

            // otherwise assume cell.x/ cell.y are already global grid coordinates:
            float cellX = cell.x;
            float cellY = cell.y;

            // now convert grid→world (meters)
            Vector3 worldPos = new Vector3(
                cellX * nodeSize + halfCell,
                1.1f,                                   // same height you were using
                cellY * nodeSize + halfCell
            );

            Gizmos.DrawCube(worldPos, Vector3.one * nodeSize * 0.2f);
        }

        if (!drawAnchors) 
            continue;

        // draw your anchors semi-transparent
        Gizmos.color = new Color(Gizmos.color.r, Gizmos.color.g, Gizmos.color.b, 0.5f);
        foreach (var anchor in plan.Anchors)
        {
            float ax = anchor.x;
            float ay = anchor.y;

            Vector3 worldPos = new Vector3(
                ax * nodeSize + halfCell,
                1.3f,
                ay * nodeSize + halfCell
            );
            Gizmos.DrawSphere(worldPos, nodeSize * 0.3f);
        }
    }
}

}
