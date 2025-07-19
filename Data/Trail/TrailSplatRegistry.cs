// TrailSplatRegistry.cs
// Central registry that maps TrailType to splat map and RGBA channel

using System.Collections.Generic;
using PlexiPark.Core.SharedEnums;

namespace PlexiPark.Data.Trail
{
    public enum SplatMapIndex
    {
        Map1, // Walkway/Biking
        Map2, // Hiking/Mountain
        Map3  // ServiceRoad/Future
    }

    public struct SplatTarget
    {
        public SplatMapIndex map;
        public int channelIndex; // 0 = R, 1 = G, 2 = B, 3 = A

        public SplatTarget(SplatMapIndex map, int channelIndex)
        {
            this.map = map;
            this.channelIndex = channelIndex;
        }
    }

    public static class TrailSplatRegistry
    {
        private static readonly Dictionary<TrailType, SplatTarget> mapping = new()
        {
            { TrailType.WalkPath,     new SplatTarget(SplatMapIndex.Map1, 0) }, // R
            { TrailType.WalkPath2,    new SplatTarget(SplatMapIndex.Map1, 1) }, // G
            { TrailType.BikePath,      new SplatTarget(SplatMapIndex.Map1, 2) }, // B
            { TrailType.BikePath2,     new SplatTarget(SplatMapIndex.Map1, 3) }, // A

            { TrailType.HikingTrail,      new SplatTarget(SplatMapIndex.Map2, 0) },
            { TrailType.HikingTrail2,     new SplatTarget(SplatMapIndex.Map2, 1) },
            { TrailType.MountainTrail,    new SplatTarget(SplatMapIndex.Map2, 2) },
            { TrailType.MountainTrail2,   new SplatTarget(SplatMapIndex.Map2, 3) },

            { TrailType.DirtRoad,  new SplatTarget(SplatMapIndex.Map3, 0) },
            { TrailType.ServiceRoad, new SplatTarget(SplatMapIndex.Map3, 1) },
            { TrailType.PublicRoad, new SplatTarget(SplatMapIndex.Map3, 2) },
            { TrailType.TrailHead, new SplatTarget(SplatMapIndex.Map3, 3) },
        };

        public static SplatTarget GetSplatTarget(TrailType type)
        {
            if (mapping.TryGetValue(type, out var target))
                return target;
            throw new System.Exception($"TrailType {type} has no splat mapping.");
        }
    }
}
