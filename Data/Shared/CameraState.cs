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
        // Previously you had position, rotation, zoomLevel.
        // Now we only care about the look-at pivot:
        public Vector3 pivot;

        public CameraState(Vector3 pivot)
        {
            this.pivot = pivot;
        }
    }
}
