// Assets/Systems/Visitor/ParkObjectComponent.cs
using UnityEngine;
using PlexiPark.Data;                // ParkObjectData lives here
using PlexiPark.Systems.Visitor;     // for ObjectManager

[RequireComponent(typeof(Collider))]
public class ParkObjectComponent : MonoBehaviour
{
    [Tooltip("Which ParkObjectData SO defines this placed object")]
    public ParkObjectData Data;

    void Awake()
    {
        // register this instance with its data
        VisitorObjectManager.Instance.RegisterObject(gameObject);
    }

    void OnDestroy()
    {
        VisitorObjectManager.Instance.UnregisterObject(gameObject);
    }
}
