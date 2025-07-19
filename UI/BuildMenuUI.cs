//  Assets/UI/BuildMenuUI.cs
using System.Linq;
using System.Collections;
using UnityEngine;
using UnityEngine.UI;
using TMPro;
using PlexiPark.Data.UI;
using PlexiPark.Systems.Placement;

namespace PlexiPark.UI
{
    public class BuildMenuUI : MonoBehaviour
    {
        [Header("Dependencies")]
        public PlaceableCatalog catalog;
        public Transform contentRoot;
        public GameObject buttonPrefab;

        [Header("Placement System")]
        [Tooltip("Drag in the PlacementUIController from your scene")]
        [SerializeField] private PlacementUIController placementUIController;

        private IEnumerator Start()
        {
            // wait until the catalog has populated
            yield return new WaitUntil(() => catalog != null && catalog.IsLoaded);

            var all = catalog.AllPlaceables.ToList();
            if (all.Count == 0)
            {
                Debug.LogWarning("⚠️ PlaceableCatalog is empty.");
                yield break;
            }

            if (buttonPrefab == null || contentRoot == null)
            {
                Debug.LogError("📛 ButtonPrefab or ContentRoot is not assigned!");
                yield break;
            }

            Debug.Log($"✅ BuildMenuUI: Loading {all.Count} placeables...");

            foreach (var obj in all)
            {
                var btn = Instantiate(buttonPrefab, contentRoot);
                var textComp = btn.GetComponentInChildren<TextMeshProUGUI>();
                if (textComp != null)
                    textComp.text = obj.DisplayName;

                var localObj = obj; // capture for listener
                btn.GetComponent<Button>().onClick.AddListener(() =>
                {
                    if (localObj.previewPrefab == null)
                    {
                        Debug.LogError($"❌ {localObj.DisplayName} is missing its Preview Prefab (ghost).");
                        return;
                    }
                    if (localObj.finalPrefab == null)
                    {
                        Debug.LogError($"❌ {localObj.DisplayName} is missing its Final Prefab.");
                        return;
                    }

                    placementUIController.BeginPlacement(localObj);
                });

            }
        }
    }
}
