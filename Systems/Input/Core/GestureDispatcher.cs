// Assets/Systems/Input/Core/GestureDispatcher.cs

using UnityEngine;
using System.Linq;
using System.Collections.Generic;
using Lean.Touch;
using PlexiPark.Core;
using PlexiPark.Core.SharedEnums;
using PlexiPark.Managers;
using PlexiPark.Systems.Input.Interfaces;
using PlexiPark.Systems.Input.Gestures;

namespace PlexiPark.Systems.Input.Core
{
    public class GestureDispatcher : MonoBehaviour
    {
        public static GestureDispatcher I { get; private set; }

        [Header("Gesture Interfaces")]
        [SerializeField] private MonoBehaviour panHandlerObject;
        [SerializeField] private MonoBehaviour zoomHandlerObject;
        [SerializeField] private MonoBehaviour tiltHandlerObject;
        [SerializeField] private MonoBehaviour rotateHandlerObject;
        [SerializeField] private MonoBehaviour dragHandlerObject;
        [SerializeField] private MonoBehaviour trailHandlerObject;

        private ICameraPanHandler panHandler;
        private ICameraZoomHandler zoomHandler;
        private ICameraTiltHandler tiltHandler;
        private ICameraRotateHandler rotateHandler;
        private IObjectDragHandler dragHandler;
        private ITrailDrawHandler trailHandler;

        private PanGesture panGesture;
        private ZoomGesture zoomGesture;
        private TiltGesture tiltGesture;
        private RotateGesture rotateGesture;
        private ObjectDragGesture dragGestureWrapper;

        private ObjectSelectGesture selectGesture;
        private TrailSelectGesture trailSelectGesture;

        private enum CommittedGesture { None, Pan, Zoom, Tilt, Rotate }
        private CommittedGesture committedGesture = CommittedGesture.None;
        private float gestureStartTime;
        private const float gestureCommitDelay = 0.1f;
        private float gestureTimer = 0f;
        private List<LeanFinger> activeFingers = new();
        [SerializeField] private LayerMask worldObjectLayer;

        void Awake()
        {
            if (I != null && I != this)
            {
                Destroy(gameObject);
                return;
            }
            I = this;

            panHandler = panHandlerObject as ICameraPanHandler;
            zoomHandler = zoomHandlerObject as ICameraZoomHandler;
            tiltHandler = tiltHandlerObject as ICameraTiltHandler;
            rotateHandler = rotateHandlerObject as ICameraRotateHandler;
            dragHandler = dragHandlerObject as IObjectDragHandler;
            trailHandler = trailHandlerObject as ITrailDrawHandler;

            panGesture = new PanGesture(panHandler);
            zoomGesture = new ZoomGesture(zoomHandler);
            tiltGesture = new TiltGesture(tiltHandler);
            rotateGesture = new RotateGesture(rotateHandler);
            dragGestureWrapper = new ObjectDragGesture(dragHandler);

            selectGesture = new ObjectSelectGesture(worldObjectLayer);

            if (trailHandler != null)
            {
                trailSelectGesture = new TrailSelectGesture(trailHandler);
            }

            GameState.I.OnModeChanged += OnModeChanged;
        }

        void OnEnable()
        {
            LeanTouch.OnGesture += HandleGesture;
            LeanTouch.OnFingerOld += selectGesture.OnFingerOld;
            UpdateTapListeners(GameState.I.Mode);
        }

        void OnDisable()
        {
            LeanTouch.OnGesture -= HandleGesture;
            LeanTouch.OnFingerOld -= selectGesture.OnFingerOld;
            RemoveTapListeners(GameState.I.Mode);
        }

        private void OnModeChanged(InputMode newMode)
        {
            Debug.Log("[GestureDispatcher] Mode changed to: " + newMode);
            RemoveTapListeners(GameState.I.Mode); // Remove old mode
            UpdateTapListeners(newMode);         // Add new mode
        }

        private void UpdateTapListeners(InputMode mode)
        {
            if (mode == InputMode.TrailPlacement && trailSelectGesture != null)
            {
                LeanTouch.OnFingerTap += trailSelectGesture.OnFingerTap;
            }
            else if (mode != InputMode.TrailPlacement)
            {
                LeanTouch.OnFingerTap += selectGesture.OnFingerTap;
            }
        }

        private void RemoveTapListeners(InputMode mode)
        {
            if (mode == InputMode.TrailPlacement && trailSelectGesture != null)
            {
                LeanTouch.OnFingerTap -= trailSelectGesture.OnFingerTap;
            }
            else if (mode != InputMode.TrailPlacement)
            {
                LeanTouch.OnFingerTap -= selectGesture.OnFingerTap;
            }
        }

        public void RefreshGestureMode()
        {
            Debug.Log($"[GestureDispatcher] Refreshing gestures for mode: {GameState.I.Mode}");
            OnModeChanged(GameState.I.Mode);
        }

        private void HandleGesture(List<LeanFinger> fingers)
        {
            if (GridManager.IsTouchOverDebugUI)
            {
                Debug.Log("IsTouchOverDebugUI = True");
                return;
            }

            if (fingers == null || fingers.Count == 0)
            {
                Debug.Log($"Finger: {fingers}");
                committedGesture = CommittedGesture.None;
                gestureStartTime = 0f;
                gestureTimer = 0f;

                zoomGesture?.EndGesture();
                rotateGesture?.EndGesture();
                return;
            }

            if (fingers.Any(f => f.IsOverUI())) {
                //Debug.Log("IsOverUI");
                return;
            }

            if (!AreFingersEqual(fingers, activeFingers) || fingers.Any(f => f.Age < 0.05f))
            {
                activeFingers = new List<LeanFinger>(fingers);

                if (fingers.Count >= 2)
                {
                    gestureTimer = 0f;
                    committedGesture = CommittedGesture.None;
                }
            }

            gestureTimer += Time.deltaTime;

            if (fingers.Count >= 2)
            {
                if (committedGesture == CommittedGesture.None && gestureTimer >= gestureCommitDelay)
                {
                    float twist = Mathf.Abs(LeanGesture.GetTwistDegrees(fingers));
                    float pinch = Mathf.Abs(1f - LeanGesture.GetPinchScale(fingers));
                    float move = LeanGesture.GetScreenDelta(fingers).magnitude;

                    if (twist > 4f)
                    {
                        committedGesture = CommittedGesture.Rotate;
                        rotateGesture?.BeginGesture(fingers);
                    }
                    else if (pinch > 0.05f)
                    {
                        committedGesture = CommittedGesture.Zoom;
                        zoomGesture?.BeginGesture(fingers);
                    }
                    else if (move > 10f)
                    {
                        committedGesture = CommittedGesture.Tilt;
                    }
                }

                switch (committedGesture)
                {
                    case CommittedGesture.Rotate:
                        rotateGesture?.Handle(fingers);
                        break;
                    case CommittedGesture.Zoom:
                        zoomGesture?.Handle(fingers);
                        break;
                    case CommittedGesture.Tilt:
                        tiltGesture?.Handle(fingers);
                        break;
                    default:
                        zoomGesture?.Handle(fingers);
                        rotateGesture?.Handle(fingers);
                        tiltGesture?.Handle(fingers);
                        break;
                }
            }
            else
            {
                switch (GameState.I.Mode)
                {
                    case InputMode.Placement:
                        dragGestureWrapper?.Handle(fingers);
                        break;
                    case InputMode.TrailPlacement:
                        panGesture?.Handle(fingers);
                        break;
                    default:
                        panGesture?.Handle(fingers);
                        break;
                }
            }
        }

        private bool AreFingersEqual(List<LeanFinger> a, List<LeanFinger> b)
        {
            if (a.Count != b.Count) return false;

            for (int i = 0; i < a.Count; i++)
            {
                if (a[i] != b[i]) return false;
            }

            return true;
        }
    }
}
