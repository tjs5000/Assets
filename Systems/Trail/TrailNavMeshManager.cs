// Assets/Systems/Trail/TrailNavMeshManager.cs
using System.Collections.Generic;
using UnityEngine;
using Unity.AI.Navigation;
using PlexiPark.Core.Interfaces;
using PlexiPark.Managers;

namespace PlexiPark.Systems.Trail
{
    [DefaultExecutionOrder(1500)]
    public class TrailNavMeshManager : MonoBehaviour
    {
        public static TrailNavMeshManager Instance { get; private set; }

        [Header("Bake Settings")]
        [Tooltip("Assign your one-and-only TrailNavMeshSurface (layer = TrailNav)")]
        [SerializeField] private NavMeshSurface _trailSurface;

        [Tooltip("Which layer(s) your TerrainChunk prefab colliders live on")]
        [SerializeField] private LayerMask _terrainMask;

        [Tooltip("Debug material for visualizing the ribbon")]
        [SerializeField] private Material _centerlineMaterial;

        [Tooltip("Width (in world units) of the baked ribbon")]
        [SerializeField] private float _trailWidth;



        // cache the integer layer index here
        private int _trailLayer;



        // track ribbon GOs so we can clean them up
        private readonly Dictionary<int, GameObject> _ribbons = new();

        void Awake()
        {
            if (Instance != null && Instance != this)
                Destroy(gameObject);
            else
                Instance = this;
            //_trailWidth = GridManager.Instance.CellSize;
            _trailLayer = LayerMask.NameToLayer("TrailNav");
        }

        /// <summary>
        /// Generates a little ribbon mesh, assigns it to the TrailNav layer,
        /// and then rebuilds the single Trail-only NavMeshSurface.
        /// </summary>
        public void RegisterTrail(int id, List<Vector3> worldSpine)
        {
            // 1) destroy old ribbon
            if (_ribbons.TryGetValue(id, out var oldGO))
                Destroy(oldGO);

            worldSpine = EdgeCenterSpine(worldSpine, GridManager.Instance.CellSize);

            // 2) build mesh
            var mesh = CenterlineMeshBuilder.BuildWorld(
                worldSpine,
                _trailWidth,
                GridManager.Instance,  // IHeightSampler
                _terrainMask,
                xOffset: 0f,
                yOffset: 0f,
                zOffset: 0f
            );
            if (mesh == null) return;
            mesh.RecalculateBounds();

            float halfW = _trailWidth * 0.5f;

            // 3) create GO
            var go = new GameObject($"TrailNav_{id}");
            go.transform.SetParent(transform);
            go.transform.localPosition = Vector3.zero;

            // 4) assign layer so only _trailSurface picks it up
            go.layer = _trailLayer;
            foreach (var t in go.GetComponentsInChildren<Transform>(true))
                t.gameObject.layer = _trailLayer;

            // 5) attach mesh & renderer
            var mf = go.AddComponent<MeshFilter>();
            mf.mesh = mesh;
            if (_centerlineMaterial != null)
            {
                var mr = go.AddComponent<MeshRenderer>();
                mr.sharedMaterial = _centerlineMaterial;
            }

            _ribbons[id] = go;

            // 6) rebuild the TrailNavMeshSurface
            if (_trailSurface != null)
                _trailSurface.BuildNavMesh();
            else
                Debug.LogError("TrailNavMeshManager: please assign a TrailNavMeshSurface in the Inspector!");
        }
        public void BuildAllRibbonNav()
        {
            if (_trailSurface == null) return;
            _trailSurface.BuildNavMesh();
            
        }

        /// <summary>
        /// Takes your old center‐based spine and pushes each point
        /// half‐a‐cell in the direction of travel so that it lies
        /// exactly on the edge between grid cells.
        /// </summary>
        static List<Vector3> EdgeCenterSpine(List<Vector3> centerSpine, float cellSize)
        {
            var outList = new List<Vector3>(centerSpine.Count);
            for (int i = 0; i < centerSpine.Count; i++)
            {
                // compute direction of travel in XZ
                Vector3 dir = (i == 0
                    ? centerSpine[1] - centerSpine[0]
                    : i == centerSpine.Count - 1
                        ? centerSpine[^1] - centerSpine[^2]
                        : centerSpine[i + 1] - centerSpine[i - 1]
                );
                dir.y = 0;
                if (dir.sqrMagnitude < 1e-6f) dir = Vector3.forward;
                else dir.Normalize();

                // shift the center by half a cell along that dir
                outList.Add(centerSpine[i] + dir * (cellSize * 0.5f));
            }
            return outList;
        }

    }
}