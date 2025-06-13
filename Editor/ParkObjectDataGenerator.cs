// Assets/Editor/ParkObjectDataGenerator.cs
using UnityEditor;
using UnityEngine;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using PlexiPark.Data;

public class ParkObjectDataGenerator : EditorWindow
{
    [MenuItem("PlexiPark/Generate ParkObjectData Assets")]
    public static void ShowWindow()
    {
        GetWindow<ParkObjectDataGenerator>("ParkObjectDataGenerator");
    }

    private void OnGUI()
    {
        GUILayout.Label("Auto-Generate ParkObjectData", EditorStyles.boldLabel);
        if (GUILayout.Button("Generate All ParkObjectData"))
            GenerateAssets();
    }

    private static void GenerateAssets()
    {
        const string baseFolder = "Assets/Data/ParkObjects";
        var definitions = new List<Definition>
        {
            // Paths
            new("HikingTrail","Hiking Trail","HikingTrailPrefab",100,5,0.1f,false,true, ParkObjectCategory.Path),
            new("WalkingPath","Walking Path","WalkingPathPrefab",80,4,0.05f,false,true, ParkObjectCategory.Path),
            new("BikeTrail","Bike Trail","BikeTrailPrefab",120,6,0.15f,false,true, ParkObjectCategory.Path),
            new("MountainTrail","Mountain Trail","MountainTrailPrefab",150,7,0.2f,false,true, ParkObjectCategory.Path),
            new("ServiceRoad","Service Road","ServiceRoadPrefab",90,3,0f,false,true, ParkObjectCategory.Path),

            // Facility
            new("Restroom","Restrooms","RestroomPrefab",200,10,0.2f,true,false, ParkObjectCategory.Facility),
            new("VisitorCenter","Visitor Center","VisitorCenterPrefab",500,25,0.5f,true,false, ParkObjectCategory.Facility),
            new("Playground","Playground","PlaygroundPrefab",300,15,0.3f,true,false, ParkObjectCategory.Facility),
            new("LookoutTower","Lookout Tower","LookoutTowerPrefab",400,20,0.4f,true,false, ParkObjectCategory.Facility),
            new("Cafe","Cafe/Food Stall","CafePrefab",350,18,0.35f,true,false, ParkObjectCategory.Facility),
            new("Campsite","Campsite","CampsitePrefab",250,12,0.25f,true,false, ParkObjectCategory.Facility),
            new("ResearchOutpost","Research Outpost","ResearchOutpostPrefab",450,22,0.45f,true,false, ParkObjectCategory.Facility),
            new("SportsField","Sports Field","SportsFieldPrefab",380,19,0.38f,true,false, ParkObjectCategory.Facility),
            new("PerformanceStage","Performance Stage","PerformanceStagePrefab",420,21,0.42f,true,false, ParkObjectCategory.Facility),

            // Natural
            new("Trees","Trees","TreesPrefab",50,2,0.05f,false,false, ParkObjectCategory.Natural),
            new("Rocks","Rocks","RockPrefab",300,12,0.3f,false,false, ParkObjectCategory.Natural),

            // Attractions
            new("PublicArt","Public Art","PublicArtPrefab",200,8,0.2f,false,false, ParkObjectCategory.Attraction),
            new("NaturalMarvel","Natural Marvels","NaturalMarvelPrefab",300,12,0.3f,false,false, ParkObjectCategory.Attraction),

            // Amenities
            new("Bench","Bench","BenchPrefab",50,2,0.05f,false,false, ParkObjectCategory.Amenity),
            new("LampPost","Lamp Post","LampPostPrefab",150,5,0.15f,false,false, ParkObjectCategory.Amenity),

            // Waterway
            new("WaterFeature","Water Features","WaterFeaturePrefab",150,5,0.15f,false,false, ParkObjectCategory.Waterway),
            new("Waterway","Waterway","WaterwayPrefab",150,5,0.15f,false,false, ParkObjectCategory.Waterway),
        };

        foreach (var def in definitions)
        {
            // Determine folder from category
            string folder = $"{baseFolder}/{def.Category}";
            if (!AssetDatabase.IsValidFolder(folder))
            {
                AssetDatabase.CreateFolder(baseFolder, def.Category.ToString());
                Debug.Log($"Created folder: {folder}");
            }

            // Instantiate and populate the SO
            var asset = ScriptableObject.CreateInstance<ParkObjectData>();
            asset.ObjectID                = def.ID;
            asset.DisplayName             = def.DisplayName;
            asset.Prefab = AssetDatabase.LoadAssetAtPath<GameObject>(
                $"Assets/Prefabs/{def.PrefabName}.prefab"
            );
            if (asset.Prefab == null)
                Debug.LogWarning($"Prefab not found at Assets/Prefabs/{def.PrefabName}.prefab");

            asset.Cost                    = def.Cost;
            asset.MaintenanceCostPerMonth = def.MaintenanceCost;
            asset.ProfitPerMonth          = 0;
            asset.RatingPenalty           = 0f;
            asset.BaseRatingImpact        = def.BaseRatingImpact;

            asset.Category = def.Category;

            // Placement defaults
            asset.AllowedSlopes        = new List<SlopeType> { SlopeType.Flat };
            asset.Footprint            = new[] { Vector2Int.zero };
            asset.preset               = FootprintPreset.Custom;
            asset.RequiresValidTerrain = false;
            asset.IsPermanent          = false;

            // Upgrade tier defaults
            asset.MaxUpgradeTier     = 1;
            asset.TierNames          = new List<string>();
            asset.MaintenancePerTier = new List<int>();

            // Visitor / Need lists
            asset.VisitorAttraction = new List<VisitorAttractionEntry>();
            asset.NeedFulfillment   = new List<NeedFulfillmentEntry>();

            // Park-type overrides
            asset.ParkTypeOverrides = new List<ParkObjectData.ParkTypeOverrideEntry>();

            // Save asset
            string path = $"{folder}/{def.ID}.asset";
            AssetDatabase.CreateAsset(asset, path);
            Debug.Log($"Created ParkObjectData: {path}");
        }

        AssetDatabase.SaveAssets();
        Debug.Log("✅ Generated all ParkObjectData assets with full defaults.");
    }

    private class Definition
    {
        public string ID, DisplayName, PrefabName;
        public int Cost, MaintenanceCost;
        public float BaseRatingImpact;
        public bool IsFacility, IsPath;
        public ParkObjectCategory Category;

        public Definition(
            string id, string displayName, string prefabName,
            int cost, int maintenanceCost, float baseRatingImpact,
            bool isFacility, bool isPath,
            ParkObjectCategory category)
        {
            ID = id;
            DisplayName = displayName;
            PrefabName = prefabName;
            Cost = cost;
            MaintenanceCost = maintenanceCost;
            BaseRatingImpact = baseRatingImpact;
            IsFacility = isFacility;
            IsPath = isPath;
            Category = category;
        }
    }
}
