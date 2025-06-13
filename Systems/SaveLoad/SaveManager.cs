// Systems/SaveLoad/SaveManager.cs
// -------------------------------------------
// Responsible for saving and loading camera and other game states
// via PlayerPrefs or file I/O.
// -------------------------------------------

using UnityEngine;
using PlexiPark.Systems;
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

        // Load camera state from PlayerPrefs (returns default if not found)
        public static CameraState LoadCameraState()
        {
            if (PlayerPrefs.HasKey(CameraStateKey))
            {
                string json = PlayerPrefs.GetString(CameraStateKey);
                CameraState state = JsonUtility.FromJson<CameraState>(json);
                Debug.Log($"📸 CameraState loaded: {json}");
                return state;
            }

            Debug.Log("📸 No saved CameraState found. Using default.");
            return new CameraState(Vector3.zero);
        }

        // Optional: Clear saved camera state
        public static void ClearCameraState()
        {
            PlayerPrefs.DeleteKey(CameraStateKey);
            Debug.Log("🧹 CameraState cleared.");
        }
    }
}
