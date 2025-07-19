// Assets/Systems/Input/Handlers/CameraPanHandler.cs

using UnityEngine;
using PlexiPark.Systems.Input.Interfaces;

namespace PlexiPark.Systems.Input.Handlers
{
    public class CameraPanHandler : MonoBehaviour, ICameraPanHandler
    {
        [SerializeField] private MonoBehaviour cameraRigObject;

        private ICameraRig cameraRig;
        private float zoomExponent = 1f;

        private void Awake()
        {
            cameraRig = cameraRigObject as ICameraRig;

            if (cameraRig == null)
            {
                Debug.LogError("🚫 CameraRigObject must implement ICameraRig.");
            }
        }

        public void Pan(Vector2 screenDelta)
        {
            Debug.Log("[CameraPanHandler] Pan started");
            if (cameraRig == null) return;

            Transform yaw = cameraRig.YawTransform;
            float distance = cameraRig.ZoomDistance;
            float panSpeed = cameraRig.PanSpeed;

            Vector3 right = yaw.right;
            Vector3 forward = new Vector3(yaw.forward.x, 0f, yaw.forward.z).normalized;

            Vector2 screenRatio = new Vector2(
                screenDelta.x / Screen.width,
                screenDelta.y / Screen.height
            );

            float zoomFactor = Mathf.Pow(distance + 1f, zoomExponent);

            Vector3 move = (forward * screenRatio.y + right * screenRatio.x) * (panSpeed * zoomFactor);

            cameraRig.LookPoint -= move;
            cameraRig.ClampWithinBounds();
            cameraRig.UpdateCameraPosition();

            //Debug.Log($"[CameraPanHandler] Pan applied. Move vector: {move}");
        }

        public void PanInstant(Vector2 screenDelta)
        {
            if (cameraRig == null) return;

            Transform yaw = cameraRig.YawTransform;

            Vector3 right = yaw.right;
            Vector3 forward = new Vector3(yaw.forward.x, 0, yaw.forward.z).normalized;

            Vector3 move = (forward * screenDelta.y + right * screenDelta.x) * cameraRig.PanSpeed * cameraRig.ZoomDistance;

            cameraRig.LookPoint -= move;
            cameraRig.ClampWithinBounds();
            cameraRig.UpdateCameraPosition();
        }
    }
}
