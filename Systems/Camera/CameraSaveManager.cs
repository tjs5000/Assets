using UnityEngine;
using PlexiPark.Data;

public static class CameraSaveManager
{
    public static void SaveCameraState(CameraState state)
    {
        // Implement PlayerPrefs or File save
    }

    public static CameraState LoadCameraState()
    {
        return new CameraState();
    }

    public static bool HasSavedState()
    {
        return false;
    }
}
