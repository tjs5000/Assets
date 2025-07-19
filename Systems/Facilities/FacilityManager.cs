// Assets/ystems/Facilities/FacilityManager.cs
using System.Collections.Generic;
using UnityEngine;
using System.Linq;
using PlexiPark.Core.SharedEnums;
using PlexiPark.Core.Utils;
using PlexiPark.Managers;

namespace PlexiPark.Systems.Facilities
{
    [DefaultExecutionOrder(500)]
    public class FacilityManager : MonoBehaviour, IFacilityProvider
    {
        public static FacilityManager Instance { get; private set; }

        [Tooltip("How far (in cells) to search at most when looking for nearest facility")]
        [SerializeField] private int maxSearchRadius = 50;

        // ← Our new spatial index
        private Dictionary<Vector2Int, List<FacilityComponent>> _buckets
            = new Dictionary<Vector2Int, List<FacilityComponent>>();

        private void Awake()
        {
            if (Instance != null && Instance != this) Destroy(gameObject);
            else Instance = this;
        }

        /// <summary>
        /// Call this from FacilityComponent.Awake()
        /// </summary>
        public void Register(FacilityComponent f)
        {
            var cell = GridUtils.WorldToCell(
                f.transform.position,
                GridManager.Instance.CellSize
            );
            if (!_buckets.TryGetValue(cell, out var list))
            {
                list = new List<FacilityComponent>();
                _buckets[cell] = list;
            }
            list.Add(f);
        }

        /// <summary>
        /// Call this from FacilityComponent.OnDestroy()
        /// </summary>
        public void Unregister(FacilityComponent f)
        {
            var cell = GridUtils.WorldToCell(
                f.transform.position,
                GridManager.Instance.CellSize
            );
            if (_buckets.TryGetValue(cell, out var list))
            {
                list.Remove(f);
                if (list.Count == 0) _buckets.Remove(cell);
            }
        }


        public IEnumerable<FacilityComponent> GetNearbyFacilities(Vector2Int center, int radius)
        {
            // same grid‐bucket pattern as your VisitorObjectManager
            for (int dx = -radius; dx <= radius; dx++)
                for (int dz = -radius; dz <= radius; dz++)
                {
                    var c = new Vector2Int(center.x + dx, center.y + dz);
                    if (_buckets.TryGetValue(c, out var list))
                    {
                        // list is guaranteed non-null
                        foreach (var fc in list)
                            yield return fc;
                    }
                }
        }

        /// <summary>
        /// BFS‐ring search out to maxSearchRadius.
        /// Compares only squared distances (dx*dx + dy*dy).
        /// </summary>
        public FacilityComponent FindClosest(NeedType need, Vector2Int fromCell)
        {
            for (int r = 0; r <= maxSearchRadius; r++)
            {
                FacilityComponent best = null;
                int bestSqr = int.MaxValue;

                // loop over the “ring” at infinity‐norm radius r
                for (int dx = -r; dx <= r; dx++)
                {
                    // top and bottom edge of the square
                    TryCheckCell(fromCell + new Vector2Int(dx, +r));
                    if (r != 0)
                        TryCheckCell(fromCell + new Vector2Int(dx, -r));
                }
                for (int dy = -r + 1; dy <= r - 1; dy++)
                {
                    // left and right edge of the square
                    TryCheckCell(fromCell + new Vector2Int(+r, dy));
                    if (r != 0)
                        TryCheckCell(fromCell + new Vector2Int(-r, dy));
                }

                // if we found *any* on this ring, return the closest‐so‐far
                if (best != null)
                    return best;

                // local helper closes over best/bestSqr
                void TryCheckCell(Vector2Int cell)
                {
                    if (!_buckets.TryGetValue(cell, out var list)) return;

                    int ddx = cell.x - fromCell.x;
                    int ddy = cell.y - fromCell.y;
                    int sqr = ddx * ddx + ddy * ddy;

                    foreach (var f in list)
                    {
                        if (!f.IsOnline) continue;
                        if (!f.ServedNeeds.Contains(need)) continue;
                        if (sqr < bestSqr)
                        {
                            bestSqr = sqr;
                            best = f;
                        }
                    }
                }
            }
            return null; // none found in radius
        }

        public bool IsOnline(FacilityComponent f) => f.IsOnline;
    }
}
