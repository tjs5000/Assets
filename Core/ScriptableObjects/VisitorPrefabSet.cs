// Assets/Systems/Visitor/Data/VisitorPrefabSet.cs
using System.Collections.Generic;
using UnityEngine;
using PlexiPark.Core.SharedEnums;

[CreateAssetMenu(menuName = "PlexiPark/Visitors/Prefab Set")]
public class VisitorPrefabSet : ScriptableObject
{
    public VisitorType type;                 // Walker, Hiker, …
    public List<GameObject> variations;      // ≥ 1
}
