// Assets/Core/SharedEnums/GridCellData.cs

using UnityEngine;

namespace PlexiPark.Core.SharedEnums
{
    [System.Serializable]
    public struct GridCellData
    {
        public bool isOccupied;
        public float elevation;       // World Y height
        public SlopeType slope;
        public TerrainType terrainType;
        public TrailType Trail;
        public TrailSegmentType SegmentType;
    }
}
