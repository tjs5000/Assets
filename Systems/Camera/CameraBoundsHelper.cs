using UnityEngine;

public static class CameraBoundsHelper
{
    public static Vector3 ClampPosition(Vector3 desired) => desired;
    public static bool IsOutOfBounds(Vector3 pos) => false;
    public static Vector3 GetBounceBackCorrection(Vector3 pos) => Vector3.zero;
}
