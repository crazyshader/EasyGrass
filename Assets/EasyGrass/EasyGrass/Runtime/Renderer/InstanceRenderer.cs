using System.Collections.Generic;
using Unity.Collections;
using Unity.Jobs;
using UnityEngine;
using UnityEngine.Profiling;
using UnityEngine.Rendering;

namespace EasyFramework.Grass.Runtime
{
    public class InstanceRenderer : IRenderer
    {
        private NativeMultiHashMap<CellIndex, Matrix4x4>[] _cellMatrixList;
        private NativeList<Matrix4x4>[] _matrixList;
        public List<Matrix4x4>[][] _matrixListArray;
        private int[] _detailBatchCount;
        private const int _maxInstanceCount = 1023;
        private const int _maxBatchCount = 64;
        private BuildMesh _buildMesh;
        private EasyGrass _easyGrass;

        public void OnInit(EasyGrass easyGrass, Camera camera)
        {
            _easyGrass = easyGrass;
            _buildMesh = new BuildMesh();
            var maxCullCount = (_easyGrass.CellCount.x * _easyGrass.CellCount.y) / 2;            
            _matrixList = new NativeList<Matrix4x4>[_easyGrass.DetailCount];
            _detailBatchCount = new int[_easyGrass.DetailCount];
            _matrixListArray = new List<Matrix4x4>[_easyGrass.DetailCount][];
            _cellMatrixList = new NativeMultiHashMap<CellIndex, Matrix4x4>[_easyGrass.DetailCount];

            for (int i = 0; i < _easyGrass.DetailCount; i++)
            {
                _matrixList[i] = new NativeList<Matrix4x4>(2048, Allocator.Persistent);
                _cellMatrixList[i] = new NativeMultiHashMap<CellIndex, Matrix4x4>(maxCullCount, Allocator.Persistent);

                _matrixListArray[i] = new List<Matrix4x4>[_maxBatchCount];
                for (int j = 0; j < _maxBatchCount; j++)
                {
                    _matrixListArray[i][j] = new List<Matrix4x4>(_maxInstanceCount);
                }
            }
        }

        private void OnDestroy()
        {
            Dispose();
        }

        public void Dispose()
        {
            _buildMesh.Dispose();
            DataLoader.Instance.Dispose();
            EasyGrassUtility.Instance.Release();

            for (int i = 0; i < _easyGrass.DetailCount; i++)
            {
                if (_cellMatrixList[i].IsCreated)
                {
                    _cellMatrixList[i].Dispose();
                }
                if (_matrixList[i].IsCreated)
                {
                    _matrixList[i].Dispose();
                }
            }
        }

        public void OnBuild(JobHandle jobHandle, NativeArray<CellIndex> cellIndexList, NativeMultiHashMap<CellIndex, CellElement>[] cellElementList)
        {
            Profiler.BeginSample("Update Matrix Job");
            NativeArray<JobHandle> updateMatrixJob = new NativeArray<JobHandle>(_easyGrass.DetailCount, Allocator.Temp);
            for (int i = 0; i < _easyGrass.DetailCount; i++)
            {
                _matrixList[i].Clear();
                updateMatrixJob[i] = new UpdateMatrixJob
                {
                    TerrainPos = _easyGrass.EasyGrassData.TerrainPos,
                    CellIndexList = cellIndexList,
                    CellElementList = cellElementList[i],
                    CellMatrixList = _cellMatrixList[i],
                    MatrixList = _matrixList[i],
                }.Schedule(jobHandle);
            }
            var allUpdateMatrixJob = JobHandle.CombineDependencies(updateMatrixJob);
            allUpdateMatrixJob.Complete();
            Profiler.EndSample();

            Profiler.BeginSample("DataConvert");
            for (int i = 0; i < _easyGrass.DetailCount; i++)
            {
                NoAllocHelpers.DataConvert<Matrix4x4>(_matrixList[i], _maxInstanceCount-1, ref _matrixListArray[i], ref _detailBatchCount[i]);
            }
            Profiler.EndSample();
        }

        public void OnRender()
        {
            Profiler.BeginSample("LateUpdate");
            for (int i = 0; i < _easyGrass.DetailCount; i++)
            {
                var grassDetailData = _easyGrass.EasyGrassData.DetailDataList[i];
                for (int j = 0; j < _detailBatchCount[i]; j++)
                {
                    var matrixList = _matrixListArray[i][j];
                    Graphics.DrawMeshInstanced(
                        grassDetailData.UseQuad ? _buildMesh.BuildQuad() : grassDetailData.DetailMesh,
                        0,
                        grassDetailData.DetailMaterial,
                        matrixList,
                        null,
                        grassDetailData.CastShadows ? ShadowCastingMode.On : ShadowCastingMode.Off,
                        grassDetailData.ReceiveShadows,
                        grassDetailData.DetailLayer);
                }
            }
            Profiler.EndSample();
        }
    }
}
