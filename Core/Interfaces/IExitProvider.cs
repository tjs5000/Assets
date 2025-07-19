using UnityEngine;
using System.Collections.Generic;

namespace PlexiPark.Core.Interfaces
{
    public interface IExitProvider
    {
        ExitData GetNearestExit(Vector3 fromPos);
        IEnumerable<ExitData> GetAllExits();
    }
}
