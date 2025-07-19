// Terrain/Data/SlopeRatioPreset.cs
using UnityEngine;

namespace PlexiPark.Terrain.Data
{
    [System.Serializable]
    public class SlopeRatioPreset
    {
        [Range(0f, 1f)] public float Flat = 0.5f;
        [Range(0f, 1f)] public float Gentle = 0.25f;
        [Range(0f, 1f)] public float Steep = 0.15f;
        [Range(0f, 1f)] public float Cliff = 0.1f;
    }
}
