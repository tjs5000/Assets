// Managers/ExitManager.cs   (assembly: PlexiPark.Managers)
using System.Linq;
using UnityEngine;
using System.Collections.Generic;
using PlexiPark.Core.Interfaces;

public class ExitManager : MonoBehaviour, IExitProvider
{
    public static ExitManager Instance { get; private set; }

    [SerializeField] private List<ExitData> exits = new();

    public ExitData GetNearestExit(Vector3 fromPos)
        => exits.OrderBy(e => (e.worldPosition - fromPos).sqrMagnitude).FirstOrDefault();

    public IEnumerable<ExitData> GetAllExits() => exits;

    // Optional helper to register exits spawned at runtime
    public void Register(ExitData data) => exits.Add(data);

    void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Debug.LogWarning("Duplicate ExitManager—destroying the new one.");
            Destroy(gameObject);
            return;
        }
        Instance = this;
    }
}
