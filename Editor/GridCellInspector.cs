// Assets/Editor/GridCellInspector.cs
using UnityEngine;
using UnityEditor;
using UnityEditor.Overlays;
using UnityEditor.Toolbars;
using UnityEngine.AI;
using PlexiPark.Core.Utils;
using PlexiPark.Managers;
using Unity.AI.Navigation;

[InitializeOnLoad]
public static class GridCellInspector
{
    static GridCellInspector()
    {
        SceneView.duringSceneGui += OnSceneGUI;
    }

    private static void OnSceneGUI(SceneView sv)
    {
        var e = Event.current;
        if (e.type == EventType.MouseDown
         && e.button == 0
         && (e.modifiers & EventModifiers.Control) != 0)
        {
            // Raycast only against your Terrain layer
            int terrainMask = LayerMask.GetMask("Terrain");
            Ray ray = HandleUtility.GUIPointToWorldRay(e.mousePosition);
            if (!Physics.Raycast(ray, out var hit, Mathf.Infinity, terrainMask))
                return;

            Vector3 worldPos = hit.point;
            float cs = GridManager.Instance.CellSize;
            Vector2Int cell = GridUtils.WorldToCell(worldPos,
                                                    GridManager.Instance.origin,
                                                    cs);

            // Compute the true world‐space center
            Vector2 cell2D = new Vector2(cell.x, cell.y) * cs;
            float height = GridManager.Instance.GetAverageElevation(cell);
            Vector3 center = new Vector3(cell2D.x + cs * 0.5f,
                                           height,
                                           cell2D.y + cs * 0.5f);

            var slope = GridManager.Instance.ClassifySlope(cell);
            Debug.Log($"CTRL+Click → Cell {cell} | Center {center} | " +
                      $"AvgH {height:F2} | Slope {slope}");

            // Draw the cell‐center marker
            Handles.color = Color.cyan;
            Handles.DrawSolidDisc(center, Vector3.up, cs * 0.3f);

            // Find the single *closest* ribbon vert across *all* TrailNav surfaces
            float bestDist = float.MaxValue;
            Vector3 bestWorldVert = Vector3.zero;

            foreach (var surf in Object.FindObjectsOfType<NavMeshSurface>())
            {
                if (!surf.name.StartsWith("TrailNav_")) continue;

                var mf = surf.GetComponent<MeshFilter>();
                if (mf?.mesh == null) continue;

                foreach (var localV in mf.mesh.vertices)
                {
                    // convert mesh‐space → world‐space
                    var worldV = surf.transform.TransformPoint(localV);
                    float d = Vector3.Distance(worldV, worldPos);
                    if (d < bestDist)
                    {
                        bestDist = d;
                        bestWorldVert = worldV;
                    }
                }
            }

            if (bestDist < float.MaxValue)
            {
                Handles.color = Color.yellow;
                Handles.DrawWireDisc(bestWorldVert, Vector3.up, cs * 0.2f);
                Debug.Log($"→ Nearest ribbon vert @ {bestWorldVert} (dist {bestDist:F2})");
            }
            else
            {
                Debug.LogWarning("No TrailNav_* surfaces found in scene.");
            }

            e.Use();
        }
    }
}
