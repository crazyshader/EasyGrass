using System;
using Unity.Collections;
using UnityEngine;

namespace EasyFramework.Grass.Runtime
{
    public struct BuildElement : IDisposable
    {
        private int _actualCount;
        private Vector2Int _cellCount;
        private Vector2 _pixelToTerrain;
        private Vector2 _terrainToPixel;
        private EasyGrassData _grassData;
        private GrassDetailData _grassDetailData;
        public NativeMultiHashMap<CellIndex, CellElement> ElementList { get; private set; }

        public BuildElement(EasyGrassData easyGrassData, int index)
        {
            _actualCount = 0;
            _grassData = easyGrassData;
            _grassDetailData = _grassData.DetailDataList[index];

            _cellCount = new Vector2Int(
                Mathf.CeilToInt(_grassData.TerrainSize.x / _grassData.CellSize.x),
                Mathf.CeilToInt(_grassData.TerrainSize.z / _grassData.CellSize.y));
            _pixelToTerrain = new Vector2(
                _grassData.TerrainSize.x / _grassData.DetailResolution,
                _grassData.TerrainSize.z / _grassData.DetailResolution);
            _terrainToPixel = new Vector2(
                _grassData.DetailResolution / _grassData.TerrainSize.x,
                _grassData.DetailResolution / _grassData.TerrainSize.z);

            ElementList = new NativeMultiHashMap<CellIndex, CellElement>(_cellCount.x * _cellCount.y, Allocator.Persistent);
        }

        public void Dispose()
        {
            if (ElementList.IsCreated)
            {
                ElementList.Dispose();
            }
        }

        public void Count()
        {
            var totalEleCount = 0;
            for (int i = 0; i < _cellCount.x; i++)
            {
                for (int j = 0; j < _cellCount.y; j++)
                {
                    var cellIndex = new CellIndex(i, j);
                    var cellEleCount = ElementList.CountValuesForKey(cellIndex);
                    totalEleCount += cellEleCount;
                    //Debug.Log($"{_grassDetailData.BrushIndex}|{cellIndex}|{cellEleCount}");
                }
            }
            //Debug.Log($"{_grassDetailData.BrushIndex} Total|{totalEleCount} {_actualCount}");
        }

        public void Build()
        {
            for (int i = 0; i < _cellCount.x; i++)
            {
                for (int j = 0; j < _cellCount.y; j++)
                {
                    var cellIndex = new CellIndex(i, j);
                    Build(cellIndex);
                }
            }
        }

        private void Build(CellIndex cellIndex)
        {
            var density = 0f;
            var localPos = CellIndex.PositionFromIndexFast(cellIndex) - _grassData.CellSize / 2;
            var localRect = new Rect(localPos * _terrainToPixel, _grassData.CellSize * _terrainToPixel);

            for (float pixelX = localRect.min.x; pixelX < localRect.max.x; pixelX += 1f)
            {
                for (float pixelY = localRect.min.y; pixelY < localRect.max.y; pixelY += 1f)
                {
                    density = DataLoader.Instance.GetDetailDensity(_grassDetailData.BrushIndex, pixelX, pixelY, _grassData.DetailResolution);
                    density *= _grassDetailData.ShowDensity * _grassData.DetailDensity;
                    if (density >= _grassDetailData.DetailThreshold)
                    {
                        Vector2 position = new Vector2(pixelX, pixelY) * _pixelToTerrain;
                        int instanceCount = Mathf.CeilToInt(density);
                        if (instanceCount > 0)
                        {
                            Build(cellIndex, position, instanceCount);
                        }
                    }
                }
            }
        }

        private void Build(CellIndex cellIndex, Vector2 position, int instanceCount)
        {
            for (int i = 0; i < instanceCount; i++)
            {
                var randomPos = EasyGrassUtility.Instance.GetRandomPosition(position, _pixelToTerrain, _actualCount++);
                var normalizedPos = new Vector2(randomPos.x / _grassData.TerrainSize.x, randomPos.y / _grassData.TerrainSize.z);
                var elementHeight = DataLoader.Instance.GetTerrainHeight(normalizedPos.x, normalizedPos.y,
                    _grassData.HeightmapResolution, _grassData.HeightmapResolution, _grassData.TerrainSize.y);
                //elementHeight = EasyGrass.Instance.UnityTerrain.terrainData.GetInterpolatedHeight(normalizedPos.x, normalizedPos.y);

                //var elementNormal = EasyGrassUtility.GetTerrainNormal(normalizedPos.x, normalizedPos.y,
                //    TerrainSize.x, TerrainSize.z);
                //Vector3 elementScale = Vector3.one;
                //                 elementScale.x = Mathf.Lerp(WidthScale.x, WidthScale.y, Random.Range(0f, 1f));
                //                 elementScale.y = Mathf.Lerp(HeightScale.x, HeightScale.y, Random.Range(0f, 1f));

                CellElement element = new CellElement(
                    new Vector3(randomPos.x, elementHeight + _grassDetailData.HeightOffset, randomPos.y)); //,
                    //elementNormal,
                    //elementScale);
                ElementList.Add(cellIndex, element);
            }
        }
    }
}