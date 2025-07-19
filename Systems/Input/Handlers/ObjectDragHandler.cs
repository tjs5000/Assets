// Assets/Systems/Input/Handlers/ObjectDragHandler.cs

using UnityEngine;
using PlexiPark.Managers;
using PlexiPark.Systems.Placement;
using PlexiPark.Core.Utility;
using PlexiPark.Systems.Input.Interfaces; // ✅ Implement interface
using PlexiPark.Systems.CameraControl;
using UnityEditor;


namespace PlexiPark.Systems.Input.Handlers
{
    public class ObjectDragHandler : MonoBehaviour, IObjectDragHandler
    {
        private readonly ICameraPanHandler panHandler;
        private readonly float edgePanMargin = 0.1f;
        private readonly float edgePanSpeed = 15f;

        // public ObjectDragHandler(ICameraPanHandler panHandler)
        // {
        //     this.panHandler = panHandler;
        // }

        public void BeginDrag(Vector2 screenPosition)
        {
            Debug.Log($"[ObjectDragHandler] Drag started at {screenPosition}");
        }

        public void Drag(Vector2 screenPosition)
        {
            Debug.Log($"[ObjectDragHandler] Drag at screen position: {screenPosition}");

            if (TouchWorldUtility.TryGetWorldPoint(screenPosition, out var worldPoint, out var hit))
            {
                Vector2Int grid = GridManager.Instance.GetGridCoordinate(worldPoint);
                Debug.Log($"[ObjectDragHandler] Ray hit terrain. Grid: {grid}");
                GhostController.Instance?.MoveGhostTo(grid);
                EdgePanIfNeeded(worldPoint);
            }
            else
            {
                Debug.LogWarning("[ObjectDragHandler] Raycast did not hit terrain.");
            }
        }

        public void EndDrag(Vector2 screenPosition)
        {
            Debug.Log($"[ObjectDragHandler] Drag ended at {screenPosition}");
        }

        private void EdgePanIfNeeded(Vector3 worldPos)
        {
            Vector3 viewport = Camera.main.WorldToViewportPoint(worldPos);
            Vector2 dir = Vector2.zero;

            if (viewport.x < edgePanMargin) dir.x = -1;
            else if (viewport.x > 1f - edgePanMargin) dir.x = +1;
            if (viewport.y < edgePanMargin) dir.y = -1;
            else if (viewport.y > 1f - edgePanMargin) dir.y = +1;

            if (dir != Vector2.zero)
            {
                float panAmount = edgePanSpeed * Time.deltaTime;

                // 🟢 Use global panner instead of direct cameraRig
                CameraPanner.I?.PanInstant(dir * panAmount);
            }
        }
    }
}