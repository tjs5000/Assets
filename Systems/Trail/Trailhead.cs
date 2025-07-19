using System.Collections.Generic;
using UnityEngine;
using UnityEngine.EventSystems;
using PlexiPark.Core.SharedEnums;

namespace PlexiPark.Systems.Trail
{
    [RequireComponent(typeof(Collider))]
    public class Trailhead : MonoBehaviour, IDragHandler, IBeginDragHandler
    {
        //[HideInInspector] public List<Vector2Int> PathCells;

        public TrailType PlanType { get; private set; }
        public List<Vector2Int> PathCells { get; private set; }
        public float NodeSize { get; private set; }
        private Camera _cam;
        private float _nodeSize;
        private float _halfCell;


        /// <summary>
        /// Which visitor types should spawn here.
        /// </summary>
        public List<VisitorType> AllowedVisitorTypes { get; private set; }



        private static readonly Dictionary<TrailType, VisitorType> TrailToVisitor =
            new()
            {
        { TrailType.HikingTrail,     VisitorType.Hiker },
        { TrailType.HikingTrail2,    VisitorType.Hiker },
        { TrailType.MountainTrail,   VisitorType.MountainBiker },
        { TrailType.MountainTrail2,  VisitorType.MountainBiker },
        { TrailType.WalkPath,        VisitorType.Walker },
        { TrailType.WalkPath2,       VisitorType.Walker },
        { TrailType.BikePath,        VisitorType.Biker },
        { TrailType.BikePath2,       VisitorType.Biker },
                // add others as needed
            };

        /// <summary>
        /// Initialize with a single default visitor type (same as the trail’s PlanType),
        /// but you can call AddVisitorType() later to expand this list.
        /// </summary>
        public void Initialize(TrailType planType, List<Vector2Int> path, float nodeSize)
        {
            PlanType = planType;
            PathCells = path;

            // ← Add this line so the public getter returns the right size
            NodeSize = nodeSize;

            AllowedVisitorTypes = new List<VisitorType>();

            if (TrailToVisitor.TryGetValue(planType, out var v))
                AllowedVisitorTypes.Add(v);
            else
                Debug.LogWarning($"No visitor mapping for trail type {planType}");

            // you can keep your private fields if you like
            _nodeSize = nodeSize;
            _halfCell = nodeSize * 0.5f;
            _cam = Camera.main;
        }

        public void AddVisitorType(VisitorType visitorType)
        {
            if (!AllowedVisitorTypes.Contains(visitorType))
                AllowedVisitorTypes.Add(visitorType);
        }
        public void OnBeginDrag(PointerEventData e)
        {
            // optional: highlight trail or show UI
        }

        public void OnDrag(PointerEventData e)
        {
            // ray-cast to terrain
            if (Physics.Raycast(_cam.ScreenPointToRay(e.position), out var hit))
            {
                // find nearest cell in PathCells
                Vector3 wp = hit.point;
                Vector2Int closest = PathCells[0];
                float bestDist = float.MaxValue;

                foreach (var cell in PathCells)
                {
                    Vector3 cellPos = new Vector3(
                        cell.x * _nodeSize + _halfCell,
                        wp.y,
                        cell.y * _nodeSize + _halfCell
                    );
                    float d = (cellPos - wp).sqrMagnitude;
                    if (d < bestDist)
                    {
                        bestDist = d;
                        closest = cell;
                    }
                }

                // snap self to that cell
                transform.position = new Vector3(
                    closest.x * _nodeSize + _halfCell,
                    transform.position.y,
                    closest.y * _nodeSize + _halfCell
                );
            }
        }
    }
}
