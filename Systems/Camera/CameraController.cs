// Systems/Camera/CameraController.cs
// ------------------------------------------------------------------
// Table of Contents
// 1. Initialization & Zoom Profiles
// 2. Input Handling (Touch & Mouse Fallback)
// 3. Camera Orbit, Zoom, Pan, Rotate, Tilt
// 4. Raycast-Based Clamping & LookPoint
// 5. Camera State Save/Load Utilities
// 6. Debug + Developer Gizmos
// ------------------------------------------------------------------

using UnityEngine;
using System.Collections;
using PlexiPark.Data;
using PlexiPark.Managers;
using PlexiPark.Systems.SaveLoad;

namespace PlexiPark.Systems.CameraControl
{
    public class CameraController : MonoBehaviour
    {
        // ------------------------------------------------------------------
        // 1. Initialization & Zoom Profiles
        // ------------------------------------------------------------------
        #region Initialization & Zoom Profiles
        public static CameraController Instance { get; private set; }

        [Header("Zoom Profiles")]
        public ZoomProfile[] zoomProfiles;

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

        [Header("Terrain Collision")]
        public float minDistanceFromTerrain = 3.0f;
        public LayerMask terrainLayerMask;

        private Camera cam;
        private Vector3 lookPoint;
        private float targetZoomDistance;
        private float currentZoomVelocity;
        private float rotationAngle;

        private enum GestureState { None, Panning, ZoomingOrRotating }
        private GestureState currentGesture = GestureState.None;

        private Vector2 previousSingleFingerPos;
        private Vector2 smoothedSingleFingerDelta;
        private float smoothingFactor = 0.15f;
        private Vector3 velocity;
        private float lastAvgY;
        private bool tiltInitialized;
        private float currentY;


public void Pan(Vector3 delta)
{
    // if your camera is rotated or uses a parent, you may want to transform this
    transform.position += delta;
}

        private void Start()
        {
            cam = Camera.main;

            if (!TryLoadCameraState(out lookPoint))
            {
                lookPoint = GetGridCenter();
            }

            SetZoomLimitsByParkType();
            ResetZoomAndRotation();
            currentY = transform.position.y;
        }

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

        #endregion

        // ------------------------------------------------------------------
        // 2. Input Handling (Touch & Mouse Fallback)
        // ------------------------------------------------------------------
        #region Input Handling

        private void Update()
        {
#if UNITY_EDITOR || UNITY_STANDALONE
            HandleMouseZoomFallback();
            HandleMousePanFallback();
            HandleMouseRotateFallback();
#endif
            if (Input.touchCount == 1 && (currentGesture == GestureState.None || currentGesture == GestureState.Panning))
            {
                HandleSingleFingerPan();
                currentGesture = GestureState.Panning;
            }
            else if (Input.touchCount == 2)
            {
                HandlePinchZoomAndRotate();
                currentGesture = GestureState.ZoomingOrRotating;
            }
            else
            {
                currentGesture = GestureState.None;
            }

            if (Input.touchCount != 2) tiltInitialized = false;

            if (Input.touchCount == 0 && momentum.IsMomentumActive())
            {
                Vector3 move = momentum.GetMomentumDelta(panSpeed, Time.deltaTime);
                move.y = 0f;
                lookPoint += move;
            }

            ClampLookPointToGrid();
            ApplyOrbit();
        }

        #endregion

        // ------------------------------------------------------------------
        // 3. Camera Orbit, Zoom, Pan, Rotate, Tilt
        // ------------------------------------------------------------------
        #region Camera Controls

        private void HandleSingleFingerPan()
        {
            Touch touch = Input.GetTouch(0);

            if (touch.phase == TouchPhase.Began)
            {
                previousSingleFingerPos = touch.position;
                smoothedSingleFingerDelta = Vector2.zero;
            }

            if (touch.phase == TouchPhase.Moved)
            {
                Vector2 rawDelta = touch.position - previousSingleFingerPos;
                smoothedSingleFingerDelta = Vector2.Lerp(smoothedSingleFingerDelta, rawDelta, smoothingFactor);

                Vector3 right = cam.transform.right;
                Vector3 forward = Vector3.Cross(right, Vector3.up);

                // Flatten both vectors to remove vertical component
                right.y = 0f;
                forward.y = 0f;

                right.Normalize();
                forward.Normalize();
                Vector3 move = (-smoothedSingleFingerDelta.x * right + -smoothedSingleFingerDelta.y * forward) * panSpeed * Time.deltaTime;

                move.y = 0f; // ⛔ Clamp vertical motion
                lookPoint += move;

                previousSingleFingerPos = touch.position;

                momentum.UpdateMomentum(rawDelta);
            }
        }

        private void HandlePinchZoomAndRotate()
        {
            Touch t0 = Input.GetTouch(0), t1 = Input.GetTouch(1);
            Vector2 prev0 = t0.position - t0.deltaPosition;
            Vector2 prev1 = t1.position - t1.deltaPosition;

            float zoomDelta = Vector2.Distance(t0.position, t1.position) - Vector2.Distance(prev0, prev1);
            targetZoomDistance -= zoomDelta * zoomSpeed;
            targetZoomDistance = Mathf.Clamp(targetZoomDistance, minZoomDistance, maxZoomDistance);

            float angleDelta = Vector2.SignedAngle(prev1 - prev0, t1.position - t0.position);
            rotationAngle += angleDelta * rotationSpeed * Time.deltaTime;

            HandleTiltDrag(t0, t1);
            momentum.StopMomentum();
        }

        private void HandleTiltDrag(Touch t0, Touch t1)
        {
            float avgY = (t0.position.y + t1.position.y) / 2f;
            if (!tiltInitialized)
            {
                lastAvgY = avgY;
                tiltInitialized = true;
                return;
            }

            float deltaY = avgY - lastAvgY;
            lastAvgY = avgY;

            pitchAngle -= deltaY * tiltSpeed;
            pitchAngle = Mathf.Clamp(pitchAngle, minPitch, maxPitch);
        }

        private void ApplyOrbit()
        {
            float smoothedDistance = Mathf.SmoothDamp(
                Vector3.Distance(transform.position, lookPoint),
                targetZoomDistance,
                ref currentZoomVelocity,
                0.15f
            );

            float yawRad = Mathf.Deg2Rad * rotationAngle;
            float pitchRad = Mathf.Deg2Rad * -pitchAngle;

            Vector3 dir = new Vector3(
                Mathf.Sin(yawRad) * Mathf.Cos(pitchRad),
                Mathf.Sin(pitchRad),
                Mathf.Cos(yawRad) * Mathf.Cos(pitchRad)
            );

            Vector3 targetPosition = lookPoint - dir * smoothedDistance;

            // Smooth the Y as before
            currentY = Mathf.Lerp(currentY, targetPosition.y, 0.15f);
            targetPosition.y = currentY;

            // Terrain collision avoidance
            if (Physics.Raycast(targetPosition, Vector3.down, out RaycastHit hit, 100f, terrainLayerMask))
            {
                float distanceToTerrain = targetPosition.y - hit.point.y;
                if (distanceToTerrain < minDistanceFromTerrain)
                {
                    float desiredMinY = hit.point.y + minDistanceFromTerrain;
                    float liftedY = Mathf.Lerp(currentY, desiredMinY, 0.1f); // tweak 0.1f as needed
                    currentY = Mathf.Max(currentY, liftedY); // only lift, never lower
                    targetPosition.y = currentY;
                }

            }

            transform.position = targetPosition;
            transform.LookAt(lookPoint);

        }


        #endregion

        // ------------------------------------------------------------------
        // 4. Raycast-Based Clamping & LookPoint
        // ------------------------------------------------------------------
        #region Raycast Clamping

        private void ClampLookPointToGrid()
        {
            if (GridManager.Instance == null) return;

            float cell = GridManager.Instance.CellSize;
            float minX = GridManager.Instance.origin.x;
            float maxX = GridManager.Instance.origin.x + GridManager.Instance.GridWidth * cell;
            float minZ = GridManager.Instance.origin.z;
            float maxZ = GridManager.Instance.origin.z + GridManager.Instance.GridDepth * cell;

            lookPoint.x = Mathf.Clamp(lookPoint.x, minX, maxX);
            lookPoint.z = Mathf.Clamp(lookPoint.z, minZ, maxZ);
        }

        private Vector3 GetGridCenter()
        {
            if (GridManager.Instance == null) return Vector3.zero;

            Vector2Int center = new(
                GridManager.Instance.GridWidth / 2,
                GridManager.Instance.GridDepth / 2
            );
            return GridManager.Instance.GetWorldPosition(center);
        }

        #endregion

        // ------------------------------------------------------------------
        // 5. Camera State Save/Load Utilities
        // ------------------------------------------------------------------
        #region Camera State

        private void OnApplicationQuit() => SaveManager.SaveCameraState(new CameraState(lookPoint));
        private void OnApplicationPause(bool pause)
        {
            if (pause) SaveManager.SaveCameraState(new CameraState(lookPoint));
        }

        private bool TryLoadCameraState(out Vector3 loaded)
        {
            CameraState state = SaveManager.LoadCameraState();
            loaded = new Vector3(state.position.x, 0, state.position.y);
            return loaded != Vector3.zero;
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
            if (GridManager.Instance == null || zoomProfiles == null) return;

            ParkType type = GridManager.Instance.currentParkType;
            foreach (var profile in zoomProfiles)
            {
                if (profile.parkType == type)
                {
                    minZoomDistance = profile.minZoom;
                    maxZoomDistance = profile.maxZoom;
                    return;
                }
            }

            Debug.LogWarning($"No ZoomProfile found for {type}. Using fallback zoom limits.");
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

        #endregion

        // ------------------------------------------------------------------
        // 6. Debug + Developer Gizmos
        // ------------------------------------------------------------------
        #region Gizmos + Editor Fallback

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
                targetZoomDistance -= delta;
                targetZoomDistance = Mathf.Clamp(targetZoomDistance, minZoomDistance, maxZoomDistance);
            }
        }

        private void HandleMousePanFallback()
        {
            if (!Input.GetMouseButton(1)) return;

            float dx = Input.GetAxis("Mouse X");
            float dy = Input.GetAxis("Mouse Y");

            Vector3 right = cam.transform.right;
            Vector3 forward = Vector3.Cross(right, Vector3.up);

            // Flatten both vectors to remove vertical component
            right.y = 0f;
            forward.y = 0f;

            right.Normalize();
            forward.Normalize();

            Vector3 move = (-dx * right + -dy * forward) * panSpeed;
            move.y = 0f; // Clamp
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
