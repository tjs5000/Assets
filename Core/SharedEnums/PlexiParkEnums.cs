// Assets/Core/SharedEnums/PlexiParkEnums.cs
using UnityEditor;
using UnityEngine;

namespace PlexiPark.Core.SharedEnums
{
    public enum VisitorType
    {
        Hiker,
        Walker,
        Biker,
        MountainBiker,
        Family,
        NatureEnthusiast,
        Dreamer,
        Socializer
    }

    public enum NeedType
    {
        Hunger,
        Restroom,
        Fun,
        Nature,
        Safety,
        Aesthetic
    }

    public enum ExitType {
        Trailhead,
        ParkGate,
        Emergency }

    public enum ParkType
    {
        Urban,
        Suburban,
        Wilderness
    }

    public enum RewardType
    {
        Money,
        RatingBoost,
        VisitorBoost,
        InspirationPoints,
        UnlockObject,
        TimeAcceleration,
        KarmaBoost,
        DiscoveryChanceBoost
    }

    public enum ParkModifierType
    {
        DonationRateMultiplier,
        MaintenanceCostMultiplier,
        VisitorCapModifier
    }

    public enum UITabType
    {
        Build,
        Manage,
        Habits,
        Finances,
        Achievements
    }
    public enum SlopeType
    {
        Flat = 0,      // 0°
        Gentle = 1,    // 15°
        Moderate = 2,  // 30°
        Steep = 3,     // 60°
        Cliff = 4      // 90°
    }

    public enum TerrainType
    {
        Grass,
        Dirt,
        Rock,
        Water,
        Snow
    }

    public enum TrailType
    {
        None,
        HikingTrail,        // Dirt-based, low maintenance
        HikingTrail2,       // Dirt-based, low maintenance
        MountainTrail,      // Dirt-based, high slope tolerance
        MountainTrail2,     // Dirt-based, high slope tolerance
        WalkPath,           // Paved, medium maintenance
        WalkPath2,          // Paved, medium maintenance
        BikePath,           // Paved, faster travel, higher maintenance
        BikePath2,          // Paved, faster travel, higher maintenance
        DirtRoad,           // Dirt-based rec vehicles
        ServiceRoad,        // Paved, vehicle-only
        PublicRoad,         // Paved, Visitor vehicles
        TrailHead           // Spawn / Despawn points for visitors
    }

    public enum TrailSegmentType
    {
        End,              // One neighbor
        Straight,         // Two aligned neighbors
        Corner,           // Two non-aligned neighbors
        TIntersection,    // Three neighbors
        CrossIntersection // Four neighbors
    }


    public enum VisitorState
    {
        Wander,
        SeekFacility,
        UseFacility,
        Leaving,
        Despawning
    }


    [System.Serializable]
    public class HabitData
    {
        public string HabitID;
        public string Name;
        public string Description;
        public int CurrentStreak;
        public int TotalCompletions;
        public int TotalFailures;
    }


    [System.Serializable]
    public struct VisitorAttractionModifier
    {
        public VisitorType Type;
        public float AttractionScore;
    }

    [System.Serializable]
    public struct NeedFulfillmentModifier
    {
        public NeedType Need;
        public float FulfillmentAmount;
    }
}
