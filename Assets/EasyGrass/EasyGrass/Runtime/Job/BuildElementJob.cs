using Unity.Jobs;
using Unity.Collections;
using Unity.Burst;

namespace EasyFramework.Grass.Runtime
{
    [BurstCompile]
    public struct BuildElementJob : IJob
    {
        [ReadOnly]
        public NativeArray<CellIndex> CellIndexList;
        [ReadOnly]
        public NativeMultiHashMap<CellIndex, CellElement> ElementList;
        public NativeMultiHashMap<CellIndex, CellElement> CellElementList;

        public void Execute()
        {
            for (int i = 0; i < CellIndexList.Length; i++)
            {
                var cellIndex = CellIndexList[i];
                if (cellIndex.state == -1)
                {
                    if (CellElementList.ContainsKey(cellIndex))
                    {
                        CellElementList.Remove(cellIndex);
                    }
                }
                else if (cellIndex.state == 1)
                {
                    if (!CellElementList.ContainsKey(cellIndex))
                    {
                        var eleCount = 0;
                        var cellElementList = ElementList.GetValuesForKey(cellIndex);
                        while(cellElementList.MoveNext())
                        {
                            ++eleCount;
                            CellElementList.Add(cellIndex, cellElementList.Current);
                        }

                        eleCount = 0;
                    }
                }
            }
        }
    }
}