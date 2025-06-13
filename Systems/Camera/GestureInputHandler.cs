using UnityEngine;

public class GestureInputHandler : MonoBehaviour
{
    public bool IsOverUI() => false;
    public bool TryGetPinchDelta(out float zoomDelta)
    {
        zoomDelta = 0f;
        return false;
    }

    public bool TryGetRotationDelta(out float angleDelta)
    {
        angleDelta = 0f;
        return false;
    }

    public bool TryGetPanDelta(out Vector2 panDelta)
    {
        panDelta = Vector2.zero;
        return false;
    }

    public bool TryGetTiltDelta(out float tiltDelta)
    {
        tiltDelta = 0f;
        return false;
    }
}
