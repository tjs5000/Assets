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
        public Vector3 position;
        public Quaternion rotation;
        public float   zoomDistance; 

        public CameraState(Vector3 pos, Quaternion rot, float zoomDist)
        {
            position = pos;
            rotation = rot;
            zoomDistance = zoomDist;
        }
    }
}
