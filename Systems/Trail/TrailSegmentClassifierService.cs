using UnityEngine;
using PlexiPark.Core.Interfaces;
using PlexiPark.Core.SharedEnums;
using PlexiPark.Systems.Trail;

namespace PlexiPark.Systems.Trail
{
    public class TrailSegmentClassifierService : MonoBehaviour, ITrailSegmentClassifier
    {
        public (TrailSegmentType, int) GetSegmentInfo(Vector2Int prev, Vector2Int current, Vector2Int next, TrailType type)
        {
            // Wrap the non-nullable input in nullable values
            return TrailSegmentClassifier.GetSegmentInfo(prev, current, next, type);
        }

        public (TrailSegmentType, int) GetSegmentInfoBetween(Vector2Int from, Vector2Int to, TrailType trailType)
        {
            return TrailSegmentClassifier.GetSegmentInfoBetween(from, to, trailType);
        }
    }
}
