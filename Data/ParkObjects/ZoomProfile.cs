using UnityEngine;
using PlexiPark.Data;

[CreateAssetMenu(fileName = "ZoomProfile", menuName = "PlexiPark/Zoom Profile")]
public class ZoomProfile : ScriptableObject
{
    [Tooltip("Which park type this zoom range applies to")]
    public ParkType parkType;

    [Header("Zoom (distance from look-point in world units)")]
    [Tooltip("Minimum allowed camera distance for this park type")]
    public float minZoom = 5f;

    [Tooltip("Maximum allowed camera distance for this park type")]
    public float maxZoom = 60f;

}
