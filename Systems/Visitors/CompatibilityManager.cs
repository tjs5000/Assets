// Assets/Systems/Visitors/CompatibilityManager.cs
using UnityEngine;
using PlexiPark.Core.SharedEnums;

public class CompatibilityManager : MonoBehaviour
{
    public static CompatibilityManager Instance { get; private set; }

    [Tooltip("Drag in your VisitorCompatibilityData SO here")]
    public VisitorCompatibilityData visitorCompatibilityData;

    void Awake()
    {
        if (Instance != null && Instance != this) Destroy(gameObject);
        else Instance = this;
    }

    public float GetVisitorCompatibility(VisitorType other, VisitorType self)
    {
        return visitorCompatibilityData.GetCompatibility(other, self);
    }
}
