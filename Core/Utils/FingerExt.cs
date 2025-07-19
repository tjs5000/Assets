//Assets/Core/Utilities/FingerExt.cs
using UnityEngine;
using Lean.Touch;
using UnityEngine.EventSystems;


namespace PlexiPark.Core
{

    public static class FingerExt
    {
        public static bool IsOverUI(this LeanFinger f)
        {
            return EventSystem.current.IsPointerOverGameObject(f.Index);
        }
    }
}