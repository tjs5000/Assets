//Assets/Systems/Camera/CameraRig.cs
using UnityEngine;
using PlexiPark.Data;
using PlexiPark.Managers;
using PlexiPark.Systems.Input.Interfaces;


namespace PlexiPark.Systems.CameraControl
{
    public class CameraRig : MonoBehaviour, ICameraRig, ICameraPanHandler, ICameraZoomHandler, ICameraRotateHandler, ICameraTiltHandler
    {
        public static CameraRig I { get; private set; }




        [Header("Rig References")]
        [SerializeField] private Transform yaw;
        [SerializeField] private Transform tilt;
        [SerializeField] private Camera cam;

        [Header("Park Profile")]
        [SerializeField] private ParkProfileSO profile;

        [Header("Camera Values")]
        [SerializeField] private bool resetCameraOnStart = false;

        [Header("Pan")]
        public float panSpeed = 5f;
        public Vector2 boundsMin = new(0f, 0f);
        public Vector2 boundsMax = new(500f, 500f);
        public float panDamping = 6f;

        [Header("Zoom")]
        public float minDist = 5f;
        public float maxDist = 60f;

        [Header("Tilt")]
        public float minTilt = 40f;
        public float maxTilt = 60f;

        [SerializeField] private float tiltLerpSpeed = 8f;
        [SerializeField] private float tiltSpeedMultiplier = 1f;
        public float TiltSpeedMultiplier => tiltSpeedMultiplier;

        private float targetTilt;
        public float TargetTilt
        {
            get => targetTilt;
            set => targetTilt = Mathf.Clamp(value, minTilt, maxTilt);
        }

        [Header("Rotate")]
        public float rotateSpeed = 1f;
        public Vector2 yawClamp = new Vector2(-45f, 135f);

        public Transform YawTransform => yaw;
        public Transform TiltTransform => tilt;

        public float MinTilt => minTilt;
        public float MaxTilt => maxTilt;

        public float MinZoom => minDist;
        public float MaxZoom => maxDist;


        public float PanSpeed => panSpeed;
        public Transform Transform => transform;

        public Transform CamTransform => cam.transform;
        public float ZoomDistance => -cam.transform.localPosition.z;

        private Vector3 lookPoint = Vector3.zero;
        public Vector3 LookPoint
        {
            get => lookPoint;
            set => lookPoint = value;
        }

        public void ClampWithinBounds()
        {
            lookPoint.x = Mathf.Clamp(lookPoint.x, boundsMin.x, boundsMax.x);
            lookPoint.z = Mathf.Clamp(lookPoint.z, boundsMin.y, boundsMax.y);
        }

        private float distance;
        public float Distance
        {
            get => distance;
            private set
            {
                distance = Mathf.Clamp(value, minDist, maxDist);
                cam.transform.localPosition = new Vector3(0, 0, -distance);
            }
        }

        public void SetZoomDistance(float distance)
        {
            Distance = distance;
        }

        void Awake()
        {
            if (I != null) { Destroy(gameObject); return; }
            I = this;
        }

        void Start()
        {
            if (profile)
            {
                minDist = profile.minZoom;
                maxDist = profile.maxZoom;
                Distance = profile.initialZoom;
                boundsMin = profile.boundsMin;
                boundsMax = profile.boundsMax;

                yaw.localEulerAngles = Vector3.up * profile.initialYaw;
                targetTilt = profile.initialTilt;
                tilt.localEulerAngles = Vector3.right * targetTilt;
            }
            else
            {
                Distance = (minDist + maxDist) * 0.5f;
            }

            if (resetCameraOnStart)
            {
                Debug.Log("🔄 Resetting camera to default view (toggle enabled).");
                ResetView();
            }
            else if (PlayerPrefs.HasKey("CameraState"))
            {
                string json = PlayerPrefs.GetString("CameraState");

                if (!string.IsNullOrEmpty(json))
                {
                    try
                    {
                        CameraState state = JsonUtility.FromJson<CameraState>(json);
                        lookPoint = state.pivot;
                        ClampWithinBounds();
                        Debug.Log($"📸 Loaded saved camera pivot: {lookPoint}");
                    }
                    catch (System.Exception ex)
                    {
                        Debug.LogWarning($"🚫 Failed to parse CameraState: {ex.Message}. Resetting to center.");
                        ResetView();
                    }
                }
                else
                {
                    Debug.LogWarning("⚠️ CameraState key exists but is empty.");
                    ResetView();
                }
            }
            else
            {
                ResetView();
            }

            ClampWithinBounds();
            UpdateCameraPosition();
        }


        public void UpdateCameraPosition()
        {
            Vector3 offset = -tilt.forward * Distance;
            transform.position = lookPoint + offset;
        }

        void Update()
        {
            float currentX = tilt.localEulerAngles.x;
            if (currentX > 180f) currentX -= 360f;
            float smoothX = Mathf.Lerp(currentX, targetTilt, tiltLerpSpeed * Time.deltaTime);
            tilt.localEulerAngles = new Vector3(smoothX, 0, 0);
            UpdateCameraPosition();
        }

        void OnApplicationQuit()
        {
            CameraState state = new CameraState(lookPoint);
            string json = JsonUtility.ToJson(state);
            PlayerPrefs.SetString("CameraState", json);
        }

        public void ResetView()
        {
            yaw.localEulerAngles = Vector3.up * 45f;
            tilt.localEulerAngles = Vector3.right * (360f + minTilt + 10f);
            Distance = (minDist + maxDist) * 0.5f;

            // ✅ Reset LookPoint to grid center
            Vector2Int gridCenter = new(
                GridManager.Instance.GridWidth / 2,
                GridManager.Instance.GridDepth / 2
            );

            lookPoint = GridManager.Instance.GetWorldPosition(gridCenter);
            ClampWithinBounds();

            // ✅ Update camera position based on LookPoint
            UpdateCameraPosition();
        }


        public void Pan(Vector2 delta)
        {
            lookPoint += new Vector3(delta.x, 0, delta.y) * panSpeed;
            ClampWithinBounds();
            UpdateCameraPosition();
        }

        public void Zoom(float amount)
        {
            Distance += amount;
        }

        public void Rotate(float angleDelta)
        {
            float currentYaw = yaw.localEulerAngles.y;
            currentYaw = Mathf.Clamp(currentYaw + angleDelta * rotateSpeed, yawClamp.x, yawClamp.y);
            yaw.localEulerAngles = Vector3.up * currentYaw;
        }

        public void Tilt(float delta)
        {
            TargetTilt += delta * tiltSpeedMultiplier;
        }
    }

    [System.Serializable]
    public struct CameraState
    {
        public Vector3 pivot;

        public CameraState(Vector3 pivot)
        {
            this.pivot = pivot;
        }
    }
}
