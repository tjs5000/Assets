// Terrain/Generation/DecorativeBorderGenerator.cs
using UnityEngine;
using System.Collections.Generic;
using PlexiPark.Core.Interfaces;
using PlexiPark.Core.SharedEnums;                 // GridManager
using PlexiPark.Terrain.Data;          // for CornerHeightMap
using PlexiPark.Core.Utils;

namespace PlexiPark.Terrain.Generation
{
    [DefaultExecutionOrder(1550)]
    public class DecorativeBorderGenerator : MonoBehaviour
    {
        // ───────────────────────── Prefabs & Materials ─────────────────────────
        [Header("Prefabs")]
        public GameObject transitionRingPrefab;   // material matches terrain
        public GameObject urbanBorderPrefab;
        public GameObject suburbanBorderPrefab;
        public GameObject wildernessBorderPrefab;



        [Tooltip("Fence segment placed on the innermost ring edge")]
        public GameObject fencePrefab;

        // ───────────────────────── Ring settings ───────────────────────────────
        [Header("Ring Settings")]
        [Min(2)] public int transitionRingCells = 2;   // slope width  (rings 1-N)
        [Min(1)] public int flatRingCount = 1;   // flat width   (rings N+1..)

        [Tooltip("Depth of the large outer plates, in cells (≥1)")]
        [Min(1)] public int outerPlateDepthCells = 6;

        [Header("Materials for outer edge plates and corners")]
        public Material outerEdgeMaterial;
        public Material outerCornerMaterial;

        // ───────────────────────── internal state ──────────────────────────────
        private IGridInfoProvider grid;
        private float horizonHeight;
        private float cell;                          // cached cell size
        private int totalRingWidth;                // slope + flat

        public int offsetXCells = 1;
        public int offsetZCells = 1;

        // ───────────────────────── Unity lifecycle ─────────────────────────────
        private void Awake()
        {
            grid = FindGridProvider();
            if (grid == null)
            {
                Debug.LogError("[DecorativeBorder] No IGridInfoProvider found.");
                enabled = false;
                return;
            }

            cell = grid.CellSize;
            totalRingWidth = transitionRingCells + flatRingCount;
            horizonHeight = ComputePerimeterAverage();
        }

        private void Start()
        {
            if (enabled) GenerateBorder();
            transform.position += new Vector3(
                offsetXCells * 0.5f * cell,
                0f,
                offsetZCells * 0.5f * cell
            );
            GenerateOuterPlates();
        }

        // ───────────────────────── Generation loop ─────────────────────────────
        private void GenerateBorder()
        {
            int decoLayer = LayerMask.NameToLayer("Decorative");

            for (int x = -totalRingWidth; x < grid.GridWidth + totalRingWidth; x++)
            {
                for (int z = -totalRingWidth; z < grid.GridDepth + totalRingWidth; z++)
                {
                    bool outside = x < 0 || z < 0 ||
                                   x >= grid.GridWidth || z >= grid.GridDepth;
                    if (!outside) continue;

                    // ----------------------------------------------------------------
                    //       Radial ring index   (1 = first ring outside playable)
                    // ----------------------------------------------------------------
                    int radial = Mathf.Max(Mathf.Abs(x - Mathf.Clamp(x, 0, grid.GridWidth - 1)),
                                           Mathf.Abs(z - Mathf.Clamp(z, 0, grid.GridDepth - 1)));

                    // choose prefab
                    GameObject quadPrefab = radial <= transitionRingCells
                        ? transitionRingPrefab
                        : PrefabFor(grid.CurrentParkType);

                    Vector3 worldPos = grid.Origin + new Vector3(x * cell, 0f, z * cell);
                    GameObject quad = Instantiate(quadPrefab, worldPos, Quaternion.identity, transform);
                    quad.name = $"Border_{x}_{z}";
                    if (decoLayer != -1) quad.layer = decoLayer;

                    // build mesh
                    var mf = quad.GetComponent<MeshFilter>() ?? quad.AddComponent<MeshFilter>();
                    mf.mesh = BuildQuadMesh(x, z);

                    // ─── Fence on innermost edge ────────────────────────────────
                    if (radial == 1 && fencePrefab != null)
                        SpawnFenceForQuad(x, z, quad.transform);
                }
            }
        }

        // ───────────────────────── Mesh helpers ────────────────────────────────
        private Mesh BuildQuadMesh(int gx, int gz)
        {
            Vector3 LocalV(int cx, int cz) =>
                new((cx - gx) * cell,
                    VertexHeight(cx, cz),
                    (cz - gz) * cell);

            Vector3[] v =
            {
                LocalV(gx    , gz    ), // bl
                LocalV(gx + 1, gz    ), // br
                LocalV(gx    , gz + 1), // tl
                LocalV(gx + 1, gz + 1)  // tr
            };

            int[] tris = { 0, 2, 1, 1, 2, 3 };
            Vector2[] uv = { Vector2.zero, Vector2.right, Vector2.up, Vector2.one };

            Mesh m = new();
            m.vertices = v;
            m.triangles = tris;
            m.uv = uv;
            m.RecalculateNormals();
            return m;
        }

        private float VertexHeight(int cx, int cz)
        {
            int ix = Mathf.Clamp(cx, 0, grid.GridWidth);
            int iz = Mathf.Clamp(cz, 0, grid.GridDepth);

            float edgeH = SampleInnerCorner(ix, iz);
            int radial = Mathf.Max(Mathf.Abs(cx - ix), Mathf.Abs(cz - iz));

            if (radial <= transitionRingCells)
            {
                float t = Mathf.InverseLerp(0, transitionRingCells, radial);
                t = t * t * (3f - 2f * t);             // smoother-step
                return Mathf.Lerp(edgeH, horizonHeight, t);
            }
            return horizonHeight;                      // flat rings
        }

        private float SampleInnerCorner(int x, int z)
        {
            if (grid is not IHeightSampler s) return 0f;
            return s.SampleHeight(grid.Origin.x + x * cell,
                                  grid.Origin.z + z * cell);
        }

        private float ComputePerimeterAverage()
        {
            if (grid is not IHeightSampler s) return 0f;
            List<float> h = new();
            for (int x = 0; x <= grid.GridWidth; x++)
            {
                h.Add(s.SampleHeight(grid.Origin.x + x * cell, grid.Origin.z));
                h.Add(s.SampleHeight(grid.Origin.x + x * cell,
                                     grid.Origin.z + grid.GridDepth * cell));
            }
            for (int z = 1; z < grid.GridDepth; z++)
            {
                h.Add(s.SampleHeight(grid.Origin.x, grid.Origin.z + z * cell));
                h.Add(s.SampleHeight(grid.Origin.x + grid.GridWidth * cell,
                                     grid.Origin.z + z * cell));
            }
            float sum = 0f; h.ForEach(v => sum += v);
            return sum / h.Count;
        }

        // ───────────────────────── Fence placement ─────────────────────────────
        private void SpawnFenceForQuad(int gx, int gz, Transform parent)
        {
            // distance from playable area by axis
            int dx = gx < 0 ? -1 :
                     gx >= grid.GridWidth ? 1 : 0;
            int dz = gz < 0 ? -1 :
                     gz >= grid.GridDepth ? 1 : 0;

            // spawn on each touching edge (corner quads get two)

            if (dx != 0 && dz != 0) return;

            if (dx != 0) PlaceFenceAlongEdge(gx, gz, EdgeDir.X, dx, parent);
            if (dz != 0) PlaceFenceAlongEdge(gx, gz, EdgeDir.Z, dz, parent);
        }

        enum EdgeDir { X, Z }

        private void PlaceFenceAlongEdge(int gx, int gz, EdgeDir dir, int sign, Transform parent)
        {

            // edge vertices in grid corner coords
            int cx0 = dir == EdgeDir.X ? (sign < 0 ? 1 : 0) : 0;   // west→inner = 1, east→0
            int cz0 = dir == EdgeDir.Z ? (sign < 0 ? 1 : 0) : 0;   // south→1, north→0
            int cx1 = dir == EdgeDir.X ? cx0 : 1;
            int cz1 = dir == EdgeDir.Z ? cz0 : 1;

            // world positions
            Vector3 p0 = CornerToWorld(gx + cx0, gz + cz0);
            Vector3 p1 = CornerToWorld(gx + cx1, gz + cz1);
            Vector3 mid = (p0 + p1) * 0.5f;
            Vector3 edgeDir = (p1 - p0).normalized;

            var fence = Instantiate(fencePrefab, mid, Quaternion.identity, parent);


            Vector3 slopeUp = Vector3.Cross(Vector3.Cross(edgeDir, Vector3.up), edgeDir).normalized;
            // base orientation = look along the edge direction
            Quaternion rot = Quaternion.LookRotation(edgeDir, slopeUp);

            fence.transform.rotation = rot;
        }

        private Vector3 CornerToWorld(int cX, int cZ)
        {
            float h = VertexHeight(cX, cZ);   // height already blended
            return grid.Origin + new Vector3(cX * cell, h, cZ * cell);
        }


        private void GenerateOuterPlates()
        {
            float d = outerPlateDepthCells * cell;                     // plate depth
            float innerMinX = (-totalRingWidth) * cell;       // outer face of flat ring
            float innerMinZ = (-totalRingWidth) * cell;
            float innerMaxX = (grid.GridWidth + totalRingWidth) * cell;
            float innerMaxZ = (grid.GridDepth + totalRingWidth) * cell;

            // convert to world space once
            Vector3 worldMin = grid.Origin + new Vector3(innerMinX + 1, 0f, innerMinZ + 1);
            Vector3 worldMax = grid.Origin + new Vector3(innerMaxX + 1, 0f, innerMaxZ + 1);
            float y = horizonHeight;

            // helper that builds & returns a flat quad mesh
            static Mesh MakePlate(Vector3 bl, Vector3 tr, float y)
            {
                Vector3[] v = {
            new(bl.x, y, bl.z),
            new(tr.x, y, bl.z),
            new(bl.x, y, tr.z),
            new(tr.x, y, tr.z)
        };
                int[] t = { 0, 2, 1, 1, 2, 3 };
                Mesh m = new(); m.vertices = v; m.triangles = t; m.RecalculateNormals();
                return m;
            }

            // ---------- corner plates -------------------------------------------------
            MakeCorner(worldMin - new Vector3(d, 0, d));                             // SW
            MakeCorner(new Vector3(worldMax.x, y, worldMin.z) + new Vector3(0, 0, -d)); // SE
            MakeCorner(new Vector3(worldMin.x, y, worldMax.z) + new Vector3(-d, 0, 0)); // NW
            MakeCorner(worldMax);                                                  // NE

            void MakeCorner(Vector3 blWorld)
            {
                Vector3 trWorld = blWorld + new Vector3(d, 0, d);
                var go = new GameObject("CornerPlate");
                go.transform.parent = transform;
                go.layer = LayerMask.NameToLayer("Decorative");

                var mf = go.AddComponent<MeshFilter>();
                mf.mesh = MakePlate(blWorld, trWorld, y);

                var mr = go.AddComponent<MeshRenderer>();
                mr.sharedMaterial = outerCornerMaterial;
            }

            // ---------- edge plates ---------------------------------------------------
            MakeEdge("South",
                     new Vector3(worldMin.x, y, worldMin.z - d),
                     new Vector3(worldMax.x, y, worldMin.z));
            MakeEdge("North",
                     new Vector3(worldMin.x, y, worldMax.z),
                     new Vector3(worldMax.x, y, worldMax.z + d));
            MakeEdge("West",
                     new Vector3(worldMin.x - d, y, worldMin.z),
                     new Vector3(worldMin.x, y, worldMax.z));
            MakeEdge("East",
                     new Vector3(worldMax.x, y, worldMin.z),
                     new Vector3(worldMax.x + d, y, worldMax.z));

            void MakeEdge(string name, Vector3 blWorld, Vector3 trWorld)
            {
                var go = new GameObject($"EdgePlate_{name}");
                go.transform.parent = transform;
                go.layer = LayerMask.NameToLayer("Decorative");

                var mf = go.AddComponent<MeshFilter>();
                mf.mesh = MakePlate(blWorld, trWorld, y);

                var mr = go.AddComponent<MeshRenderer>();
                mr.sharedMaterial = outerEdgeMaterial;
            }
        }
        // ───────────────────────── Utility ─────────────────────────────────────
        private IGridInfoProvider FindGridProvider()
        {
            foreach (var mb in FindObjectsByType<MonoBehaviour>(FindObjectsSortMode.None))
                if (mb is IGridInfoProvider p) return p;
            return null;
        }

        private GameObject PrefabFor(ParkType t) => t switch
        {
            ParkType.Urban => urbanBorderPrefab,
            ParkType.Suburban => suburbanBorderPrefab,
            _ => wildernessBorderPrefab
        };
    }
}