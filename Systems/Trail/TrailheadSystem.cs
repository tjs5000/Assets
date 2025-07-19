// Assets/Systems/Trail/TrailheadSystem.cs
using System.Collections.Generic;
using UnityEngine;
using PlexiPark.Core.SharedEnums;  // TrailType lives here
using PlexiPark.Core.Utils;
using PlexiPark.Managers;

namespace PlexiPark.Systems.Trail
{
    [DefaultExecutionOrder(1000)]
    public class TrailheadSystem : MonoBehaviour
    {
        public static TrailheadSystem Instance { get; private set; }

        [Header("Prefab & Placement")]
        public GameObject trailheadPrefab;
        public Transform parentContainer;

        public float yOffset = 0.1f;

        private void Awake()
        {
            if (Instance != null && Instance != this) Destroy(gameObject);
            else Instance = this;
        }

        /// <summary>
        /// Spawns & initializes a trailhead at the given grid-cell.
        /// </summary>
        public GameObject CreateTrailheadAt(
    Vector2Int cell,
    TrailType planType,
    List<Vector2Int> pathCells
)
        {
            if (trailheadPrefab == null) return null;

            float nodeSize = GridManager.Instance.CellSize;
            float half = nodeSize * 0.5f;
            Vector3 origin = GridManager.Instance.origin;

            // 1) build base X,Z (no Y)
            Vector3 worldPos = new Vector3(
              origin.x + cell.x * nodeSize + nodeSize,
              0f,
              origin.z + cell.y * nodeSize + nodeSize
            );

            // 2) sample terrain height with a Physics.Raycast
            //    (make sure your terrain chunks have colliders on the "Terrain" layer)
            const float rayHeight = 50f;
            const float rayDistance = 100f;
            int terrainLayer = LayerMask.NameToLayer("Terrain");
            int terrainMask = 1 << terrainLayer;

            Ray ray = new Ray(worldPos + Vector3.up * rayHeight, Vector3.down);
            if (Physics.Raycast(ray, out var hit, rayDistance, terrainMask))
            {
                worldPos.y = hit.point.y + yOffset;
            }
            else
            {
                // fallback to your corner-map sampler
                float h = GridManager.Instance.SampleHeight(worldPos.x, worldPos.z);
                worldPos.y = h + yOffset;
            }

            // 3) apply any parentContainer offset
            Transform parent = parentContainer != null ? parentContainer : transform;
            var go = Instantiate(
        trailheadPrefab,
        worldPos,
        Quaternion.identity,
        parentContainer != null ? parentContainer : transform
    );
            go.name = $"Trailhead_{planType}_{cell.x}_{cell.y}";

            // ——————————————
            // orient the sign to face back down the trail:
            if (pathCells.Count > 1)
            {
                // world‐space centers of the first two cells
                Vector3 a = GridManager.Instance.GetWorldCenter(pathCells[0]);
                Vector3 b = GridManager.Instance.GetWorldCenter(pathCells[1]);
                Vector3 dir = (b - a);
                dir.y = 0;           // only yaw
                if (dir.sqrMagnitude > 0.0001f)
                {
                    // we want the sign to look *away* from b, so flip
                    go.transform.rotation = Quaternion.LookRotation(-dir.normalized, Vector3.up);
                }
            }

            // now initialize your Trailhead component
            var th = go.GetComponent<Trailhead>();
            th.Initialize(planType, pathCells, nodeSize);

            return go;
        }
    }
}