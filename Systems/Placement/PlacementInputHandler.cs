using UnityEngine;
using System;

namespace PlexiPark.Systems.Placement
{
    /// <summary>
    /// Detects pointer movement and edge-pan gestures, raises events for placement logic.
    /// </summary>
    public class PlacementInputHandler : MonoBehaviour
    {
        public event Action<Vector2> OnPointerMoved;
        public event Action<Vector2> OnEdgePan;
        public event Action OnPointerDown;
        public event Action OnPointerUp;

        [Header("Edge-Pan Thresholds (as % of screen)")]
        [Range(0, 0.5f)] public float leftThreshold = 0.1f;
        [Range(0, 0.5f)] public float rightThreshold = 0.1f;
        [Range(0, 0.5f)] public float topThreshold = 0.05f;  // fire sooner
        [Range(0, 0.5f)] public float bottomThreshold = 0.15f;  // fire later

        [Header("Pan Speed")]
        [SerializeField] private float panSpeed = 30f;

        void Update()
        {
            if (Input.touchCount == 0)
                return;

            var t = Input.GetTouch(0);
            Vector2 pos = t.position;

            // 1) Began
            if (t.phase == TouchPhase.Began)
            {
                OnPointerDown?.Invoke();
            }

            // 2) Moved / Stationary
            if (t.phase == TouchPhase.Moved || t.phase == TouchPhase.Stationary)
            {
                // always tell listeners where the finger is
                OnPointerMoved?.Invoke(pos);

                // only edge-pan if in the zone
                Vector3 pan = ComputeEdgePan(pos);
                if (pan != Vector3.zero)
                    OnEdgePan?.Invoke(new Vector2(pan.x, pan.z));
            }

            // 3) Ended
            if (t.phase == TouchPhase.Ended || t.phase == TouchPhase.Canceled)
            {
                OnPointerUp?.Invoke();
            }
        }



        private Vector3 ComputeEdgePan(Vector2 screenPos)
        {
            float xMin = Screen.width * leftThreshold;
            float xMax = Screen.width * (1 - rightThreshold);
            float yMin = Screen.height * bottomThreshold;
            float yMax = Screen.height * (1 - topThreshold);

            Vector3 dir = Vector3.zero;

            if (screenPos.x < xMin) dir.x = -1f;
            else if (screenPos.x > xMax) dir.x = +1f;

            if (screenPos.y < yMin) dir.z = -1f;
            else if (screenPos.y > yMax) dir.z = +1f;

            return dir.normalized * panSpeed * Time.deltaTime;
        }

    }
}