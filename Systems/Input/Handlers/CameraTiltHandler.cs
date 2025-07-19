// Assets/Systems/Input/Handlers/CameraTiltHandler.cs

using UnityEngine;
using PlexiPark.Systems.Input.Interfaces;

namespace PlexiPark.Systems.Input.Handlers
{
    public class CameraTiltHandler : ICameraTiltHandler
    {
        private readonly ICameraRig rig;

        public CameraTiltHandler(ICameraRig rig)
        {
            this.rig = rig;
        }

        public void Tilt(float deltaDegrees)
        {
            float current = rig.TargetTilt;
            float proposed = Mathf.Clamp(current + deltaDegrees * rig.TiltSpeedMultiplier, rig.MinTilt, rig.MaxTilt);
            rig.TargetTilt = proposed;

            rig.UpdateCameraPosition();
        }
    }
}
