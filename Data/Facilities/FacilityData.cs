using UnityEngine;
using PlexiPark.Core.SharedEnums;
using System.Collections.Generic;
using System.Linq;
using System;

namespace PlexiPark.Data.Facilities
{
    [CreateAssetMenu(fileName = "NewFacility", menuName = "PlexiPark/Facility Data")]
    public class FacilityData : ScriptableObject
    {
        [Header("What needs this facility serves")]
        public NeedType[] servedNeeds;

        [Header("Simultaneous capacity")]
        public int capacity = 1;

        [Header("How long use takes (seconds)")]
        public float minUseDuration = 5f;
        public float maxUseDuration = 10f;
        /// <summary>
        /// Convenience: picks a random use duration between min and max.
        /// </summary>
        public float useDuration => UnityEngine.Random.Range(minUseDuration, maxUseDuration);

        [Header("Visitor Attraction")]
        [Tooltip("How this facility influences nearby visitor satisfaction (–1..+1)")]
        public List<VisitorAttractionEntry> attractionEntries = new();

        private Dictionary<VisitorType, float> _attractionMap;

        void OnEnable()
        {
            // build fast lookup
            _attractionMap = attractionEntries?
            .ToDictionary(e => e.type, e => e.value) ?? new Dictionary<VisitorType, float>();
        }

        /// <summary>
        /// Returns how much this facility bumps satisfaction
        /// for a visitor of the given type when nearby.
        /// </summary>
        public float GetAttraction(VisitorType visitorType)
            => _attractionMap.TryGetValue(visitorType, out var v) ? v : 0f;
    }

    /// <summary>
    /// Pairing of visitor type → attraction weight for facilities.
    /// </summary>
    [Serializable]
    public class VisitorAttractionEntry
    {
        public VisitorType type;
        [Range(-1f, 1f)]
        public float value;
    }
}
