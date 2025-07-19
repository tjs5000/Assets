// Assets/Systems/Placement/GhostContextUI.cs
using UnityEngine;
using UnityEngine.UI;
using System;

namespace PlexiPark.UI
{
    public class GhostContextUI : MonoBehaviour
    {
        [Header("Buttons")]
        [SerializeField] private Button commitButton;
        [SerializeField] private Button cancelButton;
        [SerializeField] private Button rotateButton;

        public event Action OnCommitPressed;
        public event Action OnCancelPressed;
        public event Action OnRotatePressed;

        void Awake()
        {
            if (commitButton != null)
                commitButton.onClick.AddListener(() => OnCommitPressed?.Invoke());
            else
                Debug.LogError("❌ Commit Button is not assigned in GhostContextUI!");

            if (cancelButton != null)
                cancelButton.onClick.AddListener(() => OnCancelPressed?.Invoke());
            else
                Debug.LogError("❌ Cancel Button is not assigned in GhostContextUI!");

            if (rotateButton != null)
                rotateButton.onClick.AddListener(() => OnRotatePressed?.Invoke());
            else
                Debug.LogError("❌ Rotate Button is not assigned in GhostContextUI!");
        }

        // Optional: Called when shown by PlacementUIController
        public void Initialize()
        {
            Cleanup(); // Prevent double registration

            if (commitButton != null)
                commitButton.onClick.AddListener(() => OnCommitPressed?.Invoke());

            if (cancelButton != null)
                cancelButton.onClick.AddListener(() => OnCancelPressed?.Invoke());

            if (rotateButton != null)
                rotateButton.onClick.AddListener(() => OnRotatePressed?.Invoke());
        }

        public void Cleanup()
        {
            OnCommitPressed = null;
            OnCancelPressed = null;
            OnRotatePressed = null;
        }
    }
}
