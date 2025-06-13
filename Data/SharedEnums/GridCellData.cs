// Data/SharedEnums/GridCellData.cs

using UnityEngine;

namespace PlexiPark.Data
{
    [System.Serializable]
    public struct GridCellData
    {
        public bool isOccupied;
        public float elevation;       // World Y height
        public SlopeType slope;
        public TerrainType terrainType;
    }
}
