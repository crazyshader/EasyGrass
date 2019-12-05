using System;
using System.IO;
using UnityEngine;

namespace EasyGrass
{
    //[ExecuteInEditMode]
    public class EasyGrass : MonoBehaviour, IDisposable
    {
        private EasyGrassRenderer[] _easyGrassRenderer = default;

        //[SerializeField] private Terrain _unityTerrain = default;
        //public Terrain UnityTerrain
        //{
        //    get => _unityTerrain;
        //    set => _unityTerrain = value;
        //}

        [SerializeField] private Camera _renderCamera = default;
        public Camera RenderCamera
        {
            get => _renderCamera;
            set
            {
                if (_renderCamera != value)
                {
                    _renderCamera = value;
                }
            }
        }

        [SerializeField] private EasyGrassData _unityTerrainData = default;
        public EasyGrassData TerrainData => _unityTerrainData;

        private void Awake()
        {
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
            if (this.isActiveAndEnabled && _easyGrassRenderer != null)
            {
                var rendererCount = _easyGrassRenderer.Length;
                if (RenderCamera != null)
                {
                    if (RenderCamera.transform.hasChanged)
                    {
                        for (int i = 0; i < rendererCount; ++i)
                        {
                            _easyGrassRenderer[i].OnBuild();
                        }
                        RenderCamera.transform.hasChanged = false;
                    }
                }
                for (int i = 0; i < rendererCount; ++i)
                {
                    _easyGrassRenderer[i].OnRender();
                }
            }
        }
    }
}
