// Assets/Systems/Placement/SelectableObject.cs

using UnityEngine;
using PlexiPark.Systems; // For GameState

namespace PlexiPark.Systems.Placement
{
    /// <summary>
    /// Attach this to any object the player can tap or long-press to interact with.
    /// Used for entering Build Mode and showing UI.
    /// </summary>
    public class SelectableObject : MonoBehaviour
    {
        [Tooltip("Prefab to instantiate when entering build mode.")]
        public GameObject buildPreviewPrefab;

        public void OnTap()
        {
            // Example: Show info panel or highlight selection
            Debug.Log($"Tapped on: {name}");
        }

        public void OnLongPress()
        {
            Debug.Log($"Entering Build Mode for: {name}");
            BuildController.I.Begin(this);
        }
    }
}
