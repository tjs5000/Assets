// Assets/Systems/Placement/PlacementController.cs

using UnityEngine;
using PlexiPark.Data;
using PlexiPark.Managers;
using PlexiPark.Core.SharedEnums;
using PlexiPark.Systems.Input.Interfaces;
using PlexiPark.Systems.CameraControl;
using Unity.AI.Navigation;
using PlexiPark.Systems.Input;

namespace PlexiPark.Systems.Placement
{
    public class PlacementController : MonoBehaviour
    {
        [Header("References (drag the Component, not the GameObject)")]
        [SerializeField] private GhostController ghostController;  // must implement IGhostController
        [SerializeField] private InputModeRouter inputRouter;      // must implement IInputRouter
        [SerializeField] private CameraRig cameraRig;        // must implement ICameraRig
        [SerializeField] private PlacementUIController uiController;

        [Header("Pan Settings")]
        [SerializeField] private float panSpeed = 10f;
        [SerializeField] private float edgeThreshold = 0.05f;

        // no more UnityEngine.Object fields; we can use these directly:
        private IGhostController ghostCtrl;
        private IInputRouter inputRt;
        private ICameraRig camRig;

        private ParkObjectData currentData;
        private Quaternion currentRotation = Quaternion.identity;
        private bool isPlacing = false;

        private void Awake()
        {
            // concrete-to-interface assignment
            ghostCtrl = ghostController;
            inputRt = inputRouter;
            camRig = cameraRig;

            if (ghostCtrl == null)
                Debug.LogError("PlacementController: you must drag a GhostController component into the inspector!");
            if (inputRt == null)
                Debug.LogError("PlacementController: you must drag an InputModeRouter component into the inspector!");
            if (camRig == null)
                Debug.LogError("PlacementController: you must drag a CameraRig component into the inspector!");
        }
        private void OnEnable()
        {
            uiController.OnBeginPlacement += HandleBeginPlacement;
            uiController.OnCommitPressed += HandleCommitPlacement;
            uiController.OnCancelPressed += HandleCancelPlacement;
            uiController.OnRotatePressed += HandleRotatePlacement;

            if (inputRouter != null)
            {
                inputRouter.OnPointerMoved += HandlePointerMoved;
                inputRouter.OnPointerDown += HandlePointerDown;
                inputRouter.OnPointerUp += HandlePointerUp;
            }
            else
            {
                Debug.LogWarning("⚠️ inputRouter is null in PlacementController.OnEnable()");
            }
        }

        private void OnDisable()
        {
            uiController.OnBeginPlacement -= HandleBeginPlacement;
            uiController.OnCommitPressed -= HandleCommitPlacement;
            uiController.OnCancelPressed -= HandleCancelPlacement;
            uiController.OnRotatePressed -= HandleRotatePlacement;

            if (inputRouter != null)
            {
                inputRouter.OnPointerMoved -= HandlePointerMoved;
                inputRouter.OnPointerDown -= HandlePointerDown;
                inputRouter.OnPointerUp -= HandlePointerUp;
            }
            else
            {
                Debug.LogWarning("⚠️ inputRouter is null in PlacementController.OnDisable()");
            }
        }

        private void HandleBeginPlacement(ParkObjectData data)
        {
            if (isPlacing)
            {
                HandleCancelPlacement();
            }

            currentData = data;
            currentRotation = Quaternion.identity;
            isPlacing = true;

            Vector2 screenCenter = new Vector2(Screen.width * 0.5f, Screen.height * 0.5f);
            Vector3 worldPoint = ghostController.ScreenPointToGround(screenCenter);
            Vector2Int startGrid = GridManager.Instance.GetGridCoordinate(worldPoint);
            ghostController.SpawnGhost(data, startGrid);

            uiController.ShowContextUI();
            GameState.I.Mode = InputMode.Placement;
        }

        private void HandlePointerDown(Vector2 fingerPos) { }
        private void HandlePointerUp(Vector2 fingerPos) { }

        private void HandlePointerMoved(Vector2 screenPos)
        {
            if (!isPlacing) return;

            Vector3 world = ghostController.ScreenPointToGround(screenPos);
            Vector2Int grid = GridManager.Instance.GetGridCoordinate(world);
            ghostController.MoveGhostTo(grid);

            Vector3 ghostScreen = Camera.main.WorldToScreenPoint(ghostController.WrapperTransform.position);

            float xMin = Screen.width * edgeThreshold;
            float xMax = Screen.width * (1f - edgeThreshold);
            float yMin = Screen.height * edgeThreshold;
            float yMax = Screen.height * (1f - edgeThreshold);

            Vector3 panDir = Vector3.zero;
            if (ghostScreen.x < xMin) panDir.x = -1f;
            else if (ghostScreen.x > xMax) panDir.x = 1f;
            if (ghostScreen.y < yMin) panDir.z = -1f;
            else if (ghostScreen.y > yMax) panDir.z = 1f;

            if (panDir != Vector3.zero)
            {
                CameraPanner.I?.PanInstant(panDir.normalized * panSpeed * Time.deltaTime);
                cameraRig.ClampWithinBounds();
            }
        }

        private void HandleCommitPlacement()
        {

            Debug.Log("[PlacementController] → HandleCommitPlacement()");
            Debug.Log($"> ghostController = {ghostController}");
            Debug.Log($"> currentData   = {currentData?.name}");
            Debug.Log($"> ObjectManager = {ObjectManager.Instance}");
            Debug.Log($"> uiController  = {uiController}");
            Debug.Log($"> GameState.I   = {GameState.I}");
            if (!isPlacing) return;
            if (ghostController == null)
            {
                Debug.LogError("❌ Cannot commit placement – IGhostController not hooked up on PlacementController!");
                return;
            }
            if (currentData == null)
            {
                Debug.LogError("❌ No ParkObjectData (`currentData`) – did you forget to BeginPlacement?");
                return;
            }
            Vector2Int origin = ghostController.CurrentGridOrigin;
            if (!PlacementValidator.IsValid(currentData, origin))
            {
                Debug.Log("[PlacementController] → placement invalid, aborting");
                return;
            }

            if (ObjectManager.Instance == null)
            {
                Debug.LogError("❌ ObjectManager.Instance is null! Make sure you have an ObjectManager in the scene.");
                return;
            }

            ObjectManager.Instance.SpawnFinalObject(currentData, origin, currentRotation);


            if (uiController == null)
                Debug.LogWarning("⚠️ uiController is null, skipping HideContextUI()");
            else
                uiController.HideContextUI();

            isPlacing = false;

            if (GameState.I == null)
                Debug.LogWarning("⚠️ GameState.I is null, cannot set Mode");
            else
                GameState.I.Mode = InputMode.Idle;

            ghostController.DespawnGhost();
            isPlacing = false;


            var surfaces = FindObjectsByType<NavMeshSurface>(FindObjectsSortMode.None);
            foreach (var surf in surfaces)
                surf.BuildNavMesh();

            Debug.Log("[PlacementController] Placement committed and mode set to Idle");
        }

        private void HandleCancelPlacement()
        {
            if (!isPlacing) return;

            ghostController.DespawnGhost();
            uiController.HideContextUI();
            isPlacing = false;
            GameState.I.Mode = InputMode.Idle;
        }

        private void HandleRotatePlacement()
        {
            if (!isPlacing) return;

            currentRotation *= Quaternion.Euler(0f, 90f, 0f);
            ghostController.ApplyRotation(currentRotation);
        }
    }
}
