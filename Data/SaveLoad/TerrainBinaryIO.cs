// Assets/Data/SaveLoad/TerrainBinaryIO.cs
using System;
using System.Collections.Generic;
using System.IO;
using UnityEngine;
using PlexiPark.Core.SharedEnums;

namespace PlexiPark.Data.SaveLoad
{
    public static class TerrainBinaryIO
    {
        public static void Save(string path,
            ParkType parkType,
            int width,
            int depth,
            Dictionary<Vector2Int, GridCellData> gridCells,
            Dictionary<Vector2Int, float> cornerHeights)
        {
            using var stream = new FileStream(path, FileMode.Create);
            using var writer = new BinaryWriter(stream);

            writer.Write((int)parkType);
            writer.Write(width);
            writer.Write(depth);

            writer.Write(cornerHeights.Count);
            foreach (var pair in cornerHeights)
            {
                writer.Write(pair.Key.x);
                writer.Write(pair.Key.y);
                writer.Write(pair.Value);
            }

            writer.Write(gridCells.Count);
            foreach (var kvp in gridCells)
            {
                writer.Write(kvp.Key.x);
                writer.Write(kvp.Key.y);
                writer.Write((byte)kvp.Value.slope);
                writer.Write((byte)kvp.Value.terrainType);
                // Add Trail/Segment if needed
            }
        }

        public static void Load(string path,
            out ParkType parkType,
            out int width,
            out int depth,
            out Dictionary<Vector2Int, GridCellData> gridCells,
            out Dictionary<Vector2Int, float> cornerHeights)
        {
            parkType = ParkType.Urban;
            width = 0;
            depth = 0;
            gridCells = new();
            cornerHeights = new();

            if (!File.Exists(path)) return;

            using var stream = new FileStream(path, FileMode.Open);
            using var reader = new BinaryReader(stream);

            parkType = (ParkType)reader.ReadInt32();
            width = reader.ReadInt32();
            depth = reader.ReadInt32();

            int cornerCount = reader.ReadInt32();
            for (int i = 0; i < cornerCount; i++)
            {
                int x = reader.ReadInt32();
                int y = reader.ReadInt32();
                float height = reader.ReadSingle();
                cornerHeights[new Vector2Int(x, y)] = height;
            }

            int cellCount = reader.ReadInt32();
            for (int i = 0; i < cellCount; i++)
            {
                int x = reader.ReadInt32();
                int y = reader.ReadInt32();
                var slope = (SlopeType)reader.ReadByte();
                var terrain = (TerrainType)reader.ReadByte();

                gridCells[new Vector2Int(x, y)] = new GridCellData
                {
                    slope = slope,
                    terrainType = terrain,
                    isOccupied = false
                };
            }
        }
    }
}
