// Assets/Editor/ParkObjectDataGenerator.cs
using UnityEditor;
using UnityEngine;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using PlexiPark.Data;
using PlexiPark.Core.SharedEnums;

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
            // Facility
            new("Restroom","Restrooms","outhouse_closed",200,10,0.2f,true,false, ParkObjectCategory.Facility),
            new("VisitorCenter","Visitor Center","VisitorCenterPrefab",500,25,0.5f,true,false, ParkObjectCategory.Facility),
            new("Playground","Playground","playground",300,15,0.3f,true,false, ParkObjectCategory.Facility),
            new("BasketballCourt","Basketball Court","basketball_court",400,20,0.4f,true,false, ParkObjectCategory.Facility),
            new("SmallCafe","Small Cafe","cafe_1",350,18,0.35f,true,false, ParkObjectCategory.Facility),
            new("LargeCafe","Large Cafe","cafe_2",350,18,0.35f,true,false, ParkObjectCategory.Facility),
            new("Shop","Shop","Shop",250,12,0.25f,true,false, ParkObjectCategory.Facility),
            new("Supermarket","Supermarket","Supermarket",250,12,0.25f,true,false, ParkObjectCategory.Facility),
            new("PowerSubstation","Power Substation","PowerSubstation",450,22,0.45f,true,false, ParkObjectCategory.Facility),
            new("FireStation","FireStation","FireStation",380,19,0.38f,true,false, ParkObjectCategory.Facility),
            new("PoliceStation","PoliceStation","PoliceStation",380,19,0.38f,true,false, ParkObjectCategory.Facility),
            new("Billboard","Billboard","billboard",420,21,0.42f,true,false, ParkObjectCategory.Facility),

            // Natural
            new("Tree","Tree","TreesPrefab",50,2,0.05f,false,false, ParkObjectCategory.Natural),
            new("FlowerBed","Flower Bed","FlowerBed",300,12,0.3f,false,false, ParkObjectCategory.Natural),
            new("PlanterBox","Planter Box","PlanterBox",300,12,0.3f,false,false, ParkObjectCategory.Natural),
            new("Bush","Bush","Bush",300,12,0.3f,false,false, ParkObjectCategory.Natural),
            new("Shrub","Shrub","Shrub",300,12,0.3f,false,false, ParkObjectCategory.Natural),
            new("Boulder","Boulder","Boulder",300,12,0.3f,false,false, ParkObjectCategory.Natural),
            new("RockPile","RockPile","RockPile",300,12,0.3f,false,false, ParkObjectCategory.Natural),
            new("OakTree","Oak Tree","OakTree",300,12,0.3f,false,false, ParkObjectCategory.Natural),
            new("PineTree","Pine Tree","PineTree",300,12,0.3f,false,false, ParkObjectCategory.Natural),
            new("Pond","Pond","Pond",300,12,0.3f,false,false, ParkObjectCategory.Natural),

            // Attractions
            new("PublicArt1","Public Art 1","art_installation_1",200,8,0.2f,false,false, ParkObjectCategory.Attraction),
            new("PublicArt2","Public Art 2","art_installment_2",200,8,0.2f,false,false, ParkObjectCategory.Attraction),
            new("PublicArt 3","Public Art 3","Statue",200,8,0.2f,false,false, ParkObjectCategory.Attraction),
            new("Fountain","Fountain","Fountain",200,8,0.2f,false,false, ParkObjectCategory.Attraction),

            // Amenities
            new("Bench","Bench","bench1",50,2,0.05f,false,false, ParkObjectCategory.Amenity),
            new("LampPost","Lamp Post","StreetLamp",150,5,0.15f,false,false, ParkObjectCategory.Amenity),
            new("Trashcan","Trashcan","trashcan_1",50,2,0.05f,false,false, ParkObjectCategory.Amenity)

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
            asset.ObjectID = def.ID;
            asset.DisplayName = def.DisplayName;
            var loaded = AssetDatabase.LoadAssetAtPath<GameObject>(
                $"Assets/Prefabs/PlaceableObjects/{def.PrefabName}.prefab"
            );
            if (loaded == null)
                Debug.LogWarning($"Prefab not found at Assets/Prefabs/{def.PrefabName}.prefab");

            asset.finalPrefab = loaded;
            asset.previewPrefab = loaded; // or point this at a dedicated ghost‐variant prefab

            asset.Cost = def.Cost;
            asset.MaintenanceCostPerMonth = def.MaintenanceCost;
            asset.ProfitPerMonth = 0;
            asset.RatingPenalty = 0f;
            asset.BaseRatingImpact = def.BaseRatingImpact;

            asset.Category = def.Category;

            // Placement defaults
            asset.AllowedSlopes = new List<SlopeType> { SlopeType.Flat };
            asset.Footprint = new[] { Vector2Int.zero };
            asset.preset = FootprintPreset.Custom;
            asset.RequiresValidTerrain = false;
            asset.IsPermanent = false;

            // Upgrade tier defaults
            asset.MaxUpgradeTier = 1;
            asset.TierNames = new List<string>();
            asset.MaintenancePerTier = new List<int>();

            // Visitor / Need lists
            asset.VisitorAttraction = new List<VisitorAttractionEntry>();
            asset.NeedFulfillment = new List<NeedFulfillmentEntry>();

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
