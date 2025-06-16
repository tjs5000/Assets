using System;
using System.IO;
using UnityEngine;
using PlexiPark.Data;

public static class CameraSaveManager
{
    // bump this any time you change the CameraState struct
    private const string SaveVersion = "1.0";
    private static readonly string FilePath = Path.Combine(
        Application.persistentDataPath, 
        "camera_state.json"
    );

    /// <summary>
    /// Write the given CameraState to disk as JSON.
    /// </summary>
    public static void SaveCameraState(CameraState state)
    {
        try
        {
            // attach version
            var wrapper = new CameraStateWrapper {
                version = SaveVersion,
                state   = state
            };
            string json = JsonUtility.ToJson(wrapper, prettyPrint: true);
            File.WriteAllText(FilePath, json);
            Debug.Log($"💾 Camera state saved to {FilePath}");
        }
        catch (Exception ex)
        {
            Debug.LogError($"❌ Failed to save camera state: {ex}");
        }
    }

    /// <summary>
    /// Loads the saved CameraState. If the file is missing or version mismatches, returns null.
    /// </summary>
    public static CameraState? LoadCameraState()
    {
        if (!File.Exists(FilePath))
            return null;

        try
        {
            string json = File.ReadAllText(FilePath);
            var wrapper = JsonUtility.FromJson<CameraStateWrapper>(json);
            if (wrapper.version != SaveVersion)
            {
                Debug.LogWarning($"⚠️ CameraState version mismatch: {wrapper.version} (expected {SaveVersion}), ignoring.");
                return null;
            }
            return wrapper.state;
        }
        catch (Exception ex)
        {
            Debug.LogError($"❌ Failed to load camera state: {ex}");
            return null;
        }
    }

    /// <summary>
    /// Returns true if there's a valid saved camera_state.json in persistentDataPath.
    /// </summary>
    public static bool HasSavedState()
    {
        return File.Exists(FilePath);
    }

    // wrapper for versioning
    [Serializable]
    private class CameraStateWrapper
    {
        public string      version;
        public CameraState state;
    }
}
