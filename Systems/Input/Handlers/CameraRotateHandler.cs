// Assets/Systems/Input/Handlers/CameraRotateHandler.cs

using UnityEngine;
using PlexiPark.Systems.Input.Interfaces; // ✅ Uses ICameraRig injection

namespace PlexiPark.Systems.Input.Handlers
{
    public class CameraRotateHandler :  ICameraRotateHandler
    {
        private readonly ICameraRig cameraRig;

        public CameraRotateHandler(ICameraRig cameraRig)
        {
            this.cameraRig = cameraRig;
        }

        public void Rotate(float deltaAngle)
        {
            float currentYaw = cameraRig.YawTransform.eulerAngles.y;
            float newYaw = currentYaw + deltaAngle;
            cameraRig.YawTransform.rotation = Quaternion.Euler(0f, newYaw, 0f);
            cameraRig.UpdateCameraPosition();
        }
    }
}
