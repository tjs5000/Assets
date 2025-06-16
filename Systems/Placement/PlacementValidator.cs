using UnityEngine;
using PlexiPark.Data;
using PlexiPark.Managers;

namespace PlexiPark.Systems.Placement
{
    /// <summary>
    /// Validates whether a ParkObjectData can be placed at a given grid origin.
    /// </summary>
    public static class PlacementValidator
    {
        public static bool IsValid(ParkObjectData data, Vector2Int origin)
        {
            if (data == null || data.Footprint == null || data.AllowedSlopes == null)
                return false;

            int width  = GridManager.Instance.GridWidth;
            int depth  = GridManager.Instance.GridDepth;
            foreach (var offset in data.Footprint)
            {
                Vector2Int coord = origin + offset;
                if (coord.x < 0 || coord.x >= width || coord.y < 0 || coord.y >= depth)
                    return false;
                if (GridManager.Instance.IsCellOccupied(coord))
                    return false;
                var cell = GridManager.Instance.GetCell(coord);
                if (!data.AllowedSlopes.Contains(cell.slope))
                    return false;
            }

            return ParkTypeRules.Instance.CanPlaceObject(data);
        }
    }
}