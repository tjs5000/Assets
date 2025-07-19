// Assets/UI/Views/TrailModeToggleButton.cs
using UnityEngine;
using UnityEngine.UI;
using PlexiPark.Systems.Input.Core;
using PlexiPark.Core.SharedEnums;

using TMPro;

namespace PlexiPark.UI
{
    public class TrailModeToggleButton : MonoBehaviour
    {
        [SerializeField] private Button button;
        [SerializeField] private TextMeshProUGUI label;

        void Awake()
        {
            Debug.Log("🔵 TrailModeToggleButton.Awake() called.");

            if (button == null)
            {
                Debug.LogWarning("⚠️ Button is NULL. Attempting to GetComponent<Button>()...");
                button = GetComponent<Button>();
                if (button == null)
                {
                    Debug.LogError("❌ Still no Button found on this GameObject.");
                    return;
                }
            }

            if (label == null)
            {
                Debug.LogWarning("⚠️ Label is NULL. Attempting to GetComponentInChildren<TextMeshProUGUI>()...");
                label = GetComponentInChildren<TextMeshProUGUI>();
                if (label == null)
                {
                    Debug.LogError("❌ Still no label found.");
                    return;
                }
            }

            if (GameState.I == null)
            {
                Debug.LogWarning("❗ GameState not ready yet.");
                label.text = "Trail Mode";
                return;
            }

            button.onClick.AddListener(ToggleMode);
            Debug.Log("✅ Button listener added. Calling UpdateLabel.");
            UpdateLabel();
        }

        void Start()
        {
            // Wait until all singletons like GameState are initialized
            UpdateLabel();
        }
        public void ToggleMode()
        {
            Debug.Log("Trail toggle button was pressed.");
            Debug.Log($"🟢 Before toggle: GameState.Mode = {GameState.I.Mode}");

            GameState.I.Mode = GameState.I.Mode == InputMode.TrailPlacement
                ? InputMode.Idle
                : InputMode.TrailPlacement;

            Debug.Log($"🟢 After toggle: GameState.Mode = {GameState.I.Mode}");

            GestureDispatcher.I?.RefreshGestureMode();
            UpdateLabel();
        }

        void UpdateLabel()
        {
            if (label == null)
            {
                Debug.LogWarning("⚠️ TrailModeToggleButton: Label is null on UpdateLabel.");
                return;
            }

            label.text = GameState.I.Mode == InputMode.TrailPlacement
                ? "Exit Trail Mode"
                : "Enter Trail Mode";

            Debug.Log($"🔤 Label updated to: {label.text}");
        }
    }
}
