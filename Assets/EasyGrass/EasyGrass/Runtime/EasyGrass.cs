using System;
using System.IO;
using Unity.Collections;
using Unity.Jobs;
using UnityEngine;
using UnityEngine.Profiling;

namespace EasyFramework.Grass.Runtime
{
    //[ExecuteInEditMode]
    public class EasyGrass : MonoBehaviour, IDisposable
    {
        //[SerializeField] private Terrain _unityTerrain;
        //public Terrain UnityTerrain
        //{
        //    get => _unityTerrain;
        //    set => _unityTerrain = value;
        //}

        [SerializeField] private Camera _currentCamera;
        public Camera CurrentCamera
        {
            get
            {
                if (_currentCamera == null)
                {
                    _currentCamera = Camera.main;
                }
                return _currentCamera;
            }
            set
            {
                if (_currentCamera != value)
                {
                    _currentCamera = value;
                }
            }
        }

        [SerializeField] private EasyGrassData _easyGrassData = default;
        public EasyGrassData EasyGrassData => _easyGrassData;
        public int DetailCount { get; private set; }
        public Vector2Int CellCount { get; private set; }

        private Vector3 _lastPosition;
        private Quaternion _lastRotation;
        private NativeArray<int> _cullResult;
        private NativeList<CellIndex> _lastCellIndexList;
        private NativeList<CellIndex> _cellIndexList;
        private NativeMultiHashMap<CellIndex, CellElement>[] _cellElementList;
        private BuildElement[] _buildElement;
        private IRenderer _renderer;

        private void Awake()
        {
            EasyGrassUtility.Instance.InitRandomCache();
            CellIndex.CellSize = EasyGrassData.CellSize;
            CellIndex.TerrainPos = EasyGrassData.TerrainPos;
            CellIndex.TerrainSize = EasyGrassData.TerrainSize;
            CellIndex.HeightmapResolution = EasyGrassData.HeightmapResolution;
            DetailCount = EasyGrassData.DetailDataList.Count;
            CellCount = new Vector2Int(
                Mathf.CeilToInt(EasyGrassData.TerrainSize.x / EasyGrassData.CellSize.x),
                Mathf.CeilToInt(EasyGrassData.TerrainSize.z / EasyGrassData.CellSize.y));

            if (_easyGrassData.InstanceDraw)
            {
                _renderer = new InstanceRenderer();
            }
            else
            {
                _renderer = new CombineRenderer();
            }
            _renderer.OnInit(this, CurrentCamera);

            _lastPosition = Vector3.one * float.MaxValue;
            _lastRotation = Quaternion.AngleAxis(-90f, Vector3.right);
            var maxCullCount = (CellCount.x * CellCount.y) / 2;
            _cullResult = new NativeArray<int>(1, Allocator.Persistent);
            _lastCellIndexList = new NativeList<CellIndex>(Allocator.Persistent);
            _cellIndexList = new NativeList<CellIndex>(Allocator.Persistent);
            _cellElementList = new NativeMultiHashMap<CellIndex, CellElement>[DetailCount];
            for (int i = 0; i < DetailCount; i++)
            {
                _cellElementList[i] = new NativeMultiHashMap<CellIndex, CellElement>(maxCullCount, Allocator.Persistent);
            }

            var heightmapPath = Path.Combine(Application.streamingAssetsPath, EasyGrassData.HeightDataPath);
            StartCoroutine(DataLoader.Instance.LoadHeightData(heightmapPath, EasyGrassData.HeightmapResolution, EasyGrassData.HeightmapResolution, LoadHeightDataFinished));
        }

        private void LoadHeightDataFinished()
        {
            var detailDataPath = Path.Combine(Application.streamingAssetsPath, EasyGrassData.DetailDataPath);
            StartCoroutine(DataLoader.Instance.LoadDetailData(detailDataPath, DetailCount, EasyGrassData.DetailResolution, LoadDetailDataFinished));
        }

        private void LoadDetailDataFinished()
        {
            _buildElement = new BuildElement[DetailCount];
            for (int i = 0; i < DetailCount; i++)
            {
                _buildElement[i] = new BuildElement(_easyGrassData, i);
                _buildElement[i].Build();
                //_buildElement[i].Count();
            }
        }

        private void OnDestroy()
        {
            Dispose();
        }

        public void Dispose()
        {
            DataLoader.Instance.Dispose();
            EasyGrassUtility.Instance.Release();

            _renderer.Dispose();
            if (_cellIndexList.IsCreated)
            {
                _cellIndexList.Dispose();
            }
            if (_lastCellIndexList.IsCreated)
            {
                _lastCellIndexList.Dispose();
            }
            if (_cullResult.IsCreated)
            {
                _cullResult.Dispose();
            }

            for (int i = 0; i < DetailCount; i++)
            {
                _buildElement[i].Dispose();
                if (_cellElementList[i].IsCreated)
                {
                    _cellElementList[i].Dispose();
                }
            }
        }

        private void Update()
        {
            if (!isActiveAndEnabled)
                return;
            var camera = CurrentCamera;
            if (camera == null)
                return;
            if (_buildElement == null)
                return;

            var camTransform = camera.transform;
            var direct = _lastPosition - camTransform.position;
            var angle = Quaternion.Angle(_lastRotation, camTransform.rotation);
            if (direct.sqrMagnitude < 1.0f && angle < 5.0f)
                return;
            _lastPosition = camTransform.position;
            _lastRotation = camTransform.rotation;

            Profiler.BeginSample("CameraCullJob");
            _cellIndexList.Clear();
            var cameraJobHandle = new CameraCullJob()
            {
                TerrainPos = EasyGrassData.TerrainPos,
                CullResult = _cullResult,
                CellCount = CellCount,
                CellSize = EasyGrassData.CellSize,
                CameraPos = camTransform.position,
                CullDistance = EasyGrassData.CullDistance,
                WorldToProjectionMatrix = camera.projectionMatrix * camera.worldToCameraMatrix,
                LastCellList = _lastCellIndexList,
                CellIndexList = _cellIndexList,
            }.Schedule();
            cameraJobHandle.Complete();
            Profiler.EndSample();

            if (_cullResult[0] == 1)
            {
                Profiler.BeginSample("Build Element Job");
                var cellIndexList = _cellIndexList.AsDeferredJobArray();
                NativeArray<JobHandle> buildElementJob = new NativeArray<JobHandle>(DetailCount, Allocator.Temp);
                for (int i = 0; i < DetailCount; i++)
                {
                    buildElementJob[i] = new BuildElementJob
                    {
                        ElementList = _buildElement[i].ElementList,
                        CellIndexList = cellIndexList,
                        CellElementList = _cellElementList[i],
                    }.Schedule(cameraJobHandle);
                }
                var allBuildElementJob = JobHandle.CombineDependencies(buildElementJob);
                allBuildElementJob.Complete();
                Profiler.EndSample();

                _renderer.OnBuild(allBuildElementJob, cellIndexList, _cellElementList);
            }
        }

        void LateUpdate()
        {
            if (!isActiveAndEnabled)
                return;

            Profiler.BeginSample("LateUpdate");
            _renderer.OnRender();
            Profiler.EndSample();
        }
    }
}
