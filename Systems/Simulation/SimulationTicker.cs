using System;
using UnityEngine;

namespace PlexiPark.Systems.Simulation
{
    /// <summary>
    /// Fires a single OnTick(deltaTime) event once per second.
    /// All time‐based systems subscribe here to decouple from Update().
    /// </summary>
    public class SimulationTicker : MonoBehaviour
    {
        public static event Action<float> OnTick;
        [Header("Tick Duration (sec.)")]
        public float secs = 5f;
        void Start()
        {
            // every 1s, invoke OnTick(1f)
            InvokeRepeating(nameof(Tick), secs, secs);
        }

        void Tick()
        {
            OnTick?.Invoke(1f);
        }

        void OnDisable()
        {
            CancelInvoke();
        }
    }
}
