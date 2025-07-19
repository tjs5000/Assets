// Assets/AI/TrailGeneration/TrailGoal.cs
// Purpose: Defines inspector-driven parameters for AI-based trail generation

using UnityEngine;
using System.Collections.Generic;
using PlexiPark.Core.SharedEnums;

namespace PlexiPark.AI.TrailGeneration
{
    [CreateAssetMenu(fileName = "NewTrailGoal", menuName = "PlexiPark/AI/Trail Goal")]
    public class TrailGoal : ScriptableObject
    {
        [Header("Generation Scope")]
        [Tooltip("How many trails to attempt to generate total.")]
        public int TargetTrailCount = 5;
        public bool AllowPartialTrails = false;

        [Tooltip("Minimum anchor segments per trail.")]
        public int MinAnchorsPerTrail = 3;

        [Tooltip("Maximum anchor segments per trail.")]
        public int MaxAnchorsPerTrail = 8;
        public int MinCellsPerSegment = 5;
        public int MaxCellsPerSegment = 12;

        [Header("Trail Type Rules")]
        [Tooltip("List of allowed trail types for generation.")]
        public List<TrailType> AllowedTypes = new List<TrailType>
        {
            TrailType.HikingTrail,
            TrailType.MountainTrail,
            TrailType.WalkPath,
            TrailType.BikePath
        };

        [Header("Placement Behavior")]
        [Tooltip("Should trail generation obey park type rules?")]
        public bool EnforceParkTypeRules = false;
        public List<Vector2Int> StartAnchors = new();

        [Tooltip("Optional seed for reproducibility. Leave 0 for random.")]
        public int RandomSeed = 0;
    }
}
