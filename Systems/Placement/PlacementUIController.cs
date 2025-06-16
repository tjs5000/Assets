// Assets/Systems/Placement/PlacementUIController.cs
using UnityEngine;
using UnityEngine.UI;
using System;
using PlexiPark.Data;

namespace PlexiPark.Systems.Placement
{
    /// <summary>
    /// Manages instantiation and callbacks of the Context-Icon UI.
    /// </summary>
    public class PlacementUIController : MonoBehaviour
    {
        public event Action<ParkObjectData> OnBeginPlacement;
        public event Action             OnCommitPressed;
        public event Action             OnCancelPressed;
        public event Action             OnRotatePressed;

        [Header("Context UI Prefab")]
        [Tooltip("The world-space UI prefab that contains GhostContextUI")]
        [SerializeField] private GameObject contextPrefab;
        [Tooltip("Where in the hierarchy to parent the instantiated UI")]
        [SerializeField] private Transform   uiRoot;

        private GhostContextUI contextUI;

        /// <summary>
        /// Called by BuildMenuUI to kick off placement.
        /// Raises OnBeginPlacement so PlacementController can spawn the ghost.
        /// </summary>
        public void BeginPlacement(ParkObjectData data)
        {
            OnBeginPlacement?.Invoke(data);
        }

        /// <summary>
        /// After the ghost is spawned, call this with its transform so the UI follows it.
        /// </summary>
        public void ShowContextUI(Transform followTarget)
        {
            // Destroy any existing UI
            if (contextUI != null)
                Destroy(contextUI.gameObject);

            // Instantiate new context UI
            GameObject go = Instantiate(contextPrefab, uiRoot);
            contextUI = go.GetComponentInChildren<GhostContextUI>();
            if (contextUI == null)
            {
                Debug.LogError("Context UI prefab is missing a GhostContextUI component.");
                return;
            }

            // Initialize it to follow the provided transform
            contextUI.Initialize(followTarget);

            // Hook up buttons
            contextUI.commitButton.onClick.AddListener(() => OnCommitPressed?.Invoke());
            contextUI.cancelButton.onClick.AddListener(() => OnCancelPressed?.Invoke());
            contextUI.rotateButton.onClick.AddListener(() => OnRotatePressed?.Invoke());
        }

        /// <summary>
        /// Removes the context UI from the scene.
        /// </summary>
        public void HideContextUI()
        {
            if (contextUI != null)
            {
                Destroy(contextUI.gameObject);
                contextUI = null;
            }
        }
    }
}
