#if UNITY_EDITOR
using UnityEditor;
#endif
using UnityEngine;

public class ExitAnchor : MonoBehaviour
{
    [SerializeField] private ExitData data;
#if UNITY_EDITOR
    void OnValidate()
    {
        if (data != null)
        {
            data.worldPosition = transform.position;
            EditorUtility.SetDirty(data);
        }
    }
#endif
}