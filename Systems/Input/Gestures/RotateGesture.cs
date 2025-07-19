// Assets/Systems/Input/Gestures/RotateGesture.cs

using System.Collections.Generic;
using Lean.Touch;
using UnityEngine;
using PlexiPark.Core;
using PlexiPark.Core.Utility;
using PlexiPark.Core.SharedEnums;
using PlexiPark.Systems.Input.Interfaces; // ✅ Use interface instead of concrete handler

namespace PlexiPark.Systems.Input.Gestures
{
    public class RotateGesture
    {
        private readonly ICameraRotateHandler rotateHandler;
        private readonly float deadZone;

        private bool isRotating = false;
        private Vector2 startMidpoint;
        private Vector3 initialWorldPoint;

        public RotateGesture(ICameraRotateHandler handler, float deadZone = 5f)
        {
            rotateHandler = handler;
            this.deadZone = deadZone;
        }

        public void Handle(List<LeanFinger> fingers)
        {
            if (GameState.I.Mode != InputMode.Idle && GameState.I.Mode != InputMode.Build) return;
            if (fingers.Count < 2)
            {
                EndGesture();
                return;
            }

            if (fingers.Exists(f => f.IsOverUI())) return;

            float twistDegrees = LeanGesture.GetTwistDegrees(fingers);
            Vector2 midpoint = LeanGesture.GetScreenCenter(fingers);

            if (!isRotating || (midpoint - startMidpoint).sqrMagnitude > 5f)
            {
                isRotating = true;
                startMidpoint = midpoint;

                if (TouchWorldUtility.TryGetWorldPoint(midpoint, out var hitPoint))
                    initialWorldPoint = hitPoint;
                else if (Camera.main != null)
                    initialWorldPoint = Camera.main.transform.position;
            }

            if (Mathf.Abs(twistDegrees) > deadZone)
            {
                rotateHandler.Rotate(twistDegrees);
            }
        }

        public void BeginGesture(List<LeanFinger> fingers)
        {
            Vector2 midpoint = LeanGesture.GetScreenCenter(fingers);
            if (TouchWorldUtility.TryGetWorldPoint(midpoint, out Vector3 hitPoint))
            {
                Camera.main?.transform.LookAt(hitPoint);
            }
        }

        public void EndGesture()
        {
            isRotating = false;
            startMidpoint = Vector2.zero;
            initialWorldPoint = Vector3.zero;
        }
    }
}
