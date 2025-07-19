// Assets/Systems/Placement/PlacementUIController.cs
using UnityEngine;
using UnityEngine.UI;
using System;
using PlexiPark.Data;
using PlexiPark.UI;

namespace PlexiPark.Systems.Placement
{
    /// <summary>
    /// Manages instantiation and callbacks of the Context-Icon UI.
    /// </summary>
    public class PlacementUIController : MonoBehaviour
    {
        [Header("Context UI")]
        [SerializeField] private GhostContextUI contextUI;
        public event Action<ParkObjectData> OnBeginPlacement;
        public event Action OnCommitPressed;
        public event Action OnCancelPressed;
        public event Action OnRotatePressed;

        [Header("Context UI Prefab")]
        [Tooltip("The world-space UI prefab that contains GhostContextUI")]
        [SerializeField] private GameObject contextPrefab;
        [Tooltip("Where in the hierarchy to parent the instantiated UI")]
        [SerializeField] private Transform uiRoot;

        /// <summary>
        /// Called by BuildMenuUI to kick off placement.
        /// Raises OnBeginPlacement so PlacementController can spawn the ghost.
        /// </summary>
        public void BeginPlacement(ParkObjectData data)
        {
            Debug.Log("[PlacementController] BeginPlacement called");
            PlacementDebugger dbg = FindFirstObjectByType<PlacementDebugger>();
            dbg?.Log("PlacementUIController.BeginPlacement called with: " + data.DisplayName);
            OnBeginPlacement?.Invoke(data);
        }

        /// <summary>
        /// After the ghost is spawned, call this with its transform so the UI follows it.
        /// </summary>
        public void ShowContextUI()
{
    if (contextUI == null)
    {
        if (contextPrefab == null)
        {
            Debug.LogError("❌ Context Prefab is not assigned.");
            return;
        }

        // Instantiate and attach to the canvas
        GameObject instance = Instantiate(contextPrefab, uiRoot);
        contextUI = instance.GetComponent<GhostContextUI>();

        if (contextUI == null)
        {
            Debug.LogError("❌ Instantiated prefab does not have GhostContextUI component.");
            return;
        }
    }

    contextUI.gameObject.SetActive(true);
    contextUI.Initialize();

    contextUI.OnCommitPressed += HandleCommitPressed;
    contextUI.OnCancelPressed += HandleCancelPressed;
    contextUI.OnRotatePressed += HandleRotatePressed;

    Debug.Log("✅ Context UI shown and listeners hooked.");
}


public void HideContextUI()
{
    if (contextUI != null)
    {
        contextUI.OnCommitPressed -= HandleCommitPressed;
        contextUI.OnCancelPressed -= HandleCancelPressed;
        contextUI.OnRotatePressed -= HandleRotatePressed;

        contextUI.gameObject.SetActive(false);
    }
}


        public void TriggerPlacementStart(ParkObjectData data)
        {
            OnBeginPlacement?.Invoke(data);
        }
        private void HandleCommitPressed()
        {
            OnCommitPressed?.Invoke();
        }

        private void HandleCancelPressed()
        {
            OnCancelPressed?.Invoke();
        }

        private void HandleRotatePressed()
        {
            OnRotatePressed?.Invoke();
        }
    }
}
