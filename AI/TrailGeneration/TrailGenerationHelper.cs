// Assets/AI/TrailGeneration/TrailGenerationHelper.cs
using System.Collections.Generic;
using UnityEngine;
using System.Linq;
using PlexiPark.AI.TrailGeneration;
using PlexiPark.Core.Interfaces;
using PlexiPark.Data.Trail;
using PlexiPark.Terrain.Rendering;
using PlexiPark.Core.SharedEnums;
using PlexiPark.Systems.Trail;
using Unity.AI.Navigation;
using PlexiPark.Core.Utils;
using PlexiPark.Managers;

namespace PlexiPark.AI.TrailGeneration
{
    public static class TrailGenerationHelper
    {
        public static List<TrailPlan> GenerateTrails(
            TrailGoal goal,
            List<TerrainChunk> terrainChunks,
            int chunkSize,
            ITrailPaintTarget painter,
            IGridInfoProvider grid,
            ITrailSegmentClassifier classifier
            )
        {
            
            if (goal == null)
            {
                Debug.Log("❌ TrailGoal is null.");
                return new();
            }

            if (terrainChunks == null || terrainChunks.Count == 0)
            {
                Debug.Log("❌ No TerrainChunks assigned.");
                return new();
            }

            if (painter == null || grid == null || classifier == null)
            {
                Debug.Log("❌ Missing one or more required interface references.");
                return new();
            }

            // build chunkMap …
            var chunkMap = new Dictionary<Vector2Int, TerrainChunk>();
            foreach (var chunk in terrainChunks)
                if (chunk != null)
                    chunkMap[chunk.GetChunkOrigin()] = chunk;

            // run the AI
            List<TrailPlan> plans = TrailGenerator.GenerateFromGoal(
                goal, chunkMap, chunkSize, painter, grid, classifier);

            // build the debug summary
            Debug.Log($"✅ Generated {plans.Count} trail(s):\n" +
                           string.Join("\n", plans
                             .Where(p => p.Anchors.Count > 0)
                             .Select(p => $"- {p.Type} | Anchors: {p.Anchors.Count} | Path: {p.Path.Count} cells")));

            // spawn & initialize one head per plan
            if (TrailheadSystem.Instance == null)
            {
                Debug.LogWarning("No TrailheadSystem in scene – can’t spawn trailheads!");
            }
            else
            {
                 Debug.Log("**** Start [CommitTrail] from [TrailGenerationHelper]");
                foreach (var plan in plans)
                { Debug.Log("##**** Call [CommitTrail] from [TrailGenerationHelper]");
                    TrailCommitter.CommitTrail(plan);
                }
            }
         
            return plans;
        }
    }
}
