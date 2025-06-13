// Managers/GridManager.cs
// -------------------------------------
// Table of Contents
// 1. Singleton Setup
// 2. Grid Initialization (with terrain)
// 3. Grid Cell Access and Modification
// 4. World/Grid Position Conversion
// 5. Terrain-Aware Utility Functions
// 6. Slope-Based Path Placement Validation
// 7. Grid Gizmo for testing

using System.Collections.Generic;
using UnityEngine;
using PlexiPark.Data;

namespace PlexiPark.Managers
{
    /// <summary>
    /// Manages the logical grid, slope, elevation, and occupancy of each park tile.
    /// Provides spatial utilities for placement, path validation, and terrain-aware positioning.
    /// </summary>
    public class GridManager : MonoBehaviour
    {
        // Singleton access
        public static GridManager Instance { get; private set; }

        [Header("Grid Configuration")]
        public ParkType currentParkType;
        public float cellSize = 1f;
        public Vector3 origin = Vector3.zero;

        [Header("Debug Options")]
        public bool showDebugGrid = true;


        private int gridWidth;
        private int gridDepth;

        // Sparse grid storage
        private Dictionary<Vector2Int, GridCellData> gridCells = new();

        #region Unity Lifecycle

        private void Awake()
        {
            if (Instance != null)
            {
                Destroy(gameObject);
                return;
            }

            Instance = this;
            DontDestroyOnLoad(gameObject);
            InitializeGridDimensions();
        }

        #endregion

        #region Initialization

        private void InitializeGridDimensions()
        {
            switch (currentParkType)
            {
                case ParkType.Urban:
                    gridWidth = 64;
                    gridDepth = 64;
                    break;
                case ParkType.Suburban:
                    gridWidth = 96;
                    gridDepth = 96;
                    break;
                case ParkType.Wilderness:
                    gridWidth = 128;
                    gridDepth = 128;
                    break;
                default:
                    gridWidth = 64;
                    gridDepth = 64;
                    break;
            }

            gridCells.Clear();

            // Optional: initialize some elevation/slope logic here for testing
            for (int x = 0; x < gridWidth; x++)
            {
                for (int z = 0; z < gridDepth; z++)
                {
                    Vector2Int coord = new(x, z);
                    GridCellData cell = new()
                    {
                        isOccupied = false,
                        elevation = 0f,
                        slope = SlopeType.Flat,
                        terrainType = TerrainType.Grass
                    };
                    gridCells[coord] = cell;
                }
            }
        }

        #endregion

        #region Accessors

        public int GridWidth => gridWidth;
        public int GridDepth => gridDepth;
        public float CellSize => cellSize;

        public bool IsWithinBounds(Vector2Int coord)
        {
            return coord.x >= 0 && coord.x < gridWidth && coord.y >= 0 && coord.y < gridDepth;
        }

public GridCellData GetCell(Vector2Int coord)
{
    if (!IsWithinBounds(coord))
        return default;

    if (!gridCells.TryGetValue(coord, out var cell))
    {
        // Initialize a default cell and store it if not present
        cell = new GridCellData
        {
            isOccupied = false,
            elevation = 0f,
            slope = SlopeType.Flat,
            terrainType = TerrainType.Grass
        };
        gridCells[coord] = cell;
    }

    return cell;
}

        public void SetCell(Vector2Int coord, GridCellData data)
        {
            if (IsWithinBounds(coord))
                gridCells[coord] = data;
        }

        public bool IsCellOccupied(Vector2Int coord)
        {
            return GetCell(coord).isOccupied;
        }

public void SetOccupied(Vector2Int coord, bool occupied)
{
    if (!IsWithinBounds(coord)) return;

    GridCellData cell = GetCell(coord); // Will now auto-initialize if needed
    cell.isOccupied = occupied;
    gridCells[coord] = cell;
}


        #endregion

        #region World/Grid Conversion

        public Vector3 GetWorldPosition(Vector2Int gridCoord)
        {
            float x = origin.x + gridCoord.x * cellSize;
            float z = origin.z + gridCoord.y * cellSize;

            GridCellData cell = GetCell(gridCoord);
            float y = cell.elevation;

            return new Vector3(x, y, z);
        }

        public Vector2Int GetGridCoordinate(Vector3 worldPosition)
        {
            int x = Mathf.FloorToInt((worldPosition.x - origin.x) / cellSize);
            int z = Mathf.FloorToInt((worldPosition.z - origin.z) / cellSize);
            return new Vector2Int(x, z);
        }

        #endregion

        #region Slope Utilities

        /// <summary>
        /// Checks whether a path can connect between two tiles based on slope constraints.
        /// </summary>
        public bool IsValidSlopeTransition(Vector2Int from, Vector2Int to, PathType pathType)
        {
            if (!IsWithinBounds(from) || !IsWithinBounds(to))
                return false;

            SlopeType fromSlope = GetCell(from).slope;
            SlopeType toSlope = GetCell(to).slope;

            int slopeDelta = Mathf.Abs((int)fromSlope - (int)toSlope);

            return pathType switch
            {
                PathType.HikingTrail => slopeDelta <= 4,
                PathType.MountainTrail => slopeDelta <= 4,
                PathType.WalkingPath => slopeDelta <= 2,
                PathType.BikeTrail => slopeDelta <= 3,
                PathType.ServiceRoad => slopeDelta <= 1,
                _ => false
            };
        }

        #endregion


        private void OnDrawGizmos()
        {
            if (!showDebugGrid || !Application.isPlaying || gridCells == null) return;

            foreach (var pair in gridCells )
            {
                Vector2Int coord = pair.Key;
                GridCellData cell = pair.Value;

                Vector3 pos = GetWorldPosition(coord);
                float size = CellSize;

                Color color = cell.isOccupied ? Color.red : SlopeColor(cell.slope);
                Gizmos.color = color;
                Gizmos.DrawWireCube(pos + Vector3.up * 0.1f, new Vector3(size, 0.1f, size));
            }
        }

        private Color SlopeColor(SlopeType slope)
        {
            return slope switch
            {
                SlopeType.Flat => Color.green,
                SlopeType.Gentle => Color.yellow,
                SlopeType.Moderate => new Color(1f, 0.5f, 0f),
                SlopeType.Steep => Color.magenta,
                SlopeType.Cliff => Color.black,
                _ => Color.white
            };
        }
    }
}
