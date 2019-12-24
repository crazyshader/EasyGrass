using UnityEngine;
using Unity.Jobs;
using Unity.Collections;
using Unity.Burst;

namespace EasyFramework.Grass.Runtime
{
    [BurstCompile]
    public struct UpdateMatrixJob : IJob
    {
        [ReadOnly]
        public Vector3 TerrainPos;
        //[ReadOnly]
        //public Vector3 CameraPos;
        //[ReadOnly]
        //public float ShowDistance;

        [ReadOnly]
        public NativeArray<CellIndex> CellIndexList;
        [WriteOnly]
        public NativeList<Matrix4x4> MatrixList;
        [ReadOnly]
        public NativeMultiHashMap<CellIndex, CellElement> CellElementList;
        public NativeMultiHashMap<CellIndex, Matrix4x4> CellMatrixList;

        //private float EaseIn_Exponential(float t)
        //{
        //    return t == 0f ? 0f : Mathf.Pow(1024f, t - 1f);
        //}

        public void Execute()
        {
            Matrix4x4 newMatrix;
            NativeMultiHashMapIterator<CellIndex> iterator;

            for (int i = 0; i < CellIndexList.Length; i++)
            {
                var cellIndex = CellIndexList[i];
                if (cellIndex.state == -1)
                {
                    if (CellMatrixList.ContainsKey(cellIndex))
                    {
                        CellMatrixList.Remove(cellIndex);
                    }
                }
                else if (cellIndex.state == 1)
                {
                    if (!CellMatrixList.ContainsKey(cellIndex)
                        && CellElementList.ContainsKey(cellIndex))
                    {
                        var cellElementList = CellElementList.GetValuesForKey(cellIndex);
                        while (cellElementList.MoveNext())
                        {
                            var elementPos = TerrainPos + cellElementList.Current.position;
                            //var direction = elementPos - CameraPos;
                            //if (direction.sqrMagnitude <= (ShowDistance * ShowDistance))
                            {
                                var matrix = Matrix4x4.Translate(elementPos);
                                CellMatrixList.Add(cellIndex, matrix);
                            }
                        }
                    }
                }

                if (CellMatrixList.ContainsKey(cellIndex))
                {
                    if (CellMatrixList.TryGetFirstValue(cellIndex, out newMatrix, out iterator))
                    {
                        MatrixList.Add(newMatrix);
                        while (CellMatrixList.TryGetNextValue(out newMatrix, ref iterator))
                        {
                            MatrixList.Add(newMatrix);
                        }
                    }
                }
            }
        }
    }
}