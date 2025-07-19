// Assets/Systems/Visitor/VisitorAI.cs
using System.Collections.Generic;
using UnityEngine;
using System.Collections;
using UnityEngine.AI;
using PlexiPark.Core.SharedEnums;
using PlexiPark.Core.Utils;
using PlexiPark.Managers;
using PlexiPark.Systems.Simulation;
using PlexiPark.Systems.Facilities;
using PlexiPark.Core.Pooling;

namespace PlexiPark.Systems.Visitor
{
    [RequireComponent(typeof(NavMeshAgent), typeof(NeedManager))]
    public class VisitorAI : MonoBehaviour
    {
        [Header("Patrol Settings")]
        [Tooltip("Seconds after spawn before this visitor will despawn once it returns to the trailhead.")]
        [SerializeField] private float maxLifetimeAtTrailhead = 60f;

        [Header("Leg Length (cells)")]
        [Tooltip("Minimum cells to advance on each hop")]
        [SerializeField] private int minLegLength = 1;
        [Tooltip("Maximum cells to advance on each hop")]
        [SerializeField] private int maxLegLength = 3;

        [Header("Visitor Settings")]
        [Tooltip("What kind of visitor is this (Hiker, Walker, etc.)")]
        [SerializeField] private VisitorType visitorType;
        public VisitorType VisitorType => visitorType;

        [Tooltip("Assign the TrailPreferenceData for this visitor type")]
        public TrailPreferenceData preferenceData;
        public static ExitManager Instance { get; private set; }
        [SerializeField][Range(0f, 1f)] private float leaveThreshold = 0.25f;

        public static event System.Action OnVisitorSpawned;
        public static event System.Action OnVisitorDespawned;

        [HideInInspector] public VisitorAI Leader;   // null = this is the family leader

        private BranchingNavigator _navigator;
        private BTNode _root;
        private NavMeshAgent _agent;
        private VisitorAI _vm;
        private NeedManager _needs;
        private System.Action<float> _tickHandler;

        private FacilityComponent _targetFacility;
        private Coroutine _useCoroutine;
        private VisitorState state;
        private List<Vector3> _waypoints;
        private List<Vector2Int> _pathCells;      // ← store grid cells
        private int _currentIndex;
        private bool _goingForward = true;
        private float _spawnTime;
        private float _stoppingThreshold = 0.1f;
        private float _nodeSize;

        public Vector2Int CurrentCell
        {
            get
            {
                if (_pathCells == null || _pathCells.Count == 0)
                    return Vector2Int.zero;
                return _pathCells[_currentIndex];
            }
        }

        public Vector2Int NextCell
        {
            get
            {
                if (_pathCells == null || _pathCells.Count == 0)
                    return Vector2Int.zero;

                int offset = _goingForward ? 1 : -1;
                int nextIndex = Mathf.Clamp(_currentIndex + offset, 0, _pathCells.Count - 1);
                return _pathCells[nextIndex];
            }
        }

        /// <summary>
        /// pathCells must be the exact same List<Vector2Int> you used for the Trailhead.
        /// nodeSize must match your grid cell size.
        /// </summary>
        public bool Initialize(List<Vector2Int> pathCells, float cellSize)
        {
            if (pathCells == null || pathCells.Count == 0)
            {
                Debug.LogError("VisitorAI.Initialize called with no pathCells");
                return false;
            }
            _nodeSize = cellSize;
            _pathCells = pathCells;
            _agent = GetComponent<NavMeshAgent>();

            // grab grid origin so our CellToWorld knows where to sit
            var origin = GridManager.Instance.origin;

            // Build our world‐space waypoints
            _waypoints = new List<Vector3>(pathCells.Count);
            foreach (var cell in pathCells)
            {
                // this overload includes origin → perfect alignment
                _waypoints.Add(
                    GridUtils.CellToWorld(cell, origin, cellSize, transform.position.y)
                );
            }

            // INITIAL PATROL STATE
            _currentIndex = 0;
            _goingForward = true;
            _spawnTime = Time.time;

            // SNAP ONTO NAVMESH
            var first = _waypoints[0];
            var samplePos = first + Vector3.up * 0.2f;       // just above the mesh
            var sampleRad = cellSize * 0.5f;                 // should cover the ribbon width

            if (!_agent.isOnNavMesh)
            {
                // use a tighter radius so you can’t accidentally hit the neighbor ribbon:
                //float sampleRad = cellSize * 0.4f;
                if (NavMesh.SamplePosition(samplePos, out var hit, sampleRad, NavMesh.AllAreas))
                {
                    _agent.Warp(hit.position);


                }
                else
                {
                    Debug.LogError($"VisitorAI: Could not find NavMesh under trailhead at {first}");
                    Destroy(gameObject);          // avoid later SetDestination crash

                }
            }

            // kick off movement
            Vector3 startTarget = _waypoints.Count > 1 ? _waypoints[1] : _waypoints[0];
            _agent.SetDestination(startTarget);
            return true;
        }

        void OnEnable()
        {
            // Capture one lambda instance…
            _tickHandler = deltaTime =>
            {
                if (_root != null)
                    _root.Tick();
            };
            // …and subscribe it
            SimulationTicker.OnTick += _tickHandler;
        }

        void Update()
        {
            if (_waypoints == null || _waypoints.Count == 0)
                return;

            if (_agent.pathPending || _agent.remainingDistance > _stoppingThreshold)
                return;

            // Despawn condition
            if (_currentIndex <= 1 && Time.time - _spawnTime >= maxLifetimeAtTrailhead)
            {

                ObjectPoolManager.Instance.ReleaseVisitor(gameObject);
                //Destroy(gameObject);
                return;
            }

            if (Leader != null)
            {
                // simple follow with fixed 0.5-cell offset
                Vector3 tgt = Leader.transform.position
                              + (transform.position - Leader.transform.position).normalized * 0.5f;
                _agent.SetDestination(tgt);
                return;          // skip normal Wander/BT when following
            }


            // 1) Pick a random leg
            int leg = Random.Range(minLegLength, maxLegLength + 1);

            int tries = 3;
            while (tries-- > 0 &&
                   _navigator.WasRecentlyVisited(
                       _pathCells[Mathf.Clamp(
                           _currentIndex + (_goingForward ? leg : -leg),
                           0, _pathCells.Count - 1)]))
            {
                leg = Random.Range(minLegLength, maxLegLength + 1);
            }

            // 2) Advance or retreat
            _currentIndex += _goingForward ? leg : -leg;

            // 3) Flip & clamp at ends BEFORE accessing waypoints
            if (_currentIndex >= _waypoints.Count)
            {
                _goingForward = false;
                _currentIndex = _waypoints.Count - 1 - leg;
                if (_currentIndex < 0)
                    _currentIndex = 0;
            }
            else if (_currentIndex < 0)
            {
                _goingForward = true;
                _currentIndex = leg;
                if (_currentIndex >= _waypoints.Count)
                    _currentIndex = _waypoints.Count - 1;
            }

            // Now it's safe to read _waypoints[_currentIndex]
            Vector3 rawTarget = _waypoints[_currentIndex];


            int nextIndex = Mathf.Clamp(_currentIndex + (_goingForward ? 1 : -1),
                0, _waypoints.Count - 1);

            // 4) Apply lane-jitter **perpendicular** to the path so it never points backwards
            Vector3 segDir = (_waypoints[nextIndex] - _waypoints[_currentIndex]).WithY(0).normalized;
            Vector3 side = Vector3.Cross(Vector3.up, segDir);          // right-hand axis
            float maxSide = _nodeSize * 0.3f;
            Vector3 finalTarget = rawTarget + side * Random.Range(-maxSide, maxSide);

            // 5) Send the agent once
            _agent.SetDestination(finalTarget);
        }

        void Awake()
        {
            // cache components
            _agent = GetComponent<NavMeshAgent>();
            _vm = this;
            _needs = GetComponent<NeedManager>();
            _navigator = new BranchingNavigator(preferenceData);

            // 1) Leave-park sequence
            var leaveCond = new ConditionNode(() => ShouldLeavePark());
            var leaveAction = new ActionNode(() => { LeavePark(); return NodeStatus.Success; });
            var leaveSeq = new Sequence(leaveCond, leaveAction);

            // 2) Use-facility sequence (once you arrive)
            var useCond = new ConditionNode(() =>
                _targetFacility != null
                && !_agent.pathPending
                && _agent.remainingDistance <= _agent.stoppingDistance
            );
            var useAction = new ActionNode(UseFacility);
            var useSeq = new Sequence(useCond, useAction);

            // 3) Seek-facility sequence (when needs are critical or if path goes bad / facility offline)
            var seekCond = new ConditionNode(() =>
                _needs.GetMostUrgentNeed(out var _)
                && (_targetFacility == null
                    || !FacilityManager.Instance.IsOnline(_targetFacility)
                    || _agent.pathStatus == NavMeshPathStatus.PathInvalid
                   )
            );
            var seekAction = new ActionNode(StartSeeking);
            var seekSeq = new Sequence(seekCond, seekAction);

            // 4) Wander fallback
            var wanderAct = new ActionNode(Wander);
            GetComponent<VisitorMoodManager>().OnMetricsUpdated += m => OnSatisfactionChanged(m.OverallSatisfaction);
            // put it all together: leave → use → seek → wander
            _root = new Selector(leaveSeq, useSeq, seekSeq, wanderAct);

            OnVisitorSpawned?.Invoke();
        }



        private NodeStatus Wander()
        {
            var cur = CurrentCell;
            var next = _navigator.GetNextCell(cur);

            // sync your index:
            int idx = _pathCells.IndexOf(next);
            if (idx >= 0) _currentIndex = idx;

            // go there
            Vector3 worldPos = GridManager.Instance.GetWorldPosition(next);
            _agent.SetDestination(worldPos);
            return NodeStatus.Success;
        }

        private NodeStatus StartSeeking()
        {

            if (_needs == null)
            {
                Debug.LogError("VisitorAI.StartSeeking → _needs is null!");
                return NodeStatus.Failure;
            }
            if (FacilityManager.Instance == null)
            {
                Debug.LogError("VisitorAI.StartSeeking → FacilityManager.Instance is null!");
                return NodeStatus.Failure;
            }


            if (!_needs.GetMostUrgentNeed(out var need))
                return NodeStatus.Failure;

            // If we were already targeting one, unsubscribe
            if (_targetFacility != null)
                _targetFacility.OnUseComplete -= OnNeedFulfilled;

            // Find the new target
            _targetFacility = FacilityManager.Instance.FindClosest(need, CurrentCell);
            if (_targetFacility == null)
            {
                Debug.Log($"No facility found for {need}");
                return NodeStatus.Failure;
            }

            // Subscribe to its completion event
            _targetFacility.OnUseComplete += OnNeedFulfilled;

            // Kick off the NavMesh path
            _agent.SetDestination(_targetFacility.transform.position);
            return NodeStatus.Running;
        }

        private NodeStatus UseFacility()
        {
            if (_targetFacility == null)
                return NodeStatus.Failure;

            // Start its Use coroutine; when it finishes it'll fire OnUseComplete
            _useCoroutine = StartCoroutine(_targetFacility.Use(this));
            return NodeStatus.Running;
        }

        /// <summary>
        /// Called by FacilityComponent when usage completes
        /// </summary>
        public void OnNeedFulfilled(VisitorAI visitor, NeedType[] servedNeeds)
        {
            // ignore other visitors
            if (visitor != this) return;

            // satisfy each need
            foreach (var need in servedNeeds)
                _needs.SatisfyNeed(need);

            // unsubscribe & clean up
            _targetFacility.OnUseComplete -= OnNeedFulfilled;
            _targetFacility = null;
            if (_useCoroutine != null) StopCoroutine(_useCoroutine);
        }
        private bool ShouldLeavePark()
        {
            if (state == VisitorState.Wander)
                // mirror your despawn logic: only when back at trailhead (index ≤ 1) AND lifetime expired
                return _currentIndex <= 1
                    && (Time.time - _spawnTime) >= maxLifetimeAtTrailhead;
            return false;
        }


        private void OnSatisfactionChanged(float newVal)
        {
            if (state == VisitorState.Leaving || state == VisitorState.Despawning) return;

            if (newVal < leaveThreshold)
                BeginLeaving();
        }

        private void BeginLeaving()
        {
            state = VisitorState.Leaving;
            ExitData exit = ExitManager.Instance.GetNearestExit(transform.position);
            if (exit == null) { Debug.LogWarning("No exits defined!"); return; }

            _agent.SetDestination(exit.worldPosition);
        }

        private void LeavePark()
        {
            // TODO: Farewell animation trigger
            if (state == VisitorState.Leaving && !_agent.pathPending && _agent.remainingDistance < 0.2f)
                StartCoroutine(FarewellAndDespawn());
            //Destroy(gameObject);
        }

        private IEnumerator FarewellAndDespawn()
        {
            // 1) Freeze the NavMeshAgent so the visitor doesn’t wander off.
            _agent.isStopped = true;

            // 2) Trigger an optional farewell anim on any Animator attached.
            //    Animator should have a trigger parameter named "FadeOut".
            Animator anim = GetComponent<Animator>();
            if (anim != null && anim.HasParameterOfType("FadeOut", AnimatorControllerParameterType.Trigger))
                anim.SetTrigger("FadeOut");

            // 3) Wait a short, fixed time OR until the animation state has finished.
            //    Replace 1.0f with your clip length if you prefer a hard wait.
            float t = 0f;
            while (t < 1.0f)
            {
                t += Time.deltaTime;
                yield return null;
            }

            // 4) Despawn. Use an object pool here if you have one.
            PopulationController.Instance.ReturnTicket(visitorType);
            ObjectPoolManager.Instance.ReleaseVisitor(gameObject);
        }

        void OnDisable()
        {
            // this will fire automatically before the GameObject is destroyed
            SimulationTicker.OnTick -= _tickHandler;
            OnVisitorDespawned?.Invoke();
        }

    }

    public static class AnimatorExt
    {
        /* small helper so we can test animator param safely */

        public static bool HasParameterOfType(this Animator self, string name, AnimatorControllerParameterType t)
        {
            foreach (var p in self.parameters)
                if (p.type == t && p.name == name)
                    return true;
            return false;
        }
    }

    internal static class VectorExt
    {
        public static Vector3 WithY(this Vector3 v, float y) => new Vector3(v.x, y, v.z);
        public static Vector3 ToXZ(this Vector2Int c) => new Vector3(c.x, 0f, c.y);
    }
}
