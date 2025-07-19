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
        const string CameraStateKey = "CameraState";

        public static void SaveCameraState(CameraState state)
        {
            string json = JsonUtility.ToJson(state);
            PlayerPrefs.SetString(CameraStateKey, json);
            PlayerPrefs.Save();
            Debug.Log($"📸 Saved camera pivot: {json}");
        }

        public static CameraState LoadCameraState()
        {
            if (PlayerPrefs.HasKey(CameraStateKey))
            {
                string json = PlayerPrefs.GetString(CameraStateKey);
                var state = JsonUtility.FromJson<CameraState>(json);

                // Optional bounds check
                if (Mathf.Abs(state.pivot.x) > 5000 || Mathf.Abs(state.pivot.z) > 5000)
                {
                    Debug.LogWarning($"🚫 Ignoring corrupted CameraState: {state.pivot}");
                    return new CameraState(Vector3.zero);
                }

                Debug.Log($"📸 Loaded camera pivot: {json}");
                return state;
            }

            return new CameraState(Vector3.zero);
        }

    }
}
