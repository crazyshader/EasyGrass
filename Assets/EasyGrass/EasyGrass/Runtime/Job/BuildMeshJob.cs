using UnityEngine;
using Unity.Jobs;
using Unity.Collections;
using Unity.Burst;

namespace EasyFramework.Grass.Runtime
{
    [BurstCompile]
    public struct BuildMeshJob : IJob
    {
        [ReadOnly]
        public int MaxCountPreBatch;
        [ReadOnly]
        public Vector3 TerrainPos;
        [ReadOnly]
        public Quaternion CameraDir;

        [ReadOnly]
        public NativeArray<CellIndex> CellIndexList;
        [ReadOnly]
        public NativeMultiHashMap<CellIndex, CellElement> CellElementList;
        public NativeMultiHashMap<CellIndex, Vector3> ElementPositionList;
        public NativeList<Vector3> PositionList;

        [WriteOnly]
        public NativeList<Vector3> VertexList;
        [WriteOnly]
        public NativeList<Vector2> UVList;
        [WriteOnly]
        public NativeList<int> TriangleList;

        public void Execute()
        {
            Vector3 newPosition;
            NativeMultiHashMapIterator<CellIndex> iterator;

            for (int i = 0; i < CellIndexList.Length; i++)
            {
                var cellIndex = CellIndexList[i];
                if (cellIndex.state == -1)
                {
                    if (ElementPositionList.ContainsKey(cellIndex))
                    {
                        ElementPositionList.Remove(cellIndex);
                    }
                }
                else if (cellIndex.state == 1)
                {
                    if (!ElementPositionList.ContainsKey(cellIndex)
                        && CellElementList.ContainsKey(cellIndex))
                    {
                        var cellElementList = CellElementList.GetValuesForKey(cellIndex);
                        while (cellElementList.MoveNext())
                        {
                            var elementPos = TerrainPos + cellElementList.Current.position;
                            ElementPositionList.Add(cellIndex, elementPos);
                        }
                    }
                }

                if (ElementPositionList.ContainsKey(cellIndex))
                {
                    if (ElementPositionList.TryGetFirstValue(cellIndex, out newPosition, out iterator))
                    {
                        PositionList.Add(newPosition);
                        while (ElementPositionList.TryGetNextValue(out newPosition, ref iterator))
                        {
                            PositionList.Add(newPosition);
                        }
                    }
                }
            }

            for (int j = 0; j < PositionList.Length; j++)
            {
                BuildBuffer(PositionList[j], j);
            }
        }

        private void BuildBuffer(Vector3 position, int index)
        {
            var vIndex = (index % MaxCountPreBatch) * 4;

            var rightVec = CameraDir * Vector3.right;
            var upVec = CameraDir * Vector3.up;
            var scale = Vector3.one;
            var p1 = scale.x * -rightVec * 0.5f + scale.y * upVec;
            var p2 = scale.x * rightVec * 0.5f + scale.y * upVec;
            var p3 = scale.x * rightVec * 0.5f;
            var p4 = scale.x * -rightVec * 0.5f;

            VertexList.Add(position + p1);
            VertexList.Add(position + p2);
            VertexList.Add(position + p3);
            VertexList.Add(position + p4);
            UVList.Add(new Vector4(0f, 1f, 0, 0));
            UVList.Add(new Vector4(1f, 1f, 0, 0));
            UVList.Add(new Vector4(1f, 0f, 0, 0));
            UVList.Add(new Vector4(0f, 0f, 0, 0));
            TriangleList.Add(vIndex + 0);
            TriangleList.Add(vIndex + 1);
            TriangleList.Add(vIndex + 2);
            TriangleList.Add(vIndex + 2);
            TriangleList.Add(vIndex + 3);
            TriangleList.Add(vIndex + 0);
        }
    }
}