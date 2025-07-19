// Systems/Trail/TrailToolbarUI.cs
using UnityEngine;
using System.Collections.Generic;
using UnityEngine.UI;
using System.Linq;
using PlexiPark.Systems.Trail;

namespace PlexiPark.Systems.Trail
{
    public class TrailToolbarUI : MonoBehaviour
    {
        [Header("UI Buttons")]
        [SerializeField] private Button commitButton;
        [SerializeField] private Button undoButton;
        [SerializeField] private Button cancelButton;

        [Header("References")]
        [SerializeField] private TrailDrawController drawController;

        void Start()
        {
            if (commitButton != null)
                commitButton.onClick.AddListener(OnCommitPressed);

            if (undoButton != null)
                undoButton.onClick.AddListener(OnUndoPressed);

            if (cancelButton != null)
                cancelButton.onClick.AddListener(OnCancelPressed);
        }

        private void OnCommitPressed()
        {
            if (drawController == null) return;

            // for each stroke drawn, build and commit a TrailPlan
            foreach (List<Vector2Int> stroke in drawController.StrokeHistory)
            {
                if (stroke == null || stroke.Count == 0)
                    continue;

                var plan = new TrailPlan
                {
                    Type = drawController.currentTrailType,
                    Anchors = new List<Vector2Int> { stroke[0] },
                    Path = new List<Vector2Int>(stroke)
                };
                Debug.Log("*** Calling TrailCommitter from OnCommitPressed");
                TrailCommitter.CommitTrail(plan);
            }

            Debug.Log("[TrailToolbarUI] Commit pressed.");
        }

        private void OnUndoPressed()
        {
            if (drawController != null)
            {
                drawController.Undo();
                Debug.Log("[TrailToolbarUI] Undo pressed.");
            }
        }

        private void OnCancelPressed()
        {
            if (drawController != null)
            {
                drawController.Cancel();
                Debug.Log("[TrailToolbarUI] Cancel pressed.");
            }
        }
    }
}
