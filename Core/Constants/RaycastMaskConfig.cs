using UnityEngine;

public static class RaycastMaskConfig
{
    public static readonly int UI              = LayerMask.NameToLayer("UI");
    public static readonly int Terrain         = LayerMask.NameToLayer("Terrain");
    public static readonly int WorldObject     = LayerMask.NameToLayer("WorldObject");
    public static readonly int StaticObjects   = LayerMask.NameToLayer("StaticObjects");
    public static readonly int BuildPreview    = LayerMask.NameToLayer("BuildPreview");
    public static readonly int TransparentFX   = LayerMask.NameToLayer("TransparentFX");
    public static readonly int IgnoreRaycast   = LayerMask.NameToLayer("Ignore Raycast");

    public static LayerMask InteractableMask =>
        LayerMask.GetMask("WorldObject", "StaticObjects");

    public static LayerMask GroundMask =>
        LayerMask.GetMask("Terrain", "StaticObjects");
}
