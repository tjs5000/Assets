// Assets/Systems/Placement/ParkBuilder.cs
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using PlexiPark.Data;
using PlexiPark.Managers;
using PlexiPark.Systems.CameraControl;

namespace PlexiPark.Systems
{
    public class ParkBuilder : MonoBehaviour
    {
        private GridManager gridManager;
        private LayerMask groundLayerMask;

        [Header("Edge-Pan Settings")]
        [Tooltip("How close (in % of screen) to the border before camera pans")]
        [Range(0.0f, 0.5f)]
        public float edgeThreshold = 0.1f;      // 10% in from each side

        [Tooltip("World units per second to pan when at edge")]
        public float panSpeed = 5f;

        public static ParkBuilder Instance { get; private set; }

        [Header("Ghost Settings")]
        public Material ghostMaterialValid;
        public Material ghostMaterialInvalid;

        [Header("Context UI Prefab")]
        public GameObject contextIconCanvasPrefab;

        [SerializeField] private Transform UIRoot;

        private GameObject ghostInstance;
        private Quaternion currentRotation = Quaternion.identity;
        private ParkObjectData currentData;
        private Vector2Int currentGridOrigin;
        private bool isPlacing = false;

        private List<Renderer> ghostRenderers = new List<Renderer>();
        private GhostContextUI contextUI;
        private GameObject contextIconInstance;

        // Dragging state
        private bool isDraggingGhost = false;
        private Camera mainCamera;
        private LayerMask ghostLayerMask;

        void Awake()
        {
            gridManager = GridManager.Instance;
            groundLayerMask = LayerMask.GetMask("Terrain");

            mainCamera = Camera.main;
            ghostLayerMask = LayerMask.GetMask("Ghost");

            if (Instance != null)
            {
                Destroy(gameObject);
                return;
            }
            Instance = this;
            DontDestroyOnLoad(gameObject);
        }
        void Update()
        {
#if UNITY_EDITOR
            if (Input.GetKeyDown(KeyCode.Escape))
                CancelPlacement();
#endif
            if (!isPlacing || currentData == null || Input.touchCount == 0)
                return;

            // Always update preview position and dragging
            UpdateGhostPosition();
            HandleGhostDragging();

            // Edge-pan if finger is held near screen edge
            Touch touch = Input.GetTouch(0);
            if (touch.phase == TouchPhase.Moved || touch.phase == TouchPhase.Stationary)
            {
                HandleEdgePan(touch.position);
            }
        }

        #region Public API

        public void BeginPlacement(ParkObjectData data)
        {

            // ensure we have a live Camera reference
            if (mainCamera == null)
            {
                mainCamera = Camera.main;
                if (mainCamera == null)
                {
                    Debug.LogError("❌ ParkBuilder could not find a Camera tagged 'MainCamera'.");
                    return;
                }
            }

            // Make sure our GridManager is live
            if (gridManager == null)
            {
                gridManager = GridManager.Instance;
                if (gridManager == null)
                {
                    Debug.LogError("❌ ParkBuilder could not find a GridManager in the scene.");
                    return;
                }
            }


            if (isPlacing)
                CancelPlacement();

            Debug.Log($"🏗️ Begin placement for: {data.DisplayName}");

            currentData = data;
            currentRotation = Quaternion.identity;

            if (data.Prefab == null)
            {
                Debug.LogError("❌ ParkObjectData has no prefab assigned!");
                currentData = null;
                return;
            }

            // 1a. Find starting grid under screen center
            Vector2 screenCenter = new Vector2(Screen.width * 0.5f, Screen.height * 0.5f);
            Ray centerRay = mainCamera.ScreenPointToRay(screenCenter);
            Vector2Int initGrid = currentGridOrigin;
            if (Physics.Raycast(centerRay, out RaycastHit centerHit, Mathf.Infinity, groundLayerMask))
            {
                initGrid = gridManager.GetGridCoordinate(centerHit.point);
            }

            // 1b. Create wrapper at that grid cell
            Vector3 cellCenter = gridManager.GetWorldPosition(initGrid);
            GameObject wrapper = new GameObject($"GhostWrapper_{data.DisplayName}");
            wrapper.transform.position = cellCenter;
            SetLayerRecursively(wrapper, LayerMask.NameToLayer("Ghost"));

            // 2. Instantiate preview under wrapper
            GameObject preview = Instantiate(data.Prefab, wrapper.transform, worldPositionStays: false);

            // Collect renderers for material swaps
            ghostRenderers.Clear();
            foreach (var r in preview.GetComponentsInChildren<Renderer>())
                ghostRenderers.Add(r);

            // Layer-mask only the preview mesh
            SetLayerRecursively(preview, LayerMask.NameToLayer("Ghost"));

            // Use wrapper as the ghostInstance to move & rotate
            ghostInstance = wrapper;
            currentGridOrigin = initGrid;

            // 3. Initial validity check & material
            bool initialValid = ValidatePlacement(currentGridOrigin);
            SetGhostMaterial(initialValid);

            // 4. Enter placement mode
            isPlacing = true;
            if (CameraController.Instance != null)
                CameraController.Instance.allowUserControl = false;

            // 5. Spawn context UI

            if (contextIconCanvasPrefab == null)
            {
                Debug.LogError("❌ ParkBuilder.Context UI Prefab is not assigned! Please drag your ContextIconCanvas prefab into the inspector.");
                return;
            }
            if (UIRoot == null)
            {
                Debug.LogError("❌ ParkBuilder.UIRoot is not assigned! Please drag your world‐space Canvas or UI container into the inspector.");
                return;
            }

            contextIconInstance = Instantiate(contextIconCanvasPrefab);
            Debug.Log($"ContextIcon instance: {contextIconInstance.name} at {contextIconInstance.transform.position}");

            contextIconInstance.transform.SetParent(UIRoot, worldPositionStays: false);
            contextUI = contextIconInstance.GetComponentInChildren<GhostContextUI>();
            if (contextUI == null)
            {
                Debug.LogError("❌ ContextIconCanvas prefab is missing a GhostContextUI component.");
                return;
            }
            contextUI.Initialize(ghostInstance.transform);

            // Hook up buttons
            if (contextUI.commitButton != null) contextUI.commitButton.onClick.AddListener(TryCommitPlacement);
            if (contextUI.cancelButton != null) contextUI.cancelButton.onClick.AddListener(CancelPlacement);
            if (contextUI.rotateButton != null) contextUI.rotateButton.onClick.AddListener(RotateGhostClockwise);

            Debug.Log("👻 Ghost created and ready to move.");
        }

        #endregion

        #region Ghost Movement & Dragging

        private void HandleGhostDragging()
        {
            if (Input.touchCount == 0) return;
            Touch t = Input.GetTouch(0);
            Vector2 pos = t.position;

            if (t.phase == TouchPhase.Began && !isDraggingGhost)
            {
                Ray ray = mainCamera.ScreenPointToRay(pos);
                if (Physics.Raycast(ray, out RaycastHit hit, Mathf.Infinity, ghostLayerMask))
                {
                    // grabbed any part of the preview mesh?
                    if (hit.collider.transform.IsChildOf(ghostInstance.transform))
                        isDraggingGhost = true;
                }
            }

            if (t.phase == TouchPhase.Moved && isDraggingGhost)
            {
                Vector3 worldPoint = ScreenPointToGround(pos);
                Vector2Int grid = gridManager.GetGridCoordinate(worldPoint);
                MoveGhostTo(grid);
            }

            if (t.phase == TouchPhase.Ended && isDraggingGhost)
                isDraggingGhost = false;
        }

        private Vector3 ScreenPointToGround(Vector2 screenPos)
        {
            Ray ray = mainCamera.ScreenPointToRay(screenPos);
            if (Physics.Raycast(ray, out RaycastHit hit, Mathf.Infinity, groundLayerMask))
                return hit.point;
            return Vector3.zero;
        }

        private void MoveGhostTo(Vector2Int origin)
        {
            currentGridOrigin = origin;
            ghostInstance.transform.position = gridManager.GetWorldPosition(origin);
            ValidatePlacement(origin);
        }

        public void RotateGhostClockwise()
        {
            if (!isPlacing || ghostInstance == null) return;
            currentRotation *= Quaternion.Euler(0f, 90f, 0f);
            ghostInstance.transform.rotation = currentRotation;
        }

        public void RotateGhostCounterClockwise()
        {
            if (!isPlacing || ghostInstance == null) return;
            currentRotation *= Quaternion.Euler(0f, -90f, 0f);
            ghostInstance.transform.rotation = currentRotation;
        }

        #endregion

        #region Preview Update & Validation

        private void UpdateGhostPosition()
        {
            Ray ray = mainCamera.ScreenPointToRay(Input.GetTouch(0).position);
            if (!Physics.Raycast(ray, out RaycastHit hit)) return;

            Vector2Int grid = gridManager.GetGridCoordinate(hit.point);
            currentGridOrigin = grid;
            if (!gridManager.IsWithinBounds(grid)) return;

            Vector3 snapped = gridManager.GetWorldPosition(grid);
            ghostInstance.transform.position = snapped;

            bool valid = ValidatePlacement(grid);
            SetGhostMaterial(valid);
        }

        private void SetGhostMaterial(bool isValid)
        {
            Material mat = isValid ? ghostMaterialValid : ghostMaterialInvalid;
            foreach (var rend in ghostRenderers)
                rend.sharedMaterial = mat;
        }

        #endregion

        #region Commit & Cancel

        private void TryCommitPlacement()
        {
            if (!isPlacing || currentData == null || ghostInstance == null)
            {
                Debug.LogWarning("⛔ No active placement to commit.");
                return;
            }

            // Detach UI listeners
            if (contextUI != null)
            {
                contextUI.commitButton.onClick.RemoveAllListeners();
                contextUI.cancelButton.onClick.RemoveAllListeners();
                contextUI.rotateButton.onClick.RemoveAllListeners();
            }

            // Destroy context UI
            if (contextIconInstance != null)
                Destroy(contextIconInstance);

            // Final validation
            if (!ValidatePlacement(currentGridOrigin))
            {
                Debug.LogWarning($"❌ Invalid placement at {currentGridOrigin}");
                return;
            }

            // Spawn final object
            ObjectManager.Instance.SpawnFinalObject(currentData, currentGridOrigin, currentRotation);
            if (currentData.Category == ParkObjectCategory.Waterway)
                WaterwayManager.Instance.AddSegment(currentGridOrigin, currentData);

            // Cleanup preview
            Destroy(ghostInstance);
            ghostInstance = null;
            currentData = null;
            isPlacing = false;
            contextUI = null;

            // Re-enable camera
            if (CameraController.Instance != null)
                CameraController.Instance.allowUserControl = true;
        }

        public void CancelPlacement()
        {
            if (contextUI != null)
            {
                contextUI.commitButton.onClick.RemoveAllListeners();
                contextUI.cancelButton.onClick.RemoveAllListeners();
                contextUI.rotateButton.onClick.RemoveAllListeners();
            }

            if (contextIconInstance != null)
                Destroy(contextIconInstance);

            if (ghostInstance != null)
                Destroy(ghostInstance);

            currentData = null;
            isPlacing = false;
            contextUI = null;

            if (CameraController.Instance != null)
                CameraController.Instance.allowUserControl = true;
        }

        #endregion

        #region Validation

        private bool ValidatePlacement(Vector2Int origin)
        {
            Debug.Log($"🧪 ValidatePlacement at {origin}");

            if (currentData == null) { Debug.LogError("currentData is null."); return false; }
            if (currentData.Footprint == null) { Debug.LogError("Footprint is null."); return false; }
            if (currentData.AllowedSlopes == null) { Debug.LogError("AllowedSlopes is null."); return false; }
            if (!gridManager.IsWithinBounds(origin)) { Debug.LogWarning("Out of bounds."); return false; }

            // Check each footprint cell
            foreach (var offset in currentData.Footprint)
            {
                Vector2Int coord = origin + offset;
                if (!gridManager.IsWithinBounds(coord)) { Debug.LogWarning("Out of bounds."); return false; }
                if (gridManager.IsCellOccupied(coord)) { Debug.LogWarning("Occupied."); return false; }

                var cell = gridManager.GetCell(coord);
                if (!currentData.AllowedSlopes.Contains(cell.slope))
                {
                    Debug.LogWarning($"Slope {cell.slope} not allowed.");
                    return false;
                }
            }

            // Park-type rules
            bool canPlace = ParkTypeRules.Instance.CanPlaceObject(currentData);
            Debug.Log($"✅ ParkTypeRules.CanPlaceObject: {canPlace}");
            return canPlace;
        }

        #endregion

        #region Edge-Pan

        private void HandleEdgePan(Vector2 screenPos)
        {
            float xMin = Screen.width * edgeThreshold;
            float xMax = Screen.width * (1 - edgeThreshold);
            float yMin = Screen.height * edgeThreshold;
            float yMax = Screen.height * (1 - edgeThreshold);

            Vector3 panDir = Vector3.zero;
            if (screenPos.x < xMin) panDir.x = -1f;
            else if (screenPos.x > xMax) panDir.x = 1f;
            if (screenPos.y < yMin) panDir.z = -1f;
            else if (screenPos.y > yMax) panDir.z = 1f;

            if (panDir != Vector3.zero && CameraController.Instance != null)
            {
                CameraController.Instance.Pan(panDir.normalized * panSpeed * Time.deltaTime);
            }
        }

        #endregion

        #region Helpers

        private void SetLayerRecursively(GameObject obj, int layer)
        {
            obj.layer = layer;
            foreach (Transform child in obj.transform)
                SetLayerRecursively(child.gameObject, layer);
        }

        #endregion
    }
}
