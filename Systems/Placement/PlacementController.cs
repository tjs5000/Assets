using UnityEngine;
using PlexiPark.Data;
using PlexiPark.Managers;
using PlexiPark.Systems.CameraControl;

namespace PlexiPark.Systems.Placement
{
    /// <summary>
    /// Orchestrates the placement flow: wiring input, UI, ghost, validation, and commit logic.
    /// </summary>
    public class PlacementController : MonoBehaviour
    {
        public static PlacementController Instance { get; private set; }

        [SerializeField] private PlacementInputHandler inputHandler;
        [SerializeField] private GhostController ghostController;
        [SerializeField] private PlacementUIController uiController;
[Header("Edge-Pan Settings")]
[Tooltip("How close (as % of screen) the ghost must be before panning begins.")]
[SerializeField] private float edgeThreshold = 0.1f;

[Tooltip("World units per second to pan when at edge.")]
[SerializeField] private float panSpeed = 5f;

        private ParkObjectData currentData;
        private Quaternion currentRotation;
        private bool isPlacing;

        void Awake()
        {
            if (Instance != null)
            {
                Destroy(gameObject);
                return;
            }
            Instance = this;
            DontDestroyOnLoad(gameObject);
        }

        void OnEnable()
        {
            uiController.OnBeginPlacement += HandleBeginPlacement;
            uiController.OnCommitPressed += HandleCommitPlacement;
            uiController.OnCancelPressed += HandleCancelPlacement;
            uiController.OnRotatePressed += HandleRotatePlacement;

            inputHandler.OnPointerMoved += HandlePointerMoved;
            inputHandler.OnPointerDown += HandlePointerDown;
            inputHandler.OnPointerUp += HandlePointerUp;

        }

        void OnDisable()
        {
            uiController.OnBeginPlacement -= HandleBeginPlacement;
            uiController.OnCommitPressed -= HandleCommitPlacement;
            uiController.OnCancelPressed -= HandleCancelPlacement;
            uiController.OnRotatePressed -= HandleRotatePlacement;

            inputHandler.OnPointerMoved -= HandlePointerMoved;
            inputHandler.OnPointerDown -= HandlePointerDown;
            inputHandler.OnPointerUp -= HandlePointerUp;
        }

        public void HandleBeginPlacement(ParkObjectData data)
        {
            currentData = data;
            currentRotation = Quaternion.identity;
            isPlacing = true;

            // Spawn ghost at center‐screen grid cell
            Vector2 screenCenter = new Vector2(Screen.width * .5f, Screen.height * .5f);
            Vector3 worldPoint = ghostController.ScreenPointToGround(screenCenter);
            Vector2Int startGrid = GridManager.Instance.GetGridCoordinate(worldPoint);
            ghostController.SpawnGhost(data, startGrid);
            uiController.ShowContextUI(ghostController.WrapperTransform);
            // disable free‐camera controls until commit/cancel
            CameraController.Instance.allowUserControl = false;
        }

        private void HandlePointerMoved(Vector2 fingerPos)
        {
            if (!isPlacing) return;

            // 1) Move the ghost under your finger as before
            Vector3 world = ghostController.ScreenPointToGround(fingerPos);
            Vector2Int grid = GridManager.Instance.GetGridCoordinate(world);
            ghostController.MoveGhostTo(grid);

            // 2) Now edge-pan based on the ghost’s screen coords:
            Vector3 ghostScreen = CameraController.Instance.MainCamera
                .WorldToScreenPoint(ghostController.WrapperTransform.position);

            float xMin = Screen.width * edgeThreshold;
            float xMax = Screen.width * (1 - edgeThreshold);
            float yMin = Screen.height * edgeThreshold;
            float yMax = Screen.height * (1 - edgeThreshold);

            Vector3 panDir = Vector3.zero;
            if (ghostScreen.x < xMin) panDir.x = -1f;
            else if (ghostScreen.x > xMax) panDir.x = 1f;
            if (ghostScreen.y < yMin) panDir.z = -1f;
            else if (ghostScreen.y > yMax) panDir.z = 1f;

            if (panDir != Vector3.zero)
                CameraController.Instance.Pan(panDir.normalized * panSpeed * Time.deltaTime);
        }


        private void HandlePointerDown()
        {
            // optional: begin dragging logic
        }

        private void HandlePointerUp()
        {
            // optional: end dragging logic
        }

        private void HandleRotatePlacement()
        {
            if (!isPlacing) return;
            currentRotation *= Quaternion.Euler(0, 90, 0);
            ghostController.RotateGhost(currentRotation);
        }

        private void HandleCommitPlacement()
        {
            if (!isPlacing) return;
            Vector2Int origin = ghostController.CurrentGridOrigin;

            // only commit if valid
            if (!PlacementValidator.IsValid(currentData, origin))
                return;

            ObjectManager.Instance.SpawnFinalObject(currentData, origin, currentRotation);
            ghostController.DespawnGhost();
            uiController.HideContextUI();
            isPlacing = false;
            CameraController.Instance.allowUserControl = true;
        }

        private void HandleCancelPlacement()
        {
            if (!isPlacing) return;

            ghostController.DespawnGhost();
            uiController.HideContextUI();

            isPlacing = false;
            CameraController.Instance.allowUserControl = true;
        }

    }
}
