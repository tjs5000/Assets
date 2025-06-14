// Assets/Systems/ParkBuilder.cs
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

        private GameObject ghostInstance;
        private Quaternion currentRotation = Quaternion.identity;
        private ParkObjectData currentData;
        private Vector2Int currentGridOrigin;
        private bool isPlacing = false;

        private List<Renderer> ghostRenderers = new List<Renderer>();
        private GhostContextUI contextUI;
        private GameObject contextIconInstance;

        // NEW:
        private bool isDraggingGhost = false;
        private Camera mainCamera;
        private LayerMask ghostLayerMask;


        [SerializeField] private Transform UIRoot;

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

            // Always update the ghost under the finger
            UpdateGhostPosition();
            HandleGhostDragging();
            var touch = Input.GetTouch(0);

            // While the finger is down, check for edge-panning
            if (touch.phase == TouchPhase.Moved || touch.phase == TouchPhase.Stationary)
            {
                HandleEdgePan(touch.position);
            }

            // On release, commit
            if (touch.phase == TouchPhase.Ended)
                TryCommitPlacement();


        }

        #region Public API

        public void BeginPlacement(ParkObjectData data)
        {
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

            // Instantiate ghost and collect renderers
            ghostInstance = Instantiate(data.Prefab);

            ghostRenderers.Clear();
            SetLayerRecursively(ghostInstance, LayerMask.NameToLayer("Ghost"));
            foreach (var r in ghostInstance.GetComponentsInChildren<Renderer>())
                ghostRenderers.Add(r);


            // Show invalid material until user moves

            SetGhostMaterial(false);

            isPlacing = true;
            if (CameraController.Instance != null)
                CameraController.Instance.allowUserControl = false;

            // Spawn context UI
            contextIconInstance = Instantiate(contextIconCanvasPrefab);
            Debug.Log("ContextIcon instance: " + contextIconInstance.name + " at " +
             contextIconInstance.transform.position);

           // contextIconInstance.transform.SetParent(ghostInstance.transform, false);
            contextIconInstance.transform.SetParent(UIRoot, worldPositionStays: false);
            contextUI = contextIconInstance.GetComponent<GhostContextUI>();
            contextUI.Initialize(ghostInstance.transform);

            // Hook up buttons
            contextUI.commitButton.onClick.AddListener(TryCommitPlacement);
            contextUI.cancelButton.onClick.AddListener(CancelPlacement);
            contextUI.rotateButton.onClick.AddListener(RotateGhostClockwise);

            // Center camera
            if (CameraController.Instance != null)
                CameraController.Instance.CenterOn(ghostInstance.transform.position);

            Debug.Log("👻 Ghost created and ready to move.");
        }


        private void HandleGhostDragging()
        {
            if (Input.touchCount == 0) return;
            Touch t = Input.GetTouch(0);
            Vector2 pos = t.position;

            if (t.phase == TouchPhase.Began && !isDraggingGhost)
            {
                Ray ray = mainCamera.ScreenPointToRay(pos);
                if (Physics.Raycast(ray, out RaycastHit hit, Mathf.Infinity, ghostLayerMask))
                    if (hit.collider.gameObject == ghostInstance)
                        isDraggingGhost = true;
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
            // your existing placement-validation & movement call
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

        #region Ghost Logic

        private void UpdateGhostPosition()
        {
            Ray ray = Camera.main.ScreenPointToRay(Input.GetTouch(0).position);
            if (!Physics.Raycast(ray, out RaycastHit hit)) return;

            Vector2Int grid = GridManager.Instance.GetGridCoordinate(hit.point);
            currentGridOrigin = grid;
            if (!GridManager.Instance.IsWithinBounds(grid)) return;

            Vector3 snapped = GridManager.Instance.GetWorldPosition(grid);
            ghostInstance.transform.position = snapped;

            bool valid = ValidatePlacement(grid);
            SetGhostMaterial(valid);
        }

        private void SetGhostMaterial(bool isValid)
        {
            var mat = isValid ? ghostMaterialValid : ghostMaterialInvalid;
            foreach (var rend in ghostRenderers)
                rend.sharedMaterial = mat;
        }

        #endregion

        #region Placement

        private void TryCommitPlacement()
        {
            if (!isPlacing || currentData == null || ghostInstance == null)
            {
                Debug.LogWarning("⛔ No active placement to commit.");
                return;
            }

            // Detach UI listeners
            contextUI.commitButton.onClick.RemoveAllListeners();
            contextUI.cancelButton.onClick.RemoveAllListeners();
            contextUI.rotateButton.onClick.RemoveAllListeners();

            // Remove context UI
            if (contextIconInstance != null)
                Destroy(contextIconInstance);

            if (!ValidatePlacement(currentGridOrigin))
            {
                Debug.LogWarning($"❌ Invalid placement at {currentGridOrigin}");
                return;
            }

            // Commit via ObjectManager
            ObjectManager.Instance.SpawnFinalObject(currentData, currentGridOrigin, currentRotation);

            if (currentData.Category == ParkObjectCategory.Waterway)
                WaterwayManager.Instance.AddSegment(currentGridOrigin, currentData);


            // Cleanup ghost
            Destroy(ghostInstance);
            ghostInstance = null;
            currentData = null;
            isPlacing = false;
            contextUI = null;
            // after Destroy(ghostInstance); … isPlacing = false;
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
            // after Destroy(ghostInstance); … isPlacing = false;
            if (CameraController.Instance != null)
                CameraController.Instance.allowUserControl = true;
        }

        private bool ValidatePlacement(Vector2Int origin)
        {
            Debug.Log($"🧪 ValidatePlacement at {origin}");

            if (currentData == null) { Debug.LogError("currentData is null."); return false; }
            if (currentData.Footprint == null) { Debug.LogError("Footprint is null."); return false; }
            if (currentData.AllowedSlopes == null) { Debug.LogError("AllowedSlopes is null."); return false; }
            if (!GridManager.Instance.IsWithinBounds(origin)) { Debug.LogWarning("Out of bounds."); return false; }

            // Footprint & slope checks
            foreach (var offset in currentData.Footprint)
            {
                var coord = origin + offset;
                if (!GridManager.Instance.IsWithinBounds(coord)) { Debug.LogWarning("Out of bounds."); return false; }
                if (GridManager.Instance.IsCellOccupied(coord)) { Debug.LogWarning("Occupied."); return false; }

                var cell = GridManager.Instance.GetCell(coord);
                if (!currentData.AllowedSlopes.Contains(cell.slope))
                {
                    Debug.LogWarning($"Slope {cell.slope} not allowed.");
                    return false;
                }
            }

            // Park-type rule
            bool canPlace = ParkTypeRules.Instance.CanPlaceObject(currentData);
            Debug.Log($"✅ ParkTypeRules.CanPlaceObject: {canPlace}");
            return canPlace;
        }

        #endregion



        private void HandleEdgePan(Vector2 screenPos)
        {
            // compute the inset rectangle
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
                // move in world XZ plane
                CameraController.Instance.Pan(panDir.normalized * panSpeed * Time.deltaTime);
            }
        }

        private void SetLayerRecursively(GameObject obj, int layer)
        {
            obj.layer = layer;
            foreach (Transform child in obj.transform)
                SetLayerRecursively(child.gameObject, layer);
        }

    }
}

