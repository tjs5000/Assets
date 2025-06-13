// Assets/Data/SharedEnums/PlexiParkEnums.cs
namespace PlexiPark.Data
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

    public enum PathType
    {
        HikingTrail,
        MountainTrail,
        WalkingPath,
        BikeTrail,
        ServiceRoad
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
