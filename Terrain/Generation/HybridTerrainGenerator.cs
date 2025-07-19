// Terrain/Generation/HybridTerrainGenerator.cs
// --------------------------------------------------
// Hybrid terrain generator using clustered slope zoning
// blended with Perlin noise for natural variation.
// --------------------------------------------------

using UnityEngine;
using System.Collections.Generic;
using PlexiPark.Data;
using PlexiPark.Terrain.Data;
using PlexiPark.Core.SharedEnums;

namespace PlexiPark.Terrain.Generation
{
    public static class HybridTerrainGenerator
    {
        public static void Generate(
            int width,
            int depth,
            TerrainGenerationConfig config,
            CornerHeightMap cornerMap,
            Dictionary<Vector2Int, GridCellData> gridCells)
        {
            float blendWeight = 1f - config.regionBias;
            float snapStep = config.elevationSnapStep;

            // Step 1: Assign clustered zones by SlopeType
            var slopeMap = GenerateSlopeZones(width, depth, config);

            // Step 2: Assign target height per slope type
            Dictionary<SlopeType, float> slopeHeights = new()
            {
                { SlopeType.Flat, 0f },
                { SlopeType.Gentle, 3.33f },
                { SlopeType.Steep, 6.66f },
                { SlopeType.Cliff, 10f }
            };

            // Step 3: Generate corner heightmap with blended elevation
            for (int x = 0; x <= width; x++)
            {
                for (int z = 0; z <= depth; z++)
                {
                    Vector2Int coord = new(x, z);

                    // Sample Perlin noise
                    float noise = Mathf.PerlinNoise(x * config.perlinScale, z * config.perlinScale);

                    // Get nearest slope zone (fallback to Flat if missing)
                    SlopeType zone = slopeMap.TryGetValue(new Vector2Int(x, z), out var s) ? s : SlopeType.Flat;
                    float targetHeight = slopeHeights[zone] / config.heightMultiplier;

                    // Blend height and snap
                    float elevation = Mathf.Lerp(targetHeight, noise, blendWeight);
                    float final = Mathf.Round(elevation * config.heightMultiplier / snapStep) * snapStep;

                    cornerMap.SetCorner(coord, Mathf.Max(0f, final));
                }
            }

            // Step 4: Build GridCellData with slope classification
            for (int x = 0; x < width; x++)
            {
                for (int z = 0; z < depth; z++)
                {
                    Vector2Int coord = new(x, z);

                    float min = float.MaxValue;
                    float max = float.MinValue;

                    foreach (var key in cornerMap.GetCellCornerKeys(coord))
                    {
                        float h = cornerMap.GetCorner(key);
                        if (h < min) min = h;
                        if (h > max) max = h;
                    }

                    float delta = max - min;

                    // Fixed thresholds (can be exposed in config later)
                    SlopeType slope = delta switch
                    {
                        <= 0.25f => SlopeType.Flat,
                        <= 1.0f => SlopeType.Gentle,
                        <= 2.5f => SlopeType.Steep,
                        _ => SlopeType.Cliff
                    };

                    gridCells[coord] = new GridCellData
                    {
                        isOccupied = false,
                        slope = slope,
                        terrainType = TerrainType.Grass
                    };
                }
            }
        }

        /// <summary>
        /// Creates contiguous clusters of cells tagged with SlopeType,
        /// weighted by slope ratio targets in config.
        /// </summary>
        private static Dictionary<Vector2Int, SlopeType> GenerateSlopeZones(int width, int depth, TerrainGenerationConfig config)
        {
            Dictionary<Vector2Int, SlopeType> result = new();

            int totalCells = width * depth;
            Dictionary<SlopeType, int> slopeCounts = new()
            {
                { SlopeType.Flat, Mathf.RoundToInt(config.flatRatio * totalCells) },
                { SlopeType.Gentle, Mathf.RoundToInt(config.gentleRatio * totalCells) },
                { SlopeType.Steep, Mathf.RoundToInt(config.steepRatio * totalCells) },
                { SlopeType.Cliff, Mathf.RoundToInt(config.cliffRatio * totalCells) }
            };

            List<Vector2Int> allCoords = new();
            for (int x = 0; x < width; x++)
                for (int z = 0; z < depth; z++)
                    allCoords.Add(new Vector2Int(x, z));

            allCoords.Shuffle(); // ✅ Requires extension method below

            foreach (var slopeType in slopeCounts.Keys)
            {
                int toAssign = slopeCounts[slopeType];
                while (toAssign > 0 && allCoords.Count > 0)
                {
                    Vector2Int seed = allCoords[0];
                    allCoords.RemoveAt(0);

                    Queue<Vector2Int> frontier = new();
                    frontier.Enqueue(seed);

                    int clusterSize = Mathf.Min(Random.Range(5, 15), toAssign);

                    int assigned = 0;
                    while (frontier.Count > 0 && assigned < clusterSize)
                    {
                        Vector2Int current = frontier.Dequeue();
                        if (result.ContainsKey(current)) continue;

                        result[current] = slopeType;
                        assigned++;
                        toAssign--;

                        foreach (Vector2Int neighbor in GetNeighbors(current, width, depth))
                        {
                            if (!result.ContainsKey(neighbor) && Random.value < 0.6f)
                                frontier.Enqueue(neighbor);
                        }
                    }
                }
            }
            SmoothSlopeZones(result, width, depth, iterations: 32); // 👈 Adjust iterations as needed

            return result;
        }

        private static IEnumerable<Vector2Int> GetNeighbors(Vector2Int coord, int width, int depth)
        {
            for (int dx = -1; dx <= 1; dx++)
            {
                for (int dz = -1; dz <= 1; dz++)
                {
                    if (dx == 0 && dz == 0) continue; // Skip self

                    int nx = coord.x + dx;
                    int nz = coord.y + dz;

                    if (nx >= 0 && nx < width && nz >= 0 && nz < depth)
                        yield return new Vector2Int(nx, nz);
                }
            }
        }


        // Extension for shuffling a list in-place
        private static void Shuffle<T>(this List<T> list)
        {
            for (int i = list.Count - 1; i > 0; i--)
            {
                int j = Random.Range(0, i + 1);
                (list[i], list[j]) = (list[j], list[i]);
            }
        }

        private static void SmoothSlopeZones(Dictionary<Vector2Int, SlopeType> slopeMap, int width, int depth, int iterations = 1)
        {
            for (int i = 0; i < iterations; i++)
            {
                var newMap = new Dictionary<Vector2Int, SlopeType>(slopeMap);

                foreach (var kvp in slopeMap)
                {
                    Vector2Int coord = kvp.Key;
                    Dictionary<SlopeType, int> neighborCounts = new();

                    foreach (var neighbor in GetNeighbors(coord, width, depth))
                    {
                        if (!slopeMap.TryGetValue(neighbor, out var nType)) continue;

                        if (!neighborCounts.ContainsKey(nType)) neighborCounts[nType] = 0;
                        neighborCounts[nType]++;
                    }

                    if (neighborCounts.Count == 0) continue;

                    // Majority slope wins
                    SlopeType mostCommon = kvp.Value;
                    int max = 0;
                    foreach (var pair in neighborCounts)
                    {
                        if (pair.Value > max)
                        {
                            mostCommon = pair.Key;
                            max = pair.Value;
                        }
                    }

                    newMap[coord] = mostCommon;
                }

                // Apply the smoothed result
                foreach (var entry in newMap)
                    slopeMap[entry.Key] = entry.Value;
            }
        }

    }
}
