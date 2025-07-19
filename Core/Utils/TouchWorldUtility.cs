// Assets/Core/Utility/TouchWorldUtility.cs
using UnityEngine;
using Lean.Touch;

namespace PlexiPark.Core.Utility
{
    public static class TouchWorldUtility
    {
        // Default terrain mask
        private static readonly LayerMask DefaultMask = LayerMask.GetMask("Terrain");

        // LeanTouch-compatible depth setup for future use
        private static LeanScreenDepth screenDepth = new LeanScreenDepth(
            LeanScreenDepth.ConversionType.PhysicsRaycast,
            DefaultMask
        );

        /// <summary>
        /// Raycasts against the Terrain layer by default, returns world point and hit info.
        /// </summary>
        public static bool TryGetWorldPoint(Vector2 screenPosition, out Vector3 worldPosition, out RaycastHit hit)
        {
            return TryGetWorldPoint(screenPosition, DefaultMask, out worldPosition, out hit);
        }

        /// <summary>
        /// Raycasts against a custom layer mask, returns world point and hit info.
        /// </summary>
        public static bool TryGetWorldPoint(Vector2 screenPosition, LayerMask mask, out Vector3 worldPosition, out RaycastHit hit)
        {
            Ray ray = Camera.main.ScreenPointToRay(screenPosition);

            // 🔍 Visualize all raycasts across systems
            Debug.DrawRay(ray.origin, ray.direction * 500f, Color.red, 8f); // Scene view only

            if (Physics.Raycast(ray, out hit, 500f, mask))
            {
                worldPosition = hit.point;

                // 🟢 Visualize the hit point as a small green cross
                Debug.DrawLine(hit.point + Vector3.up * 0.5f, hit.point - Vector3.up * 0.5f, Color.green, 8f);
                Debug.DrawLine(hit.point + Vector3.right * 0.5f, hit.point - Vector3.right * 0.5f, Color.green, 8f);
                Debug.DrawLine(hit.point + Vector3.forward * 0.5f, hit.point - Vector3.forward * 0.5f, Color.green, 8f);


                return true;
            }

            worldPosition = default;
            return false;
        }

        /// <summary>
        /// Shorthand if hit info isn’t needed. Uses Terrain layer.
        /// </summary>
        public static bool TryGetWorldPoint(Vector2 screenPosition, out Vector3 worldPosition)
        {
            return TryGetWorldPoint(screenPosition, DefaultMask, out worldPosition, out _);
        }

        /// <summary>
        /// Shorthand with custom layer mask and no hit info.
        /// </summary>
        public static bool TryGetWorldPoint(Vector2 screenPosition, LayerMask mask, out Vector3 worldPosition)
        {
            return TryGetWorldPoint(screenPosition, mask, out worldPosition, out _);
        }

        /// <summary>
        /// Translate camera lookpoint to World Point raycast.
        /// </summary>
        public static bool TryGetGroundPointFromLookPoint(Vector3 lookPoint, out Vector3 groundPoint)
        {
            Ray ray = new Ray(lookPoint + Vector3.up * 100f, Vector3.down); // Cast down from above
            if (Physics.Raycast(ray, out RaycastHit hit, 500f, LayerMask.GetMask("Terrain")))
            {
                groundPoint = hit.point;
                return true;
            }

            groundPoint = lookPoint; // fallback
            return false;
        }
    }
}
