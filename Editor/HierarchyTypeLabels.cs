// Editor/HierarchyTypeLabels.cs
using UnityEditor;
using UnityEngine;
using System.Linq; // Required for LINQ extensions like .Where() and .Select()
using System.Collections.Generic; // Needed for List

[InitializeOnLoad]
public class HierarchyTypeLabels
{
    // Define your desired colors for different component types
    private static readonly Color kMonoScriptColor = new Color(0.7f, 0.9f, 1.0f); // A light blue/cyan for MonoScripts
    private static readonly Color kBuiltInComponentColor = new Color(0.6f, 0.6f, 0.6f); // A darker gray for built-in components
    private static readonly Color kMissingComponentColor = new Color(1.0f, 0.2f, 0.2f); // Red for missing scripts

    static HierarchyTypeLabels()
    {
        EditorApplication.hierarchyWindowItemOnGUI += OnHierarchyGUI;
    }

    static void OnHierarchyGUI(int instanceID, Rect selectionRect)
    {
        GameObject obj = EditorUtility.InstanceIDToObject(instanceID) as GameObject;
        if (obj == null) return;

        // Get all components attached to the GameObject.
        Component[] components = obj.GetComponents<Component>();

        // We'll build up a list of (ComponentType, Color) pairs to render
        List<(string label, Color color)> componentLabels = new List<(string, Color)>();

        foreach (var component in components)
        {
            if (component == null)
            {
                // This handles "Missing (Mono Script)" components
                componentLabels.Add(("[Missing Script]", kMissingComponentColor));
            }
            else if (component is Transform)
            {
                // Skip Transform, as it's always there and usually not what we want to label
                continue;
            }
            else if (component is MonoBehaviour)
            {
                // This is a "monoscript" (your custom scripts or built-in MonoBehaviours)
                componentLabels.Add(($"[{component.GetType().Name}]", kMonoScriptColor));
            }
            else
            {
                // All other built-in components (MeshRenderer, Collider, etc.)
                componentLabels.Add(($"[{component.GetType().Name}]", kBuiltInComponentColor));
            }
        }

        if (!componentLabels.Any())
        {
            return;
        }

        // --- Prepare the GUIStyle ---
        var style = new GUIStyle(EditorStyles.label);
        style.fontSize = 9;
        style.alignment = TextAnchor.MiddleRight; // Align text to the right

        // Calculate the total width needed for all labels
        float totalLabelWidth = 0;
        foreach (var item in componentLabels)
        {
            // Estimate the width of each label with the current style.
            // This is an estimate, actual width can vary slightly.
            totalLabelWidth += style.CalcSize(new GUIContent(item.label)).x;
        }

        // Add some padding between labels
        totalLabelWidth += (componentLabels.Count - 1) * 4; // 4 pixels per gap

        // Adjust the rectangle for where the labels will be drawn
        // Start from the right edge and go left by the calculated total width
        // Ensure it doesn't go off the left edge of the selectionRect
        float startX = Mathf.Max(selectionRect.x, selectionRect.xMax - totalLabelWidth - 5); // -5 for some right padding
        Rect r = new Rect(startX, selectionRect.y, selectionRect.xMax - startX, selectionRect.height);

        // --- Draw each component label with its specific color ---
        float currentX = startX;
        foreach (var item in componentLabels)
        {
            style.normal.textColor = item.color; // Set the text color for this label
            GUIContent content = new GUIContent(item.label);
            
            // Calculate width for this specific label
            Vector2 labelSize = style.CalcSize(content);
            Rect currentLabelRect = new Rect(currentX, selectionRect.y, labelSize.x, selectionRect.height);

            // You can optionally add tooltips for each individual label if desired
            // content.tooltip = item.label; 

            GUI.Label(currentLabelRect, content, style);
            currentX += labelSize.x + 4; // Move currentX for the next label, with 4px padding
        }
    }
}