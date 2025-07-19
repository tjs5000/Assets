// Assets/Managers/WaterwayManager.cs

using UnityEngine;
using System.Collections.Generic;
using PlexiPark.Data;


namespace PlexiPark.Managers
{
    public class WaterwayManager : MonoBehaviour
    {
        public static WaterwayManager Instance { get; private set; }


        [Header("Prefabs")]
        [Tooltip("Prefab used for waterfall effects")]
        public GameObject waterfallPrefab;
        [Tooltip("Prefab used for pond/lake tiles")]
        public GameObject pondPrefab;

        private readonly HashSet<Vector2Int> _segments = new();

        // Graph of connections between segment coords
        private readonly Dictionary<Vector2Int, HashSet<Vector2Int>> _adjacency
            = new Dictionary<Vector2Int, HashSet<Vector2Int>>();


        // Cardinal neighbor offsets + world‐space facing
        private static readonly (Vector2Int offset, Vector3 forward)[] Directions =
        {
        (new Vector2Int(0, 1), Vector3.forward),
        (new Vector2Int(1, 0), Vector3.right),
        (new Vector2Int(0,-1), Vector3.back),
        (new Vector2Int(-1,0), Vector3.left),
    };

        void Awake()
        {
            if (Instance != null && Instance != this)
            {
                Destroy(gameObject);
                return;
            }
            Instance = this;
            DontDestroyOnLoad(gameObject);
        }


        /// Call when a waterway segment is placed at 'coord'.
        public void AddSegment(Vector2Int coord, ParkObjectData data)
        {
            _adjacency[coord] = new HashSet<Vector2Int>();
            UpdateConnections(coord);
            TryDetectLoop(coord);
            TrySpawnWaterfall(coord);
        }

        /// Connects the new segment to any existing neighbors in the 4 directions.
        private void UpdateConnections(Vector2Int coord)
        {
            foreach (var (offset, _) in Directions)
            {
                var neighbor = coord + offset;
                if (_segments.Contains(neighbor))
                {
                    _adjacency[coord].Add(neighbor);
                    _adjacency[neighbor].Add(coord);
                }
            }
        }

        /// If placing 'coord' closed a loop, flood‐fill inside and spawn pond tiles.
        private void TryDetectLoop(Vector2Int coord)
        {
            // DFS to find a simple cycle starting/ending at coord
            var visited = new HashSet<Vector2Int>();
            var path = new List<Vector2Int>();
            if (FindCycle(coord, coord, new Vector2Int(int.MinValue, int.MinValue), visited, path))
            {
                // path now contains the cycle loop (last==first)
                CreatePond(path);
            }
        }


        private bool FindCycle(
            Vector2Int start,
            Vector2Int current,
            Vector2Int parent,
            HashSet<Vector2Int> visited,
            List<Vector2Int> path)
        {
            visited.Add(current);
            path.Add(current);

            foreach (var neighbor in _adjacency[current])
            {
                if (neighbor == parent)
                    continue;
                if (neighbor == start)
                {
                    path.Add(neighbor);
                    return true;
                }
                if (!visited.Contains(neighbor))
                {
                    if (FindCycle(start, neighbor, current, visited, path))
                        return true;
                }
            }

            path.RemoveAt(path.Count - 1);
            return false;
        }


        /// Spawns a waterfall wherever 'coord' drops at least one height tier to a neighbor.
        private void TrySpawnWaterfall(Vector2Int coord)
        {
            var cell = GridManager.Instance.GetCell(coord);
            var worldBase = GridManager.Instance.GetWorldPosition(coord);

            foreach (var (offset, forward) in Directions)
            {
                var neighbor = coord + offset;
                if (!_segments.Contains(neighbor)) continue;

                var neighborCell = GridManager.Instance.GetCell(neighbor);
                float heightDiff = neighborCell.elevation - cell.elevation;
                if (heightDiff >= 1f && waterfallPrefab != null)
                {
                    // position at cliff edge
                    Vector3 spawnPos = worldBase + Vector3.up * (cell.elevation + 0.5f);
                    Instantiate(
                        waterfallPrefab,
                        spawnPos,
                        Quaternion.LookRotation(forward)
                    );
                }
            }
        }


        /// Flood‐fills the interior of the closed loop 'loopCoords' and spawns pondPrefab there.

        private void CreatePond(List<Vector2Int> loopCoords)
        {
            // Determine bounds
            int minX = int.MaxValue, maxX = int.MinValue;
            int minY = int.MaxValue, maxY = int.MinValue;
            var loopSet = new HashSet<Vector2Int>(loopCoords);
            foreach (var c in loopCoords)
            {
                minX = Mathf.Min(minX, c.x);
                maxX = Mathf.Max(maxX, c.x);
                minY = Mathf.Min(minY, c.y);
                maxY = Mathf.Max(maxY, c.y);
            }

            // Flood‐fill from outside
            var exterior = new HashSet<Vector2Int>();
            var stack = new Stack<Vector2Int>();
            stack.Push(new Vector2Int(minX - 1, minY - 1));
            while (stack.Count > 0)
            {
                var c = stack.Pop();
                if (exterior.Contains(c)) continue;
                exterior.Add(c);

                foreach (var (offset, _) in Directions)
                {
                    var n = c + offset;
                    // only explore within expanded bounds
                    if (n.x < minX - 1 || n.x > maxX + 1 || n.y < minY - 1 || n.y > maxY + 1)
                        continue;
                    if (loopSet.Contains(n)) continue;
                    stack.Push(n);
                }
            }

            // Any cell inside the bounding rect that’s neither loop nor exterior is interior
            for (int x = minX; x <= maxX; x++)
            {
                for (int y = minY; y <= maxY; y++)
                {
                    var c = new Vector2Int(x, y);
                    if (loopSet.Contains(c) || exterior.Contains(c)) continue;
                    // spawn pond tile
                    if (pondPrefab != null)
                    {
                        Vector3 pos = GridManager.Instance.GetWorldPosition(c);
                        Instantiate(
                            pondPrefab,
                            pos,
                            Quaternion.identity
                        );
                    }
                }
            }
        }
    }
}