// Assets/Systems/Input/Gestures/ObjectSelectGesture.cs

using UnityEngine;
using Lean.Touch;
using Lean.Common;
using PlexiPark.Core;
using PlexiPark.Core.SharedEnums;
using PlexiPark.Systems.Input.Interfaces; // ✅ Use ISelectable interface

namespace PlexiPark.Systems.Input.Gestures
{
    public class ObjectSelectGesture
    {
        private readonly float longPressTime = 0.35f;
        private readonly LeanScreenQuery screenQuery;
bool IsSelectableMode => GameState.I.Mode == InputMode.Idle;

        public ObjectSelectGesture(LayerMask selectionLayer)
        {
            screenQuery = new LeanScreenQuery(LeanScreenQuery.MethodType.Raycast, selectionLayer);
            screenQuery.Search = LeanScreenQuery.SearchType.GetComponentInParent;
            screenQuery.Distance = 100f;
        }

        public void OnFingerTap(LeanFinger finger)
        {
            if (!IsSelectableMode || finger.IsOverUI()) return;

            if (TrySelect(finger, out var target))
                target.OnTap();
        }

        public void OnFingerOld(LeanFinger finger)
        {
            if (!IsSelectableMode || finger.Age < longPressTime || finger.IsOverUI()) return;

            if (TrySelect(finger, out var target))
                target.OnLongPress();
        }

        private bool TrySelect(LeanFinger finger, out ISelectable selected)
        {
            selected = screenQuery.Query<ISelectable>(null, finger.ScreenPosition);
            return selected != null;
        }
    }
}
