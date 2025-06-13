// Assets/Data/UI/PlaceableCatalog.cs

using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.AddressableAssets;
using UnityEngine.ResourceManagement.ResourceProviders;
using UnityEngine.ResourceManagement.AsyncOperations;
using PlexiPark.Data; // for ParkObjectData, ParkObjectCategory

namespace PlexiPark.Data.UI
{
    /// <summary>
    /// Singleton that loads and provides access to all ParkObjectData assets via Addressables.
    /// </summary>
    public class PlaceableCatalog : MonoBehaviour
    {
        public static PlaceableCatalog Instance { get; private set; }

        // Internal lookup by ObjectID
        private readonly Dictionary<string, ParkObjectData> _catalog = new();

        // Public lists for UI categories
        public IReadOnlyList<ParkObjectData> Paths { get; private set; }
        public IReadOnlyList<ParkObjectData> Facility { get; private set; }
        public IReadOnlyList<ParkObjectData> Natural { get; private set; }
        public IReadOnlyList<ParkObjectData> Attractions { get; private set; }
        public IReadOnlyList<ParkObjectData> Amenities { get; private set; }
        public IReadOnlyList<ParkObjectData> Waterways { get; private set; }

        public bool IsLoaded { get; private set; }

        public IEnumerable<ParkObjectData> AllPlaceables =>
            Paths
            .Concat(Facility)
            .Concat(Natural)
            .Concat(Attractions)
            .Concat(Amenities)
            .Concat(Waterways);

        [Header("Addressables Labels")]
        [Tooltip("One or more Addressables labels to load all ParkObjectData under.")]
        [SerializeField] private List<string> addressableLabels = new() { "ParkObjectAssets" };

        private void Awake()
        {
            if (Instance == null)
            {
                Instance = this;
                DontDestroyOnLoad(gameObject);
                LoadAllPlaceables();
            }
            else
            {
                Destroy(gameObject);
            }
        }

        private void LoadAllPlaceables()
        {
            // instead of addressableLabels, hard-code each category label
            var keys = new List<object>
                {
                    "Path",
                    "Facility",
                    "Natural",
                    "Attraction",
                    "Amenity",
                    "Waterway"
                };

            Addressables
              // third param is the merge mode: Union = take the union of all results
              .LoadAssetsAsync<ParkObjectData>(
                  keys,
                            data =>
                            {
                                if (data != null)
                                    _catalog[data.ObjectID] = data;
                            },
                  Addressables.MergeMode.Union
              )
              .Completed += OnCatalogLoaded;
        }

        private void OnCatalogLoaded(AsyncOperationHandle<IList<ParkObjectData>> handle)
        {
            var all = handle.Result;

            // ① Build lookup dictionary
            _catalog.Clear();
            foreach (var data in all)
                _catalog[data.ObjectID] = data;

            // ② Split into category lists
            Paths = all.Where(d => d.Category == ParkObjectCategory.Path).ToList();
            Facility = all.Where(d => d.Category == ParkObjectCategory.Facility).ToList();
            Natural = all.Where(d => d.Category == ParkObjectCategory.Natural).ToList();
            Attractions = all.Where(d => d.Category == ParkObjectCategory.Attraction).ToList();
            Amenities = all.Where(d => d.Category == ParkObjectCategory.Amenity).ToList();
            Waterways = all.Where(d => d.Category == ParkObjectCategory.Waterway).ToList();

            IsLoaded = true;
            Debug.Log($"PlaceableCatalog loaded {all.Count} items from labels: {string.Join(", ", addressableLabels)}");
        }

        /// <summary>
        /// Lookup a ParkObjectData by its ObjectID.
        /// </summary>
        public ParkObjectData GetParkObjectData(string objectID)
        {
            _catalog.TryGetValue(objectID, out var data);
            return data;
        }

        /// <summary>
        /// Returns all objects in the given category.
        /// </summary>
        public IEnumerable<ParkObjectData> GetByCategory(ParkObjectCategory category)
            => _catalog.Values.Where(d => d.Category == category);
    }
}
