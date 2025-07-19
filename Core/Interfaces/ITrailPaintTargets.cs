// Assets/Core/Interfaces/ITrailPaintTarget.cs

using UnityEngine;
using PlexiPark.Core.SharedEnums;

namespace PlexiPark.Core.Interfaces
{
    public interface ITrailPaintTarget
    {
        void PaintTrailSplat(Vector2Int cell, TrailType type);
        void PaintSegmentMap(Vector2Int cell, TrailSegmentType segmentType, int rotationID);
    }
}