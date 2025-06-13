using UnityEditor;
using UnityEngine;
using PlexiPark.Data;

[CustomEditor(typeof(ParkObjectData))]
public class ParkObjectDataEditor : Editor
{
    public override void OnInspectorGUI()
    {
        // Draw default inspector first
        DrawDefaultInspector();

        ParkObjectData data = (ParkObjectData)target;

        if (data.Footprint == null || data.Footprint.Length == 0)
        {
            EditorGUILayout.HelpBox("Footprint is empty or null.", MessageType.Warning);
            return;
        }

        EditorGUILayout.Space();
        EditorGUILayout.LabelField("🧱 Footprint Preview", EditorStyles.boldLabel);

        DrawFootprintGrid(data);
    }

    private void DrawFootprintGrid(ParkObjectData data)
    {
        Vector2Int[] cells = data.Footprint;

        // Calculate bounds
        int minX = int.MaxValue, maxX = int.MinValue;
        int minY = int.MaxValue, maxY = int.MinValue;

        foreach (var cell in cells)
        {
            if (cell.x < minX) minX = cell.x;
            if (cell.x > maxX) maxX = cell.x;
            if (cell.y < minY) minY = cell.y;
            if (cell.y > maxY) maxY = cell.y;
        }

        int width = maxX - minX + 1;
        int height = maxY - minY + 1;

        // Build grid
        for (int y = maxY; y >= minY; y--)
        {
            EditorGUILayout.BeginHorizontal();
            for (int x = minX; x <= maxX; x++)
            {
                Vector2Int current = new Vector2Int(x, y);
                bool isInFootprint = System.Array.Exists(cells, cell => cell == current);
                bool isOrigin = current == Vector2Int.zero;

                GUIStyle style = new GUIStyle(EditorStyles.miniButtonMid);
                style.fixedWidth = 24;
                style.fixedHeight = 24;
                style.alignment = TextAnchor.MiddleCenter;

                Color prevColor = GUI.color;

                if (isOrigin)
                    GUI.color = Color.green;
                else if (isInFootprint)
                    GUI.color = Color.cyan;
                else
                    GUI.color = Color.gray;

                GUILayout.Button(isInFootprint ? (isOrigin ? "O" : "■") : "", style);

                GUI.color = prevColor;
            }
            EditorGUILayout.EndHorizontal();
        }
    }
}
