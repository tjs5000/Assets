// Assets/Systems/Input/Handlers/CameraZoomHandler.cs

using UnityEngine;
using PlexiPark.Systems.Input.Interfaces; // ✅ Uses ICameraRig injection

namespace PlexiPark.Systems.Input.Handlers
{
    public class CameraZoomHandler :  ICameraZoomHandler
    {
        private readonly ICameraRig cameraRig;

        public CameraZoomHandler(ICameraRig cameraRig)
        {
            this.cameraRig = cameraRig;
        }

        public void Zoom(float pinchScale)
        {
            float currentDist = cameraRig.ZoomDistance;
            float newDist = Mathf.Clamp(currentDist * (1f / pinchScale), cameraRig.MinZoom, cameraRig.MaxZoom);
            cameraRig.SetZoomDistance(newDist);
        }
    }
}
