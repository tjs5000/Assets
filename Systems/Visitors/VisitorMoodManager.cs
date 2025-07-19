// Assets/Systems/Visitor/VisitorMoodManager.cs
using System;
using System.Linq;
using UnityEngine;
using PlexiPark.Core.SharedEnums;
using PlexiPark.Systems.Simulation;
using PlexiPark.Managers;
using PlexiPark.Systems.Facilities;

namespace PlexiPark.Systems.Visitor
{
    [RequireComponent(typeof(NeedManager), typeof(VisitorAI))]
    public class VisitorMoodManager : MonoBehaviour
    {
        public VisitorMetrics Metrics { get; } = new VisitorMetrics();
        public event Action<VisitorMetrics> OnMetricsUpdated;

        [Tooltip("Scale of slope influence per tick (e.g. 0.02)")]
        [SerializeField] private float slopeModifierFactor = 0.02f;
        [Tooltip("How strongly density multiplies positive/negative proximity influence")]
        [SerializeField] private float densityFactor = 0.2f;

        private NeedManager _needs;
        private VisitorAI _visitor;
        private IGridInfoProvider _grid;

        void Awake()
        {
            _needs = GetComponent<NeedManager>();
            _visitor = GetComponent<VisitorAI>();
            _grid = GridManager.Instance
                       ?? throw new Exception("VisitorMoodManager: missing GridManager");

            // Seed the overall satisfaction metric
            Metrics.SetMetric(MetricType.OverallSatisfaction, SampleInitialSatisfaction());
        }

        void OnEnable()
        {
            SimulationTicker.OnTick += EvaluateMood;
        }

        void OnDisable()
        {
            SimulationTicker.OnTick -= EvaluateMood;
        }

        private void EvaluateMood(float dt)
        {
            try
            {
                // 1) Quick sanity check on your core references
                if (_visitor == null)
                {
                    Debug.LogWarning($"[VisitorMood] {_visitor} was destroyed—unsubscribing.");
                    SimulationTicker.OnTick -= EvaluateMood;
                    return;
                }
                if (_needs == null)
                {
                    Debug.LogError($"[VisitorMood] NeedManager missing on {name}! Unsubscribing.");
                    SimulationTicker.OnTick -= EvaluateMood;
                    return;
                }
                if (_grid == null)
                {
                    Debug.LogError($"[VisitorMood] GridManager.Instance missing! Unsubscribing.");
                    SimulationTicker.OnTick -= EvaluateMood;
                    return;
                }
                if (VisitorRegistry.Instance == null)
                {
                    Debug.LogError($"[VisitorMood] VisitorRegistry.Instance is null!");
                    return;
                }
                if (VisitorObjectManager.Instance == null)
                {
                    Debug.LogError($"[VisitorMood] VisitorObjectManager.Instance is null!");
                    return;
                }
                if (FacilityManager.Instance == null)
                {
                    Debug.LogError($"[VisitorMood] FacilityManager.Instance is null!");
                    return;
                }
                if (CompatibilityManager.Instance == null)
                {
                    Debug.LogError($"[VisitorMood] CompatibilityManager.Instance is null!");
                    return;
                }

                // 2) Compute our grid cell
                var cell = _visitor.CurrentCell;

                // 3) Needs metric
                var needValues = _needs.Needs?.Values;
                float needsScore = (needValues != null && needValues.Any())
                    ? needValues.Average()
                    : 1f;
                Metrics.SetMetric(MetricType.Needs, needsScore);

                // 4) Slope delta metric
                SlopeType sc = _grid.GetSlopeType(cell);
                SlopeType sn = _grid.GetSlopeType(_visitor.NextCell);
                float slopeDelta = (SlopePref(sn) - SlopePref(sc)) * slopeModifierFactor;
                Metrics.SetMetric(MetricType.SlopeDelta, slopeDelta);

                // 5) Proximity loops
                float pos = 0f, neg = 0f;

                // a) other visitors
                var visitors = VisitorRegistry.Instance
                                .GetVisitorsInNeighborhood(cell, 1)
                                ?.Where(v => v != _visitor)
                                ?? Enumerable.Empty<VisitorAI>();
                foreach (var other in visitors)
                {
                    float c = CompatibilityManager.Instance
                                    .GetVisitorCompatibility(other.VisitorType, _visitor.VisitorType);
                    if (c > 0) pos += c; else neg += -c;
                }

                // b) placeable objects
                var objects = VisitorObjectManager.Instance
                                .GetNearbyObjects(cell, 1)
                                ?? Enumerable.Empty<GameObject>();
                foreach (var go in objects)
                {
                    var comp = go.GetComponent<ObjectCompatibilityComponent>()?.compatibilityData;
                    if (comp == null) continue;
                    float c = comp.GetCompatibility(go, _visitor.VisitorType);
                    if (c > 0) pos += c; else neg += -c;
                }

                // c) facilities
                var facs = FacilityManager.Instance
                                .GetNearbyFacilities(cell, 1)
                                ?? Enumerable.Empty<FacilityComponent>();
                foreach (var facComp in facs)
                {
                    if (facComp?.Data == null) continue;
                    float a = facComp.Data.GetAttraction(_visitor.VisitorType);
                    if (a > 0) pos += a; else neg += -a;
                }

                // 6) density‐scale and write back
                int count = visitors.Count()
                          + objects.Count()
                          + facs.Count();
                float scale = Mathf.Exp(densityFactor * count);

                Metrics.SetMetric(MetricType.Happiness, pos * scale);
                Metrics.SetMetric(MetricType.Annoyance, neg * scale);

                // 7) final overall and fire event
                Metrics.RecalculateOverall();
                OnMetricsUpdated?.Invoke(Metrics);
            }
            catch (Exception ex)
            {
                Debug.LogError($"[VisitorMood] EvaluateMood threw: {ex}");
            }
        }


        private float SlopePref(SlopeType s) => s switch
        {
            SlopeType.Flat => +1f,
            SlopeType.Gentle => +0.5f,
            SlopeType.Steep => 0f,
            SlopeType.Cliff => -0.5f,
            _ => 0f
        };

        private static float SampleInitialSatisfaction()
        {
            const float mean = 0.8f, sigma = 0.074f;
            float u1 = UnityEngine.Random.value;
            float u2 = UnityEngine.Random.value;
            float z0 = Mathf.Sqrt(-2f * Mathf.Log(u1))
                     * Mathf.Cos(2f * Mathf.PI * u2);
            return Mathf.Clamp(mean + z0 * sigma, 0.6f, 1f);
        }
    }
}
