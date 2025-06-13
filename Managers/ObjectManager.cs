// Managers/ObjectManager.cs
//
// Spawns and manages finalized placed objects in the park.
// Used by ParkBuilder after placement is validated.

using UnityEngine;
using System.Collections.Generic;
using PlexiPark.Data;        // for PlacedObject
using PlexiPark.Data.UI;     // for PlaceableCatalog

namespace PlexiPark.Managers
{
    public class ObjectManager : MonoBehaviour
    {
        public static ObjectManager Instance { get; private set; }
        private List<GameObject> placedObjects = new();

        private void Awake()
        {
            if (Instance != null)
            {
                Destroy(gameObject);
                return;
            }
            Instance = this;
            DontDestroyOnLoad(gameObject);
        }

        /// <summary>
        /// Instantiate a final object in‐game and return it.
        /// </summary>
        public GameObject SpawnFinalObject(ParkObjectData data, Vector2Int originGridCoord, Quaternion rotation = default)
        {
            if (data == null || data.Prefab == null)
                return null;

            Vector3 worldPos = GridManager.Instance.GetWorldPosition(originGridCoord);
            GameObject placed = Instantiate(data.Prefab, worldPos, rotation);
            placed.name = data.DisplayName;
            placedObjects.Add(placed);

            // Mark footprint as occupied
            foreach (Vector2Int offset in data.Footprint)
            {
                GridManager.Instance.SetOccupied(originGridCoord + offset, true);
            }

            return placed;
        }

        /// <summary>
        /// Re‐spawn a saved PlacedObject record.
        /// </summary>
        public GameObject SpawnPlacedObject(PlacedObject p)
        {
            var data = PlaceableCatalog.Instance.GetParkObjectData(p.ObjectID);
            if (data == null)
                return null;

            GameObject go = SpawnFinalObject(data, p.GridPosition, p.Rotation);
            if (go != null)
                go.transform.localScale = p.Scale;

            return go;
        }
    }
}
