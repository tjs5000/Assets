// Assets/Systems/Placement/GhostController.cs
using UnityEngine;
using PlexiPark.Data;
using PlexiPark.Managers;
using System.Collections.Generic;
using PlexiPark.Core.Utility;
using PlexiPark.Systems.Input.Interfaces;

namespace PlexiPark.Systems.Placement
{
    /// <summary>
    /// Manages spawning, moving, rotating, and material updates for the placement ghost.
    /// </summary>
    public class GhostController : MonoBehaviour, IGhostController
    {

        public static GhostController Instance { get; private set; }

        [SerializeField] private Material validMaterial;
        [SerializeField] private Material invalidMaterial;

        private GameObject wrapper;

        public Transform WrapperTransform => wrapper.transform;

        private GameObject preview;
        private List<Renderer> renderers = new List<Renderer>();
        private ParkObjectData currentData;           // <-- store the data asset

        public Vector2Int CurrentGridOrigin { get; private set; }


        private void Awake()
        {
            if (Instance != null && Instance != this)
            {
                Destroy(gameObject);
                return;
            }
            Instance = this;
        }


        /// <summary>
        /// Create the ghost wrapper & preview and initialize materials.
        /// </summary>
        public void SpawnGhost(ParkObjectData data, Vector2Int startGrid)
        {
            currentData = data;
            var world = GridManager.Instance.GetWorldPosition(startGrid);
            wrapper = new GameObject($"GhostWrapper_{data.DisplayName}");
            wrapper.transform.position = world;

            // Instantiate the ghost preview, parented under the wrapper:
            preview = Instantiate(
              data.previewPrefab,
              wrapper.transform.position,
              Quaternion.identity,
              wrapper.transform
            );

            CollectRenderers();
            CurrentGridOrigin = startGrid;
            UpdateMaterial(PlacementValidator.IsValid(currentData, startGrid));
        }


        /// <summary>
        /// Move the ghost to a new grid cell and re‐validate.
        /// </summary>
        public void MoveGhostTo(Vector2Int grid)
        {
            if (wrapper == null)
            {
                Debug.LogWarning("[GhostController] MoveGhostTo called but wrapper is null.");
                return;
            }
            Debug.Log($"[GhostController] Moving ghost to grid {grid}");
            Vector3 world = GridManager.Instance.GetWorldPosition(grid);
            wrapper.transform.position = world;
            CurrentGridOrigin = grid;

            // use the stored data (not GetComponent) for validation
            bool valid = PlacementValidator.IsValid(currentData, grid);
            UpdateMaterial(valid);
        }

        public void ApplyRotation(Quaternion rotation)
        {
            if (WrapperTransform != null)
            {
                WrapperTransform.rotation = rotation;
            }
        }

        /// <summary>
        /// Destroy the ghost preview.
        /// </summary>
        public void DespawnGhost()
        {
            if (wrapper != null) Destroy(wrapper);
            wrapper = null;
            preview = null;
            renderers.Clear();
            currentData = null;
        }

        /// <summary>
        /// Raycast a screen point to the terrain to get a world position.
        /// </summary>
        public Vector3 ScreenPointToGround(Vector2 screenPos)
        {
            // ✅ Use TouchWorldUtility instead of local raycasting
            if (TouchWorldUtility.TryGetWorldPoint(screenPos, out Vector3 world, out _))
                return world;

            return Vector3.zero;
        }

        private void CollectRenderers()
        {
            renderers.Clear();
            renderers.AddRange(preview.GetComponentsInChildren<Renderer>());
        }

        private void UpdateMaterial(bool isValid)
        {
            Material mat = isValid ? validMaterial : invalidMaterial;
            foreach (var r in renderers)
                r.sharedMaterial = mat;
        }
    }
}
