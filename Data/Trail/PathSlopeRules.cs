// Assets/Data/Path/PathSlopeRules.cs

using System.Collections.Generic;
using PlexiPark.Core.SharedEnums;

namespace PlexiPark.Data
{
    public static class PathSlopeRules
    {
        public static readonly Dictionary<TrailType, HashSet<SlopeType>> AllowedSlopes = new()
        {
            { TrailType.WalkPath, new HashSet<SlopeType> { SlopeType.Flat, SlopeType.Gentle, SlopeType.Steep } },
            { TrailType.HikingTrail, new HashSet<SlopeType> { SlopeType.Flat, SlopeType.Gentle, SlopeType.Steep } },
            { TrailType.BikePath, new HashSet<SlopeType> { SlopeType.Flat, SlopeType.Gentle, SlopeType.Steep } },
            { TrailType.MountainTrail, new HashSet<SlopeType> { SlopeType.Flat, SlopeType.Gentle, SlopeType.Steep } },
            { TrailType.ServiceRoad, new HashSet<SlopeType> { SlopeType.Flat, SlopeType.Gentle } }
        };

        public static bool IsSlopeAllowed(TrailType type, SlopeType slope)
        {
            return AllowedSlopes.TryGetValue(type, out var allowed) && allowed.Contains(slope);
        }
    }
}
