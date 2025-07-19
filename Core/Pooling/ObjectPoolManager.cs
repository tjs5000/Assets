// Assets/Core/Pooling/ObjectPoolManager.cs
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Pool;                    // Unity 2022+ generic pool
using PlexiPark.Core.SharedEnums;
using System.Linq;


namespace PlexiPark.Core.Pooling
{


    /// <summary>
    /// Pre-warms and dispenses pooled GameObjects.
    /// - One pool per VisitorPrefab **variation**  (= one pool per VisitorType in practice)
    /// - One pool for the wildlife prefab
    /// Pool sizes are driven by ParkPopulationConfig.
    /// </summary>
    public class ObjectPoolManager : MonoBehaviour
    {


        [System.Serializable]                // ← small, Core-local data holder
        public struct VisitorPrefabs
        {
            public VisitorType type;         // Hiker, Walker, …
            public List<GameObject> variants;
        }

        /* ─────────────── Singleton ─────────────── */
        public static ObjectPoolManager Instance { get; private set; }

        void Awake()
        {
            if (Instance != null && Instance != this) { Destroy(gameObject); return; }
            Instance = this; DontDestroyOnLoad(gameObject);
            InitializePools();
        }

        /* ─────────────── Inspector ─────────────── */
        [Header("Population Caps")]
        [SerializeField] private ParkPopulationConfig config;

        [Header("Visitor Prefabs (1 block per VisitorType)")]
        [SerializeField] private List<VisitorPrefabs> visitorPrefabs = new();

        [Header("Wildlife")]
        [SerializeField] private GameObject wildlifePrefab;

        /* ─────────────── Internal state ─────────────── */
        private readonly Dictionary<GameObject, IObjectPool<GameObject>> _visitorPools = new();
        private readonly Dictionary<GameObject, IObjectPool<GameObject>> _wildlifePools = new();

        /* ─────────────── Public API ─────────────── */
        public GameObject GetVisitor(GameObject prefab) => GetFromPool(prefab, _visitorPools);
        public void ReleaseVisitor(GameObject go) => ReleaseToPool(go, _visitorPools);

        public GameObject GetWildlife() => GetFromPool(wildlifePrefab, _wildlifePools);
        public void ReleaseWildlife(GameObject go) => ReleaseToPool(go, _wildlifePools);

        /* ─────────────── Bootstrap ─────────────── */
        void InitializePools()
        {
            if (config == null)
            {
                Debug.LogError("[ObjectPool] No ParkPopulationConfig assigned");
                return;
            }

            int types = Mathf.Max(1, visitorPrefabs.Count);
            int capPerType = Mathf.CeilToInt(config.visitorCap / (float)types);

            foreach (var block in visitorPrefabs)
                foreach (var prefab in block.variants)
                    _visitorPools[prefab] = CreatePool(prefab, capPerType);

            if (wildlifePrefab != null)
                _wildlifePools[wildlifePrefab] = CreatePool(wildlifePrefab, config.wildlifeCap);
        }

        /* ───────── helpers ───────── */
        IObjectPool<GameObject> CreatePool(GameObject prefab, int capacity)
        {
            return new ObjectPool<GameObject>(
                () =>                                    // createFunc
                {
                    var go = Instantiate(prefab, transform);
                    var id = go.AddComponent<PrefabId>(); // tag instance
                    id.OriginalPrefab = prefab;
                    return go;
                },
                go => go.SetActive(true),                // actionOnGet
                go => go.SetActive(false),               // actionOnRelease
                go => Destroy(go),                       // actionOnDestroy
                false,
                capacity,
                capacity);
        }
        GameObject GetFromPool(GameObject prefab,
                               Dictionary<GameObject, IObjectPool<GameObject>> dict)
        {
            if (!dict.TryGetValue(prefab, out var pool))
                dict[prefab] = pool = CreatePool(prefab, config.visitorCap);
            return pool.Get();
        }

        void ReleaseToPool(GameObject go,
                   Dictionary<GameObject, IObjectPool<GameObject>> dict)
        {
            if (go == null) return;

            var id = go.GetComponent<PrefabId>();
            if (id != null &&
                id.OriginalPrefab != null &&
                dict.TryGetValue(id.OriginalPrefab, out var pool))
            {
                pool.Release(go);
            }
            else
            {
                // Fallback: this object didn’t originate from any known pool
                Destroy(go);
            }
        }
        private sealed class PrefabId : MonoBehaviour
        {
            public GameObject OriginalPrefab;
        }
    }

}
