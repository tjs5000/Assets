//Assets/Core/Interfaces/ITrailSegmentClassifier.cs

using UnityEngine;
using PlexiPark.Core.SharedEnums;

namespace PlexiPark.Core.Interfaces
{
    public interface ITrailSegmentClassifier
{
    (TrailSegmentType, int) GetSegmentInfo(Vector2Int prev, Vector2Int current, Vector2Int next, TrailType type);
    (TrailSegmentType, int) GetSegmentInfoBetween(Vector2Int from, Vector2Int to, TrailType trailType);
}
}
