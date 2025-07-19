using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using PlexiPark.Core.SharedEnums;
using PlexiPark.Systems.Simulation;
using PlexiPark.Data;
using PlexiPark.Systems.Facilities;

namespace PlexiPark.Systems.Visitor
{
    [RequireComponent(typeof(VisitorAI))]
    public class NeedManager : MonoBehaviour
    {
        [Tooltip("List your NeedData assets here, one per NeedType")]
        public List<NeedData> needDefinitions;

        // backing store
        private Dictionary<NeedType, float> _needs = new Dictionary<NeedType, float>();

        /// <summary>
        /// Read-only view from the outside.
        /// </summary>
        public IReadOnlyDictionary<NeedType, float> Needs => _needs;

        /// <summary>Fired whenever a need is manually satisfied.</summary>
        public event Action<NeedType, float> OnNeedChanged;

        void OnEnable()
        {
            SimulationTicker.OnTick += OnSimulationTick;
            InitializeNeeds();
        }

        void OnDisable()
        {
            SimulationTicker.OnTick -= OnSimulationTick;
        }

        private void InitializeNeeds()
        {
            _needs.Clear();
            foreach (var def in needDefinitions)
                _needs[def.needType] = 1f;
        }

        public bool GetMostUrgentNeed(out NeedType need)
        {
            if (_needs.Count == 0)
            {
                need = default;
                return false;
            }
            // smallest value => most urgent
            var kvp = _needs.OrderBy(k => k.Value).First();
            need = kvp.Key;
            return true;
        }

        /// <summary>
        /// Instantly satisfy (reset) a single need and fire the change event.
        /// </summary>
        public void SatisfyNeed(NeedType needType)
        {
            const float full = 1f;
            // 1) update the raw data
            _needs[needType] = full;

            // 2) fire your local event
            OnNeedChanged?.Invoke(needType, full);

            // 3) push it into the visitor’s metrics
            var mood = GetComponent<VisitorMoodManager>();
            if (mood != null)
                mood.Metrics.RecordNeedChange(needType, full);
        }

        private void OnSimulationTick(float dt)
        {
            foreach (var def in needDefinitions)
            {
                float current = _needs[def.needType];
                current = Mathf.Max(0f, current - def.decayRate * dt);
                _needs[def.needType] = current;
            }
        }
    }
}
