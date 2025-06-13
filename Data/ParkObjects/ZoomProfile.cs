using UnityEngine;
using PlexiPark.Data;

[CreateAssetMenu(fileName = "ZoomProfile", menuName = "PlexiPark/Zoom Profile")]
public class ZoomProfile : ScriptableObject
{
    public ParkType parkType;

    [Header("Zoom Settings")]
    public float minZoom = 10f;
    public float maxZoom = 60f;
}
