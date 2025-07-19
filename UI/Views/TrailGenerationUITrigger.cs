// Assets/UI/TrailGenerationUITrigger.cs
// Runtime UI trigger to generate AI trails with optional feedback

using System.Collections.Generic;
using UnityEngine;
using PlexiPark.AI.TrailGeneration;
using PlexiPark.Core.Interfaces;
using PlexiPark.Data.Trail;
using PlexiPark.Terrain.Rendering;
using PlexiPark.Core.SharedEnums;
using TMPro;
using PlexiPark.Systems.Trail;

public class TrailGenerationUITrigger : MonoBehaviour
{
    [Header("Trail Generation Settings")]
    public TrailGoal trailGoal;

    [Tooltip("All terrain chunks to be passed into the generator.")]
    public List<TerrainChunk> terrainChunks; //Terrain/Rendering/TerrainChunk.cs

    [Tooltip("Chunk size in grid cells (must match terrain mesh init).")]
    public int chunkSize = 16;

    [Header("Interface References")]
    public MonoBehaviour painterReference;       // Must implement ITrailPaintTarget [TerrainRenderer]
    public MonoBehaviour gridReference;          // Must implement IGridInfoProvider [GridManager]
    public MonoBehaviour classifierReference;    // Must implement ITrailSegmentClassifier [TerrainClassifier]

    [Header("Optional Debug Output")]
    public TextMeshProUGUI debugTextUI; // Optional runtime log display

    public void TriggerTrailGeneration()
    {
        // 1) Painter check
        if (painterReference == null)
        {
            DebugOutput("❌ Painter Reference is not assigned.");
            return;
        }
        if (painterReference is not ITrailPaintTarget painter)
        {
            DebugOutput($"❌ Painter Reference assigned to a '{painterReference.GetType().Name}' which does not implement ITrailPaintTarget.");
            return;
        }

        // 2) Grid check
        if (gridReference == null)
        {
            DebugOutput("❌ Grid Reference is not assigned.");
            return;
        }
        if (gridReference is not IGridInfoProvider grid)
        {
            DebugOutput($"❌ Grid Reference assigned to a '{gridReference.GetType().Name}' which does not implement IGridInfoProvider.");
            return;
        }

        // 3) Classifier check
        if (classifierReference == null)
        {
            DebugOutput("❌ Classifier Reference is not assigned.");
            return;
        }
        if (classifierReference is not ITrailSegmentClassifier classifier)
        {
            DebugOutput($"❌ Classifier Reference assigned to a '{classifierReference.GetType().Name}' which does not implement ITrailSegmentClassifier.");
            return;
        }

        // 4) Other sanity checks
        if (trailGoal == null)
        {
            DebugOutput("❌ TrailGoal is not assigned.");
            return;
        }
        if (terrainChunks == null || terrainChunks.Count == 0)
        {
            DebugOutput("❌ TerrainChunks list is empty or unassigned.");
            return;
        }


        List<TrailPlan> trails = TrailGenerationHelper.GenerateTrails(
            trailGoal,
            terrainChunks,
            chunkSize,
            painter,
            grid,
            classifier);
    }


    private void DebugOutput(string msg)
    {
        Debug.Log(msg);
        if (debugTextUI != null)
            debugTextUI.text = msg;
    }
}
