// Assets/Systems/Trail/TrailSegmentClassifier.cs
// -------------------------------------------------------------
// Determines the segment type and orientation of a trail cell
// based on its cardinal neighbors and trail type.
// -------------------------------------------------------------

using UnityEngine;
using PlexiPark.Data;
using PlexiPark.Managers;
using PlexiPark.Core.SharedEnums;


namespace PlexiPark.Systems.Trail
{
    public static class TrailSegmentClassifier
    {
        /// <summary>
        /// Analyzes the 4 cardinal neighbors of a cell to determine
        /// its TrailSegmentType and rotationID (0–3).
        /// </summary>
        public static (TrailSegmentType, int rotationID) GetSegmentInfo(Vector2Int cell, TrailType trailType)
        {
            bool north = IsSameTrailType(cell + Vector2Int.up, trailType);
            bool south = IsSameTrailType(cell + Vector2Int.down, trailType);
            bool east = IsSameTrailType(cell + Vector2Int.right, trailType);
            bool west = IsSameTrailType(cell + Vector2Int.left, trailType);

            int count = (north ? 1 : 0) + (south ? 1 : 0) + (east ? 1 : 0) + (west ? 1 : 0);

            // Determine segment type and rotation
            if (count == 1)
            {
                if (north) return (TrailSegmentType.End, 0);
                if (east) return (TrailSegmentType.End, 1);
                if (south) return (TrailSegmentType.End, 2);
                if (west) return (TrailSegmentType.End, 3);
            }

            if (count == 2)
            {
                if (north && south) return (TrailSegmentType.Straight, 0);
                if (east && west) return (TrailSegmentType.Straight, 1);
                if (north && east) return (TrailSegmentType.Corner, 0);
                if (east && south) return (TrailSegmentType.Corner, 1);
                if (south && west) return (TrailSegmentType.Corner, 2);
                if (west && north) return (TrailSegmentType.Corner, 3);
            }

            if (count == 3)
            {
                if (!north) return (TrailSegmentType.TIntersection, 0);
                if (!east) return (TrailSegmentType.TIntersection, 1);
                if (!south) return (TrailSegmentType.TIntersection, 2);
                if (!west) return (TrailSegmentType.TIntersection, 3);
            }

            if (count == 4)
            {
                return (TrailSegmentType.CrossIntersection, 0);
            }

            // Fallback
            return (TrailSegmentType.End, 0);
        }

        public static (TrailSegmentType, int) GetSegmentInfo(Vector2Int? prev, Vector2Int current, Vector2Int? next, TrailType trailType)
        {
            Vector2Int inDir = prev.HasValue ? ClampToCardinal(current - prev.Value) : Vector2Int.zero;
            Vector2Int outDir = next.HasValue ? ClampToCardinal(next.Value - current) : Vector2Int.zero;

            // Case: Single end cap
            if (!prev.HasValue && next.HasValue)
                return GetSegmentInfoBetween(current, next.Value, trailType);

            if (prev.HasValue && !next.HasValue)
                return GetSegmentInfoBetween(prev.Value, current, trailType);

            // Case: Middle segment
            if (inDir == outDir)
            {
                if (inDir == Vector2Int.up || inDir == Vector2Int.down)
                    return (TrailSegmentType.Straight, 0); // Vertical
                if (inDir == Vector2Int.left || inDir == Vector2Int.right)
                    return (TrailSegmentType.Straight, 1); // Horizontal
            }

            // Corner logic
            if (inDir == Vector2Int.up && outDir == Vector2Int.right) return (TrailSegmentType.Corner, 0);
            if (inDir == Vector2Int.right && outDir == Vector2Int.down) return (TrailSegmentType.Corner, 1);
            if (inDir == Vector2Int.down && outDir == Vector2Int.left) return (TrailSegmentType.Corner, 2);
            if (inDir == Vector2Int.left && outDir == Vector2Int.up) return (TrailSegmentType.Corner, 3);

            if (inDir == Vector2Int.right && outDir == Vector2Int.up) return (TrailSegmentType.Corner, 3);
            if (inDir == Vector2Int.down && outDir == Vector2Int.right) return (TrailSegmentType.Corner, 0);
            if (inDir == Vector2Int.left && outDir == Vector2Int.down) return (TrailSegmentType.Corner, 1);
            if (inDir == Vector2Int.up && outDir == Vector2Int.left) return (TrailSegmentType.Corner, 2);

            // Fallback
            return (TrailSegmentType.End, 0);
        }


        public static (TrailSegmentType segment, int rotation) GetSegmentInfoBetween(Vector2Int from, Vector2Int to, TrailType trailType)
        {
            Debug.Log($"## [GetSegmentInfoBetween] called with: {from} to {to} and is a {trailType}");
            Vector2Int delta = to - from;

            if (Mathf.Abs(delta.x) + Mathf.Abs(delta.y) != 1)
            {
                Debug.LogWarning($"🚫 Invalid direction from {from} to {to}. Only cardinal directions are allowed.");
                return (TrailSegmentType.End, 0);
            }

            // Check if the target cell is within bounds
            if (!GridManager.Instance.IsWithinBounds(to))
            {
                Debug.LogWarning($"🚫 Cell {to} is out of bounds.");
                return (TrailSegmentType.End, 0);
            }

            // Slope validation
            var slope = GridManager.Instance.ClassifySlope(to);
            bool allowed = PathSlopeRules.IsSlopeAllowed(trailType, slope);

            if (!allowed)
            {
                Debug.LogWarning($"🚫 Slope {slope} at {to} is not allowed for {trailType}.");
                return (TrailSegmentType.End, 0);
            }

            return delta switch
            {
                { x: 0, y: 1 } => (TrailSegmentType.End, 0),  // North
                { x: 1, y: 0 } => (TrailSegmentType.End, 1),  // East
                { x: 0, y: -1 } => (TrailSegmentType.End, 2), // South
                { x: -1, y: 0 } => (TrailSegmentType.End, 3), // West
                _ => (TrailSegmentType.End, 0)
            };
        }

        private static Vector2Int ClampToCardinal(Vector2Int dir)
        {
            return new Vector2Int(Mathf.Clamp(dir.x, -1, 1), Mathf.Clamp(dir.y, -1, 1));
        }
        private static bool IsSameTrailType(Vector2Int coord, TrailType type)
        {
            if (!GridManager.Instance.IsWithinBounds(coord)) return false;
            return GridManager.Instance.GetCell(coord).Trail == type;
        }
    }
}
