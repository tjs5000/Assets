// Assets/Systems/Camera/CameraPanner.cs

using UnityEngine;
using PlexiPark.Systems.Input.Interfaces;

namespace PlexiPark.Systems.CameraControl
{
    /// <summary>
    /// Global access point for camera panning, usable from any system (gestures, auto-panning, cutscenes).
    /// </summary>
    public class CameraPanner : MonoBehaviour, ICameraPanHandler
    {
        public static CameraPanner I { get; private set; }

        [SerializeField] private MonoBehaviour cameraRigObject; // Must implement ICameraRig
        private ICameraRig rig;

        [SerializeField] private float zoomExponent = 1f;

        private void Awake()
        {
            if (I != null && I != this)
            {
                Destroy(gameObject);
                return;
            }
            I = this;

            rig = cameraRigObject as ICameraRig;
            if (rig == null)
                Debug.LogError("🚫 Assigned cameraRigObject does not implement ICameraRig.");
        }

        public void Pan(Vector2 screenDelta)
        {
            if (rig == null) return;

            Transform yaw = rig.YawTransform;
            float distance = rig.ZoomDistance;
            float panSpeed = rig.PanSpeed;

            Vector3 right = yaw.right;
            Vector3 forward = new Vector3(yaw.forward.x, 0f, yaw.forward.z).normalized;

            Vector2 screenRatio = new Vector2(
                screenDelta.x / Screen.width,
                screenDelta.y / Screen.height
            );

            float zoomFactor = Mathf.Pow(distance + 1f, zoomExponent);
            Vector3 move = (forward * screenRatio.y + right * screenRatio.x) * (panSpeed * zoomFactor);

            rig.LookPoint -= move;
            rig.ClampWithinBounds();
            rig.UpdateCameraPosition();
        }

        public void PanInstant(Vector2 screenDelta)
        {
            if (rig == null) return;

            Transform yaw = rig.YawTransform;
            Vector3 right = yaw.right;
            Vector3 forward = new Vector3(yaw.forward.x, 0f, yaw.forward.z).normalized;

            Vector3 move = (forward * screenDelta.y + right * screenDelta.x) * rig.PanSpeed * rig.ZoomDistance;

            rig.LookPoint -= move;
            rig.ClampWithinBounds();
            rig.UpdateCameraPosition();
        }
    }
}
