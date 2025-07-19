//Assets/AI/TrailGeneration/TrailPlanAI.cs
// Purpose: Stores one trail (path, type, strategy flags)

using UnityEngine;
using PlexiPark.Core.SharedEnums;
using System.Collections.Generic;

namespace PlexiPark.AI.TrailGeneration
{
    public class TrailPlanAI
    {
        public TrailType Type;
        public List<Vector2Int> Path = new();
        public List<Vector2Int> Anchors = new();
    }
}
