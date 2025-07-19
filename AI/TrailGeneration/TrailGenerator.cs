// Assets/AI/TrailGeneration/TrailGenerator.cs
// Purpose: Stateless static class for drawing trails with directional retry logic and post-processing

using System;
using System.Collections.Generic;
using UnityEngine;
using PlexiPark.Core.Interfaces;
using PlexiPark.Core.SharedEnums;
using PlexiPark.Data.Trail;
using PlexiPark.Terrain.Rendering;
using PlexiPark.Systems.Trail;

namespace PlexiPark.AI.TrailGeneration
{
    public static class TrailGenerator
    {
        private const int StartAnchorRadius = 15;

        public static List<TrailPlan> GenerateFromGoal(
            TrailGoal goal,
            Dictionary<Vector2Int, TerrainChunk> chunkMap,
            int chunkSize,
            ITrailPaintTarget painter,
            IGridInfoProvider grid,
            ITrailSegmentClassifier classifier)
        {
            System.Random rng = goal.RandomSeed != 0 ? new System.Random(goal.RandomSeed) : new System.Random();
            HashSet<Vector2Int> blocked = new();
            List<TrailPlan> results = new();
            List<TrailPlan> partials = new();

            int generated = 0;
            int attempts = 0;
            int maxAttempts = goal.TargetTrailCount * 10;
            int minCellsPerSegment = goal.MinCellsPerSegment > 0 ? goal.MinCellsPerSegment : 3;

            var candidateTrailTypes = goal.AllowedTypes ?? new List<TrailType>();
            if (candidateTrailTypes.Count == 0)
                candidateTrailTypes = BuildAllValidTrailTypes();

            Vector2Int center = ComputeTerrainCenter(chunkMap, chunkSize);

            while (generated < goal.TargetTrailCount && attempts < maxAttempts)
            {
                attempts++;

                TrailType trailType = candidateTrailTypes[rng.Next(candidateTrailTypes.Count)];

                // Replace anchor selection with constrained point near center
                Vector2Int anchor;

                if (goal.StartAnchors != null && generated < goal.StartAnchors.Count)
                {
                    anchor = goal.StartAnchors[generated];

                    if (!grid.IsWithinBounds(anchor) ||
                        grid.IsCellOccupied(anchor) ||
                        !grid.IsValidTrailCell(anchor, trailType))
                    {
                        Debug.LogWarning($"🚫 Invalid explicit anchor {anchor} for {trailType}, skipping...");
                        continue;
                    }
                }
                else
                {
                    Vector2Int? maybeAnchor = TryGetStartAnchorNearCenter(center, StartAnchorRadius, grid, trailType, rng);
                    if (!maybeAnchor.HasValue) continue;
                    anchor = maybeAnchor.Value;
                }


                int numAnchors = rng.Next(goal.MinAnchorsPerTrail, goal.MaxAnchorsPerTrail + 1);

                TrailPlan plan = new() { Type = trailType };
                bool trailAbandoned = false;

                for (int a = 0; a < numAnchors; a++)
                {
                    List<Vector2Int> directions = new() { Vector2Int.up, Vector2Int.down, Vector2Int.left, Vector2Int.right };
                    Shuffle(rng, directions);

                    bool segmentPlaced = false;
                    int requiredLength = rng.Next(goal.MinCellsPerSegment, goal.MaxCellsPerSegment + 1);

                    while (!segmentPlaced && requiredLength >= 1)
                    {
                        foreach (var dir in directions)
                        {
                            List<Vector2Int> segment = new();
                            Vector2Int cursor = anchor;

                            for (int i = 0; i < requiredLength; i++)
                            {
                                cursor += dir;

                                if (!grid.IsWithinBounds(cursor) ||
                                    blocked.Contains(cursor) ||
                                    grid.IsCellOccupied(cursor) ||
                                    !grid.IsValidTrailCell(cursor, trailType))
                                {
                                    segment.Clear();
                                    break;
                                }

                                segment.Add(cursor);
                            }

                            if (segment.Count == requiredLength)
                            {
                                plan.Anchors.Add(anchor);
                                foreach (var cell in segment)
                                {
                                    painter.PaintTrailSplat(cell, trailType);
                                    plan.Path.Add(cell);

                                    blocked.Add(cell);
                                }

                                anchor = segment[^1]; // new anchor = segment end
                                segmentPlaced = true;
                                break;
                            }
                        }

                        if (!segmentPlaced)
                            requiredLength--;
                    }

                    if (!segmentPlaced)
                    {
                        trailAbandoned = true;
                        break;
                    }
                }

                if (!trailAbandoned)
                {
                    ClassifyPathSegments(plan, trailType, classifier, painter);
                    Debug.Log($"✅ Trail generated: {trailType}, Anchors: {plan.Anchors.Count}, Cells: {plan.Path.Count}");
                    results.Add(plan);
                    generated++;
                    continue;
                }
                else if (plan.Path.Count > 0 && goal.AllowPartialTrails)
                {
                    ClassifyPathSegments(plan, trailType, classifier, painter);
                    Debug.LogWarning($"⚠️ Partial trail accepted: {trailType}, Cells: {plan.Path.Count}");
                    results.Add(plan);
                    generated++;
                    continue;
                }
                else if (plan.Path.Count > 0)
                {
                    Debug.Log($"❌ Trail discarded after partial progress: {trailType}, Cells={plan.Path.Count}");
                    partials.Add(plan);
                    continue;
                }

            }

#if UNITY_EDITOR
            var visualizer = UnityEngine.Object.FindFirstObjectByType<TrailDebugVisualizer>();
            if (visualizer != null)
                visualizer.Trails = results;
#endif

            Debug.Log($"🧾 Trail Generation Complete — Success: {results.Count}, Partial: {partials.Count}, Attempts: {attempts}");
            return results;
        }

        private static Vector2Int ComputeTerrainCenter(Dictionary<Vector2Int, TerrainChunk> chunkMap, int chunkSize)
        {
            Vector2Int min = new(int.MaxValue, int.MaxValue);
            Vector2Int max = new(int.MinValue, int.MinValue);

            foreach (var chunk in chunkMap.Keys)
            {
                min = Vector2Int.Min(min, chunk);
                max = Vector2Int.Max(max, chunk + new Vector2Int(chunkSize - 1, chunkSize - 1));
            }

            return (min + max) / 2;
        }

        private static Vector2Int? TryGetStartAnchorNearCenter(Vector2Int center, int radius, IGridInfoProvider grid, TrailType trailType, System.Random rng)
        {
            const int maxTries = 30;

            for (int i = 0; i < maxTries; i++)
            {
                int dx = rng.Next(-radius, radius + 1);
                int dy = rng.Next(-radius, radius + 1);
                Vector2Int candidate = center + new Vector2Int(dx, dy);

                if (!grid.IsWithinBounds(candidate)) continue;
                if (grid.IsCellOccupied(candidate)) continue;
                if (!grid.IsValidTrailCell(candidate, trailType)) continue;

                return candidate;
            }

            return null; // fallback if no good point found
        }

        private static void ClassifyPathSegments(TrailPlan plan, TrailType type, ITrailSegmentClassifier classifier, ITrailPaintTarget painter)
        {
            for (int i = 0; i < plan.Path.Count; i++)
            {
                Vector2Int? prev = (i > 0) ? plan.Path[i - 1] : null;
                Vector2Int current = plan.Path[i];
                Vector2Int? next = (i < plan.Path.Count - 1) ? plan.Path[i + 1] : null;

                var (segmentType, rotationID) = classifier.GetSegmentInfo(
                    prev ?? current,
                    current,
                    next ?? current,
                    type
                );

                painter.PaintSegmentMap(current, segmentType, rotationID);
            }
        }

        private static void Shuffle<T>(System.Random rng, List<T> list)
        {
            int n = list.Count;
            while (n > 1)
            {
                int k = rng.Next(n--);
                (list[n], list[k]) = (list[k], list[n]);
            }
        }

        private static List<TrailType> BuildAllValidTrailTypes()
        {
            List<TrailType> valid = new();
            foreach (TrailType type in Enum.GetValues(typeof(TrailType)))
            {
                if (type != TrailType.None)
                    valid.Add(type);
            }
            return valid;
        }
    }
}
