// Assets/Systems/Visitor/VisitorMetrics.cs
using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using PlexiPark.Core.SharedEnums;

namespace PlexiPark.Systems.Visitor
{
    public enum MetricType
    {
        Needs,
        SlopeDelta,
        Happiness,
        Annoyance,
        OverallSatisfaction
    }

    /// <summary>
    /// Container for all sub-metrics of a visitor’s mood,
    /// recalculates its own overall satisfaction when any sub-metric changes.
    /// </summary>
    public class VisitorMetrics
    {
        // storage for each metric
        private readonly Dictionary<MetricType, float> _metrics =
            Enum.GetValues(typeof(MetricType))
                .Cast<MetricType>()
                .ToDictionary(m => m, _ => 0f);

        public IReadOnlyDictionary<MetricType, float> All => _metrics;

        public float BaseNeeds =>
            _metrics[MetricType.Needs];

        public float SlopeDelta =>
            _metrics[MetricType.SlopeDelta];

        public float Happiness =>
            _metrics[MetricType.Happiness];

        public float Annoyance =>
            _metrics[MetricType.Annoyance];

        public float OverallSatisfaction =>
            _metrics[MetricType.OverallSatisfaction];

        /// <summary>
        /// Set one of the sub-metrics. Automatically re-computes OverallSatisfaction.
        /// </summary>
        public void SetMetric(MetricType type, float value)
        {
            _metrics[type] = value;
            // whenever any component changes, recalc overall
            RecalculateOverall();
        }

        /// <summary>
        /// Whenever you manually satisfy a need, call this to bump
        /// the “Needs” metric (so your UI can update immediately).
        /// </summary>
        public void RecordNeedChange(NeedType needType, float newValue)
        {
            // For now, just treat the single-need reset as your new "Needs" score:
            _metrics[MetricType.Needs] = newValue;
            RecalculateOverall();
        }

        /// <summary>
        /// Example combine function; tweak weights or formula as you like.
        /// </summary>
        public void RecalculateOverall()
        {
            float baseNeeds = BaseNeeds;
            float slopeDelta = SlopeDelta;
            float happy = Happiness;
            float annoy = Annoyance;

            // simple blend: overall = needs + slope + 0.1*(happy – annoy)
            float o = baseNeeds
                    + slopeDelta
                    + 0.1f * (happy - annoy);

            _metrics[MetricType.OverallSatisfaction]
                = Mathf.Clamp01(o);
        }
    }
}
