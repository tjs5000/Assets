// File: Assets/Editor/ChatGPTExportTool.cs

using UnityEditor;
using UnityEngine;
using System.IO;
using System.IO.Compression;
using System;

public class ChatGPTExportTool : EditorWindow
{
    private static readonly string[] defaultFolders = new[]
    {
        "Assets/Managers",
        "Assets/Systems",
        "Assets/Data",
        "Assets/UI",
        "Assets/AI",
        "Assets/Scenes",
        "Assets/Terrain",
        "Assets/Core",
        "Assets/Tests",
        "Assets/Editor"
    };

    private string exportFolderName = "ChatGPTExports";
    private string[] foldersToExport;
    private Vector2 scroll;

    [MenuItem("PlexiPark/Export for ChatGPT Review")]
    public static void ShowWindow()
    {
        GetWindow<ChatGPTExportTool>("ChatGPT Export Tool");
    }

    private void OnEnable()
    {
        foldersToExport = defaultFolders;
    }

    private void OnGUI()
    {
        EditorGUILayout.LabelField("📦 ChatGPT Export Tool", EditorStyles.boldLabel);
        EditorGUILayout.HelpBox("Packages selected folders into a .zip for ChatGPT upload.\nUse this to sync key systems or milestone progress.", MessageType.Info);

        exportFolderName = EditorGUILayout.TextField("Export Folder:", exportFolderName);

        EditorGUILayout.LabelField("Folders to Include:", EditorStyles.boldLabel);
        scroll = EditorGUILayout.BeginScrollView(scroll, GUILayout.Height(120));
        for (int i = 0; i < foldersToExport.Length; i++)
        {
            foldersToExport[i] = EditorGUILayout.TextField(foldersToExport[i]);
        }
        EditorGUILayout.EndScrollView();

        if (GUILayout.Button("➤ Export Now"))
        {
            ExportProjectZip();
        }
    }

    private void ExportProjectZip()
    {
        string projectPath = Application.dataPath.Substring(0, Application.dataPath.Length - "Assets".Length);
        string exportDir = Path.Combine(projectPath, exportFolderName);
        Directory.CreateDirectory(exportDir);

        string timestamp = DateTime.Now.ToString("yyyyMMdd_HHmm");
        string zipName = $"PlexiPark_Export_{timestamp}.zip";
        string zipPath = Path.Combine(exportDir, zipName);

        string tempDir = Path.Combine(projectPath, "Temp_ChatGPTExport");
        if (Directory.Exists(tempDir))
            Directory.Delete(tempDir, true);

        Directory.CreateDirectory(tempDir);

        foreach (string relativeFolder in foldersToExport)
        {
            string sourcePath = Path.Combine(projectPath, relativeFolder);
            if (Directory.Exists(sourcePath))
            {
                string folderName = Path.GetFileName(relativeFolder);
                string destPath = Path.Combine(tempDir, folderName);
                CopyDirectory(sourcePath, destPath);
            }
        }

        if (File.Exists(zipPath)) File.Delete(zipPath);
        ZipFile.CreateFromDirectory(tempDir, zipPath);
        Directory.Delete(tempDir, true);

        EditorUtility.RevealInFinder(zipPath);
        Debug.Log($"✅ Export complete: {zipPath}");
    }

    private static void CopyDirectory(string sourceDir, string targetDir)
    {
        Directory.CreateDirectory(targetDir);
        foreach (string file in Directory.GetFiles(sourceDir))
        {
            string dest = Path.Combine(targetDir, Path.GetFileName(file));
            File.Copy(file, dest, true);
        }

        foreach (string dir in Directory.GetDirectories(sourceDir))
        {
            string dest = Path.Combine(targetDir, Path.GetFileName(dir));
            CopyDirectory(dir, dest);
        }
    }
}
