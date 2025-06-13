// Systems/ParkTypeRules.cs
using UnityEngine;
using PlexiPark.Data;

namespace PlexiPark.Systems
{
    public class ParkTypeRules : MonoBehaviour
    {
        public static ParkTypeRules Instance { get; private set; }

        [Header("Current Park Type")]
        public ParkType CurrentParkType;

        private void Awake()
        {
            if (Instance != null)
            {
                Destroy(gameObject);
                return;
            }

            Instance = this;
            DontDestroyOnLoad(gameObject);
        }

                public bool CanPlaceObject(ParkObjectData data)
        {
    
            return true;
        }
    }
}
