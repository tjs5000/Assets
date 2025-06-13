// UI/BuildMenuUI.cs
using System.Linq;
using System.Collections;
using UnityEngine;
using UnityEngine.UI;
using TMPro;
using PlexiPark.Data.UI;
using PlexiPark.Systems;

namespace PlexiPark.UI
{
    public class BuildMenuUI : MonoBehaviour
    {
        [Header("Dependencies")]
        public PlaceableCatalog catalog;
        public Transform contentRoot;
        public GameObject buttonPrefab;

        private IEnumerator Start()
        {
            // wait until the catalog has populated those lists
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
                    if (ParkBuilder.Instance == null)
                        Debug.LogError("❌ ParkBuilder.Instance is null!");
                    else
                        ParkBuilder.Instance.BeginPlacement(localObj);
                });
            }
        }
    }
}
