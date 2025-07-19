// Assets/System/Input/Interfaces/IInputInterfaces.cs

using UnityEngine;
using System;
using PlexiPark.Data;
namespace PlexiPark.Systems.Input.Interfaces
{

    public interface ICameraRig
    {
        float TargetTilt { get; set; }
        float TiltSpeedMultiplier { get; }
        float MinTilt { get; }
        float MaxTilt { get; }


        float ZoomDistance { get; }
        float PanSpeed { get; }
        Vector3 LookPoint { get; set; }

        float MinZoom { get; }
        float MaxZoom { get; }
        void SetZoomDistance(float value);

        Transform Transform { get; }

        // Yaw access (needed for pan/rotate)
        Transform YawTransform { get; }

        void ClampWithinBounds();
        void UpdateCameraPosition();

        
    }
    
    public interface IGesture
    {
        void OnTouchStart(Touch touch);
        void OnTouchMove(Touch touch);
        void OnTouchEnd(Touch touch);
    }

    public interface ISelectable
    {
        void OnTap();
        void OnLongPress();
    }
    public interface ICameraPanHandler
    {
        void Pan(Vector2 screenDelta);
    }

    public interface ICameraZoomHandler
    {
        void Zoom(float pinchAmount);
    }

    public interface ICameraRotateHandler
    {
        void Rotate(float angleDelta);
    }

    public interface ICameraTiltHandler
    {
        void Tilt(float tiltDelta);
    }

    public interface IObjectDragHandler
    {
        void BeginDrag(Vector2 screenPosition);
        void Drag(Vector2 screenPosition);
        void EndDrag(Vector2 screenPosition);
    }

    public interface ITrailDrawHandler
    {
         void OnCellTapped(Vector2Int cell);
    }

    public interface IGhostController
    {
        Transform WrapperTransform { get; }
        Vector2Int CurrentGridOrigin { get; }

        void SpawnGhost(ParkObjectData data, Vector2Int startGrid);
        void MoveGhostTo(Vector2Int grid);
        void ApplyRotation(Quaternion rotation);
        void DespawnGhost();
        Vector3 ScreenPointToGround(Vector2 screenPos);
    }
    
        public interface IInputRouter
    {
        event Action<Vector2> OnPointerMoved;
        event Action<Vector2> OnPointerDown;
        event Action<Vector2> OnPointerUp;
    }
}
