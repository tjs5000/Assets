// CornerHeightMap.cs
// ---------------------------------------------------------
// Purpose: Stores shared corner heights and provides utility
// access for terrain mesh and slope calculation
// ---------------------------------------------------------

using System.Collections.Generic;
using UnityEngine;

namespace PlexiPark.Terrain.Data
{
    /// <summary>
    /// Manages the shared corner height data used by all terrain cells.
    /// </summary>
    public class CornerHeightMap
    {
        // Corner height values (quantized): 0.0, 3.33, 6.66, 10.0
        private Dictionary<Vector2Int, float> cornerHeights = new();

        /// <summary>
        /// Gets the height of a specific corner position.
        /// </summary>
        public float GetCorner(Vector2Int cornerCoord)
        {
            return cornerHeights.TryGetValue(cornerCoord, out var height) ? height : 0f;
        }

        /// <summary>
        /// Sets the height of a specific corner position.
        /// </summary>
        public void SetCorner(Vector2Int cornerCoord, float height)
        {
            cornerHeights[cornerCoord] = height;
        }

        /// <summary>
        /// Returns the 4 corner keys that define a terrain cell at a given grid cell coordinate.
        /// BottomLeft, BottomRight, TopLeft, TopRight (in that order).
        /// </summary>
        public Vector2Int[] GetCellCornerKeys(Vector2Int cellCoord)
        {
            int x = cellCoord.x;
            int y = cellCoord.y;

            return new Vector2Int[]
            {
                new Vector2Int(x,     y),     // Bottom Left
                new Vector2Int(x + 1, y),     // Bottom Right
                new(x + 1, y + 1), // ✅ Top Right (corrected)
                new(x,     y + 1)  // ✅ Top Left  (corrected)
            };
        }

        /// <summary>
        /// Returns all corner data (used for saving/loading or diagnostics).
        /// </summary>
        public Dictionary<Vector2Int, float> GetAllCorners()
        {
            return cornerHeights;
        }

        /// <summary>
        /// Clears all corner data (e.g., when regenerating terrain).
        /// </summary>
        public void Clear()
        {
            cornerHeights.Clear();
        }

        //Read-only validation before using a value
        public bool ContainsCorner(Vector2Int coord)
        {
            return cornerHeights.ContainsKey(coord);
        }
    }
}
