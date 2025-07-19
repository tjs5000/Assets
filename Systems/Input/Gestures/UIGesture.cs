// Assets/Systems/Input/Gestures/UIGesture.cs

using UnityEngine;
using UnityEngine.EventSystems;

namespace PlexiPark.Systems.Input.Gestures
{
    /// <summary>
    /// Handles top-level UI touch interactions, including slider changes, tap blocking, and scroll areas.
    /// </summary>
    public class UIGesture : MonoBehaviour, IPointerDownHandler, IPointerUpHandler, IDragHandler
    {
        public void OnPointerDown(PointerEventData eventData)
        {
            // Optional: Log or route event to analytics
        }

        public void OnPointerUp(PointerEventData eventData)
        {
            // Optional: confirm gesture ended on a UI element
        }

        public void OnDrag(PointerEventData eventData)
        {
            // Optional: hook into scroll views or sliders if needed
        }

        /// <summary>
        /// Check if any active finger is over UI.
        /// Can be called from gestures to cancel game input.
        /// </summary>
        public static bool IsTouchOverUI(int fingerId = -1)
        {
            if (EventSystem.current == null) return false;
            return EventSystem.current.IsPointerOverGameObject(fingerId);
        }
    }
}
