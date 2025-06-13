// Systems/Camera/CameraState.cs
// -------------------------------------------
// Serializable data structure representing 
// persistent parts of the camera state.
// -------------------------------------------

using System;
using UnityEngine;

namespace PlexiPark.Data
{
    [Serializable]
    public struct CameraState
    {
        public float X;
        public float Z;
        public Vector2 position;

        // Future fields (optional)
        // public float Yaw;
        // public float ZoomLevel;

        public CameraState(Vector3 pos)
        {
            X = pos.x;
            Z = pos.z;
            // Yaw = 0f;
            // ZoomLevel = 0f;
            position = new Vector2(X, Z);
        }

        public Vector3 GetXZPosition()
        {
            return new Vector3(X, 0f, Z);
        }

        public override string ToString()
        {
            return $"CameraState: (X: {X:F2}, Z: {Z:F2})";
        }
    }
}
