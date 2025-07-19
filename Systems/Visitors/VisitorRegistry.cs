// Assets/Systems/Visitors/VisitorRegistry.cs
using System.Collections.Generic;
using UnityEngine;
using PlexiPark.Systems.Simulation;

namespace PlexiPark.Systems.Visitor
{
    [DefaultExecutionOrder(101)]
    public class VisitorRegistry : MonoBehaviour
    {
        public static VisitorRegistry Instance { get; private set; }

        Dictionary<Vector2Int, List<VisitorAI>> _buckets = new();

        void Awake()
        {
            if (Instance != null && Instance != this) Destroy(gameObject);
            else Instance = this;
        }

        void OnEnable()
        {
            SimulationTicker.OnTick += RebuildBuckets;
        }
        void OnDisable()
        {
            SimulationTicker.OnTick -= RebuildBuckets;
        }

        void RebuildBuckets(float dt)
        {
            _buckets.Clear();
            foreach (var visitor in FindObjectsOfType<VisitorAI>())
            {
                var cell = visitor.CurrentCell;
                if (!_buckets.TryGetValue(cell, out var list))
                {
                    list = new List<VisitorAI>();
                    _buckets[cell] = list;
                }
                list.Add(visitor);
            }
        }

        public List<VisitorAI> GetVisitorsInNeighborhood(Vector2Int center, int radius)
        {
            var result = new List<VisitorAI>();
            for (int dx = -radius; dx <= radius; dx++)
            for (int dz = -radius; dz <= radius; dz++)
            {
                var c = new Vector2Int(center.x + dx, center.y + dz);
                if (_buckets.TryGetValue(c, out var list))
                    result.AddRange(list);
            }
            return result;
        }
    }
}
