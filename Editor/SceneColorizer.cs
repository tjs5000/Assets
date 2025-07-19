// Assets/Editor/HierarchyColorizer.cs
using UnityEditor;
using UnityEngine;
using System.Collections.Generic;
using System.Linq; // Required for .ToList()
using UnityEditor.SceneManagement; // Needed for EditorSceneManager for scene open/close events

[InitializeOnLoad]
static class HierarchyColorizer
{
    // A list to store the top-level GameObjects in their hierarchy order
    private static List<GameObject> s_OrderedRootObjects;

    // Optional: Make the tint color configurable
    private static readonly Color kSecondaryStripeTint = new Color(75f / 255f, 75f / 255f, 75f / 255f);

    static HierarchyColorizer()
    {
        EditorApplication.hierarchyWindowItemOnGUI += OnHierarchyGUI;
        EditorApplication.hierarchyChanged += RecalculateRootOrderAndColors;
        
        // Also recalculate when a scene is opened or closed, as GetRootGameObjects() depends on the active scene.
        EditorSceneManager.sceneOpened += (scene, mode) => RecalculateRootOrderAndColors();
        EditorSceneManager.sceneClosed += (scene) => RecalculateRootOrderAndColors();

        RecalculateRootOrderAndColors(); // Initial calculation on load
    }

    private static void RecalculateRootOrderAndColors()
    {
        // Get all root GameObjects in the scene.
        // GetRootGameObjects() gives them in hierarchy order.
        s_OrderedRootObjects = UnityEngine.SceneManagement.SceneManager.GetActiveScene()
                                    .GetRootGameObjects()
                                    .Where(go => (go.hideFlags & HideFlags.HideInHierarchy) == 0) // Filter out hidden objects
                                    .ToList();
    }

    /// <summary>
    /// Generates a color from the full spectrum (Red to Violet) based on an index and total count.
    /// </summary>
    /// <param name="index">The current index of the item.</param>
    /// <param name="totalItems">The total number of items.</param>
    /// <returns>A color from the spectrum.</returns>
    private static Color GetSpectrumColor(int index, int totalItems)
    {
        if (totalItems <= 1) // Handle cases with 0 or 1 top-level object
        {
            if (totalItems == 1) return Color.red; // If only one, make it red
            return Color.white; // Or default to white if no objects
        }

        // Hue value from 0 to 1, representing the full spectrum
        float hue = (float)index / (totalItems + 1); 

        // Convert HSV to RGB. Full saturation and value for vibrant colors.
        return Color.HSVToRGB(hue, 1f, 1f);
    }

    private static void OnHierarchyGUI(int instanceID, Rect rowRect)
    {
        var go = EditorUtility.InstanceIDToObject(instanceID) as GameObject;
        if (go == null || s_OrderedRootObjects == null || s_OrderedRootObjects.Count == 0) return;

        // Find top-level ancestor
        var t = go.transform;
        while (t.parent != null) t = t.parent;

        // Find the index of the top-level GameObject in our ordered list
        int rootIndex = s_OrderedRootObjects.IndexOf(t.gameObject); // Corrected: t.gameObject

        if (rootIndex == -1)
        {
            // This can happen if an object was just created and not yet registered by hierarchyChanged,
            // or if it's a hidden Unity internal object.
            return;
        }

        Color baseColor = GetSpectrumColor(rootIndex, s_OrderedRootObjects.Count);

        // --- Calculate tint for the primary (left) stripe ---
        int depth = 0;
        var cursor = go.transform;
        while (cursor.parent != null && cursor.parent != t)
        {
            depth++;
            cursor = cursor.parent;
        }
        
        float factor = Mathf.Clamp01(depth * 0.15f); // Adjusted depth factor
        var primaryStripeColor = Color.Lerp(baseColor, kSecondaryStripeTint, factor); // Use the new configurable tint color
        primaryStripeColor.a = 0.4f; // a bit translucent

        // --- Draw the primary (left) stripe ---
        var primaryStripeRect = new Rect(
            rowRect.x,           // start at the very left
            rowRect.y,
            16,                  // 16px width
            rowRect.height
        );
        EditorGUI.DrawRect(primaryStripeRect, primaryStripeColor);


        // --- Calculate color for the secondary (full width) rectangle ---
        // It should be the color value of the first rectangle (baseColor) but at 50% transparency.
        // We'll use the 'baseColor' for the secondary stripe, as per your request.
        Color secondaryStripeColor = Color.Lerp(baseColor, kSecondaryStripeTint, factor);
        secondaryStripeColor.a = 0.06f; // 50% of the original alpha (0.25 * 0.5 = 0.125), but let's try a lower value like 0.05f or 0.1f to make it subtle.
                                        // The original primary stripe has 0.25f alpha. If this one is 0.5f, it might be too strong.
                                        // Let's set it to a fixed, subtle alpha. You can adjust this value (0.0f to 1.0f).
                                        // A value of 0.05f or 0.1f usually looks good for a subtle background hint.

        // --- Draw the secondary (full width, offset) rectangle ---
        var secondaryStripeRect = new Rect(
            rowRect.x + 16,       // Offset from the left by 16 pixels (after the first stripe)
            rowRect.y,
            rowRect.width - 16,   // Spans the rest of the width
            rowRect.height
        );
        EditorGUI.DrawRect(secondaryStripeRect, secondaryStripeColor);
    }
}