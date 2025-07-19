// Terrain/Rendering/TerrainChunk.cs
// ----------------------------------
// Renders a mesh chunk from shared corner height data and manages trail splat maps
// ----------------------------------

using System;
using UnityEngine;
using UnityEngine.AI;
using Unity.AI.Navigation;
using PlexiPark.Core.SharedEnums;

namespace PlexiPark.Terrain.Rendering
{
    [RequireComponent(typeof(MeshFilter), typeof(MeshRenderer))]
    public class TerrainChunk : MonoBehaviour
    {
        [HideInInspector] public int chunkSize;

        [Header("Splat Map Settings")]
        public Material terrainMaterial;

        private Texture2D[] trailSplats = new Texture2D[3];
        private Vector2Int chunkOrigin;
        private Mesh mesh;
        private Material _mat;

        public void GenerateMesh(Vector2Int origin, int size, float cellSize, Func<Vector2Int, float> getCornerHeight)
        {
            chunkOrigin = origin;
            chunkSize = size;
            
            mesh = new Mesh();
            mesh.indexFormat = UnityEngine.Rendering.IndexFormat.UInt32;

            Vector3[] vertices = new Vector3[size * size * 4];
            int[] triangles = new int[size * size * 6];
            Vector2[] uvs = new Vector2[vertices.Length];
            Color[] colors = new Color[vertices.Length]; // NEW: Vertex colors

            int v = 0;
            int t = 0;

            for (int x = 0; x < size; x++)
            {
                for (int z = 0; z < size; z++)
                {
                    Vector2Int cell = origin + new Vector2Int(x, z);

                    Vector2Int bl = new Vector2Int(cell.x, cell.y);
                    Vector2Int br = new Vector2Int(cell.x + 1, cell.y);
                    Vector2Int tl = new Vector2Int(cell.x, cell.y + 1);
                    Vector2Int tr = new Vector2Int(cell.x + 1, cell.y + 1);

                    float hBL = getCornerHeight(bl);
                    float hBR = getCornerHeight(br);
                    float hTL = getCornerHeight(tl);
                    float hTR = getCornerHeight(tr);

                    Vector3 vBL = new Vector3(x * cellSize, hBL, z * cellSize);
                    Vector3 vBR = new Vector3((x + 1) * cellSize, hBR, z * cellSize);
                    Vector3 vTL = new Vector3(x * cellSize, hTL, (z + 1) * cellSize);
                    Vector3 vTR = new Vector3((x + 1) * cellSize, hTR, (z + 1) * cellSize);

                    vertices[v + 0] = vBL;
                    vertices[v + 1] = vBR;
                    vertices[v + 2] = vTL;
                    vertices[v + 3] = vTR;

                    // Estimate face normal and slope
                    Vector3 edge1 = vBR - vBL;
                    Vector3 edge2 = vTL - vBL;
                    Vector3 normal = Vector3.Cross(edge1, edge2).normalized;

                    float slope = 1f - Vector3.Dot(normal, Vector3.up); // 0 = flat, 1 = vertical
                    float elevation = (hBL + hBR + hTL + hTR) / 4f;

                    Color vertexColor = new Color(elevation, slope, 0f, 1f); // R = elevation, G = slope
                    colors[v + 0] = vertexColor;
                    colors[v + 1] = vertexColor;
                    colors[v + 2] = vertexColor;
                    colors[v + 3] = vertexColor;

                    float u0 = x / (float)chunkSize;
                    float u1 = (x + 1) / (float)chunkSize;
                    float v0 = z / (float)chunkSize;
                    float v1 = (z + 1) / (float)chunkSize;

                    uvs[v + 0] = new Vector2(u0, v0);
                    uvs[v + 1] = new Vector2(u1, v0);
                    uvs[v + 2] = new Vector2(u0, v1);
                    uvs[v + 3] = new Vector2(u1, v1);

                    float diag1 = Mathf.Abs(hBL - hTR);
                    float diag2 = Mathf.Abs(hBR - hTL);

                    if (diag1 < diag2)
                    {
                        triangles[t + 0] = v + 0;
                        triangles[t + 1] = v + 2;
                        triangles[t + 2] = v + 3;

                        triangles[t + 3] = v + 0;
                        triangles[t + 4] = v + 3;
                        triangles[t + 5] = v + 1;
                    }
                    else
                    {
                        triangles[t + 0] = v + 0;
                        triangles[t + 1] = v + 2;
                        triangles[t + 2] = v + 1;

                        triangles[t + 3] = v + 1;
                        triangles[t + 4] = v + 2;
                        triangles[t + 5] = v + 3;
                    }

                    v += 4;
                    t += 6;
                }
            }

            mesh.vertices = vertices;
            mesh.triangles = triangles;
            mesh.uv = uvs;
            mesh.colors = colors; // Final assignment

            mesh.RecalculateNormals();

            MeshFilter mf = GetComponent<MeshFilter>();
            MeshRenderer mr = GetComponent<MeshRenderer>();

            mf.sharedMesh = mesh;
            _mat = Instantiate(terrainMaterial);
            mr.material = _mat;

            Vector2 terrainOrigin = new Vector2(chunkOrigin.x, chunkOrigin.y) * cellSize;
            float terrainSize = chunkSize * cellSize;

            _mat.SetVector("_TerrainOrigin", terrainOrigin);
            _mat.SetFloat("_TerrainWorldSize", terrainSize);
            _mat.SetFloat("_ChunkSize", chunkSize);

            GetComponent<MeshCollider>().sharedMesh = mesh;

            InitTrailSplats();
            InitSegmentMap();
        }


        private void InitTrailSplats()
        {
            // loop over your three splat maps
            for (int i = 0; i < trailSplats.Length; i++)
            {
                // 1) Allocate a fresh Texture2D for this chunk
                trailSplats[i] = new Texture2D(chunkSize, chunkSize, TextureFormat.RGBA32, false)
                {
                    wrapMode = TextureWrapMode.Clamp,
                    filterMode = FilterMode.Point
                };

                // 2) Clear it completely to transparent
                ClearSplat(i);

                // 3) Assign it to the per‐chunk material instance
                _mat.SetTexture($"_TrailSplatMap{i + 1}", trailSplats[i]);
            }
        }


        public void PaintGhostSegment(Vector2Int cell, int segmentID, int rotationID)
        {
           // Debug.Log($"#### [TerrainChunk.PaintGhostSegment] called -- {cell}, {segmentID}, {rotationID}");
            Vector2Int local = cell - chunkOrigin;
            if (!IsInsideChunk(local)) return;

            Color pixel = segmentMap.GetPixel(local.x, local.y);
            pixel.b = segmentID / 255f; // Segment type
            pixel.a = rotationID / 255f; // Rotation
            segmentMap.SetPixel(local.x, local.y, pixel);
            segmentMap.Apply();
        }


        public void ClearGhostSegment(Vector2Int cell)
        {
            Vector2Int local = cell - chunkOrigin;
            if (!IsInsideChunk(local)) return;

            Color pixel = segmentMap.GetPixel(local.x, local.y);
            pixel.b = 0f;
            pixel.a = 0f;
            segmentMap.SetPixel(local.x, local.y, pixel);
            segmentMap.Apply();
        }


        public void PaintTrailPixel(Vector2Int cell, int mapIndex, int channelIndex, float value = 1f)
        {
            Vector2Int local = cell - chunkOrigin;
            if (!IsInsideChunk(local)) return;

            Color pixel = trailSplats[mapIndex].GetPixel(local.x, local.y);
            pixel[channelIndex] = value;
            //pixel.a = 1f;
            trailSplats[mapIndex].SetPixel(local.x, local.y, pixel);

            trailSplats[mapIndex].Apply();
        }

        // Call this during Init
        private Texture2D segmentMap;
        private void InitSegmentMap()
        {
            segmentMap = new Texture2D(chunkSize, chunkSize, TextureFormat.RGBA32, false);
            segmentMap.wrapMode = TextureWrapMode.Clamp;
            segmentMap.filterMode = FilterMode.Point;

            // Clear
            Color[] clearPixels = new Color[chunkSize * chunkSize];
            for (int i = 0; i < clearPixels.Length; i++)
                clearPixels[i] = Color.clear;

            segmentMap.SetPixels(clearPixels);
            segmentMap.Apply();

            if (terrainMaterial != null)
                terrainMaterial.SetTexture("_TrailSegmentMap", segmentMap);
        }

        public void PaintSegmentType(Vector2Int cell, int segmentID, int rotationID)
        {
            Vector2Int local = cell - chunkOrigin;
            if (!IsInsideChunk(local)) return;

            float segmentValue = segmentID / 255f;
            float rotationValue = rotationID / 255f;

            Color pixel = new Color(segmentValue, rotationValue, 0f, 1f);
            segmentMap.SetPixel(local.x, local.y, pixel);
            segmentMap.Apply();
        }


        public void ClearTrailPixel(Vector2Int cell)
        {
            Vector2Int local = cell - chunkOrigin;
            if (!IsInsideChunk(local)) return;

            for (int i = 0; i < trailSplats.Length; i++)
            {
                trailSplats[i].SetPixel(local.x, local.y, Color.clear);
            }
        }

        public void ApplySplats()

        {
            for (int i = 0; i < trailSplats.Length; i++)
            {
                trailSplats[i].Apply();

#if UNITY_EDITOR
                DebugSaveSplat(trailSplats[i], $"TrailSplat{i + 1}_{chunkOrigin.x}_{chunkOrigin.y}");
#endif
            }

#if UNITY_EDITOR
          //  Debug.Log("TrailSplat images saved.");
#endif
        }
#if UNITY_EDITOR
        public void DebugSaveSegmentMap()
        {
            var bytes = segmentMap.EncodeToPNG();
            System.IO.File.WriteAllBytes(Application.dataPath + $"/Art/SplatMaps/TrailSegmentMap_{chunkOrigin.x}_{chunkOrigin.y}.png", bytes);
         //   Debug.Log($"✅ SegmentMap saved: TrailSegmentMap_{chunkOrigin.x}_{chunkOrigin.y}.png");
        }
#endif


        private void ClearSplat(int mapIndex)
        {
            Color[] clearPixels = new Color[chunkSize * chunkSize];
            for (int i = 0; i < clearPixels.Length; i++)
                clearPixels[i] = Color.clear;

            trailSplats[mapIndex].SetPixels(clearPixels);
            trailSplats[mapIndex].Apply();
        }

        private bool IsInsideChunk(Vector2Int localCoord)
        {
            return localCoord.x >= 0 && localCoord.x < chunkSize &&
                   localCoord.y >= 0 && localCoord.y < chunkSize;
        }

        public void AssignNavMeshArea(SlopeType slope)
        {
            var mod = GetComponent<NavMeshModifier>();
            mod.overrideArea = true;

            mod.area = slope switch
            {
                SlopeType.Flat => 0,
                SlopeType.Gentle => 1,
                SlopeType.Steep => 2,
                SlopeType.Cliff => 3,
                _ => 0
            };
        }

        public Vector2Int GetChunkOrigin()
        {
            return chunkOrigin;
        }

        public void SetTrailSplatTexture(int index, Texture2D tex)
        {
            if (index < 0 || index >= trailSplats.Length) return;

            trailSplats[index] = tex;
            string propName = $"_TrailSplatMap{index + 1}";
            GetComponent<MeshRenderer>()
            .material   // ← per-chunk instance
            .SetTexture(propName, tex);
        }

        public void DebugSaveSplat(Texture2D tex, string name)
        {
            var bytes = tex.EncodeToPNG();
            System.IO.File.WriteAllBytes(Application.dataPath + $"/Art/SplatMaps/{name}.png", bytes);
            //Debug.Log($"Saved {name}.png to {Application.dataPath}/Art/SplatMaps/");
        }
    }
}
