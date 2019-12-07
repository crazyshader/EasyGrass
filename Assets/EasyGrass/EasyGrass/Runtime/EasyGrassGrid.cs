using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using UnityEngine;

namespace EasyGrass
{
    public class EasyGrassGrid
    {
        public struct CellIndex : IEquatable<CellIndex>
        {
            public readonly int x;
            public readonly int y;
            public readonly int hash;

            public CellIndex(int x, int y)
            {
                this.x = x;
                this.y = y;
                this.hash = x + (y << 16);
            }

            public override string ToString() => "(" + x + "," + y + ")";
            public override int GetHashCode() => hash;
            public override bool Equals(object obj) => obj is CellIndex other && Equals(other);
            public bool Equals(CellIndex other) => hash == other.hash;
        }

        private int _cellCount;
        private float _cellSize;
        private float _cellHalfSize;
        private Rect _terrainRect;
        private EasyGrass _massiveGrass;
        private List<CellIndex> _activeIndices = new List<CellIndex>();

        public EasyGrassGrid(EasyGrass massiveGrass, int cellCount)
        {
            _massiveGrass = massiveGrass;
            var bounds = massiveGrass.TerrainData.TerrainBounds;
            _terrainRect = new Rect(
                bounds.min.x + massiveGrass.TerrainData.TerrainPos.x,
                bounds.min.z + massiveGrass.TerrainData.TerrainPos.z,
                bounds.size.x,
                bounds.size.z);

            _cellCount = cellCount;
            _cellSize = _terrainRect.width / _cellCount;
            _cellHalfSize = _cellSize / 2f;
        }

        private Vector2 MinimumPos(CellIndex index)
        {
            return new Vector2(
                       index.x * _cellSize,
                       index.y * _cellSize)
                   + _terrainRect.min;
        }

        private Rect RectFromIndex(CellIndex index)
        {
            return new Rect(MinimumPos(index), _cellSize * Vector2.one);
        }

        private CellIndex IndexFromPosition(Vector2 position)
        {
            var local = new Vector2(position.x - _terrainRect.xMin, position.y - _terrainRect.yMin);
            return new CellIndex(
                Mathf.FloorToInt(local.x / _cellSize),
                Mathf.FloorToInt(local.y / _cellSize));
        }

        private Vector2 CenterPos(CellIndex index)
        {
            return new Vector2(
                       index.x * _cellSize + _cellHalfSize,
                       index.y * _cellSize + _cellHalfSize)
                   + _terrainRect.min;
        }

        private Vector3 CenterPos3D(CellIndex index)
        {
            var centerPos2D = CenterPos(index);
            var terrainPos = _massiveGrass.TerrainData.TerrainPos;
            var localPos = centerPos2D - new Vector2(terrainPos.x, terrainPos.z);
            localPos /= _terrainRect.size.x;
            //var height = _massiveGrass.UnityTerrain.terrainData.GetInterpolatedHeight(localPos.x, localPos.y);
            var height = EasyGrassUtility.GetTerrainHeight( localPos.x, localPos.y, 
                _massiveGrass.TerrainData.HeightmapResolution, _massiveGrass.TerrainData.HeightmapResolution,
                _massiveGrass.TerrainData.TerrainSize.y);
            return new Vector3(
                index.x * _cellSize + _cellHalfSize,
                height + _cellHalfSize,
                index.y * _cellSize + _cellHalfSize) + terrainPos;
        }

        private List<CellIndex> InnerSphereIndices(Vector3 cameraPos, float cullDistance)
        {
            Plane[] planes = GeometryUtility.CalculateFrustumPlanes(_massiveGrass.CurrentCamera);
            var hPos = new Vector2(cameraPos.x, cameraPos.z);
            var rectMinIndex = IndexFromPosition(hPos - Vector2.one * cullDistance);
            var rectMaxIndex = IndexFromPosition(hPos + Vector2.one * cullDistance);
            var indexList = new List<CellIndex>();
            for (var x = rectMinIndex.x; x < rectMaxIndex.x; x++)
            {
                if (x < 0 || _cellCount <= x) continue;
                for (var y = rectMinIndex.y; y < rectMaxIndex.y; y++)
                {
                    if (y < 0 || _cellCount <= y) continue;
                    var index = new CellIndex(x, y);
                    var cellPos = CenterPos3D(index);
                    var direction = cellPos - cameraPos;
                    var bounds = new Bounds(cellPos, new Vector3(_cellSize, _cellSize, _cellSize));
                    if (direction.magnitude <= cullDistance && GeometryUtility.TestPlanesAABB(planes, bounds))
                        indexList.Add(index);
                }
            }
            return indexList;
        }

        public async Task OnBuild(Vector3 cameraPos, float cullDistance, EasyGrassRenderer renderer)
        {
            var activatedIndices = InnerSphereIndices(cameraPos, cullDistance);
            var entered = new List<CellIndex>();
            var exited = new List<CellIndex>();
            await Task.Run(() =>
            {
                foreach (var activatedIndex in activatedIndices)
                {
                    var found = false;
                    foreach (var activeIndex in _activeIndices)
                    {
                        if (!activatedIndex.Equals(activeIndex)) continue;
                        found = true;
                        break;
                    }
                    if (!found)
                        entered.Add(activatedIndex);
                }

                foreach (var activeIndex in _activeIndices)
                {
                    var found = false;
                    foreach (var activatedIndex in activatedIndices)
                    {
                        if (!activeIndex.Equals(activatedIndex)) continue;
                        found = true;
                        break;
                    }
                    if (!found)
                        exited.Add(activeIndex);
                }
            });
            foreach (var cellIndex in exited)
                renderer.Remove(cellIndex);
            foreach (var cellIndex in entered)
                renderer.Create(cellIndex, RectFromIndex(cellIndex));
            _activeIndices = activatedIndices;
        }

    }
}
