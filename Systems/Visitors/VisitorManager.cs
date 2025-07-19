// Assets/Systems/Visitor/VisitorManager.cs
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.AI;
using PlexiPark.Systems.Trail;       // for Trailhead
using PlexiPark.Core.Utils;
using PlexiPark.Core.SharedEnums;
using PlexiPark.Core.Pooling;
using PlexiPark.Managers;

namespace PlexiPark.Systems.Visitor
{
    [DefaultExecutionOrder(2000)]
    public class VisitorManager : MonoBehaviour
    {
        [Header("Assign one prefab per VisitorType (by enum index)")]
        public List<VisitorPrefabSet> prefabSets;
        public GameObject visitorParentObject;

        [Header("Seconds between spawn waves")]
        [Min(0.1f)] public float minSpawnInterval = 5f;
        [Min(0.1f)] public float maxSpawnInterval = 15f;
        [Range(0, 100)] public int singleSpawnWeight = 70;



        //  private readonly List<SpawnClock> _clocks = new();

        [Header("Population cap")]
        [Min(1)]
        public int maxVisitors = 300;       // hard ceiling †
        [Min(1)]
        public int softBurstSize = 10;        // how many you may add in one Update

        /* --------------------------------------------------------------------- */

        int _liveCount;                       // updated by the registry below
        readonly List<SpawnClock> _clocks = new();
        void Start()
        {
            foreach (var th in
                     Object.FindObjectsByType<Trailhead>(FindObjectsSortMode.None))
                _clocks.Add(new SpawnClock(th,
                            Random.Range(minSpawnInterval, maxSpawnInterval)));

            // ❶ wire a callback so we always know how many are alive
            VisitorAI.OnVisitorSpawned += () => _liveCount++;
            VisitorAI.OnVisitorDespawned += () => _liveCount--;
            _liveCount = FindObjectsOfType<VisitorAI>().Length;
        }

        void Update()
        {
            float dt = Time.deltaTime;

            /* ---------- build clocks once all Trailheads exist ---------- */
            if (_clocks.Count == 0)                       // ← NEW
            {                                             //    find Trailheads that were
                foreach (var th in                       //    spawned by the generator
                    Object.FindObjectsByType<Trailhead>(FindObjectsSortMode.None))
                {                                         //    and create one timer each
                    float t = Random.Range(minSpawnInterval, maxSpawnInterval);
                    _clocks.Add(new SpawnClock(th, t));
                }
                if (_clocks.Count == 0) return;          // none yet – try again next frame
            }
            /* ------------------------------------------------------------- */

            // existing per-trail-head timers

            if (_liveCount >= maxVisitors) return;            // population full

            int budget = Mathf.Min(softBurstSize,
                                   maxVisitors - _liveCount); // don’t exceed cap

            foreach (var clk in _clocks)
            {
                if (budget <= 0) break;                       // out of quota

                clk.timer -= dt;
                if (clk.timer > 0f) continue;

                budget -= SpawnAtTrailhead(clk.head, budget); // returns spawned #
                clk.timer = Random.Range(minSpawnInterval, maxSpawnInterval);
            }
        }


        /// <summary>
        /// Spawn up to <paramref name="budgetLeft"/> individuals at the given trailhead
        /// and return how many were actually spawned.
        /// </summary>
        private int SpawnAtTrailhead(Trailhead th, int budgetLeft)
        {
            int spawned = 0;
            if (budgetLeft <= 0) return 0;
            if (th.PathCells == null || th.PathCells.Count == 0) return 0;

            foreach (var vType in th.AllowedVisitorTypes)
            {
                // --- look-up prefab variations for this visitor type ----------------
                VisitorPrefabSet set = prefabSets.Find(s => s.type == vType);
                if (set == null || set.variations.Count == 0) continue;

                bool canGroup = vType == VisitorType.Hiker || vType == VisitorType.Walker;
                bool spawnGroup = canGroup && Random.Range(0, 100) > singleSpawnWeight;
                int groupSize = spawnGroup ? Random.Range(2, 4) : 1;

                VisitorAI leader = null;

                for (int i = 0; i < groupSize && budgetLeft > 0; i++)
                {

                    // ---- ticket check ----------------------------------------
                    if (!PopulationController.Instance.RequestTicket(vType))
                        break;                           // quota exhausted for this type


                    /* ------------------- pull from pool & position ----------------- */
                    GameObject prefab = set.variations[Random.Range(0, set.variations.Count)];
                    GameObject inst = ObjectPoolManager.Instance.GetVisitor(prefab);

                    Vector3 spawnPos = BuildSpawnPos(th, i);
                    inst.transform.SetPositionAndRotation(spawnPos, Quaternion.identity);

                    /* ------------------- component sanity checks ------------------- */
                    VisitorAI ai = inst.GetComponent<VisitorAI>();
                    if (ai == null || !ai.Initialize(th.PathCells, th.NodeSize))
                    {
                        Debug.LogError($"[VisitorManager] {prefab.name} missing/failed VisitorAI – releasing to pool");
                        ObjectPoolManager.Instance.ReleaseVisitor(inst);
                        PopulationController.Instance.ReturnTicket(vType);
                        break;                                  // abort this group
                    }

                    /* ------------------- leader / follower wiring ------------------ */
                    if (i == 0)                                // leader
                    {
                        leader = ai;
                        if (inst.TryGetComponent(out NavMeshAgent nav))
                        {
                            nav.radius = th.NodeSize * 0.5f;
                            nav.avoidancePriority = Random.Range(0, 100);
                            nav.obstacleAvoidanceType =
                                ObstacleAvoidanceType.HighQualityObstacleAvoidance;
                        }
                    }
                    else                                        // follower
                    {
                        ai.Leader = leader;
                    }

                    spawned++;
                    budgetLeft--;
                }

                // Stop iterating visitor types if we exhausted the per-frame quota
                if (budgetLeft == 0) break;
            }

            return spawned;
        }

        // small record that keeps a per-trailhead timer
        private sealed class SpawnClock
        {
            public Trailhead head;
            public float timer;
            public SpawnClock(Trailhead h, float t) { head = h; timer = t; }
        }
        private Vector3 BuildSpawnPos(Trailhead th, int followerIndex)
        {
            Vector3 basePos = GridUtils.CellToWorld(
                th.PathCells[0], GridManager.Instance.origin, th.NodeSize, 0f);

            basePos += new Vector3(Random.Range(-0.2f, 0.2f), 0f,
                                   Random.Range(-0.2f, 0.2f));

            // follower lateral offset
            if (followerIndex > 0 && th.PathCells.Count > 1)
            {
                Vector3 dir = (th.PathCells[1] - th.PathCells[0]).ToXZ().normalized;
                Vector3 right = Vector3.Cross(Vector3.up, dir);
                basePos += right * th.NodeSize * 0.4f * (followerIndex % 2 == 0 ? 1 : -1);
            }

            // snap to NavMesh
            if (NavMesh.SamplePosition(basePos, out var hit, th.NodeSize * 2f, NavMesh.AllAreas))
                basePos = hit.position + Vector3.up * 0.05f;
            else
                basePos.y += 2f;

            return basePos;
        }


    }


}
