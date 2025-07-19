// CenterlineMeshBuilder.cs
using UnityEngine;
using PlexiPark.Core.Interfaces;
using System.Collections.Generic;

namespace PlexiPark.Systems.Trail
{
    public static class CenterlineMeshBuilder
    {
        /// <summary>
        /// worldSpine: list of world‐space cell‐centers.
        /// sampler: fallback height sampler if raycast misses.
        /// terrainMask: which Unity layers to raycast against.
        /// </summary>
        public static Mesh BuildWorld(
            List<Vector3> worldSpine,
            float width,
            IHeightSampler sampler,
            LayerMask terrainMask,    // ← new
            float xOffset, float yOffset, float zOffset
        )
        {
            if (worldSpine == null || worldSpine.Count < 2) return null;

            int n = worldSpine.Count;
            float halfW = width * 1f;

            var verts = new Vector3[n * 2];
            var uvs = new Vector2[n * 2];
            var tris = new int[(n - 1) * 6];

            Vector3 userOffset = new Vector3(xOffset, yOffset, zOffset);

            for (int i = 0; i < n; i++)
            {
                // 1) forward tangent flattened in Y
                Vector3 fw = (i == 0 ? worldSpine[1] - worldSpine[0]
                              : i == n - 1 ? worldSpine[n - 1] - worldSpine[n - 2]
                                           : worldSpine[i + 1] - worldSpine[i - 1]);
                fw.y = 0f;
                if (fw.sqrMagnitude < 1e-6f) fw = Vector3.forward;
                else fw.Normalize();

                // 2) perp
                Vector3 perp = new Vector3(-fw.z, 0, fw.x) * halfW;

                // 3) left/right base positions
                Vector3 left = worldSpine[i] + perp + userOffset;
                Vector3 right = worldSpine[i] - perp + userOffset;

                // 4) sample the actual terrain mesh
                left = SampleTerrainHeight(left, terrainMask, sampler);
                right = SampleTerrainHeight(right, terrainMask, sampler);

                verts[i * 2 + 0] = left;
                verts[i * 2 + 1] = right;

                float u = i / (float)(n - 1);
                uvs[i * 2 + 0] = new Vector2(u, 0f);
                uvs[i * 2 + 1] = new Vector2(u, 1f);
            }

            // build tris
            int ti = 0;
            for (int i = 0; i < n - 1; i++)
            {
                int v = i * 2;
                // our four corners:
                //   v+0 = left(i)
                //   v+1 = right(i)
                //   v+2 = left(i+1)
                //   v+3 = right(i+1)
                float h00 = verts[v + 0].y;
                float h01 = verts[v + 1].y;
                float h10 = verts[v + 2].y;
                float h11 = verts[v + 3].y;

                // two possible diagonals:
                // diagA: left(i) → right(i+1)
                // diagB: left(i+1) → right(i)
                float diagA = Mathf.Abs(h00 - h11);
                float diagB = Mathf.Abs(h10 - h01);

                if (diagA < diagB)
                {
                    // cut along left(i) → right(i+1)
                    tris[ti++] = v + 0;
                    tris[ti++] = v + 2;
                    tris[ti++] = v + 3;

                    tris[ti++] = v + 0;
                    tris[ti++] = v + 3;
                    tris[ti++] = v + 1;
                }
                else
                {
                    // cut along left(i+1) → right(i) (your old pattern)
                    tris[ti++] = v + 0;
                    tris[ti++] = v + 2;
                    tris[ti++] = v + 1;

                    tris[ti++] = v + 2;
                    tris[ti++] = v + 3;
                    tris[ti++] = v + 1;
                }
            }

            var mesh = new Mesh
            {
                name = "TrailCenterline",
                vertices = verts,
                uv = uvs,
                triangles = tris
            };
            mesh.triangles = tris;
            mesh.RecalculateNormals();
            mesh.RecalculateBounds();
            return mesh;
        }

        /// <summary>
        /// Raycasts straight down onto your TerrainChunk mesh.
        /// Falls back to IHeightSampler if nothing is hit.
        /// </summary>
        private static Vector3 SampleTerrainHeight(
           Vector3 point, int terrainLayerMask, IHeightSampler sampler
        )
        {
            // start way up
            Ray ray = new Ray(point + Vector3.up * 50f, Vector3.down);
            if (Physics.Raycast(ray, out var hit, 100f, terrainLayerMask))
            {
                point.y = hit.point.y;
            }
            else
            {
                // fallback to corner‐map or other sampler
                point.y = sampler.SampleHeight(point.x, point.z);
            }
            return point;
        }
    }
}
