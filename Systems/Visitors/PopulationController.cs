using System.Collections.Generic;
using UnityEngine;
using System.Linq;
using PlexiPark.Core.SharedEnums;
using PlexiPark.Managers;     // GridManager
using PlexiPark.Core.Pooling;


namespace PlexiPark.Systems.Visitor
{
    /// <summary>
    /// Keeps a per-VisitorType “ticket” budget so the park never exceeds its
    /// TotalCap (from ParkPopulationConfig).  VisitorManager checks out tickets
    /// when spawning; VisitorAI returns them on despawn.
    /// </summary>
    [DefaultExecutionOrder(50)]   // initialise before VisitorManager (2000)
    public class PopulationController : MonoBehaviour
    {
        public static PopulationController Instance { get; private set; }
        // public int TotalCap => _config.visitorCap;
        readonly Dictionary<VisitorType, int> _live = new();
        readonly Dictionary<VisitorType, int> _tickets = new();
        [SerializeField] private ParkPopulationConfig populationConfig;

        /* ─────────────────────── Bootstrap ─────────────────────── */
        void Awake()
        {
            if (Instance != null && Instance != this) { Destroy(gameObject); return; }
            Instance = this; DontDestroyOnLoad(gameObject);

            // seed with zero counts for every enum value
            foreach (VisitorType t in System.Enum.GetValues(typeof(VisitorType)))
            {
                _live[t] = 0; _tickets[t] = 0;
            }

            RecalculateTickets();     // first pass
        }

        /* ─────────────────────── Public API ─────────────────────── */
        public bool RequestTicket(VisitorType type)
        {
            if (_live[type] >= _tickets[type]) return false;
            _live[type]++; return true;
        }

        public void ReturnTicket(VisitorType type)
        {
            _live[type] = Mathf.Max(0, _live[type] - 1);
        }

        public int LiveVisitors() => _live.Values.Sum();
        public int LiveVisitors(VisitorType vt) => _live[vt];

        /* ─────────────────── Ticket Re-balancing ────────────────── */
        public void RecalculateTickets()
        {

            if (populationConfig == null) { Debug.LogWarning("PopulationController: No ParkPopulationConfig assigned"); return; }
            
            // sample trail lengths once all trails exist
            int walk = 0;
            int hike = 0;
            int bike = 0;
            int mBike = 0;

            // walkers
            walk += GridManager.Instance.GetTrailCellCount(TrailType.WalkPath);
            walk += GridManager.Instance.GetTrailCellCount(TrailType.WalkPath2);

            // hikers
            hike += GridManager.Instance.GetTrailCellCount(TrailType.HikingTrail);
            hike += GridManager.Instance.GetTrailCellCount(TrailType.HikingTrail2);

            // cyclists
            bike += GridManager.Instance.GetTrailCellCount(TrailType.BikePath);
            bike += GridManager.Instance.GetTrailCellCount(TrailType.BikePath2);

            // mountain bikers
            mBike += GridManager.Instance.GetTrailCellCount(TrailType.MountainTrail);
            mBike += GridManager.Instance.GetTrailCellCount(TrailType.MountainTrail2);

            float sum = walk + hike + bike + mBike;
            if (sum == 0) sum = 1;   // avoid div-by-zero

            int cap = populationConfig.visitorCap;   // overall cap
            _tickets[VisitorType.Walker] = Mathf.RoundToInt(cap * walk / sum);
            _tickets[VisitorType.Hiker] = Mathf.RoundToInt(cap * hike / sum);
            _tickets[VisitorType.Biker] = Mathf.RoundToInt(cap * bike / sum);
            _tickets[VisitorType.MountainBiker] = Mathf.RoundToInt(cap * mBike / sum);
        }
    }
}