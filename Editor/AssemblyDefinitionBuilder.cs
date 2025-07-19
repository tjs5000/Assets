using UnityEngine;
using UnityEditor;
using System.IO;
using System.Text;
using System.Collections.Generic;

public class AssemblyDefinitionBuilder : EditorWindow
{
    [MenuItem("PlexiPark/Auto-Create Assembly Definitions")]
    public static void CreateAssemblyDefinitions()
    {
        Debug.Log("📦 Generating .asmdef files...");

        // Define assemblies and their dependencies
        var definitions = new List<AssemblyDef>
        {
            new AssemblyDef("PlexiPark.Data", "Assets/Data", false),
            new AssemblyDef("PlexiPark.Terrain", "Assets/Terrain", false, new[] { "PlexiPark.Data","PlexiPark.Systems" }),
            new AssemblyDef("PlexiPark.Managers", "Assets/Managers", false, new[] { "PlexiPark.Data" }),
            new AssemblyDef("PlexiPark.Systems", "Assets/Systems", false, new[] { "PlexiPark.Data" }),
            new AssemblyDef("PlexiPark.UI", "Assets/UI", false, new[] { "PlexiPark.Data" }),
            new AssemblyDef("Tests.EditMode", "Assets/Tests/EditMode", true, new[] { "PlexiPark.Data", "PlexiPark.Managers" }),
            new AssemblyDef("Tests.PlayMode", "Assets/Tests/PlayMode", true, new[] { "PlexiPark.Data", "PlexiPark.Managers" })
        };

        foreach (var def in definitions)
            CreateAsmdef(def);

        AssetDatabase.Refresh();
        Debug.Log("✅ Assembly definitions created and linked.");
    }

    private static void CreateAsmdef(AssemblyDef def)
    {
        Directory.CreateDirectory(def.folder);

        string asmdefPath = Path.Combine(def.folder, def.name + ".asmdef");

        if (File.Exists(asmdefPath))
        {
            Debug.Log($"ℹ️ {def.name}.asmdef already exists — skipping.");
            return;
        }

        StringBuilder sb = new StringBuilder();
        sb.AppendLine("{");
        sb.AppendLine($"  \"name\": \"{def.name}\",");

        if (def.references.Length > 0)
        {
            sb.AppendLine("  \"references\": [");
            for (int i = 0; i < def.references.Length; i++)
            {
                string r = def.references[i];
                sb.Append($"    \"{r}\"");
                if (i < def.references.Length - 1) sb.Append(",");
                sb.AppendLine();
            }
            sb.AppendLine("  ],");
        }

        sb.AppendLine($"  \"includePlatforms\": [],");
        sb.AppendLine($"  \"excludePlatforms\": [],");
        sb.AppendLine($"  \"allowUnsafeCode\": false,");
        sb.AppendLine($"  \"overrideReferences\": false,");
        sb.AppendLine($"  \"precompiledReferences\": [],");
        sb.AppendLine($"  \"autoReferenced\": true,");
        sb.AppendLine($"  \"defineConstraints\": [],");
        sb.AppendLine($"  \"versionDefines\": [],");
        sb.AppendLine($"  \"noEngineReferences\": false,");
        sb.AppendLine($"  \"testAssemblies\": {def.isTest.ToString().ToLower()}");
        sb.AppendLine("}");

        File.WriteAllText(asmdefPath, sb.ToString());
        Debug.Log($"✅ Created {def.name}.asmdef in {def.folder}");
    }

    private struct AssemblyDef
    {
        public string name;
        public string folder;
        public bool isTest;
        public string[] references;

        public AssemblyDef(string name, string folder, bool isTest, string[] references = null)
        {
            this.name = name;
            this.folder = folder;
            this.isTest = isTest;
            this.references = references ?? new string[0];
        }
    }
}
