// File: Assets/Editor/FolderCreator.cs

using UnityEditor;
using UnityEngine;

public static class FolderCreator
{
    [MenuItem("PlexiPark/Generate Folder Structure")]
    public static void GenerateFolders()
    {
        CreateFolder("Assets", "Core");
        CreateSubFolders("Assets/Core", new[]
        {
            "Interfaces",
            "Utilities",
            "Constants",
            "Documentation"
        });

        CreateFolder("Assets", "Data");
        CreateSubFolders("Assets/Data", new[]
        {
            "ParkObjects/Facilities",
            "ParkObjects/Landscape",
            "ParkObjects/Paths",
            "Visitors",
            "Habits",
            "Achievements",
            "UI",
            "SharedEnums"
        });

        CreateFolder("Assets", "Managers");

        CreateFolder("Assets", "Systems");
        CreateSubFolders("Assets/Systems", new[]
        {
            "Placement",
            "Simulation",
            "Visitors",
            "Financial",
            "SaveLoad",
            "Events"
        });

        CreateFolder("Assets", "UI");
        CreateSubFolders("Assets/UI", new[]
        {
            "Views",
            "Panels",
            "Prefabs",
            "Icons",
            "Animations"
        });

        CreateFolder("Assets", "Plugins");
        CreateSubFolders("Assets/Plugins", new[]
        {
            "Android",
            "Editor"
        });

        CreateFolder("Assets", "Art");
        CreateSubFolders("Assets/Art", new[]
        {
            "Models",
            "Textures",
            "Sprites",
            "Icons",
            "Animations"
        });

        CreateFolder("Assets", "Audio");
        CreateSubFolders("Assets/Audio", new[]
        {
            "Music",
            "SFX",
            "AudioMixers"
        });

        CreateFolder("Assets", "AddressableGroups");
        CreateSubFolders("Assets/AddressableGroups", new[]
        {
            "ParkObjectAssets",
            "UIIcons",
            "HealthData"
        });

        CreateFolder("Assets", "Resources");
        CreateFolder("Assets", "Scenes");
        CreateSubFolders("Assets/Scenes", new[]
        {
            "MainMenu",
            "ParkScene_Urban",
            "TestScenes"
        });

        CreateFolder("Assets", "Tests");
        CreateSubFolders("Assets/Tests", new[]
        {
            "EditMode",
            "PlayMode"
        });

        AssetDatabase.Refresh();
        Debug.Log("✅ PlexiPark folder structure generated.");
    }

    private static void CreateFolder(string parent, string child)
    {
        if (!AssetDatabase.IsValidFolder($"{parent}/{child}"))
            AssetDatabase.CreateFolder(parent, child);
    }

    private static void CreateSubFolders(string root, string[] subfolders)
    {
        foreach (string path in subfolders)
        {
            string[] parts = path.Split('/');
            string current = root;
            foreach (string part in parts)
            {
                string next = $"{current}/{part}";
                if (!AssetDatabase.IsValidFolder(next))
                    AssetDatabase.CreateFolder(current, part);
                current = next;
            }
        }
    }
}
