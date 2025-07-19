using UnityEngine;

[CreateAssetMenu(menuName = "PlexiPark/Park Profile")]
public class ParkProfileSO : ScriptableObject
{
    public string parkName;
    public Vector2 boundsMin;
    public Vector2 boundsMax;
    public float minZoom = 10f;
    public float maxZoom = 60f;
    public float initialZoom = 5f;
    public float initialYaw = 45f;
    public float initialTilt = -50f;
}
