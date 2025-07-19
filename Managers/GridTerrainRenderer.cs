// Assets/Managers/GridTerrainRenderer.cs
// ----------------------------------------------
// Manages procedural rendering of terrain using chunked mesh strategy
// ----------------------------------------------

using System.Collections.Generic;
using UnityEngine;
using PlexiPark.Terrain.Rendering;
using PlexiPark.Core.SharedEnums;
using PlexiPark.Core.Interfaces;
using PlexiPark.Data.Trail;
using PlexiPark.Core.Utils;

namespace PlexiPark.Managers
{
    public class GridTerrainRenderer : MonoBehaviour, ITrailPaintTarget
    {
        public static GridTerrainRenderer Instance { get; private set; }

        [Header("Terrain Chunk Settings")]
        public GameObject chunkPrefab;
        public int chunkSize = 16;

        private Dictionary<Vector2Int, TerrainChunk> chunkMap = new();

        void Awake()
        {
            if (Instance != null && Instance != this)
            {
                Destroy(gameObject);
                return;
            }
            Instance = this;
        }

        public void GenerateVisualTerrain()
        {
            ClearAllChunks();

            int width = GridManager.Instance.GridWidth;
            int depth = GridManager.Instance.GridDepth;
            float cellSize = GridManager.Instance.cellSize;  // lowercase 'cellSize'

            for (int x = 0; x < width; x += chunkSize)
            {
                for (int z = 0; z < depth; z += chunkSize)
                {
                    // chunkCoord in grid‐space
                    Vector2Int chunkCoord = new Vector2Int(x, z);

                    // worldPos: grid origin + offset to chunk's lower‐left corner
                    Vector3 worldPos = GridManager.Instance.origin
                        + GridUtils.CellToWorld(chunkCoord, GridManager.Instance.origin, cellSize, 0f);

                    GameObject go = Instantiate(
                        chunkPrefab,
                        worldPos,
                        Quaternion.identity,
                        transform
                    );
                    go.name = $"Chunk_{x}_{z}";

                    if (go.TryGetComponent<TerrainChunk>(out var chunk))
                    {
                        chunk.GenerateMesh(
                            chunkCoord,
                            chunkSize,
                            cellSize,
                            GridManager.Instance.CornerMap.GetCorner
                        );

                        RegisterChunk(chunkCoord, chunk);

                        // Assign NavMesh area based on dominant slope in this chunk
                        SlopeType slope = GetDominantSlope(chunkCoord, chunkSize);
                        chunk.AssignNavMeshArea(slope);
                    }
                    else
                    {
                        Debug.LogError("Chunk prefab missing TerrainChunk script.");
                    }
                }
            }

            GenerateTestSplatMaps();
        }

        public void PaintTrailSplat(Vector2Int cell, TrailType type)
        {
            //Debug.Log($"#### [PaintTrailSplat] called with {cell} and {type}.");
            var chunkCoord = GetChunkCoord(cell);
            if (!chunkMap.TryGetValue(chunkCoord, out var chunk))
            {
                Debug.LogWarning($"No chunk found for cell {cell}");
                return;
            }

            SplatTarget target = TrailSplatRegistry.GetSplatTarget(type);
            chunk.PaintTrailPixel(cell, (int)target.map, target.channelIndex, 1f);
        }

        public void ApplyAllSplats()
        {
            Debug.Log("## [ApplyAllSplats] is called");
            foreach (var chunk in chunkMap.Values)
            {
                chunk.ApplySplats(); // Pushes to GPU
            }
        }

        public void RegisterChunk(Vector2Int chunkCoord, TerrainChunk chunk)
        {
            chunkMap[chunkCoord] = chunk;
        }

        public bool TryGetChunk(Vector2Int chunkCoord, out TerrainChunk chunk)
        {
            return chunkMap.TryGetValue(chunkCoord, out chunk);
        }

        public void ClearAllChunks()
        {
            foreach (var chunk in chunkMap.Values)
            {
                if (chunk != null) Destroy(chunk.gameObject);
            }
            chunkMap.Clear();
        }

        public void RegenerateChunk(Vector2Int coord)
        {
            Vector2Int chunkCoord = GetChunkCoord(coord);
            if (chunkMap.TryGetValue(chunkCoord, out var chunk))
            {
                chunk.GenerateMesh(chunkCoord, chunkSize, GridManager.Instance.CellSize, GridManager.Instance.CornerMap.GetCorner);
            }
        }

        public Vector2Int GetChunkCoord(Vector2Int cellCoord)
        {
            return GridUtils.CellToRegionOrigin(cellCoord, chunkSize);
        }


        // Paints the segment type and rotation into the segment map
        public void PaintSegmentMap(Vector2Int cell, TrailSegmentType segmentType, int rotationID)
        {

            //Debug.Log($"#### [PaintSegmentMap] called with {cell}, {segmentType}, {rotationID}");
            Vector2Int chunkCoord = GetChunkCoord(cell);
            if (!chunkMap.TryGetValue(chunkCoord, out var chunk))
            {
                Debug.LogWarning($"[GridTerrainRenderer] No chunk found for cell {cell}");
                return;
            }

            chunk.PaintSegmentType(cell, (int)segmentType, rotationID);
        }

        private SlopeType GetDominantSlope(Vector2Int chunkCoord, int size)
        {
            Dictionary<SlopeType, int> counts = new();
            for (int x = 0; x < size; x++)
            {
                for (int z = 0; z < size; z++)
                {
                    Vector2Int cell = chunkCoord + new Vector2Int(x, z);
                    if (!GridManager.Instance.IsWithinBounds(cell)) continue;

                    var slope = GridManager.Instance.GetCell(cell).slope;
                    if (!counts.ContainsKey(slope)) counts[slope] = 0;
                    counts[slope]++;
                }
            }

            SlopeType dominant = SlopeType.Flat;
            int max = 0;
            foreach (var pair in counts)
            {
                if (pair.Value > max)
                {
                    max = pair.Value;
                    dominant = pair.Key;
                }
            }

            return dominant;
        }


        private void GenerateTestSplatMaps()
        {
            // TrailGenerator.GenerateAllTrailStrokes(chunkMap, chunkSize, this);
            ApplyAllSplats();
        }


        public void DebugLogMaterialProperties()
        {
            foreach (var chunk in chunkMap.Values)
            {
                var mat = chunk.GetComponent<MeshRenderer>().sharedMaterial;
                Debug.Log($"Chunk Shader: {mat.shader.name}");
                Debug.Log($"  TrailSplatMap1 = {mat.GetTexture("_TrailSplatMap1")}");
                Debug.Log($"  TrailSplatMap2 = {mat.GetTexture("_TrailSplatMap2")}");
                Debug.Log($"  TrailSplatMap3 = {mat.GetTexture("_TrailSplatMap3")}");
                Debug.Log($"  TrailSegmentMap = {mat.GetTexture("_TrailSegmentMap")}");
                Debug.Log($"  TerrainOrigin = {mat.GetVector("_TerrainOrigin")}");
                Debug.Log($"  TerrainWorldSize = {mat.GetFloat("_TerrainWorldSize")}");
            }
        }
    }
}
