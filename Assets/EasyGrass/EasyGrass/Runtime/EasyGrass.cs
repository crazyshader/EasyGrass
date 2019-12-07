using System;
using System.IO;
using UnityEngine;

namespace EasyGrass
{
    //[ExecuteInEditMode]
    public class EasyGrass : MonoBehaviour, IDisposable
    {
        public static EasyGrass Instance { get; private set; }

        private EasyGrassRenderer[] _easyGrassRenderer = default;

        //[SerializeField] private Terrain _unityTerrain = default;
        //public Terrain UnityTerrain
        //{
        //    get => _unityTerrain;
        //    set => _unityTerrain = value;
        //}

        [SerializeField] private Camera _currentCamera = default;
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

        [SerializeField] private EasyGrassData _unityTerrainData = default;
        public EasyGrassData TerrainData => _unityTerrainData;

        private void Awake()
        {
            if (Instance != null && Instance != this)
            {
                Destroy(this.gameObject);
            }
            Instance = this;

            var heightmapPath = Path.Combine(Application.streamingAssetsPath, _unityTerrainData.HeightmapPath);
            EasyGrassUtility.LoadHeightmap(heightmapPath, _unityTerrainData.HeightmapResolution, _unityTerrainData.HeightmapResolution);
            //MassiveGrassUtility.LoadHeightmap(_unityTerrainData.HeightMap);
            EasyGrassUtility.LoadNormalmap(_unityTerrainData.NormalMap);

            var detailCount = _unityTerrainData.DetailDataList.Count;
            _easyGrassRenderer = new EasyGrassRenderer[detailCount];
            for (int i = 0; i < detailCount; ++i)
            {
                _easyGrassRenderer[i] = new EasyGrassRenderer(i, this);
            }
        }

        public void Dispose()
        {
            var rendererCount = _easyGrassRenderer.Length;
            for (int i = 0; i < rendererCount; ++i)
            {
                _easyGrassRenderer[i].Dispose();
                _easyGrassRenderer[i] = null;
            }
        }

        private void Update()
        {
            var camera = CurrentCamera;
            if (camera == null)
                return;
            if (!this.isActiveAndEnabled)
                return;

            var rendererCount = _easyGrassRenderer.Length;
            if (camera.transform.hasChanged)
            {
                for (int i = 0; i < rendererCount; ++i)
                {
                    _easyGrassRenderer[i].OnBuild();
                }
                camera.transform.hasChanged = false;
            }
            for (int i = 0; i < rendererCount; ++i)
            {
                _easyGrassRenderer[i].OnRender();
            }
        }
    }
}
