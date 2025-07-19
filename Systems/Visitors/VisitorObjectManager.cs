// Assets/Systems/Visitors/ObjectManager.cs
using System.Collections.Generic;
using UnityEngine;
using PlexiPark.Managers;
using PlexiPark.Core.Utils;
using PlexiPark.Core.SharedEnums;

namespace PlexiPark.Systems.Visitor
{
    [DefaultExecutionOrder(-100)]
    public class VisitorObjectManager : MonoBehaviour
    {
        public static VisitorObjectManager Instance { get; private set; }

        // map from grid cell → all objects in that cell
        Dictionary<Vector2Int, List<GameObject>> _buckets = new();


    void OnEnable()
    {
        Debug.Log("🟢 ObjectManager.OnEnable");
    }
        void Awake()
        {
            Debug.Log("⛰️ ObjectManager.Awake");
            if (Instance != null && Instance != this)
                Destroy(gameObject);
            else
                Instance = this;
        }

        public void RegisterObject(GameObject go)
        {
            var cell = GridUtils.WorldToCell(go.transform.position, GridManager.Instance.cellSize);
            if (!_buckets.TryGetValue(cell, out var list))
                _buckets[cell] = list = new List<GameObject>();
            list.Add(go);
        }

        public void UnregisterObject(GameObject go)
        {
            var cell = GridUtils.WorldToCell(go.transform.position, GridManager.Instance.cellSize);
            if (_buckets.TryGetValue(cell, out var list))
                list.Remove(go);
        }

        public GameObject GetNearestFacilityForNeed(Vector2Int fromCell, NeedType needType)
        {
            GameObject bestGO = null;
            float bestDist = float.MaxValue;

            foreach (var kv in _buckets)
            {
                var cell = kv.Key;
                float d = Vector2Int.Distance(fromCell, cell);
                if (d >= bestDist) continue;

                foreach (var go in kv.Value)
                {
                    var poc = go.GetComponent<ParkObjectComponent>();
                    if (poc == null) continue;

                    float amt = poc.Data.GetNeedFulfillment(needType);
                    if (amt > 0f)
                    {
                        bestDist = d;
                        bestGO = go;
                        break;
                    }
                }
            }

            return bestGO;
        }
        public IEnumerable<GameObject> GetNearbyObjects(Vector2Int center, int radius)
        {
            for (int dx = -radius; dx <= radius; dx++)
                for (int dz = -radius; dz <= radius; dz++)
                {
                    var c = new Vector2Int(center.x + dx, center.y + dz);
                    if (_buckets.TryGetValue(c, out var list))
                        foreach (var go in list)
                            yield return go;
                }
        }
    }
}
