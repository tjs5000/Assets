using UnityEngine;
using PlexiPark.Core.SharedEnums;

public interface IGridInfoProvider
{
    // ── existing methods ─────────────────────────────
    bool IsWithinBounds(Vector2Int coord);
    bool IsCellOccupied(Vector2Int coord);
    bool IsValidTrailCell(Vector2Int coord, TrailType type);
    SlopeType GetSlopeType(Vector2Int cell);

    // ── NEW read-only grid facts (decorative terrain needs these) ──
    int     GridWidth        { get; }
    int     GridDepth        { get; }
    float   CellSize         { get; }
    Vector3 Origin           { get; }
    ParkType CurrentParkType { get; }
}
