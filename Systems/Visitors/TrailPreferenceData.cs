using System.Collections.Generic;
using UnityEngine;
using PlexiPark.Core.SharedEnums;

[CreateAssetMenu(menuName = "AI/Trail Preference Data")]
public class TrailPreferenceData : ScriptableObject
{
    [Tooltip("Which VisitorType this data drives")]
    public VisitorType visitorType;

    [System.Serializable]
    public struct Entry
    {
        public TrailType trailType;
        public float weight;
    }

    [Tooltip("Weights for each TrailType. 0 to 5. Higher=stronger preference.Lower=less likely to be chosen. 0=never take that kind of trail.")]
    public List<Entry> preferences = new List<Entry>();

    // Fast lookup
    private Dictionary<TrailType,float> _map;

    private void OnEnable()
    {
        _map = new Dictionary<TrailType, float>();
        foreach (var e in preferences)
            _map[e.trailType] = e.weight;
    }

    /// <summary>Get the configured weight for this trail, or 0 if none.</summary>
    public float GetWeight(TrailType t)
        => _map != null && _map.TryGetValue(t, out var w) ? w : 0f;
}
