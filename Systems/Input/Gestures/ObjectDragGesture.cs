// Assets/Systems/Gestures/ObjectDragGesture.cs
using UnityEngine;
using System.Collections.Generic;
using Lean.Touch;
using PlexiPark.Systems.Input.Interfaces; // ✅ Use interface instead of concrete handler

namespace PlexiPark.Systems.Input.Gestures
{
    public class ObjectDragGesture
    {
        private readonly IObjectDragHandler dragHandler;

        public ObjectDragGesture(IObjectDragHandler handler)
        {
            dragHandler = handler;
        }

        public void Handle(List<LeanFinger> fingers)
        {
            if (fingers == null || fingers.Count == 0) return;

            LeanFinger f = fingers[0];
            if (f == null || f.IsOverGui) return;

            Vector2 screenPos = f.ScreenPosition;

            if (f.Down)
            {
                Debug.Log("[ObjectDragGesture] Drag began");
                dragHandler.BeginDrag(screenPos);
            }

            if (f.Set)
            {
                Debug.Log("[ObjectDragGesture] Drag moved");
                dragHandler.Drag(screenPos);
            }

            if (f.Up)
            {
                Debug.Log("[ObjectDragGesture] Drag ended");
                dragHandler.EndDrag(screenPos);
            }
        }
    }
}
