// Assets/Systems/Input/Gestures/TrailSelectGesture.cs

using UnityEngine;
using Lean.Touch;
using PlexiPark.Managers;
using PlexiPark.Core.Utility;
using PlexiPark.Systems.Input.Interfaces;

namespace PlexiPark.Systems.Input.Gestures
{
    public class TrailSelectGesture
    {
        private readonly ITrailDrawHandler trailHandler;

        public TrailSelectGesture(ITrailDrawHandler trailHandler)
        {
            this.trailHandler = trailHandler;
        }

        public void OnFingerTap(LeanFinger finger)
        {
            if (finger == null || finger.IsOverGui) return;

            if (TouchWorldUtility.TryGetWorldPoint(finger.ScreenPosition, out Vector3 worldPos, out RaycastHit hit))
            {
                Vector2Int gridCoord = GridManager.Instance.GetGridCoordinate(worldPos);
                trailHandler?.OnCellTapped(gridCoord);
            }
            else
            {
                Debug.LogWarning("[TrailSelectGesture] 🚫 Tap did not hit terrain.");
            }
        }
    }
}
