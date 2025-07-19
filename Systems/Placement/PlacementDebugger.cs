// Assets/Systems/Placement/PlacementDebugger.cs
using UnityEngine;
using System.Collections;
using UnityEngine.UI;
using TMPro;

namespace PlexiPark.Systems.Placement
{
    public class PlacementDebugger : MonoBehaviour
    {
        [Header("UI Elements")]
        public TextMeshProUGUI statusLabel;
        public GameObject debugPanel;
        public KeyCode toggleKey = KeyCode.BackQuote; // ` key

        private void Update()
        {
#if UNITY_EDITOR
            if (UnityEngine.Input.GetKeyDown(toggleKey))
                debugPanel.SetActive(!debugPanel.activeSelf);
#endif
        }

        public void Log(string message)
        {
            Debug.Log($"🛠️ [Placement] {message}");
            if (statusLabel != null)
                statusLabel.text = $"🛠️ {message}";
        }
    }
}
