using System;
using UnityEngine;

namespace EasyFramework.Grass.Runtime
{
    public struct CellIndex : IEquatable<CellIndex>
    {
        public readonly int x;
        public readonly int y;
        public readonly int hash;

        public int state;

        public static Vector2Int CellSize;
        public static Vector3 TerrainPos;
        public static Vector3 TerrainSize;
        public static float HeightmapResolution;

        public CellIndex(int x, int y)
        {
            this.state = 0;
            this.x = x;
            this.y = y;
            this.hash = x + (y << 16);
        }

        public CellIndex(int x, int y, int state)
        {
            this.state = state;
            this.x = x;
            this.y = y;
            this.hash = x + (y << 16);
        }

        public override string ToString() => "(" + x + "," + y + ")";
        public override int GetHashCode() => hash;
        public override bool Equals(object obj) => obj is CellIndex other && Equals(other);
        public bool Equals(CellIndex other) => hash == other.hash;

        public static CellIndex IndexFromPosition(Vector3 localPos)
        {
            return new CellIndex(
                Mathf.FloorToInt(localPos.x / CellSize.x),
                Mathf.FloorToInt(localPos.z / CellSize.y));
        }

        public static Vector2 PositionFromIndexFast(CellIndex index)
        {
            var cellHalfSize = CellSize / 2;
            var cellPosition = new Vector2(
                       index.x * CellSize.x + cellHalfSize.x,
                       index.y * CellSize.y + cellHalfSize.y);
            return cellPosition;
        }

        public static Vector3 PositionFromIndex(CellIndex index)
        {
            var cellPosition = PositionFromIndexFast(index);
            var cellHeight = DataLoader.Instance.GetTerrainHeight(
                cellPosition.x / TerrainSize.x, cellPosition.y / TerrainSize.z,
                HeightmapResolution, HeightmapResolution, TerrainSize.y);
            return new Vector3(cellPosition.x, 
                cellHeight + Mathf.Max(CellSize.x, CellSize.y) * 0.5f, cellPosition.y);
        }
    }
}