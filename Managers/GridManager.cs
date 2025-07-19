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
using System.Linq;
using UnityEngine;
using PlexiPark.Data;
using PlexiPark.Terrain.Data;
using PlexiPark.Terrain.Generation;
using PlexiPark.Data.SaveLoad;
using PlexiPark.Core.SharedEnums;
using PlexiPark.Core.Utils;
using PlexiPark.Core.Interfaces;


namespace PlexiPark.Managers
{
    [System.Serializable]

    public class GridSaveData
    {
        public ParkType parkType;
        public int width, depth;
        public List<GridCellSaveEntry> cells = new();
        public List<CornerHeightEntry> cornerHeights = new();
    }



    [System.Serializable]
    public class GridCellSaveEntry
    {
        public int x, z;
        // public float elevation;
        public SlopeType slope;
        public TerrainType terrainType;
    }

    /// <summary>
    /// Manages the logical grid, slope, elevation, and occupancy of each park tile.
    /// Provides spatial utilities for placement, path validation, and terrain-aware positioning.
    /// </summary>
    public class GridManager : MonoBehaviour, IGridInfoProvider, IHeightSampler
    {
        // Singleton access
        public static GridManager Instance { get; private set; }

        [Header("Grid Configuration")]
        public ParkType currentParkType;
        public float cellSize = 1f;
        public Vector3 origin = Vector3.zero;

        [Header("Debug Options")]
        public bool showDebugGrid = true;

        [Header("Terrain Generation Settings")]
        public TerrainGenerationConfig terrainConfig;
        private int gridWidth;
        private int gridDepth;

        // Sparse grid storage
        private Dictionary<Vector2Int, GridCellData> gridCells = new();
        public CornerHeightMap CornerMap { get; private set; } = new();
        public static bool IsTouchOverDebugUI = false;


        float IHeightSampler.SampleHeight(float worldX, float worldZ)
        {
            Vector3 origin = this.origin;
            var cellSize = this.CellSize;
            float gx = worldX - origin.x;
            float gz = worldZ - origin.z;
            int cx = Mathf.FloorToInt(gx / cellSize);
            int cy = Mathf.FloorToInt(gz / cellSize);
            var cell = new Vector2Int(cx, cy);

            var keys = CornerMap.GetCellCornerKeys(cell);
            float h00 = CornerMap.GetCorner(keys[0]);
            float h10 = CornerMap.GetCorner(keys[1]);
            float h11 = CornerMap.GetCorner(keys[2]);
            float h01 = CornerMap.GetCorner(keys[3]);

            float u = Mathf.Clamp01((gx - cx * cellSize) / cellSize);
            float v = Mathf.Clamp01((gz - cy * cellSize) / cellSize);

            float h0 = Mathf.Lerp(h00, h10, u);
            float h1 = Mathf.Lerp(h01, h11, u);
            return Mathf.Lerp(h0, h1, v);
        }


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


            // Auto-create default config if none assigned
            if (terrainConfig == null)
            {
                terrainConfig = ScriptableObject.CreateInstance<TerrainGenerationConfig>();
                terrainConfig.name = "DefaultTerrainConfig";
                terrainConfig.regionBias = 0.5f;
                terrainConfig.flatRatio = 0.5f;
                terrainConfig.gentleRatio = 0.25f;
                terrainConfig.steepRatio = 0.15f;
                terrainConfig.cliffRatio = 0.1f;
                Debug.Log("⚠️ No TerrainGenerationConfig assigned. Using default.");
            }

            string savePath = Application.persistentDataPath + "/terrain.dat";

            if (System.IO.File.Exists(savePath))
            {
                LoadTerrainBinary(savePath);
                Debug.Log("📦 Loaded saved terrain.");
            }
            else
            {
                InitializeGridDimensions();
                GenerateTerrain();
                Debug.Log("🌄 Generated new terrain.");
                Debug.Log($"🔍 Total grid cells: {gridCells.Count}, total corner heights: {CornerMap.GetAllCorners().Count}");

            }



            FindFirstObjectByType<GridTerrainRenderer>()?.GenerateVisualTerrain();
        }

        private void Start()
        {
            Debug.Log("✅ GridManager is active and listening.");
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
                    gridWidth = 128;
                    gridDepth = 128;
                    break;
                case ParkType.Wilderness:
                    gridWidth = 256;
                    gridDepth = 256;
                    break;
                default:
                    gridWidth = 64;
                    gridDepth = 64;
                    break;
            }

            gridCells.Clear();
            CornerMap.Clear();
            GenerateTerrain();
        }

        #endregion

        TrailSegmentType DetermineSegmentType(Vector2Int cell, TrailType type)
        {
            bool north = IsSameTrailType(cell + Vector2Int.up, type);
            bool south = IsSameTrailType(cell + Vector2Int.down, type);
            bool east = IsSameTrailType(cell + Vector2Int.right, type);
            bool west = IsSameTrailType(cell + Vector2Int.left, type);

            int count = (north ? 1 : 0) + (south ? 1 : 0) + (east ? 1 : 0) + (west ? 1 : 0);

            return count switch
            {
                1 => TrailSegmentType.End,
                2 when (north && south) || (east && west) => TrailSegmentType.Straight,
                2 => TrailSegmentType.Corner,
                3 => TrailSegmentType.TIntersection,
                4 => TrailSegmentType.CrossIntersection,
                _ => TrailSegmentType.End
            };
        }

        bool IsSameTrailType(Vector2Int coord, TrailType type)
        {
            if (!GridManager.Instance.IsWithinBounds(coord)) return false;
            var cell = GridManager.Instance.GetCell(coord);
            return cell.Trail == type;
        }

        public void SaveTerrainBinary(string path)
        {
            var grid = gridCells;
            var corners = CornerMap.GetAllCorners();

            TerrainBinaryIO.Save(path, currentParkType, gridWidth, gridDepth, grid, corners);
            Debug.Log($"💾 Saved binary terrain to: {path}");
        }

        public void LoadTerrainBinary(string path)
        {
            TerrainBinaryIO.Load(path,
                out currentParkType,
                out gridWidth,
                out gridDepth,
                out Dictionary<Vector2Int, GridCellData> loadedCells,
                out Dictionary<Vector2Int, float> loadedCorners);

            gridCells = loadedCells;
            CornerMap.Clear();
            foreach (var pair in loadedCorners)
                CornerMap.SetCorner(pair.Key, pair.Value);

            FindFirstObjectByType<GridTerrainRenderer>()?.GenerateVisualTerrain();
            Debug.Log($"📂 Loaded binary terrain from: {path}");
        }

        public float SampleHeight(float worldX, float worldZ)
        {
            // 1) world→local cell coords
            float gx = worldX - origin.x;
            float gz = worldZ - origin.z;
            int cx = Mathf.FloorToInt(gx / CellSize);
            int cy = Mathf.FloorToInt(gz / CellSize);
            var cell = new Vector2Int(cx, cy);

            // 2) fetch the four corner heights
            var keys = CornerMap.GetCellCornerKeys(cell);
            float h00 = CornerMap.GetCorner(keys[0]); // bl
            float h10 = CornerMap.GetCorner(keys[1]); // br
            float h11 = CornerMap.GetCorner(keys[2]); // tr
            float h01 = CornerMap.GetCorner(keys[3]); // tl

            // 3) local uv
            float localX = Mathf.Clamp01((gx - cx * CellSize) / CellSize);
            float localZ = Mathf.Clamp01((gz - cy * CellSize) / CellSize);

            // 4) bilinear
            float h0 = Mathf.Lerp(h00, h10, localX);
            float h1 = Mathf.Lerp(h01, h11, localX);
            return Mathf.Lerp(h0, h1, localZ);
        }
        public void ResetTerrain()
        {
            if (terrainConfig == null)
            {
                Debug.LogError("❌ TerrainGenerationConfig is not assigned. Cannot reset terrain.");
                return;
            }

            InitializeGridDimensions(); // Resets size and clears cells
            GenerateTerrain();          // Uses the assigned config
            FindFirstObjectByType<GridTerrainRenderer>()?.GenerateVisualTerrain();

            Debug.Log("🔄 Terrain has been reset using current config.");
        }


        #region Accessors

        public int GridWidth => gridWidth;
        public int GridDepth => gridDepth;
        public float CellSize => cellSize;
        public Vector3 Origin => origin;
        public ParkType CurrentParkType => currentParkType;

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
                    //  elevation = 0f,
                    slope = SlopeType.Flat,
                    terrainType = TerrainType.Grass,
                    SegmentType = TrailSegmentType.End
                };
                gridCells[coord] = cell;
            }

            return cell;
        }


        public void SetTrailType(Vector2Int coord, TrailType type)
        {
            if (!IsWithinBounds(coord)) return;

            GridCellData cell = GetCell(coord); // auto-initializes
            cell.Trail = type;
            gridCells[coord] = cell;

            Debug.Log($"✅ Trail set at {coord} to {type}");
        }



        public bool IsValidTrailCell(Vector2Int coord, TrailType trailType)
        {
            if (!IsWithinBounds(coord)) return false;

            SlopeType slope = ClassifySlope(coord); // Uses your existing slope classification
            bool allowed = PathSlopeRules.IsSlopeAllowed(trailType, slope);

            //Debug.Log($"[GridManager] IsValidTrailCell({coord}, {trailType}) = {allowed} (Slope: {slope})");

            return allowed;
        }

        public void RefreshTrailSegmentType(Vector2Int coord)
        {
            var cell = GetCell(coord);
            if (cell.Trail == TrailType.None) return;

            TrailSegmentType type = DetermineSegmentType(coord, cell.Trail);
            cell.SegmentType = type;
            SetCell(coord, cell);

            // Recurse to neighbors to keep all trails updated
            Vector2Int[] dirs = {
        Vector2Int.up, Vector2Int.down,
        Vector2Int.left, Vector2Int.right
    };

            foreach (var dir in dirs)
            {
                Vector2Int neighbor = coord + dir;
                if (IsWithinBounds(neighbor))
                {
                    var neighborCell = GetCell(neighbor);
                    if (neighborCell.Trail == cell.Trail)
                        RefreshTrailSegmentType(neighbor);
                }
            }
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

        public float GetAverageElevation(Vector2Int coord)
        {
            var corners = CornerMap.GetCellCornerKeys(coord);
            float sum = 0f;
            foreach (var corner in corners)
                sum += CornerMap.GetCorner(corner);
            return sum / 4f;
        }

        #endregion

        #region World/Grid Conversion

        public Vector3 GetWorldPosition(Vector2Int gridCoord)
        {
            float x = origin.x + gridCoord.x * cellSize;
            float z = origin.z + gridCoord.y * cellSize;

            // GridCellData cell = GetCell(gridCoord);
            float y = GetAverageElevation(gridCoord);

            return new Vector3(x, y, z);
        }

        public Vector3 GetWorldCenter(Vector2Int gridCoord)
        {
            Vector3 bottomLeft = GetWorldPosition(gridCoord);
            return bottomLeft + new Vector3(cellSize, 0f, cellSize);
        }

        public Vector2Int GetGridCoordinate(Vector3 worldPosition)
        {
            return GridUtils.WorldToCell(worldPosition, origin, cellSize);
        }

        #endregion

        #region Slope Utilities

        internal void RecalculateSlopes()
        {

            foreach (var coord in gridCells.Keys)
            {
                GridCellData cell = GetCell(coord);
                cell.slope = ClassifySlope(coord);
                gridCells[coord] = cell;
            }

        }

/// <summary>
        /// Counts how many grid cells currently belong to the given trail type.
        /// Lightweight LINQ; fine to call occasionally (e.g. once per ticket rebalance).
        /// </summary>
        public int GetTrailCellCount(TrailType type)
        {
            // gridCells holds every cell that has ever been touched; missing keys are “None”
            return gridCells.Values.Count(c => c.Trail == type);
        }

        public IEnumerable<Vector2Int> GetAllCoordinates()
        {
            return gridCells.Keys;
        }
        private void GenerateTerrain()
        {

            if (terrainConfig == null)
            {
                Debug.LogError("❌ TerrainGenerationConfig is missing on GridManager.");
                return;
            }

            HybridTerrainGenerator.Generate(
                gridWidth,
                gridDepth,
                terrainConfig,
                CornerMap,
                gridCells
            );
        }

        #endregion


        private void OnDrawGizmos()
        {
            if (!showDebugGrid || !Application.isPlaying || gridCells == null) return;

            foreach (var pair in gridCells)
            {
                Vector2Int coord = pair.Key;
                GridCellData cell = pair.Value;

                Vector3 pos = GetWorldPosition(coord);
                float size = CellSize;

                Color color = cell.isOccupied ? Color.red : SlopeColor(cell.slope);
                Gizmos.color = color;
                Gizmos.DrawWireCube(pos + Vector3.up * 0.1f, new Vector3(size, 0.1f, size));

#if UNITY_EDITOR

#endif
            }
        }

#if UNITY_EDITOR


        private GUIStyle fixedSizeButtonStyle;
        private bool lockFlat, lockGentle, lockSteep, lockCliff;
        private enum SliderCategory { None, Flat, Gentle, Steep, Cliff }
        private SliderCategory lastChangedSlider = SliderCategory.None;

        private Dictionary<SliderCategory, bool> isActive = new()
{
    { SliderCategory.Flat, false },
    { SliderCategory.Gentle, false },
    { SliderCategory.Steep, false },
    { SliderCategory.Cliff, false },
};
        float GetMaxAllowed(SliderCategory category)
        {
            float totalLocked = 0f;

            if (lockFlat && category != SliderCategory.Flat) totalLocked += terrainConfig.flatRatio;
            if (lockGentle && category != SliderCategory.Gentle) totalLocked += terrainConfig.gentleRatio;
            if (lockSteep && category != SliderCategory.Steep) totalLocked += terrainConfig.steepRatio;
            if (lockCliff && category != SliderCategory.Cliff) totalLocked += terrainConfig.cliffRatio;

            return Mathf.Clamp01(1f - totalLocked);
        }

        private void OnGUI()
        {
            // UI Styling
            fixedSizeButtonStyle = new GUIStyle(GUI.skin.button)
            {
                fontSize = 28,
                alignment = TextAnchor.MiddleCenter
            };
            var labelStyle = new GUIStyle(GUI.skin.label) { fontSize = 24, richText = true };
            var toggleStyle = new GUIStyle(GUI.skin.toggle) { fontSize = 20 };



            if (Application.isPlaying && terrainConfig != null)
            {
                Rect debugRect = new Rect(50, 50, 400, 400); // Match your GUILayout.BeginArea rect
                if (debugRect.Contains(Event.current.mousePosition) &&
                    (Event.current.type == EventType.MouseDown ||
                     Event.current.type == EventType.MouseDrag ||
                     Event.current.type == EventType.MouseUp))
                {
                    IsTouchOverDebugUI = true;
                }
                else if (Event.current.type == EventType.Repaint)
                {
                    IsTouchOverDebugUI = false;
                }

                GUILayout.BeginArea(debugRect, GUI.skin.box);

                GUILayout.Label("<b>Custom Terrain</b>", labelStyle);

                GUILayout.Label("Hilly                                          Flat", labelStyle);
                terrainConfig.regionBias = GUILayout.HorizontalSlider(terrainConfig.regionBias, 0f, 1f);

                GUILayout.Space(10);
                GUILayout.Label("Slope Ratios", labelStyle);
                GUILayout.Space(10);

                DrawLockedSlider(SliderCategory.Flat, "Flat Surface", ref terrainConfig.flatRatio, ref lockFlat, GetMaxAllowed(SliderCategory.Flat), labelStyle, toggleStyle);
                DrawLockedSlider(SliderCategory.Gentle, "Gentle Slope", ref terrainConfig.gentleRatio, ref lockGentle, GetMaxAllowed(SliderCategory.Gentle), labelStyle, toggleStyle);
                DrawLockedSlider(SliderCategory.Steep, "Steep Slope", ref terrainConfig.steepRatio, ref lockSteep, GetMaxAllowed(SliderCategory.Steep), labelStyle, toggleStyle);
                DrawLockedSlider(SliderCategory.Cliff, "Cliff", ref terrainConfig.cliffRatio, ref lockCliff, GetMaxAllowed(SliderCategory.Cliff), labelStyle, toggleStyle);


                NormalizeRatios();

                GUILayout.EndArea();
            }

            if (GUI.Button(new Rect(50, 475, 400, 80), "Generate Terrain", fixedSizeButtonStyle))
            {
                ResetTerrain();
                FindFirstObjectByType<GridTerrainRenderer>()?.GenerateVisualTerrain();
                foreach (var key in isActive.Keys.ToList())
                    isActive[key] = false;
                Debug.Log("🔄 Regenerated terrain with updated config.");
            }

            if (GUI.Button(new Rect(50, 565, 195, 80), "Save Terrain", fixedSizeButtonStyle))
            {
                SaveTerrainBinary(Application.persistentDataPath + "/terrain.dat");
                Debug.Log("💾 Terrain saved.");
            }

            if (GUI.Button(new Rect(255, 565, 195, 80), "Load Terrain", fixedSizeButtonStyle))
            {
                LoadTerrainBinary(Application.persistentDataPath + "/terrain.dat");
                Debug.Log("📂 Terrain loaded.");
            }
        }

        // Slider + Lock Toggle UI
        private void DrawLockedSlider(SliderCategory type, string label, ref float value, ref bool isLocked, float maxLimit, GUIStyle labelStyle, GUIStyle toggleStyle)
        {
            GUILayout.BeginHorizontal();
            GUILayout.Label($"{label}: {(value * 100f):F0}%", labelStyle, GUILayout.Width(250));
            isLocked = GUILayout.Toggle(isLocked, "🔒", toggleStyle, GUILayout.Width(50));
            GUILayout.EndHorizontal();

            if (!isLocked)
            {
                float oldValue = value;
                float newValue = GUILayout.HorizontalSlider(value, 0f, maxLimit);
                if (!Mathf.Approximately(oldValue, newValue))
                {
                    value = newValue;
                    lastChangedSlider = type;
                    isActive[type] = true; // 👈 mark as active when adjusted
                }
            }
            else
            {
                GUILayout.Space(18);
            }
        }

        // Auto-balancing logic
        private void NormalizeRatios()
        {
            if (lastChangedSlider == SliderCategory.None) return;

            // Snapshot current values
            Dictionary<SliderCategory, float> values = new()
    {
        { SliderCategory.Flat, terrainConfig.flatRatio },
        { SliderCategory.Gentle, terrainConfig.gentleRatio },
        { SliderCategory.Steep, terrainConfig.steepRatio },
        { SliderCategory.Cliff, terrainConfig.cliffRatio },
    };

            float activeSum = 0f;
            float inactiveSum = 0f;

            foreach (var pair in values)
            {
                if (isActive[pair.Key])
                    activeSum += pair.Value;
                else
                    inactiveSum += pair.Value;
            }

            float available = Mathf.Clamp01(1f - activeSum);

            if (inactiveSum > 0f)
            {
                // Redistribute proportionally among inactive sliders
                foreach (var key in values.Keys.ToList())
                {
                    if (!isActive[key])
                    {
                        float ratio = values[key] / inactiveSum;
                        values[key] = ratio * available;
                    }
                }
            }
            else
            {
                // All sliders active—normalize across all
                float total = activeSum > 0f ? activeSum : 1f;
                foreach (var key in values.Keys.ToList())
                {
                    values[key] = Mathf.Clamp01(values[key] / total);
                }
            }

            // Push updated values back
            terrainConfig.flatRatio = values[SliderCategory.Flat];
            terrainConfig.gentleRatio = values[SliderCategory.Gentle];
            terrainConfig.steepRatio = values[SliderCategory.Steep];
            terrainConfig.cliffRatio = values[SliderCategory.Cliff];

            // Final correction to ensure total sum = 1f
            float flat = terrainConfig.flatRatio;
            float gentle = terrainConfig.gentleRatio;
            float steep = terrainConfig.steepRatio;
            float cliff = terrainConfig.cliffRatio;

            float remainder = 1f - (flat + gentle + steep + cliff);
            terrainConfig.cliffRatio += remainder;

            // Optional: clamp to avoid tiny negatives after rounding
            terrainConfig.cliffRatio = Mathf.Clamp01(terrainConfig.cliffRatio);
            lastChangedSlider = SliderCategory.None; // 👈 Reset
        }

        public SlopeType GetSlopeType(Vector2Int cell)
        {
            var data = GetCell(cell);
            return data.slope;
        }

        void Update()
        {
            if (Input.GetKeyDown(KeyCode.R))
            {
                ResetTerrain();
                Debug.Log("Terrain reset.");
            }
            if (Input.GetKeyDown(KeyCode.Home))
            {
                SaveTerrainBinary(Application.persistentDataPath + "/terrain.dat");
                Debug.Log("Terrain saved.");
            }
            if (Input.GetKeyDown(KeyCode.L))
            {
                LoadTerrainBinary(Application.persistentDataPath + "/terrain.dat");
                Debug.Log("Terrain loaded.");
            }
        }
#endif

        public SlopeType ClassifySlope(Vector2Int cellCoord)
        {
            var keys = CornerMap.GetCellCornerKeys(cellCoord);

            float min = float.MaxValue;
            float max = float.MinValue;

            foreach (var key in keys)
            {
                float h = CornerMap.GetCorner(key);
                if (h < min) min = h;
                if (h > max) max = h;
            }

            float delta = max - min;

            return delta switch
            {
                <= 0.25f => SlopeType.Flat,
                <= 1.0f => SlopeType.Gentle,
                <= 2.5f => SlopeType.Steep,
                _ => SlopeType.Cliff
            };
        }


        private Color SlopeColor(SlopeType slope)
        {
            return slope switch
            {
                SlopeType.Flat => Color.green,
                SlopeType.Gentle => Color.yellow,
                SlopeType.Steep => Color.magenta,
                SlopeType.Cliff => Color.black,
                _ => Color.white
            };
        }

        public IEnumerable<Vector2Int> GetAdjacentTrailCells(Vector2Int cell)
        {
            Vector2Int[] dirs = {
        Vector2Int.up, Vector2Int.down,
        Vector2Int.left, Vector2Int.right
    };
            foreach (var d in dirs)
            {
                var n = cell + d;
                if (IsWithinBounds(n) && GetCell(n).Trail != TrailType.None)
                    yield return n;
            }
        }
    }
}
