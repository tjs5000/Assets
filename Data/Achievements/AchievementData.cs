using System;

[Serializable]
public class AchievementData
{
    /// <summary> Unique ID for this achievement </summary>
    public string AchievementID;

    /// <summary> Short title shown in-game </summary>
    public string Title;

    /// <summary> Description of what the player must do </summary>
    public string Description;

    /// <summary> Category of milestone (e.g. BuildObjectCount, ReachVisitorCount) </summary>
    public AchievementType Type;

    /// <summary> Numeric goal to reach before unlocking </summary>
    public float TargetValue;

    /// <summary> What kind of reward to give (e.g. Money, RatingBoost, VisitorBoost) </summary>
    public RewardType RewardType;

    /// <summary> Amount of the reward (e.g. cash amount, boost magnitude) </summary>
    public float RewardValue;

    /// <summary> True once the achievement has been earned </summary>
    public bool IsUnlocked;

    /// <summary> Tracks incremental progress toward TargetValue </summary>
    public float CurrentProgress;
    
    // TODO: Add methods for reporting progress and triggering unlock events.
}
