// Assets/Systems/Visitor/ObjectCompatibilityComponent.cs
using UnityEngine;
using PlexiPark.Systems.Visitor;

[RequireComponent(typeof(Collider))]
public class ObjectCompatibilityComponent : MonoBehaviour
{
    [Tooltip("Which ObjectCompatibilityData SO applies")]
    public ObjectCompatibilityData compatibilityData;

    void Awake()
    {
        VisitorObjectManager.Instance.RegisterObject(this.gameObject);
    }

    void OnDestroy()
    {
        VisitorObjectManager.Instance.UnregisterObject(this.gameObject);
    }
}
