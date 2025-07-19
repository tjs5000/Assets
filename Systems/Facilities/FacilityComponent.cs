using System;
using System.Linq;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using PlexiPark.Data.Facilities;
using PlexiPark.Systems.Visitor;
using PlexiPark.Core.SharedEnums;
using PlexiPark.Managers;
using PlexiPark.Systems.Trail;

namespace PlexiPark.Systems.Facilities
{
    [RequireComponent(typeof(Collider))]
    public class FacilityComponent : MonoBehaviour
    {
        public FacilityData Data;
        public bool IsOnline { get; private set; } = true;
        public IReadOnlyList<NeedType> ServedNeeds => Data.servedNeeds;

        /// <summary>
        /// Fired when a visitor has finished using this facility.
        /// </summary>
        public event Action<VisitorAI, NeedType[]> OnUseComplete;

        private void Awake()
        {
            FacilityManager.Instance.Register(this);
        }

        private void OnDestroy()
        {
            FacilityManager.Instance.Unregister(this);
        }

        /// <summary>
        /// Simulates a visitor using the facility for Data.useDuration seconds,
        /// then fires the OnUseComplete event exactly once.
        /// </summary>
        public IEnumerator Use(VisitorAI visitor)
        {
            // 1) wait the prescribed use-duration
            yield return new WaitForSeconds(Data.useDuration);

            // 2) grab the list of needs this facility serves
            var needs = ServedNeeds.ToArray();

            // 3) fire the completion event
            OnUseComplete?.Invoke(visitor, needs);
        }

        public void SetOnline(bool online)
        {
            if (IsOnline == online) return;
            IsOnline = online;
            // you could fire another event here so visitors can re-route immediately
        }
    }

}