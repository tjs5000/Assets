// Assets/Systems/Trail/TrailDrawController.cs
using UnityEngine;
using System.Collections.Generic;
using PlexiPark.Core.SharedEnums;
using PlexiPark.Managers;
using PlexiPark.Systems.Input.Interfaces;
using System.Linq;
using PlexiPark.Core.Utils;


namespace PlexiPark.Systems.Trail
{
    public class TrailDrawController : MonoBehaviour, ITrailDrawHandler
    {
        public static TrailDrawController I { get; private set; }

        [Header("Trail Drawing")]
        public TrailType currentTrailType = TrailType.WalkPath;
        public int maxUndoHistory = 10;

        public IReadOnlyList<List<Vector2Int>> StrokeHistory => strokeHistory;

        [Header("References")]
        [SerializeField] private TrailOverlayRenderer overlayRenderer;

        private Vector2Int? selectedStartCell = null;
        private List<Vector2Int> currentStroke = new();
        private List<List<Vector2Int>> strokeHistory = new();
        private bool isDrawing = false;
        private Vector2Int anchorCell;


        void Awake()
        {

            overlayRenderer = FindFirstObjectByType<TrailOverlayRenderer>();
            if (overlayRenderer == null)
            {
                Debug.LogError("❌ TrailOverlayRenderer not found in scene.");
            }

            Debug.Log("[TrailDrawController] Awake. Trail controller ready.");
            if (I != null && I != this)
            {
                Destroy(gameObject);
                return;
            }
            I = this;
        }


        private void ShowCardinalHighlightPaths(Vector2Int anchor, TrailType trailType)
        {
            Debug.Log($"## [ShowCardinalHighlightPaths] called at: {anchor} || {trailType}");
            Vector2Int[] directions = {
        Vector2Int.up, Vector2Int.down,
        Vector2Int.left, Vector2Int.right
    };

            foreach (var dir in directions)
            {
                Vector2Int current = anchor + dir;

                while (GridManager.Instance.IsWithinBounds(current))
                {
                    if (GridManager.Instance.IsCellOccupied(current))
                        break;

                    if (!GridManager.Instance.IsValidTrailCell(current, trailType))
                        break;

                    overlayRenderer.HighlightCell(current, trailType, true);

                    current += dir;
                }
            }
        }

        public void OnCellTapped(Vector2Int tappedCell)
        {

            if (GameState.I.Mode != InputMode.TrailPlacement)
            {
                Debug.LogWarning($"[TrailDrawController] Ignored tap in mode {GameState.I.Mode}");
                return;
            }

            Debug.Log($"## [OnCellTapped] Called on: {tappedCell}");
            if (!isDrawing)
            {
                //Debug.Log($"#### !isDrawing --- ");
                // First tap – start trail
                anchorCell = tappedCell;
                ShowCardinalHighlightPaths(anchorCell, currentTrailType);
                isDrawing = true;
            }
            else if (IsValidTrailStep(tappedCell))
            {
                //Debug.Log($"#### IsValidTrailStep --- ");
                // Valid neighbor – draw ghost trail
                var segmentInfo = TrailSegmentClassifier.GetSegmentInfoBetween(anchorCell, tappedCell, currentTrailType);
                overlayRenderer.PaintGhostSegment(anchorCell, segmentInfo.segment, segmentInfo.rotation);

                // Move anchor and continue
                anchorCell = tappedCell;
                overlayRenderer.ClearAllHighlights();
                ShowCardinalHighlightPaths(anchorCell, currentTrailType);
            }
            else
            {
                // Invalid tap – cancel drawing
                anchorCell = Vector2Int.zero;
                isDrawing = false;
                overlayRenderer.ClearAllHighlights();
                Debug.Log("❌ Invalid trail tap – exiting draw mode.");
            }
        }


        private void ApplyGhostStroke(List<Vector2Int> stroke)
        {
            foreach (var cell in stroke)
            {
                GridManager.Instance.SetTrailType(cell, currentTrailType);

                var (segment, rotation) = TrailSegmentClassifier.GetSegmentInfo(cell, currentTrailType);
                overlayRenderer.ShowTrailGhost(cell, currentTrailType, segment, rotation);
            }
        }

        public void Cancel()
        {
            foreach (var stroke in strokeHistory)
                foreach (var cell in stroke)
                    overlayRenderer.ClearTrailGhost(cell);

            strokeHistory.Clear();
            selectedStartCell = null;
            overlayRenderer.ClearAllHighlights();
        }

        public void Undo()
        {
            if (strokeHistory.Count == 0) return;
            var last = strokeHistory[^1];
            foreach (var cell in last)
                overlayRenderer.ClearTrailGhost(cell);

            strokeHistory.RemoveAt(strokeHistory.Count - 1);
        }

        private void ShowDirectionalRays(Vector2Int origin)
        {
            Debug.Log($"[TrailDrawController] Showing rays from origin: {origin}");

            overlayRenderer.ClearAllHighlights();

            Vector2Int[] directions = new Vector2Int[]
            {
                Vector2Int.up, Vector2Int.down, Vector2Int.left, Vector2Int.right
            };

            foreach (var dir in directions)
            {
                Vector2Int current = origin + dir;
                Debug.Log($"[TrailDrawController] Checking direction {dir}, starting at {current}");

                while (GridManager.Instance.IsWithinBounds(current) &&
                       GridManager.Instance.IsValidTrailCell(current, currentTrailType))
                {
                    Debug.Log($"[TrailDrawController] Valid highlight at {current}");
                    overlayRenderer.HighlightCell(current, currentTrailType, true);
                    current += dir;
                }
            }
        }

        private bool IsValidTrailStep(Vector2Int tappedCell)
        {

            Debug.Log("[IsValidTrailStep] checks for valid cell");
            return overlayRenderer.IsHighlighted(tappedCell);
        }


        private List<Vector2Int> GetLineBetween(Vector2Int start, Vector2Int end)
        {
            List<Vector2Int> line = new();
            Vector2Int dir = end - start;

            if (dir.x != 0 && dir.y != 0) return line; // No diagonal trails

            Vector2Int step = dir.x != 0
                ? new Vector2Int((int)Mathf.Sign(dir.x), 0)
                : new Vector2Int(0, (int)Mathf.Sign(dir.y));

            Vector2Int current = start;

            while (current != end)
            {
                current += step;
                line.Add(current);
            }

            return line;
        }

    }
}
