// Assets/Systems/Input/Gestures/TiltGesture.cs

using System.Collections.Generic;
using Lean.Touch;
using UnityEngine;
using PlexiPark.Managers;
using PlexiPark.Core;
using PlexiPark.Core.SharedEnums;
using PlexiPark.Systems.Input.Interfaces; // ✅ Use interface instead of concrete handler

namespace PlexiPark.Systems.Input.Gestures
{
    public class TiltGesture
    {
        private readonly ICameraTiltHandler tiltHandler;
        private readonly float tiltDeadZoneRatio;
        private readonly float tiltScale;

        public TiltGesture(ICameraTiltHandler handler, float deadZone = 0.02f, float scale = 0.2f)
        {
            tiltHandler = handler;
            tiltDeadZoneRatio = deadZone;
            tiltScale = scale;
        }

        public void Handle(List<LeanFinger> fingers)
        {
            if (GameState.I.Mode != InputMode.Idle) return;
            if (fingers.Count < 2) return;
            if (fingers.Exists(f => f.IsOverUI())) return;

            Vector2 delta = LeanGesture.GetScreenDelta(fingers);
            if (Mathf.Abs(delta.y) / (Mathf.Abs(delta.x) + 1f) > tiltDeadZoneRatio)
            {
                tiltHandler.Tilt(-delta.y * tiltScale);
            }
        }
    }
}
