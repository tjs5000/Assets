// Assets/Core/Utils/GridUtils.cs
// Assets/Core/Utils/GridUtils.cs
using UnityEngine;
using PlexiPark.Core.Interfaces;  // <— needed for IHeightSampler

namespace PlexiPark.Core.Utils
{
    public static class GridUtils
    {
        // ─── WorldToCell overloads ────────────────────────────────────

        /// <summary>
        /// World → cell assuming grid origin is (0,0,0).
        /// </summary>
        public static Vector2Int WorldToCell(Vector3 worldPos, float cellSize)
        {
            int x = Mathf.FloorToInt(worldPos.x / cellSize);
            int y = Mathf.FloorToInt(worldPos.z / cellSize);
            return new Vector2Int(x, y);
        }

        /// <summary>
        /// World → cell when grid is offset by an arbitrary origin.
        /// </summary>
        public static Vector2Int WorldToCell(Vector3 worldPos, Vector3 origin, float cellSize)
        {
            // shift into local‐grid space then call the 2-arg version
            return WorldToCell(worldPos - origin, cellSize);
        }


        // ─── CellToWorld overloads ────────────────────────────────────

        /// <summary>
        /// Cell → world (centered) when origin is (0,0,0).
        /// </summary>
        public static Vector3 CellToWorld(Vector2 coord, Vector3 origin, float cellSize, float yOffset)
        {
            float half = cellSize * 0.5f;
            return new Vector3(
                origin.x + coord.x * cellSize + half,
                origin.y + yOffset,
                origin.z + coord.y * cellSize + half
            );
        }

        /// <summary>
        /// Cell → world (centered) with an arbitrary origin offset.
        /// </summary>
        public static Vector3 CellToWorld(Vector2Int cell, Vector3 origin, float cellSize, float yOffset)
        {
            float half = cellSize * 0.5f;
            return new Vector3(
                origin.x + cell.x * cellSize + half,
                origin.y + yOffset,
                origin.z + cell.y * cellSize + half
            );
        }

        /// <summary>
        /// Which region (multiple of regionSize) this cell belongs to.
        /// </summary>
        public static Vector2Int CellToRegionOrigin(Vector2Int cell, int regionSize)
        {
            int x = Mathf.FloorToInt(cell.x / (float)regionSize) * regionSize;
            int y = Mathf.FloorToInt(cell.y / (float)regionSize) * regionSize;
            return new Vector2Int(x, y);
        }

        // ----------------------------------------------------------------
        // Height sampling—all calls go through the IHeightSampler
        // ----------------------------------------------------------------


        /// <summary>
        /// Converts a height‐sampled corner (grid intersection) into world‐space.
        /// corner.x/corner.y are the grid‐space corner indices.
        /// </summary>
        public static Vector3 CornerToWorld(
            Vector2Int corner,
            Vector3 gridOrigin,
            float cellSize,
            float height
        )
        {
            // corners lie on the grid lines, so *no* half‐cell offset here
            float wx = gridOrigin.x + corner.x * cellSize;
            float wz = gridOrigin.z + corner.y * cellSize;
            return new Vector3(wx, height, wz);
        }

        /// <summary>
        /// Bilinearly samples terrain height under (worldX, worldZ) via IHeightSampler.
        /// </summary>
        public static float SampleCornerHeightAt(
            float worldX,
            float worldZ,
            IHeightSampler sampler
        )
        {
            return sampler.SampleHeight(worldX, worldZ);
        }

        /// <summary>
        /// Overload taking a Vector3.
        /// </summary>
        public static float SampleCornerHeightAt(
            Vector3 worldPos,
            IHeightSampler sampler
        ) => SampleCornerHeightAt(worldPos.x, worldPos.z, sampler);
    }
}
