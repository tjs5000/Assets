// Assets/Systems/Placement/GhostController.cs
using UnityEngine;
using PlexiPark.Data;
using PlexiPark.Managers;
using System.Collections.Generic;

namespace PlexiPark.Systems.Placement
{
    /// <summary>
    /// Manages spawning, moving, rotating, and material updates for the placement ghost.
    /// </summary>
    public class GhostController : MonoBehaviour
    {
        [SerializeField] private Material validMaterial;
        [SerializeField] private Material invalidMaterial;

        private GameObject wrapper;
        private GameObject preview;
        private List<Renderer> renderers = new List<Renderer>();
        private ParkObjectData currentData;           // <-- store the data asset

        public Vector2Int CurrentGridOrigin { get; private set; }

        /// <summary>
        /// Create the ghost wrapper & preview and initialize materials.
        /// </summary>
        public void SpawnGhost(ParkObjectData data, Vector2Int startGrid)
        {
            currentData = data;  // store for later validation

            Vector3 world = GridManager.Instance.GetWorldPosition(startGrid);
            wrapper = new GameObject($"GhostWrapper_{data.DisplayName}");
            wrapper.transform.position = world;

            preview = Instantiate(data.Prefab, wrapper.transform);
            CollectRenderers();

            CurrentGridOrigin = startGrid;
            UpdateMaterial(PlacementValidator.IsValid(currentData, startGrid));
        }

        /// <summary>
        /// Move the ghost to a new grid cell and re‐validate.
        /// </summary>
        public void MoveGhostTo(Vector2Int grid)
        {
            if (wrapper == null) return;

            Vector3 world = GridManager.Instance.GetWorldPosition(grid);
            wrapper.transform.position = world;
            CurrentGridOrigin = grid;

            // use the stored data (not GetComponent) for validation
            bool valid = PlacementValidator.IsValid(currentData, grid);
            UpdateMaterial(valid);
        }

        /// <summary>
        /// Rotate the ghost wrapper.
        /// </summary>
        public void RotateGhost(Quaternion rotation)
        {
            if (wrapper == null) return;
            wrapper.transform.rotation = rotation;
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
            Ray ray = Camera.main.ScreenPointToRay(screenPos);
            if (Physics.Raycast(ray, out RaycastHit hit, Mathf.Infinity, LayerMask.GetMask("Terrain")))
                return hit.point;
            return Vector3.zero;
        }


        public Transform WrapperTransform => wrapper != null ? wrapper.transform : null;

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
