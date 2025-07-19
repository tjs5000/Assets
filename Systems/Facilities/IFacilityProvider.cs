using PlexiPark.Core.SharedEnums;
using PlexiPark.Systems.Visitor;

namespace PlexiPark.Systems.Facilities
{
    public interface IFacilityProvider
    {
        /// <summary>
        /// Find the nearest online facility that serves `need`, from grid cell `fromCell`.
        /// </summary>
        FacilityComponent FindClosest(NeedType need, UnityEngine.Vector2Int fromCell);

        /// <summary>
        /// Is the given facility currently available?
        /// </summary>
        bool IsOnline(FacilityComponent f);
    }
}
