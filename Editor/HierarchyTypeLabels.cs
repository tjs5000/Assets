// Editor/HierarchyTypeLabels.cs
using UnityEditor;
using UnityEngine;

[InitializeOnLoad]
public class HierarchyTypeLabels
{
    static HierarchyTypeLabels()
    {
        EditorApplication.hierarchyWindowItemOnGUI += OnHierarchyGUI;
    }

    static void OnHierarchyGUI(int instanceID, Rect selectionRect)
    {
        GameObject obj = EditorUtility.InstanceIDToObject(instanceID) as GameObject;
        if (obj == null) return;

        MonoBehaviour[] scripts = obj.GetComponents<MonoBehaviour>();
        if (scripts == null || scripts.Length == 0) return;

        string label = "";
        foreach (var script in scripts)
        {
            if (script != null)
                label += $"[{script.GetType().Name}] ";
        }

        if (!string.IsNullOrEmpty(label))
        {
            var style = new GUIStyle(EditorStyles.label);
            style.fontSize = 9;
            style.normal.textColor = Color.white;

            Rect r = new Rect(selectionRect.xMax - 200, selectionRect.y, 190, selectionRect.height);
            GUI.Label(r, label.Trim(), style);
        }
    }
}
