// Assets/Systems/Placement/BuildController.cs
using UnityEngine;

namespace PlexiPark.Systems.Placement
{
    public class BuildController : MonoBehaviour
    {
        public static BuildController I { get; private set; }
        public Transform PreviewTransform => preview;
        public LayerMask TerrainMask => terrainMask;
        public float GridSize => gridSize;

        [Header("Placement")]
        [SerializeField] private LayerMask terrainMask;
        [SerializeField] private float gridSize = 1f;

        private SelectableObject current;
        private Transform preview;

        void Awake()
        {
            if (I != null) { Destroy(gameObject); return; }
            I = this;
        }

        public void Begin(SelectableObject obj)
        {
            PlacementDebugger dbg = FindFirstObjectByType<PlacementDebugger>();
            dbg?.Log("BuildController.Begin called with: " + obj);


            current = obj;
            preview = Instantiate(obj.buildPreviewPrefab).transform;
        }

        public void End()
        {
            if (preview) Destroy(preview.gameObject);
            current = null;
        }

    }
}
