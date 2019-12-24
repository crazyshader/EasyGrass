using UnityEngine;
using Unity.Jobs;
using Unity.Collections;
using Unity.Burst;

namespace EasyFramework.Grass.Runtime
{
    //[BurstCompile]
    public struct CameraCullJob : IJob
    {
        [ReadOnly]
        public Vector3 TerrainPos;
        [ReadOnly]
        public Vector2Int CellCount;
        [ReadOnly]
        public Vector2Int CellSize;
        [ReadOnly]
        public Vector3 CameraPos;
        [ReadOnly]
        public float CullDistance;
        [ReadOnly]
        public Matrix4x4 WorldToProjectionMatrix;

        public NativeArray<int> CullResult;
        public NativeList<CellIndex> LastCellList;
        public NativeList<CellIndex> CellIndexList;

        public void Execute()
        {
            var rectMinIndex = CellIndex.IndexFromPosition(CameraPos - Vector3.one * CullDistance - TerrainPos);
            var rectMaxIndex = CellIndex.IndexFromPosition(CameraPos + Vector3.one * CullDistance - TerrainPos);
            var planes = GeometryUtility.CalculateFrustumPlanes(WorldToProjectionMatrix);

            for (var x = rectMinIndex.x; x < rectMaxIndex.x; x++)
            {
                if (x < 0 || CellCount.x <= x) continue;
                for (var y = rectMinIndex.y; y < rectMaxIndex.y; y++)
                {
                    if (y < 0 || CellCount.y <= y) continue;
                    var cellIndex = new CellIndex(x, y);
                    var cellPos = TerrainPos + CellIndex.PositionFromIndex(cellIndex);
                    var direction = cellPos - CameraPos;
                    var bounds = new Bounds(cellPos, new Vector3(CellSize.x, Mathf.Max(CellSize.x, CellSize.y), CellSize.y));
                    if (direction.magnitude <= CullDistance
                        && GeometryUtility.TestPlanesAABB(planes, ref bounds) != GeometryUtility.TestPlanesResults.Outside)
                    {
                        CellIndexList.Add(cellIndex);
                    }
                }
            }

            CullResult[0] = 0;
            for (int i = 0; i < CellIndexList.Length; i++)
            {
                var cellIndex = CellIndexList[i];
                if (!LastCellList.Contains(cellIndex))
                {
                    CullResult[0] = 1;
                    cellIndex.state = 1;
                    CellIndexList[i] = cellIndex;
                }
                else
                {
                    cellIndex.state = 0;
                    CellIndexList[i] = cellIndex;
                }
            }
            for (int j = 0; j < LastCellList.Length; j++)
            {
                var cellIndex = LastCellList[j];
                if (!CellIndexList.Contains(cellIndex))
                {
                    CullResult[0] = 1;
                    cellIndex.state = -1;
                    CellIndexList.Add(cellIndex);
                }
                else
                {
                    cellIndex.state = 0;
                    var index = CellIndexList.IndexOf(cellIndex);
                    CellIndexList[index] = cellIndex;
                }
            }

            LastCellList.Clear();
            for (int k = 0; k < CellIndexList.Length; k++)
            {
                var cellIndex = CellIndexList[k];
                if (cellIndex.state != -1)
                {
                    LastCellList.Add(cellIndex);
                }
            }
        }
    }
}
