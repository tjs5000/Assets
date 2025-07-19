// Data/ParkObjects/ParkObjectData.cs
using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using PlexiPark.Core.SharedEnums;

namespace PlexiPark.Data
{
    [CreateAssetMenu(fileName = "NewParkObject", menuName = "PlexiPark/Park Object")]
    public class ParkObjectData : ScriptableObject
    {
        // ----------------------------------------------------------------------
        // Core Identification
        // ----------------------------------------------------------------------
        [Header("Identification")]
        [Tooltip("Unique string ID for this object (used in save data and placement).")]
        public string ObjectID;

        [Tooltip("Display name shown in UI.")]
        public string DisplayName;

        [Tooltip("Prefab to instantiate for ghost and final placement.")]
        // public GameObject Prefab;
        public GameObject finalPrefab;
        public GameObject previewPrefab;

        // ----------------------------------------------------------------------
        // Categorization
        // ----------------------------------------------------------------------


        [Header("Classification")]
        public ParkObjectCategory Category;


        // ----------------------------------------------------------------------
        // Placement Constraints
        // ----------------------------------------------------------------------
        [Header("Placement Constraints")]
        [Tooltip("List of slope types this object can be placed on.")]
        public List<SlopeType> AllowedSlopes = new() { SlopeType.Flat };

        [Tooltip("Footprint relative to origin (Vector2Int.zero is base cell).")]
        public Vector2Int[] Footprint = new Vector2Int[] { Vector2Int.zero };

        [Header("Footprint Generation")]
        [Tooltip("Optional footprint preset pattern.")]
        public FootprintPreset preset = FootprintPreset.Custom;

        [Tooltip("If true, object cannot be removed after placement.")]
        public bool IsPermanent = false;

        [Tooltip("If true, terrain type compatibility will be enforced.")]
        public bool RequiresValidTerrain = false;



        [Header("ParkType Overrides")]
        public List<ParkTypeOverrideEntry> ParkTypeOverrides = new();


        [Serializable]
        public class ParkTypeOverrideEntry
        {
            public ParkType ParkType;

            [Tooltip("Multiply the base maintenance by this (e.g. 1.5× in Wilderness)")]
            public float MaintenanceMultiplier = 1f;

            [Tooltip("Multiply the base rating impact by this (e.g. 0.8× in Wilderness)")]
            public float RatingImpactMultiplier = 1f;

            [Tooltip("Extra attraction/punishment per visitor type for this park type")]
            public List<VisitorAttractionEntry> VisitorAttractionDelta = new();

            // (Optionally add NeedFulfillmentDelta if you want per-park need tweaks)
        }


        // ----------------------------------------------------------------------
        // Upgrade Tiering
        // ----------------------------------------------------------------------
        [Header("Tier Settings")]
        [Tooltip("Maximum upgrade tier supported (1 = no upgrade).")]
        [Range(1, 3)]
        public int MaxUpgradeTier = 1;

        [Tooltip("Optional name overrides per upgrade tier.")]
        public List<string> TierNames = new();

        [Tooltip("Optional maintenance cost per upgrade tier.")]
        public List<int> MaintenancePerTier = new();

        // ----------------------------------------------------------------------
        // Financial Attributes
        // ----------------------------------------------------------------------
        [Header("Financials")]
        [Tooltip("Placement cost.")]
        public int Cost = 100;

        [Tooltip("Monthly maintenance cost.")]
        public int MaintenanceCostPerMonth = 10;

        [Tooltip("Commercial profit per month (if applicable).")]
        public int ProfitPerMonth = 0;

        [Tooltip("Reputation penalty if object is considered 'commercial'.")]
        public float RatingPenalty = 0.0f;

        // ----------------------------------------------------------------------
        // Gameplay Impact
        // ----------------------------------------------------------------------
        [Header("Impact on Park Rating")]
        [Tooltip("Base impact on overall park rating when placed.")]
        public float BaseRatingImpact = 0.0f;

        [Header("Visitor Interaction")]
        [Tooltip("How this object attracts or repels specific visitor types.")]
        public List<VisitorAttractionEntry> VisitorAttraction = new();

        [Tooltip("How much this object fulfills visitor needs.")]
        public List<NeedFulfillmentEntry> NeedFulfillment = new();

        // ----------------------------------------------------------------------
        // Lifecycle Hooks
        // ----------------------------------------------------------------------


        // ──────────────────────────────────────────────────────────────────
        // Runtime lookup maps (not serialized)
        private Dictionary<VisitorType, float> _visitorAttractionMap;
        private Dictionary<NeedType, float> _needFulfillmentMap;

        void OnEnable()
        {

            // Safeguard against null lists
            var va = VisitorAttraction ?? new List<VisitorAttractionEntry>();
            var nf = NeedFulfillment ?? new List<NeedFulfillmentEntry>();

            _visitorAttractionMap = va.ToDictionary(e => e.type, e => e.value);
            _needFulfillmentMap = nf.ToDictionary(e => e.type, e => e.value);

            if (Footprint == null || Footprint.Length == 0)
            {
                Footprint = new Vector2Int[] { Vector2Int.zero };
            }
        }

        void OnValidate()
        {
            if (preset != FootprintPreset.Custom)
            {
                Footprint = GenerateFootprint(preset);
                Debug.Log($"📐 Generated '{preset}' footprint with {Footprint.Length} cells for {DisplayName}.");
            }
            else if (Footprint == null || Footprint.Length == 0)
            {
                Debug.LogWarning($"🛠️ Footprint was empty — assigning default 1x1 for {DisplayName}.");
                Footprint = new Vector2Int[] { Vector2Int.zero };
            }
            if (AllowedSlopes == null || AllowedSlopes.Count == 0)
            {
                AllowedSlopes = new List<SlopeType> { SlopeType.Flat };
                Debug.LogWarning($"⚠️ Auto-assigning Flat slope for {DisplayName}");
            }
            if (!Application.isPlaying)
                OnEnable();
        }

        private Vector2Int[] GenerateFootprint(FootprintPreset preset)
        {
            switch (preset)
            {
                case FootprintPreset.OneByOne:
                    return new Vector2Int[] { Vector2Int.zero };

                case FootprintPreset.TwoByOne:
                    return new Vector2Int[] { Vector2Int.zero, new Vector2Int(1, 0) };

                case FootprintPreset.TwoByTwo:
                    return new Vector2Int[]
                    {
                        new Vector2Int(0, 0), new Vector2Int(1, 0),
                        new Vector2Int(0, 1), new Vector2Int(1, 1)
                    };

                case FootprintPreset.ThreeByThree:
                    List<Vector2Int> threeByThree = new();
                    for (int x = 0; x < 3; x++)
                        for (int y = 0; y < 3; y++)
                            threeByThree.Add(new Vector2Int(x, y));
                    return threeByThree.ToArray();

                case FootprintPreset.LShape:
                    return new Vector2Int[]
                    {
                        new Vector2Int(0, 0),
                        new Vector2Int(1, 0),
                        new Vector2Int(2, 0),
                        new Vector2Int(2, 1)
                    };

                case FootprintPreset.Line3x1:
                    return new Vector2Int[]
                    {
                        new Vector2Int(0, 0),
                        new Vector2Int(1, 0),
                        new Vector2Int(2, 0)
                    };

                default:
                    return new Vector2Int[] { Vector2Int.zero };
            }
        }


        public float GetMaintenanceCost(ParkType parkType)
        {
            var entry = ParkTypeOverrides.FirstOrDefault(o => o.ParkType == parkType);
            return Mathf.CeilToInt(MaintenanceCostPerMonth
                   * (entry?.MaintenanceMultiplier ?? 1f));
        }

        public float GetRatingImpact(ParkType parkType)
        {
            var entry = ParkTypeOverrides.FirstOrDefault(o => o.ParkType == parkType);
            return BaseRatingImpact
                   * (entry?.RatingImpactMultiplier ?? 1f);
        }

        public float GetAttraction(VisitorType visitorType, ParkType parkType)
        {
            // base value
            float baseVal = GetAttraction(visitorType);

            // delta from override (if any)
            var entry = ParkTypeOverrides.FirstOrDefault(o => o.ParkType == parkType);
            if (entry != null)
            {
                var delta = entry.VisitorAttractionDelta
                             .FirstOrDefault(d => d.type == visitorType);
                baseVal += (delta != null ? delta.value : 0f);
            }
            return baseVal;
        }


        /// Returns the attraction weight for this visitor type.
        public float GetAttraction(VisitorType type)
            => _visitorAttractionMap.TryGetValue(type, out var v) ? v : 0f;

        /// Returns how much this object fulfills the given need.
        public float GetNeedFulfillment(NeedType type)
              => _needFulfillmentMap.TryGetValue(type, out var v) ? v : 0f;

    }

    public enum FootprintPreset
    {
        Custom,
        OneByOne,
        TwoByOne,
        TwoByTwo,
        ThreeByThree,
        LShape,
        Line3x1
    }

    public enum ParkObjectCategory
    {
        Facility,
        Path,
        Natural,
        Attraction,
        Amenity,     // next-to-path objects like benches, trash cans, lamp posts
        Waterway
    }

    [System.Serializable]
    public class VisitorAttractionEntry
    {
        public VisitorType type;
        [Range(-1f, 1f)]
        public float value;
    }

    [System.Serializable]
    public class NeedFulfillmentEntry
    {
        public NeedType type;
        [Range(0f, 1f)]
        public float value;
    }
}
