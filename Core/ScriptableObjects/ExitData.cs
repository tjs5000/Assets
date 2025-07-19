// Core/ScriptableObjects/ExitData.cs
using UnityEngine;
using PlexiPark.Core.SharedEnums;

[CreateAssetMenu(menuName = "PlexiPark/Exit")]
public class ExitData : ScriptableObject
{
    public string Id;
    public ExitType type;           // Trailhead, ParkGate, Emergency
    public Vector3 worldPosition;   // set by designer or spawner
}
