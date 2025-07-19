// Assets/Data/Visitors/ObjectCompatibilityData.cs
using System.Collections.Generic;
using UnityEngine;
using PlexiPark.Core.Interfaces;
using PlexiPark.Core.SharedEnums;

[CreateAssetMenu(fileName = "ObjectCompatibilityData", menuName = "PlexiPark/Visitor/Object Compatibility Data")]
public class ObjectCompatibilityData : ScriptableObject, ICompatibilitySource<GameObject>
{
    [Tooltip("Which prefab this data applies to")]
    public GameObject prefab;

    [System.Serializable]
    public struct Entry
    {
        public VisitorType type;
        public float score;
    }

    [Tooltip("Compatibility scores for each visitor type")]
    public List<Entry> compatibility = new();

    Dictionary<VisitorType,float> _map;

    void OnEnable()
    {
        _map = new Dictionary<VisitorType, float>();
        foreach (var e in compatibility)
            _map[e.type] = e.score;
    }

    public float GetCompatibility(GameObject subject, VisitorType vType)
    {
        // subject is the object instance; data is same per prefab
        return _map.TryGetValue(vType, out var s) ? s : 0f;
    }
}
