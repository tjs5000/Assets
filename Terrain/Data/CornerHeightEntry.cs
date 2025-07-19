using UnityEngine;

[System.Serializable]
public class CornerHeightEntry
{
    public Vector2Int position;
    public float height;

    public CornerHeightEntry(Vector2Int pos, float h)
    {
        position = pos;
        height = h;
    }
}