using System.Collections.Generic;
using Unity.Collections;
using Unity.Jobs;
using UnityEngine;
using UnityEngine.Profiling;
using UnityEngine.Rendering;

namespace EasyFramework.Grass.Runtime
{
    public class CombineRenderer : IRenderer
    {
        private NativeMultiHashMap<CellIndex, Vector3>[] _elementPositionList;
        private NativeList<Vector3>[] _positionList;
        private NativeList<Vector3>[] _vertexList;
        private NativeList<Vector2>[] _uvList;
        private NativeList<int>[] _triangleList;
        private Dictionary<int, int> _meshCountList;
        private List<int> _triangleArray;
        private Mesh[] _meshCacheList;
        private int meshIndex = 0;
        private const int _maxBatchCount = 64;
        private EasyGrass _easyGrass;
        private Camera _camera;

        public void OnInit(EasyGrass easyGrass, Camera camera)
        {
            _easyGrass = easyGrass;
            _camera = camera;

            var maxCullCount = (_easyGrass.CellCount.x * _easyGrass.CellCount.y) / 2;
            _meshCountList = new Dictionary<int, int>(_easyGrass.DetailCount);
            _vertexList = new NativeList<Vector3>[_easyGrass.DetailCount];
            _uvList = new NativeList<Vector2>[_easyGrass.DetailCount];
            _triangleList = new NativeList<int>[_easyGrass.DetailCount];
            _positionList = new NativeList<Vector3>[_easyGrass.DetailCount];
            _elementPositionList = new NativeMultiHashMap<CellIndex, Vector3>[_easyGrass.DetailCount];
            _triangleArray = new List<int>();

            for (int i = 0; i < _easyGrass.DetailCount; i++)
            {
                _meshCountList[i] = 0;
                _vertexList[i] = new NativeList<Vector3>(1024, Allocator.Persistent);
                _uvList[i] = new NativeList<Vector2>(1024, Allocator.Persistent);
                _triangleList[i] = new NativeList<int>(1024, Allocator.Persistent);
                _positionList[i] = new NativeList<Vector3>(1024, Allocator.Persistent);
                _elementPositionList[i] = new NativeMultiHashMap<CellIndex, Vector3>(maxCullCount, Allocator.Persistent);
            }

            _meshCacheList = new Mesh[_maxBatchCount];
            for (int k = 0; k < _maxBatchCount; k++)
            {
                _meshCacheList[k] = new Mesh();
                _meshCacheList[k].MarkDynamic();
                _meshCacheList[k].indexFormat = IndexFormat.UInt16;
            }
        }

        private void OnDestroy()
        {
            Dispose();
        }

        public void Dispose()
        {
            for (int j = 0; j < _maxBatchCount; j++)
            {
                SafeDestroy(_meshCacheList[j]);
            }

            for (int i = 0; i < _easyGrass.DetailCount; i++)
            {
                if (_elementPositionList[i].IsCreated)
                {
                    _elementPositionList[i].Dispose();
                }
                if (_positionList[i].IsCreated)
                {
                    _positionList[i].Dispose();
                }
                if (_vertexList[i].IsCreated)
                {
                    _vertexList[i].Dispose();
                }
                if (_uvList[i].IsCreated)
                {
                    _uvList[i].Dispose();
                }
                if (_triangleList[i].IsCreated)
                {
                    _triangleList[i].Dispose();
                }
            }
        }
        private void SafeDestroy(Mesh mesh)
        {
            if (mesh == null)
                return;
            if (Application.isPlaying)
                GameObject.Destroy(mesh);
            else
                GameObject.DestroyImmediate(mesh);
        }

        public void OnBuild(JobHandle jobHandle, NativeArray<CellIndex> cellIndexList, NativeMultiHashMap<CellIndex, CellElement>[] cellElementList)
        {
            Profiler.BeginSample("Build Mesh Job");
            if (_camera == null) _camera = _easyGrass.CurrentCamera;
            var maxCountPerPatch = _easyGrass.EasyGrassData.MaxCountPerPatch;
            var cameraDir = Quaternion.LookRotation(_camera.transform.forward);
            NativeArray<JobHandle> buildMeshJob = new NativeArray<JobHandle>(_easyGrass.DetailCount, Allocator.Temp);

            for (int i = 0; i < _easyGrass.DetailCount; i++)
            {
                _positionList[i].Clear();
                _vertexList[i].Clear();
                _uvList[i].Clear();
                _triangleList[i].Clear();
                buildMeshJob[i] = new BuildMeshJob
                {
                    MaxCountPreBatch = maxCountPerPatch,
                    TerrainPos = _easyGrass.EasyGrassData.TerrainPos,
                    CameraDir = cameraDir,
                    CellIndexList = cellIndexList,
                    CellElementList = cellElementList[i],
                    ElementPositionList = _elementPositionList[i],
                    PositionList = _positionList[i],
                    VertexList = _vertexList[i],
                    UVList = _uvList[i],
                    TriangleList = _triangleList[i],
                }.Schedule(jobHandle);
            }
            var allBuildMeshJob = JobHandle.CombineDependencies(buildMeshJob);
            allBuildMeshJob.Complete();
            Profiler.EndSample();

            Profiler.BeginSample("DataConvert");
            meshIndex = 0;
            for (int i = 0; i < _easyGrass.DetailCount; i++)
            {
                _meshCountList[i] = 0;
                if (_triangleList[i].Length == 0)
                    continue;

                int vertexIndex = 0;
                int triangleIndex = 0;
                var vertexTotalCount = _vertexList[i].Length;
                var triangleTotalCount = _triangleList[i].Length;
                var vertexStride = maxCountPerPatch * 4;
                var triangleStride = maxCountPerPatch * 6;
                _triangleArray.Clear();
                NoAllocHelpers.DataConvert(_triangleList[i], ref _triangleArray);

                while (vertexIndex < vertexTotalCount && triangleIndex < triangleTotalCount)
                {
                    var endIndex = Mathf.Min(vertexIndex + vertexStride - 1, vertexTotalCount - 1);
                    var indexLength = endIndex - vertexIndex + 1;
                    _meshCacheList[meshIndex].Clear();
                    _meshCacheList[meshIndex].SetVertices<Vector3>(_vertexList[i], vertexIndex, indexLength);
                    _meshCacheList[meshIndex].SetUVs<Vector2>(0, _uvList[i], vertexIndex, indexLength);

                    endIndex = Mathf.Min(triangleIndex + triangleStride - 1, triangleTotalCount - 1);
                    indexLength = endIndex - triangleIndex + 1;
                    _meshCacheList[meshIndex].SetTriangles(_triangleArray, triangleIndex, indexLength, 0, false);

                    vertexIndex = vertexIndex + vertexStride;
                    triangleIndex = triangleIndex + triangleStride;
                    _meshCountList[i]++;
                    meshIndex++;
                }
            }
            Profiler.EndSample();
        }

        public void OnRender()
        {
            Profiler.BeginSample("LateUpdate");
            meshIndex = 0;
            for (int i = 0; i < _easyGrass.DetailCount; i++)
            {
                var grassDetailData = _easyGrass.EasyGrassData.DetailDataList[i];
                for (int j = 0; j < _meshCountList[i]; j++)
                {
                    Graphics.DrawMesh(
                        _meshCacheList[meshIndex++],
                        Vector3.zero,
                        Quaternion.identity,
                        grassDetailData.DetailMaterial,
                        grassDetailData.DetailLayer,
                        null,
                        0,
                        null,
                        grassDetailData.CastShadows ? ShadowCastingMode.On : ShadowCastingMode.Off,
                        grassDetailData.ReceiveShadows);
                }
            }
            Profiler.EndSample();
        }
    }
}
