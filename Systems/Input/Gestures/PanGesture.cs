// Assets/Systems/Input/Gestures/PanGesture.cs
using System.Collections.Generic;
using Lean.Touch;
using UnityEngine;
using PlexiPark.Core;
using PlexiPark.Core.SharedEnums;
using PlexiPark.Systems.Input.Interfaces; // ✅ Use interface instead of concrete handler

namespace PlexiPark.Systems.Input.Gestures
{
    public class PanGesture
    {
        private readonly ICameraPanHandler panHandler;

        public PanGesture(ICameraPanHandler handler)
        {
            Debug.Log($"[PanGesture]");
            panHandler = handler;
        }

        public void Handle(List<LeanFinger> fingers)
        {
            if (GameState.I.Mode != InputMode.Idle && GameState.I.Mode != InputMode.TrailPlacement) return;
            if (fingers.Count != 1) { Debug.Log("Finger Count != 1"); return; }
            if (fingers[0].IsOverUI()) return;

            Vector2 delta = LeanGesture.GetScreenDelta(fingers);
            if (delta.sqrMagnitude > 0.01f)
            {
                Debug.Log("**** Start ApplyPan");
                panHandler.Pan(delta); // Preserves legacy velocity scale
               // Debug.Log($"[PanGesture] Screen delta: {delta}");
            }
        }
    }
}