using UnityEngine;
using System.Collections;
using PlexiPark.Data;
using PlexiPark.Managers;
using PlexiPark.Systems.SaveLoad;
using UnityEngine.EventSystems;

namespace PlexiPark.Systems.CameraControl
{
    [RequireComponent(typeof(Camera))]
    public class CameraController : MonoBehaviour
    {
        public static CameraController Instance { get; private set; }
        public Camera MainCamera => mainCamera;
        public bool allowUserControl = true;

        [Header("Zoom Profiles")]
        public ZoomProfile[] zoomProfiles;
        private ZoomProfile zoomProfile;

        [Header("Zoom Settings")]
        public float minZoomDistance = 5f;
        public float maxZoomDistance = 400f;
        public float zoomSpeed = 5f;

        [Header("Pan Settings")]
        public float panSpeed = 5f;
        public float panClampBuffer = 5f;

        [Header("Momentum")]
        public float bounceSmoothTime = 0.3f;
        private MomentumTracker momentum = new MomentumTracker();

        [Header("Rotation")]
        public float rotationSpeed = 100f;

        [Header("Tilt")]
        public float pitchAngle = 45f;
        public float minPitch = 30f;
        public float maxPitch = 75f;
        [Range(0f, 1f)] public float tiltSpeed = 0.1f;

        [Header("Smoothing")]
        [Tooltip("How quickly the camera smooths to its target position. Lower is slower/smoother.")]
        [Range(0.01f, 0.5f)] public float smoothSpeed = 0.125f;

        [Header("Terrain Collision")]
        public float minDistanceFromTerrain = 3.0f;
        public LayerMask terrainLayerMask;

        // --- private state ---
        private Camera mainCamera;
        private Vector3 desiredPosition;
        private Vector3 smoothedPosition;

        private Vector3 lookPoint;
        private float targetZoomDistance;
        private float currentZoomVelocity;
        private float rotationAngle;
        private float currentY;

        private enum GestureState { None, Panning, ZoomingOrRotating }
        private GestureState currentGesture = GestureState.None;

        private Vector2 previousSingleFingerPos;
        private Vector2 smoothedSingleFingerDelta;
        private float smoothingFactor = 0.15f;
        private float lastAvgY;
        private bool tiltInitialized;

/// <summary>
/// Exposes the current target zoom distance for debugging.
/// </summary>
public float ZoomDistance => targetZoomDistance;


        #region Unity Lifecycle

        void Awake()
        {
            if (Instance != null)
            {
                Destroy(gameObject);
                return;
            }
            Instance = this;
            DontDestroyOnLoad(gameObject);
            mainCamera = GetComponent<Camera>();
        }

        IEnumerator Start()
        {
            // Determine zoom profile and limits
            SetZoomLimitsByParkType();

            // Initialize lookPoint from saved or grid center
            if (!TryLoadCameraState(out lookPoint))
                lookPoint = GetGridCenter();

            // Initial rotation & distance
            ResetZoomAndRotation();
            currentY = transform.position.y;

            // Align smoothing targets
            desiredPosition = transform.position;
            Vector3 initialDir = (transform.position - lookPoint).normalized;
            targetZoomDistance = Vector3.Distance(transform.position, lookPoint);

            // Wait a frame then load camera state if present
            yield return null;

            var saved = CameraSaveManager.LoadCameraState();
            if (saved.HasValue)
            {
                targetZoomDistance = saved.Value.zoomDistance;  // new field
                ApplyOrbit();                                   // recompute desiredPosition
                Debug.Log("📍 Restored saved camera distance.");

                Debug.Log("📍 Restored saved camera state.");
            }
        }

        void Update()
        {
            ClampLookPointToGrid();
            ApplyOrbit();

            if (!allowUserControl || IsPointerOverUI())
                return;

            if (Input.touchCount > 0)
                momentum.StopMomentum();

#if UNITY_EDITOR || UNITY_STANDALONE
            HandleMouseZoomFallback();
            HandleMousePanFallback();
            HandleMouseRotateFallback();
#endif

            if (Input.touchCount >= 2)
            {
                HandlePinchZoomAndRotate();
                currentGesture = GestureState.ZoomingOrRotating;
            }
            else if (Input.touchCount == 1 &&
                     (currentGesture == GestureState.None ||
                      currentGesture == GestureState.Panning))
            {
                HandleSingleFingerPan();
                currentGesture = GestureState.Panning;
            }
            else
            {
                currentGesture = GestureState.None;
            }

            if (Input.touchCount != 2)
                tiltInitialized = false;

            if (Input.touchCount == 0 && momentum.IsMomentumActive())
            {
                Vector3 move = momentum.GetMomentumDelta(panSpeed, Time.deltaTime);
                move.y = 0f;
                lookPoint += move;
            }

        }

        void LateUpdate()
        {
            smoothedPosition = Vector3.Lerp(
                transform.position,
                desiredPosition,
                smoothSpeed
            );
            transform.position = smoothedPosition;
            transform.LookAt(lookPoint);
        }

        void OnApplicationPause(bool paused)
        {
            if (paused && allowUserControl)
            {
                var state = new CameraState
                {
                    position = transform.position,
                    rotation = transform.rotation,
                    zoomDistance = targetZoomDistance
                };
                CameraSaveManager.SaveCameraState(state);
            }
        }


        void OnApplicationQuit()
        {
            var state = new CameraState
            {
                position = transform.position,
                rotation = transform.rotation,
                zoomDistance = targetZoomDistance
            };
            CameraSaveManager.SaveCameraState(state);
        }
        #endregion

        #region Input Handlers

        private void HandleSingleFingerPan()
        {
            Touch t = Input.GetTouch(0);
            if (t.phase == TouchPhase.Began)
            {
                previousSingleFingerPos = t.position;
                smoothedSingleFingerDelta = Vector2.zero;
            }
            else if (t.phase == TouchPhase.Moved)
            {
                Vector2 rawDelta = t.position - previousSingleFingerPos;
                smoothedSingleFingerDelta = Vector2.Lerp(
                    smoothedSingleFingerDelta,
                    rawDelta,
                    smoothingFactor
                );

                Vector3 right = mainCamera.transform.right;
                Vector3 forward = Vector3.Cross(right, Vector3.up);
                right.y = forward.y = 0f;
                right.Normalize(); forward.Normalize();

                Vector3 worldDelta = (-smoothedSingleFingerDelta.x * right -
                                      smoothedSingleFingerDelta.y * forward)
                                      * panSpeed * Time.deltaTime;
                worldDelta.y = 0f;

                lookPoint += worldDelta;
                momentum.UpdateMomentum(rawDelta);
                previousSingleFingerPos = t.position;
            }
        }

        private void HandlePinchZoomAndRotate()
        {
            Touch t0 = Input.GetTouch(0), t1 = Input.GetTouch(1);
            Vector2 prev0 = t0.position - t0.deltaPosition;
            Vector2 prev1 = t1.position - t1.deltaPosition;

            float zoomDelta = Vector2.Distance(t0.position, t1.position)
                             - Vector2.Distance(prev0, prev1);
            targetZoomDistance = Mathf.Clamp(
                targetZoomDistance - zoomDelta * zoomSpeed,
                minZoomDistance, maxZoomDistance
            );

            float angleDelta = Vector2.SignedAngle(prev1 - prev0,
                                                  t1.position - t0.position);
            rotationAngle += angleDelta * rotationSpeed * Time.deltaTime;

            float avgY = (t0.position.y + t1.position.y) * 0.5f;
            if (!tiltInitialized)
            {
                lastAvgY = avgY;
                tiltInitialized = true;
            }
            else
            {
                float deltaY = avgY - lastAvgY;
                pitchAngle = Mathf.Clamp(
                    pitchAngle - deltaY * tiltSpeed,
                    minPitch, maxPitch
                );
                lastAvgY = avgY;
            }

            momentum.StopMomentum();
        }

        public void Pan(Vector3 panVector)
        {
            if (panVector == Vector3.zero) return;
            lookPoint += panVector;
            ClampLookPointToGrid();
            ApplyOrbit();
        }

        #endregion

        #region Camera Controls

        private void ApplyOrbit()
        {
            float smoothedDistance = Mathf.SmoothDamp(
                Vector3.Distance(transform.position, lookPoint),
                targetZoomDistance,
                ref currentZoomVelocity,
                bounceSmoothTime
            );

            float yawRad = Mathf.Deg2Rad * rotationAngle;
            float pitchRad = Mathf.Deg2Rad * -pitchAngle;

            Vector3 dir = new Vector3(
                Mathf.Sin(yawRad) * Mathf.Cos(pitchRad),
                Mathf.Sin(pitchRad),
                Mathf.Cos(yawRad) * Mathf.Cos(pitchRad)
            );

            Vector3 targetPosition = lookPoint - dir * smoothedDistance;

            if (Physics.Raycast(
                targetPosition, Vector3.down,
                out RaycastHit hit, 100f, terrainLayerMask))
            {
                float distToGround = targetPosition.y - hit.point.y;
                if (distToGround < minDistanceFromTerrain)
                {
                    float liftY = hit.point.y + minDistanceFromTerrain;
                    currentY = Mathf.Max(
                        currentY,
                        Mathf.Lerp(currentY, liftY, 0.1f)
                    );
                    targetPosition.y = currentY;
                }
            }

            desiredPosition = targetPosition;
        }

        #endregion

        #region Clamping & Helpers

        private bool IsPointerOverUI()
        {
            if (EventSystem.current == null) return false;
#if UNITY_EDITOR || UNITY_STANDALONE
            return EventSystem.current.IsPointerOverGameObject();
#else
            return Input.touchCount > 0 &&
                   EventSystem.current.IsPointerOverGameObject(
                       Input.GetTouch(0).fingerId);
#endif
        }

        private void ClampLookPointToGrid()
        {
            if (GridManager.Instance == null) return;
            float cell = GridManager.Instance.CellSize;
            Vector2 origin = GridManager.Instance.origin;
            lookPoint.x = Mathf.Clamp(
                lookPoint.x,
                origin.x,
                origin.x + GridManager.Instance.GridWidth * cell
            );
            lookPoint.z = Mathf.Clamp(
                lookPoint.z,
                origin.y,
                origin.y + GridManager.Instance.GridDepth * cell
            );
        }

        private Vector3 GetGridCenter()
        {
            if (GridManager.Instance == null) return Vector3.zero;
            var size = new Vector2Int(
                GridManager.Instance.GridWidth,
                GridManager.Instance.GridDepth
            );
            return GridManager.Instance.GetWorldPosition(size / 2);
        }

        #endregion

        #region Save/Load Helpers

        private bool TryLoadCameraState(out Vector3 loaded)
        {
            var state = CameraSaveManager.LoadCameraState();
            if (!state.HasValue)
            {
                loaded = Vector3.zero;
                return false;
            }

            loaded = state.Value.position;
            return true;
        }

        public void ResetZoomAndRotation()
        {
            pitchAngle = 45f;
            rotationAngle = 0f;
            targetZoomDistance = 20f;
            ApplyOrbit();
        }

        private void SetZoomLimitsByParkType()
        {
            if (GridManager.Instance == null || zoomProfiles == null)
                return;

            ParkType type = GridManager.Instance.currentParkType;
            foreach (var p in zoomProfiles)
            {
                if (p.parkType == type)
                {
                    zoomProfile = p;
                    minZoomDistance = p.minZoom;
                    maxZoomDistance = p.maxZoom;
                    return;
                }
            }

            Debug.LogWarning($"No ZoomProfile for {type}; using defaults.");
            zoomProfile = null;
            minZoomDistance = 5f;
            maxZoomDistance = 400f;
        }

        public void SnapToCenter()
        {
            lookPoint = GetGridCenter();
            ApplyOrbit();
        }

        public void CenterOn(Vector3 worldPoint, float smoothTime = 0.25f)
        {
            StopAllCoroutines();
            StartCoroutine(CenterCoroutine(worldPoint, smoothTime));
        }

        private IEnumerator CenterCoroutine(Vector3 worldPoint, float time)
        {
            Vector3 start = transform.position;
            Vector3 target = new Vector3(worldPoint.x, start.y, worldPoint.z);
            float t = 0f;
            while (t < 1f)
            {
                t += Time.deltaTime / time;
                transform.position = Vector3.Lerp(start, target, t);
                yield return null;
            }
        }

        #region Debug & Editor
#if UNITY_EDITOR || UNITY_STANDALONE

        private void OnDrawGizmos()
        {
            Gizmos.color = Color.cyan;
            Gizmos.DrawSphere(lookPoint, 0.3f);
            Gizmos.DrawLine(lookPoint, transform.position);
        }

        private void HandleMouseZoomFallback()
        {
            float scroll = Input.GetAxis("Mouse ScrollWheel");
            float delta = scroll * 20f;
            if (Input.GetKey(KeyCode.Z)) delta += 1f;
            if (Input.GetKey(KeyCode.X)) delta -= 1f;

            if (Mathf.Abs(delta) > 0.01f)
            {
                targetZoomDistance = Mathf.Clamp(
                    targetZoomDistance - delta,
                    minZoomDistance, maxZoomDistance
                );
            }
        }

        private void HandleMousePanFallback()
        {
            if (!Input.GetMouseButton(1)) return;
            float dx = Input.GetAxis("Mouse X");
            float dy = Input.GetAxis("Mouse Y");

            Vector3 right = mainCamera.transform.right;
            Vector3 forward = Vector3.Cross(right, Vector3.up);
            right.y = forward.y = 0f;
            right.Normalize(); forward.Normalize();

            Vector3 move = (-dx * right + -dy * forward) * panSpeed;
            move.y = 0f;
            lookPoint += move;
        }

        private void HandleMouseRotateFallback()
        {
            if (Input.GetMouseButton(2))
            {
                float delta = Input.GetAxis("Mouse X") * rotationSpeed * Time.deltaTime;
                rotationAngle += delta;
            }
            if (Input.GetKey(KeyCode.Q)) rotationAngle -= rotationSpeed * Time.deltaTime;
            if (Input.GetKey(KeyCode.E)) rotationAngle += rotationSpeed * Time.deltaTime;
        }
#endif
        #endregion
    }
}
#endregion