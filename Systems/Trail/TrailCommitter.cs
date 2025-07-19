using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using PlexiPark.Core.SharedEnums;
using PlexiPark.Managers;
using PlexiPark.Systems.Trail;
using PlexiPark.Systems.Visitor;

namespace PlexiPark.Systems.Trail
{
    /// <summary>
    /// Shared “commit” logic for both AI‐ and user‐drawn trails.
    /// </summary>
    public static class TrailCommitter
    {
        public static void CommitTrail(TrailPlan plan)
        {
            Debug.Log("**** Enter [CommitTrail]");
            var grid = GridManager.Instance;
            var overlay = Object.FindFirstObjectByType<TrailOverlayRenderer>();
            if (overlay == null)
                Debug.LogError("TrailCommitter: no TrailOverlayRenderer found in scene");       // your singleton overlay
            var terrain = GridTerrainRenderer.Instance;    // your terrain splat painter

            // 1) paint splats & segments
            foreach (var cell in plan.Path)
            {
                // update backing data
                var data = grid.GetCell(cell);
                var (segType, rotationID) = TrailSegmentClassifier.GetSegmentInfo(cell, plan.Type);
                data.Trail = plan.Type;
                data.SegmentType = segType;
                grid.SetCell(cell, data);

                // paint
                overlay?.ClearTrailGhost(cell);
                terrain.PaintTrailSplat(cell, plan.Type);
                terrain.PaintSegmentMap(cell, segType, rotationID);
            }
            Debug.Log("**** Calling [ApplyAllSplats] from [CommitTrail]");
            terrain.ApplyAllSplats();

            // 2) spawn the Trailhead
            TrailheadSystem.Instance.CreateTrailheadAt(plan.Path.First(), plan.Type, plan.Path);


            Vector2Int first = plan.Path[0];
            Vector2Int second = plan.Path.Count > 1 ? plan.Path[1] : first;


            Vector2Int backDir = first - second;      // e.g. if second=(5,5), first=(6,5) then backDir=(1,0)
            Vector2Int before = first + backDir;     // e.g. (6,5)+(1,0) = (7,5)

            // 3a) build a List<Vector2Int> that *starts* with the trail-head cell
            var fullPath = new List<Vector2Int>();
            fullPath.Add(before);      // the “one before” cell
            fullPath.AddRange(plan.Path);         // <-- the rest of the cells

            // 3b) turn *that* into world-space
            var spine = fullPath
              .Select(c => grid.GetWorldCenter(c))
              .ToList();

            //    3c) register & bake
            Debug.Log("**** Calling [RegisterTrail] from [CommitTrail]");
            TrailNavMeshManager.Instance.RegisterTrail(plan.GetHashCode(), spine);
            TrailNavMeshManager.Instance.BuildAllRibbonNav();
            PopulationController.Instance.RecalculateTickets();

        }
    }
}
