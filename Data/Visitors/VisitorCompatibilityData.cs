using System.Collections.Generic;
using UnityEngine;
using PlexiPark.Core.SharedEnums;
using PlexiPark.Core.Interfaces;

[CreateAssetMenu(fileName = "VisitorCompatibilityData", menuName = "PlexiPark/Visitor/Visitor Compatibility Data")]
public class VisitorCompatibilityData : ScriptableObject, ICompatibilitySource<VisitorType>
{
    [System.Serializable]
    public struct Entry
    {
        public VisitorType type;
        public float score;
    }

    [Tooltip("How each visitor type affects each other")]
    public List<Entry> compatibility = new();

    // fast lookup at runtime
    Dictionary<VisitorType,float> _map;

    void OnEnable()
    {
        _map = new Dictionary<VisitorType, float>();
        foreach (var e in compatibility)
            _map[e.type] = e.score;
    }

    public float GetCompatibility(VisitorType subject, VisitorType vType)
    {
        // subject = other visitor’s type
        // vType   = this visitor’s own type
        return _map.TryGetValue(subject, out var s) ? s : 0f;
    }
}
