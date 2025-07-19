// Assets/Systems/Input/Gestures/ZoomGesture.cs
using System.Collections.Generic;
using Lean.Touch;
using UnityEngine;
using PlexiPark.Core;
using PlexiPark.Core.Utility;
using PlexiPark.Core.SharedEnums;
using PlexiPark.Systems.Input.Interfaces; // ✅ Use ICameraZoomHandler interface

namespace PlexiPark.Systems.Input.Gestures
{
    public class ZoomGesture
    {
        private readonly ICameraZoomHandler zoomHandler;
        private const float pinchThreshold = 0.01f;

        private bool isZooming = false;
        private Vector2 initialMidpoint;
        private Vector3 initialHitPoint;

        public ZoomGesture(ICameraZoomHandler handler)
        {
            zoomHandler = handler;
        }

        public void Handle(List<LeanFinger> fingers)
        {
            if (GameState.I.Mode != InputMode.Idle && GameState.I.Mode != InputMode.Build)
                return;
            if (fingers.Count < 2) { EndGesture(); return; }
            if (fingers.Exists(f => f.IsOverUI())) return;

            float pinch = LeanGesture.GetPinchScale(fingers);
            Vector2 midpoint = LeanGesture.GetScreenCenter(fingers);

            if (!isZooming)
            {
                isZooming = true;
                initialMidpoint = midpoint;

                if (TouchWorldUtility.TryGetWorldPoint(midpoint, out var hitPoint))
                    initialHitPoint = hitPoint;
                else if (Camera.main != null)
                    initialHitPoint = Camera.main.transform.position;
            }

            if (Mathf.Abs(1f - pinch) > pinchThreshold)
            {
                float zoomAmount = (1f - pinch) * 20f; // Optional scaling
                zoomHandler.Zoom(zoomAmount);
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
            isZooming = false;
            initialMidpoint = Vector2.zero;
            initialHitPoint = Vector3.zero;
        }
    }
}
