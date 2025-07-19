// Terrain/Generation/TerrainGenerationConfig.cs
// ----------------------------------------------
// Holds adjustable parameters for hybrid terrain generation
// ----------------------------------------------

using UnityEngine;
using PlexiPark.Data;
using PlexiPark.Core.SharedEnums;

namespace PlexiPark.Terrain.Generation
{
    [CreateAssetMenu(fileName = "TerrainGenerationConfig", menuName = "PlexiPark/Terrain Generation Config")]
    public class TerrainGenerationConfig : ScriptableObject
    {
        [Header("General Settings")]
        public ParkType parkType;
        public float cellSize = 1f;
        public Vector3 origin = Vector3.zero;

        [Header("Height Noise Settings")]
        public float perlinScale = 0.05f;
        public float heightMultiplier = 30f;
        public float elevationSnapStep = 0.5f;

        [Header("Slope Ratio Targets")]
        [Range(0f, 1f)] public float flatRatio = 0.5f;
        [Range(0f, 1f)] public float gentleRatio = 0.25f;
        [Range(0f, 1f)] public float steepRatio = 0.15f;
        [Range(0f, 1f)] public float cliffRatio = 0.1f;

        [Header("Blend Settings")]
        [Range(0f, 1f)] public float regionBias = 0.25f;

        [Header("Cliff Disruption Settings")]
[Range(0f, 1f)] public float cliffBreakFrequency = 0.25f;
    }
}
