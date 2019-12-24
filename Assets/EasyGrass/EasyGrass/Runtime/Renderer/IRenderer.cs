using System;
using Unity.Collections;
using Unity.Jobs;
using UnityEngine;

namespace EasyFramework.Grass.Runtime
{
    public interface IRenderer : IDisposable
    {
        void OnInit(EasyGrass easyGrass, Camera camera);
        void OnBuild(JobHandle jobHandle, NativeArray<CellIndex> cellIndexList, NativeMultiHashMap<CellIndex, CellElement>[] cellElementList);
        void OnRender();
    }
}