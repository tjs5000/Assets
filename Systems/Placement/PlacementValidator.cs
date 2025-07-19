// Assets/Systems/Placement/PlacementValidator.cs
// -------------------------------------------
// Validates placement of objects based on slope, occupancy, and corner rules
// -------------------------------------------

using UnityEngine;
using PlexiPark.Data;
using PlexiPark.Managers;
using System.Linq;
using PlexiPark.Terrain.Data;
using PlexiPark.Core.SharedEnums;

namespace PlexiPark.Systems.Placement
{
    public static class PlacementValidator
    {
        public static bool IsValid(ParkObjectData data, Vector2Int origin)
        {
            if (data == null || data.Footprint == null || data.AllowedSlopes == null)
                return false;

            var grid = GridManager.Instance;
            var cornerMap = grid.CornerMap;

            foreach (var offset in data.Footprint)
            {
                Vector2Int coord = origin + offset;

                if (!grid.IsWithinBounds(coord)) return false;
                if (grid.IsCellOccupied(coord)) return false;

                var cell = grid.GetCell(coord);
                if (!data.AllowedSlopes.Contains(cell.slope)) return false;

                // Optional per-object placement rules
             /*    if (data.RequiresCornerAdjacencyRule)
                {
                    if (!HasAtLeastTwoEqualAdjacentCorners(coord, cornerMap))
                        return false;
                } */
            }

            return ParkTypeRules.Instance.CanPlaceObject(data);
        }

        /// <summary>
        /// Returns true if at least 2 adjacent corners of the given cellCoord are the same height.
        /// </summary>
        private static bool HasAtLeastTwoEqualAdjacentCorners(Vector2Int cellCoord, CornerHeightMap map)
        {
            var corners = map.GetCellCornerKeys(cellCoord);
            float[] heights = corners.Select(map.GetCorner).ToArray();

            // Check all 6 combinations of 2 corners
            int matchCount = 0;
            for (int i = 0; i < heights.Length; i++)
            {
                for (int j = i + 1; j < heights.Length; j++)
                {
                    if (Mathf.Approximately(heights[i], heights[j]))
                        matchCount++;
                }
            }

            return matchCount >= 1; // 1 match = 2 same-height corners
        }
    }
}
