// Managers/ObjectManager.cs
//
// Spawns and manages finalized placed objects in the park.
// Used by ParkBuilder after placement is validated.

using UnityEngine;
using System.Collections.Generic;
using PlexiPark.Data;        // for PlacedObject
using PlexiPark.Data.UI;     // for PlaceableCatalog
using PlexiPark.Data.SaveLoad;

namespace PlexiPark.Managers
{
    public class ObjectManager : MonoBehaviour
    {
        public static ObjectManager Instance { get; private set; }
        [System.Serializable]
        public class PlacedObjectSaveData
        {
            public List<PlacedObject> placedObjects = new();
        }
        private List<PlacedObject> placedObjectsList = new();
        private List<GameObject> spawnedObjects = new();

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
            if (data == null || data.finalPrefab == null)
                return null;

            Vector3 worldPos = GridManager.Instance.GetWorldPosition(originGridCoord);
            GameObject placed = Instantiate(data.finalPrefab, worldPos, rotation);
            placed.name = data.DisplayName;
            spawnedObjects.Add(placed); // optional

            var placedData = new PlacedObject
            {
                ObjectID = data.ObjectID,
                GridPosition = originGridCoord,
                Rotation = rotation,
                Scale = placed.transform.localScale
            };

            placedObjectsList.Add(placedData);

            if (data.Footprint != null)
            {
                foreach (Vector2Int offset in data.Footprint)
                    GridManager.Instance.SetOccupied(originGridCoord + offset, true);
            }
            else
            {
                Debug.LogWarning($"SpawnFinalObject: `{data.name}.Footprint` is null, skipping occupancy.", placed);
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
            {
                go.transform.localScale = p.Scale;
                spawnedObjects.Add(go);
            }
            return go;
        }

        public void SavePlacedObjects()
        {
            string path = Application.persistentDataPath + "/placed_objects.secure";
            var data = new PlacedObjectSaveData { placedObjects = placedObjectsList }; // or Dictionary if you use one
            EncryptedJsonUtility.Save(path, data);
        }

        public void LoadPlacedObjects()
        {
            string path = Application.persistentDataPath + "/placed_objects.secure";
            var data = EncryptedJsonUtility.Load<PlacedObjectSaveData>(path);
            if (data != null && data.placedObjects != null)
            {
                placedObjectsList = data.placedObjects;
                foreach (var p in placedObjectsList)
                    SpawnPlacedObject(p);
            }
        }


    }
}
