// Assets/Data/SaveLoad/PlacedObject.cs

using System;
using UnityEngine;
namespace PlexiPark.Data
{
    [Serializable]
    public class PlacedObject
    {
        public string ObjectID;
        public Vector2Int GridPosition;
        public Quaternion Rotation;
        public Vector3 Scale = Vector3.one;
        public string CustomDataJson = "{}";


        public PlacedObject() { }
        public PlacedObject(string id, Vector2Int pos, Quaternion rot, Vector3 scale, string customJson = "{}")
        {
            ObjectID = id;
            GridPosition = pos;
            Rotation = rot;
            Scale = scale;
            CustomDataJson = customJson;
        }
    }
}
