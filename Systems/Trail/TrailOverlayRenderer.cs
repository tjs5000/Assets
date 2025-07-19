// Assets/System/Trail/TrailOverlayRenderer.cs
using System.Collections.Generic;
using UnityEngine;
using PlexiPark.Core.SharedEnums;
using PlexiPark.Managers;


namespace PlexiPark.Systems.Trail
{
    public class TrailOverlayRenderer : MonoBehaviour
    {
        [Header("Settings")]
        [SerializeField] private GameObject overlayQuadPrefab;
        [SerializeField] private Material defaultMaterial;
        [SerializeField] private Material invalidMaterial;
        [SerializeField] private Material highlightMaterial;

        [System.Serializable]
        public struct TrailSegmentMaterialMapping
        {
            public TrailSegmentType segmentType;
            public Material material;
        }

        // [SerializeField] private List<TrailSegmentMaterialMapping> segmentMaterials;
        // private Dictionary<TrailSegmentType, Material> segmentMaterialLookup;

        //  [SerializeField] private List<TrailMaterialMapping> trailMaterials;

        //  private Dictionary<Vector2Int, GameObject> ghostOverlays = new();
        private Dictionary<Vector2Int, GameObject> highlightOverlays = new();
        // private Dictionary<TrailType, Material> materialLookup;

        [System.Serializable]
        public struct TrailMaterialMapping
        {
            public TrailType trailType;
            public Material material;
        }

        void Awake()
        {
            /*             materialLookup = new();
                        foreach (var map in trailMaterials)
                            materialLookup[map.trailType] = map.material;

                        segmentMaterialLookup = new();
                        foreach (var map in segmentMaterials)
                            segmentMaterialLookup[map.segmentType] = map.material; */
        }

        // --- Ghost Placement Preview ---

        public void ShowDebugOverlay(Vector2Int cell)
        {
            return;
           /*  if (overlayQuadPrefab == null)
            {
                Debug.LogWarning("Overlay Quad Prefab is not assigned.");
                return;
            }

            GameObject quad = Instantiate(overlayQuadPrefab, transform);
            quad.name = $"DebugOverlay_{cell.x}_{cell.y}";

            // Use GridManager to get corner heights
            var cornerKeys = GridManager.Instance.CornerMap.GetCellCornerKeys(cell);
            float hBL = GridManager.Instance.CornerMap.GetCorner(cornerKeys[0]);
            float hBR = GridManager.Instance.CornerMap.GetCorner(cornerKeys[1]);
            float hTR = GridManager.Instance.CornerMap.GetCorner(cornerKeys[2]);
            float hTL = GridManager.Instance.CornerMap.GetCorner(cornerKeys[3]);

            Vector3 bl = GetWorldCorner(cornerKeys[0], hBL);
            Vector3 br = GetWorldCorner(cornerKeys[1], hBR);
            Vector3 tr = GetWorldCorner(cornerKeys[2], hTR);
            Vector3 tl = GetWorldCorner(cornerKeys[3], hTL);

            Mesh mesh = CreateQuadMesh(bl, br, tr, tl);
            var filter = quad.AddComponent<MeshFilter>();
            var renderer = quad.AddComponent<MeshRenderer>();

            filter.mesh = mesh;

            // Use a transparent debug material
            if (defaultMaterial != null)
            {
                renderer.material = new Material(defaultMaterial);
                renderer.material.color = new Color(1f, 0f, 1f, 0.5f); // translucent magenta
            }

            Vector3 worldCenter = GridManager.Instance.GetWorldCenter(cell);
            quad.transform.position = worldCenter + Vector3.up * 0.06f; // Slight offset to avoid z-fighting */
        }


        public void ShowTrailGhost(Vector2Int cell, TrailType type, TrailSegmentType segment = TrailSegmentType.Straight, int rotation = 0)
        {
            PaintGhostSegment(cell, segment, rotation);
        }

        public void ClearTrailGhost(Vector2Int cell)
        {
            ClearGhostSegment(cell);
        }





        /* public void ShowTrailGhost(Vector2Int cell, TrailType type, TrailSegmentType segment = TrailSegmentType.Straight, int rotation = 0)
        {
            if (ghostOverlays.ContainsKey(cell)) return;

            // Get corner keys and heights
            var cornerKeys = GridManager.Instance.CornerMap.GetCellCornerKeys(cell);
            float hBL = GridManager.Instance.CornerMap.GetCorner(cornerKeys[0]);
            float hBR = GridManager.Instance.CornerMap.GetCorner(cornerKeys[1]);
            float hTR = GridManager.Instance.CornerMap.GetCorner(cornerKeys[2]);
            float hTL = GridManager.Instance.CornerMap.GetCorner(cornerKeys[3]);

            // Get world positions
            Vector3 bl = GetWorldCorner(cornerKeys[0], hBL);
            Vector3 br = GetWorldCorner(cornerKeys[1], hBR);
            Vector3 tr = GetWorldCorner(cornerKeys[2], hTR);
            Vector3 tl = GetWorldCorner(cornerKeys[3], hTL);

            // Create mesh
            Mesh mesh = CreateQuadMesh(bl, br, tr, tl);

            // Create ghost GameObject
            Vector3 worldPos = GridManager.Instance.GetWorldCenter(cell);  // ✅ use cell center
            GameObject ghost = new GameObject($"TrailGhost_{cell.x}_{cell.y}");
            ghost.transform.position = worldPos + Vector3.up * 0.08f;                           // ✅ set position first
            ghost.transform.parent = this.transform;

            var filter = ghost.AddComponent<MeshFilter>();
            var renderer = ghost.AddComponent<MeshRenderer>();

            filter.mesh = mesh;
            renderer.material = GetTrailGhostMaterial(type, segment);

            // Rotate around vertical axis
            //ghost.transform.rotation = Quaternion.Euler(0f, rotation * 90f, 0f);

            ghostOverlays[cell] = ghost;
        }



        public void ClearTrailGhost(Vector2Int cell)
        {
            if (ghostOverlays.TryGetValue(cell, out var ghost))
            {
                Destroy(ghost);
                ghostOverlays.Remove(cell);
            }
        }

        public void ClearAllGhosts()
        {
            foreach (var ghost in ghostOverlays.Values)
                if (ghost != null)
                    Destroy(ghost);
            ghostOverlays.Clear();
        }
 */
        // --- Directional Highlights ---
        public void HighlightCell(Vector2Int cell, TrailType type, bool valid)
        {
            if (highlightOverlays.ContainsKey(cell)) return;

            var (segment, rotation) = TrailSegmentClassifier.GetSegmentInfo(cell, type);

            var cornerKeys = GridManager.Instance.CornerMap.GetCellCornerKeys(cell);
            float hBL = GridManager.Instance.CornerMap.GetCorner(cornerKeys[0]);
            float hBR = GridManager.Instance.CornerMap.GetCorner(cornerKeys[1]);
            float hTR = GridManager.Instance.CornerMap.GetCorner(cornerKeys[2]);
            float hTL = GridManager.Instance.CornerMap.GetCorner(cornerKeys[3]);

            Vector3 bl = GetWorldCorner(cornerKeys[0], hBL);
            Vector3 br = GetWorldCorner(cornerKeys[1], hBR);
            Vector3 tr = GetWorldCorner(cornerKeys[2], hTR);
            Vector3 tl = GetWorldCorner(cornerKeys[3], hTL);

            Mesh mesh = CreateQuadMesh(bl, br, tr, tl);

            GameObject highlight = new GameObject($"Highlight_{cell.x}_{cell.y}");
            highlight.transform.parent = this.transform;

            Vector3 worldPos = GridManager.Instance.GetWorldCenter(cell);
            highlight.transform.position = worldPos + Vector3.up * 0.05f;

            var filter = highlight.AddComponent<MeshFilter>();
            var renderer = highlight.AddComponent<MeshRenderer>();
            filter.mesh = mesh;

            // 🔁 Use dedicated highlight material, not trail type/segment material
            renderer.material = highlightMaterial;

            // ✅ Rotate to match segment (optional, for arrows or direction cues)
            highlight.transform.rotation = Quaternion.Euler(0f, rotation * 90f, 0f);

            highlightOverlays[cell] = highlight;

            // Optionally mark ghost segment now — this can be deferred until user taps
            PaintGhostSegment(cell, segment, rotation);
        }




        public void ClearAllHighlights()
        {
            Debug.Log("## [ClearAllHighlights] called");
            foreach (var obj in highlightOverlays.Values)
                if (obj != null)
                    Destroy(obj);
            highlightOverlays.Clear();
        }



        public void PaintGhostSegment(Vector2Int cell, TrailSegmentType segment, int rotation)
        {
            //Debug.Log($"#### [TrailOverlayRenderer.PaintGhostSegment] called -- {cell}, {segment}, {rotation} ");
            Vector2Int chunkCoord = GridTerrainRenderer.Instance.GetChunkCoord(cell);

            if (GridTerrainRenderer.Instance.TryGetChunk(chunkCoord, out var chunk))
            {
                chunk.PaintGhostSegment(cell, segmentID: (int)segment, rotationID: rotation);
            }
        }

        public void ClearGhostSegment(Vector2Int cell)
        {
            Vector2Int chunkCoord = GridTerrainRenderer.Instance.GetChunkCoord(cell);
            if (GridTerrainRenderer.Instance.TryGetChunk(chunkCoord, out var chunk))
            {
                chunk.ClearGhostSegment(cell);
            }
        }



        public bool IsHighlighted(Vector2Int cell)
        {
            return highlightOverlays.ContainsKey(cell);
        }

        /*         private Material GetTrailGhostMaterial(TrailType type, TrailSegmentType segment)
                {
                    if (materialLookup.TryGetValue(type, out var mat)) return mat;
                    if (segmentMaterialLookup != null && segmentMaterialLookup.TryGetValue(segment, out mat))
                        return mat;

                    if (materialLookup != null && materialLookup.TryGetValue(type, out var fallback))
                        return fallback;

                    return defaultMaterial;
                }

                private Material GetTrailMaterial(TrailType type)
                {
                    if (materialLookup.TryGetValue(type, out var mat)) return mat;
                    return defaultMaterial;
                } */

        public Vector3 GetWorldCorner(Vector2Int cornerCoord, float height)
        {
            float x = GridManager.Instance.origin.x + cornerCoord.x * GridManager.Instance.CellSize;
            float z = GridManager.Instance.origin.z + cornerCoord.y * GridManager.Instance.CellSize;
            return new Vector3(x, height + 0.05f, z);
        }

        private Mesh CreateQuadMesh(Vector3 bl, Vector3 br, Vector3 tr, Vector3 tl)
        {
            Mesh mesh = new Mesh();

            // Calculate center
            Vector3 center = (bl + br + tr + tl) / 4f;

            // Offset all vertices to center mesh at origin
            Vector3[] vertices = {
        bl - center,
        br - center,
        tr - center,
        tl - center
    };

            Vector2[] uvs = {
        new Vector2(0, 0),
        new Vector2(1, 0),
        new Vector2(1, 1),
        new Vector2(0, 1)
    };

            float diagBLTR = Mathf.Abs(bl.y - tr.y);
            float diagBRTL = Mathf.Abs(br.y - tl.y);
            int[] triangles = diagBLTR <= diagBRTL
                ? new[] { 0, 1, 2, 2, 3, 0 }
                : new[] { 0, 1, 3, 3, 2, 1 };

            mesh.vertices = vertices;
            mesh.triangles = triangles;
            mesh.uv = uvs;
            mesh.RecalculateNormals();
            return mesh;
        }


    }

}