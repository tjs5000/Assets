// Systems/SaveLoad/SaveManager.cs
// -------------------------------------------
// Responsible for saving and loading camera and other game states
// via PlayerPrefs or file I/O.
// -------------------------------------------

using UnityEngine;
using PlexiPark.Data;

namespace PlexiPark.Systems.SaveLoad
{
    public static class SaveManager
    {
        private const string CameraStateKey = "CameraState";

        // Save camera state to PlayerPrefs as JSON
        public static void SaveCameraState(CameraState state)
        {
            string json = JsonUtility.ToJson(state);
            PlayerPrefs.SetString(CameraStateKey, json);
            PlayerPrefs.Save();
            Debug.Log($"📸 CameraState saved: {json}");
        }

        // Load camera state from PlayerPrefs; returns null if not found
        public static CameraState? LoadCameraState()
        {
            if (!PlayerPrefs.HasKey(CameraStateKey))
            {
                Debug.Log("📸 No saved CameraState found.");
                return null;
            }

            string json = PlayerPrefs.GetString(CameraStateKey);
            var state = JsonUtility.FromJson<CameraState>(json);
            Debug.Log($"📸 CameraState loaded: {json}");
            return state;
        }

        // Optional: Clear saved camera state
        public static void ClearCameraState()
        {
            PlayerPrefs.DeleteKey(CameraStateKey);
            Debug.Log("🧹 CameraState cleared.");
        }
    }
}
