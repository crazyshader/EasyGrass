using System.Collections.Generic;
using UnityEngine;
using System;
using System.Threading.Tasks;
using System.Linq;
using UnityEngine.Rendering;

namespace EasyGrass
{
    internal class Request
    {
        public EasyGrassGrid.CellIndex index;
        public Rect rect;

        public Request(EasyGrassGrid.CellIndex index, Rect rect)
        {
            this.index = index;
            this.rect = rect;
        }
    }

    public class EasyGrassRenderer : IDisposable
    {
        private List<Element> _elementList = new List<Element>(1024);
        private Dictionary<EasyGrassGrid.CellIndex, List<Element>> _cellElementList
            = new Dictionary<EasyGrassGrid.CellIndex, List<Element>>(1024);
        private List<Matrix4x4> _instanceList = new List<Matrix4x4>(1024);
        private Dictionary<EasyGrassGrid.CellIndex, List<Matrix4x4>> _cellInstanceList
        = new Dictionary<EasyGrassGrid.CellIndex, List<Matrix4x4>>();
        private HashSet<EasyGrassGrid.CellIndex> _activeIndices
            = new HashSet<EasyGrassGrid.CellIndex>();
        private Dictionary<EasyGrassGrid.CellIndex, Request> _requestQueue
            = new Dictionary<EasyGrassGrid.CellIndex, Request>();

        private EasyGrass _massiveGrass;
        private GrassDetailData _unityDetailData;
        private EasyGrassGrid _grassGrid;
        private EasyGrassBuilder massiveBuilder;
        private const int _maxInstancePerPatch = 1022;
        private const int _maxElementPerPatch = 2048;
        private List<Mesh> _renderMeshList = new List<Mesh>();      

        public EasyGrassRenderer(int detailIndex, EasyGrass massiveGrass)
        {
            _massiveGrass = massiveGrass;
            _unityDetailData = massiveGrass.TerrainData.DetailDataList[detailIndex];
            _grassGrid = new EasyGrassGrid(massiveGrass, 
                Mathf.CeilToInt(massiveGrass.TerrainData.TerrainSize.x / Mathf.Min(32, massiveGrass.TerrainData.GridSize)));
            massiveBuilder = new EasyGrassBuilder(massiveGrass, massiveGrass.TerrainData.DetailDataList[detailIndex]);
        }

        public void OnRender()
        {
            if (_massiveGrass.TerrainData.InstanceDraw)
            {
                if (_cellInstanceList.Count == 0) return;
                _instanceList.Clear();
                foreach (var item in _cellInstanceList)
                {
                    _instanceList.AddRange(item.Value);
                }
                var length = _instanceList.Count();
                for (int beginIndex = 0; beginIndex < length; beginIndex = beginIndex + _maxInstancePerPatch)
                {
                    var endIndex = Mathf.Min(beginIndex + _maxInstancePerPatch, length - 1);
                    var instanceCount = endIndex - beginIndex + 1;
                    var cellInstance = _instanceList.GetRange(beginIndex, instanceCount);
                    Graphics.DrawMeshInstanced(_unityDetailData.DetailMesh,
                        0,
                        _unityDetailData.DetailMaterial,
                        cellInstance.ToArray(),
                        instanceCount,
                        null,
                        _unityDetailData.CastShadows ? ShadowCastingMode.On : ShadowCastingMode.Off,
                        _unityDetailData.ReceiveShadows,
                        _unityDetailData.DetailLayer);
                }
            }
            else
            {
                if (_massiveGrass.RenderCamera.transform.hasChanged)
                {
                    BuildMesh();
                    _massiveGrass.RenderCamera.transform.hasChanged = false;
                }

                foreach (var mesh in _renderMeshList)
                {
                    Graphics.DrawMesh(
                        mesh,
                        Vector3.zero,
                        Quaternion.identity,
                        _unityDetailData.DetailMaterial,
                        _unityDetailData.DetailLayer,
                        null,
                        0,
                        null,
                        _unityDetailData.CastShadows ? ShadowCastingMode.On : ShadowCastingMode.Off,
                        _unityDetailData.ReceiveShadows);
                }
            }
        }

        public void BuildMesh()
        {
            if (_cellElementList.Count == 0) return;
            _elementList.Clear();
            foreach (var item in _renderMeshList)
            {
                SafeDestroy(item);
            }
            _renderMeshList.Clear();
            foreach (var item in _cellElementList)
            {
                _elementList.AddRange(item.Value);
            }
            var length = _elementList.Count();
            for (int beginIndex = 0; beginIndex < length; beginIndex = beginIndex + _maxElementPerPatch)
            {
                var endIndex = Mathf.Min(beginIndex + _maxElementPerPatch, length - 1);
                var elementCount = endIndex - beginIndex + 1;
                var cellEmentList = _elementList.GetRange(beginIndex, elementCount);
                var mesh = massiveBuilder.BuildMesh(cellEmentList);
                _renderMeshList.Add(mesh);
                Graphics.DrawMesh(
                    mesh,
                    Vector3.zero,
                    Quaternion.identity,
                    _unityDetailData.DetailMaterial,
                    _unityDetailData.DetailLayer,
                    null,
                    0,
                    null,
                    _unityDetailData.CastShadows ? ShadowCastingMode.On : ShadowCastingMode.Off,
                    _unityDetailData.ReceiveShadows);
            }
        }

        public async void OnBuild()
        {
            await _grassGrid.OnBuild(_massiveGrass.RenderCamera.transform.position, _unityDetailData.CullDistance, this);
        }

        private async Task ProcessQueue()
        {
            while (_requestQueue.Count > 0)
            {
                //var processSize = Mathf.Min(50, Mathf.CeilToInt(_requestQueue.Count));
                var processSize = Mathf.Max(1, Mathf.CeilToInt(_requestQueue.Count));
                var tasks = _requestQueue.Take(processSize).Select(x => Build(x.Key));
                await Task.WhenAll(tasks);
            }

            if (!_massiveGrass.TerrainData.InstanceDraw)
            {
                BuildMesh();
            }
        }

        private async Task Build(EasyGrassGrid.CellIndex index)
        {
            if (_massiveGrass.TerrainData.InstanceDraw)
            {
                var cellRect = _requestQueue[index].rect;
                var instanceMatrixList = await massiveBuilder.BuildInstance(cellRect);
                if (!_activeIndices.Contains(index))
                {
                    if (_requestQueue.ContainsKey(index))
                        _requestQueue.Remove(index);
                    return;
                }

                if (instanceMatrixList != null && instanceMatrixList.Count > 0)
                {
                    _cellInstanceList[index] = instanceMatrixList;
                }
                _requestQueue.Remove(index);
            }
            else
            {
                var cellRect = _requestQueue[index].rect;
                var elementList = await massiveBuilder.BuildElement(cellRect);
                if (!_activeIndices.Contains(index))
                {
                    if (_requestQueue.ContainsKey(index))
                        _requestQueue.Remove(index);
                    return;
                }

                if (elementList != null && elementList.Count > 0)
                {
                    _cellElementList[index] = elementList;
                }

                _requestQueue.Remove(index);
            }
        }

        private void SafeDestroy(Mesh mesh)
        {
            if (Application.isPlaying)
                UnityEngine.Object.Destroy(mesh);
            else
                UnityEngine.Object.DestroyImmediate(mesh);
        }

        public void Dispose()
        {
            _activeIndices.Clear();
            _requestQueue.Clear();
            if (_massiveGrass.TerrainData.InstanceDraw)
            {
                _cellInstanceList.Clear();
            }
            else
            {
                foreach (var mesh in _renderMeshList)
                    SafeDestroy(mesh);
                _renderMeshList.Clear();
                _cellElementList.Clear();
            }
        }

#pragma warning disable 4014
        public void Create(EasyGrassGrid.CellIndex index, Rect rect)
        {
            if (_activeIndices.Contains(index)) return;

            _activeIndices.Add(index);
            if (!_requestQueue.ContainsKey(index))
            {
                _requestQueue[index] = (new Request(index, rect));
                if (_requestQueue.Count == 1)
                    ProcessQueue();
            }
        }
#pragma warning restore 4014

        public void Remove(EasyGrassGrid.CellIndex index)
        {
            if (!_activeIndices.Contains(index)) return;

            if (_massiveGrass.TerrainData.InstanceDraw)
            {
                if (_cellInstanceList.ContainsKey(index))
                {
                    _cellInstanceList.Remove(index);
                }
            }
            else
            {
                if (_cellElementList.ContainsKey(index))
                {
                    _cellElementList.Remove(index);
                }
            }

            _activeIndices.Remove(index);
        }
    }
}

