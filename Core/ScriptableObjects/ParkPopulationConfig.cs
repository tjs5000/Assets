// Core/ScriptableObjects/ParkPopulationConfig.cs
using UnityEngine;
using PlexiPark.Core.SharedEnums;

[CreateAssetMenu(fileName = "ParkPopulationConfig",
                 menuName = "PlexiPark/Population Caps")]
public class ParkPopulationConfig : ScriptableObject
{
    public int visitorCap;
    public int wildlifeCap;
}