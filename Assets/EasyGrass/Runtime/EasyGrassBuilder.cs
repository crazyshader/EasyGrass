﻿using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using UnityEngine;

namespace EasyGrass
{
    public enum TextureChannel
    {
        R = 0,
        G,
        B,
        A
    }

    public struct Element
    {
        public readonly Vector3 position;
        public readonly Vector3 normal;
        public readonly Vector3 scale;

        public Element(Vector3 position, Vector3 normal, Vector3 scale)
        {
            this.position = position;
            this.normal = normal;
            this.scale = scale;
        }
    }

    public class MeshData
    {
        public Vector3[] vertices;
        public int[] triangles;
        public Vector4[] uvs;
        //public Color[] colors;
        public Vector3[] normals;

        public MeshData(int amount, int triangleCount)
        {
            var vertSize = triangleCount + 2;
            var triSize = triangleCount * 3;

            vertices = new Vector3[amount * vertSize];
            triangles = new int[amount * triSize];
            uvs = new Vector4[amount * vertSize];
            //colors = new Color[amount * vertSize];
            normals = new Vector3[amount * vertSize];
        }
    }

    public class EasyGrassBuilder
    {
        private EasyGrass _easyGrass;
        private GrassDetailData _unityDetailData;
        private EasyGrassData _unityTerrainData;
        private List<Texture2D> _detailMaps;
        private Vector3 _terrainPos;
        private Vector3 _terrainSize;
        private Vector2 _pixelToTerrain;
        private Vector2 _terrainToPixel;
        private Color[][] _detailDensity;

        static EasyGrassBuilder()
        {
            InitRandomCache();
        }

        public EasyGrassBuilder(EasyGrass easyGrass, GrassDetailData unityDetailData)
        {
            _easyGrass = easyGrass;
            _unityDetailData = unityDetailData;
            _unityTerrainData = easyGrass.TerrainData;
            _detailMaps = _unityTerrainData.DetailMapList;
            _terrainPos = _unityTerrainData.TerrainPos;
            _terrainSize = _unityTerrainData.TerrainSize;

            var detailResolution = (float)_unityTerrainData.DetailResolution;
            _pixelToTerrain = new Vector2(_terrainSize.x / detailResolution, _terrainSize.x / detailResolution);
            _terrainToPixel = new Vector2(detailResolution / _terrainSize.x , detailResolution / _terrainSize.x);

            var textureCount = _detailMaps.Count;
            _detailDensity = new Color[textureCount][];
            for (int i = 0; i < textureCount; i++)
            {
                _detailDensity[i] = _detailMaps[i].GetPixels();
            }
        }

        private static float[] _randomCache = new float[0];
        private static void InitRandomCache()
        {
            if (_randomCache.Length == 0)
            {
                _randomCache = new float[9999];
                System.Random random = new System.Random(9999);
                for (int i = 0; i < _randomCache.Length; i++)
                {
                    _randomCache[i] = (float)random.NextDouble();
                }
            }
        }

        private List<Tuple<Vector2,int>> GetPositionList(Rect cellRect)
        {
            var terrainXZPos = new Vector2(_terrainPos.x, _terrainPos.z);
            var localRect = new Rect(cellRect.min - terrainXZPos, cellRect.size);
            var localNormalizedRect = new Rect(localRect.position * _terrainToPixel, localRect.size * _terrainToPixel);

            float density = 0;
            var cameraPos = _easyGrass.RenderCamera.transform.position;
            List<Tuple<Vector2, int>> positionList = new List<Tuple<Vector2, int>>(1023);
            for (float pixelX = localNormalizedRect.min.x; pixelX < localNormalizedRect.max.x; pixelX += 1f)
            {
                for (float pixelY = localNormalizedRect.min.y; pixelY < localNormalizedRect.max.y; pixelY += 1f)
                {
                    var detailMapNumber = (_unityDetailData.BrushIndex % 16) / 4;
                    var detailChannelNum = _unityDetailData.BrushIndex % 4;
                    var v = _detailDensity[detailMapNumber][Mathf.RoundToInt(pixelY) * _easyGrass.TerrainData.DetailResolution + Mathf.RoundToInt(pixelX)];

                    switch ((TextureChannel)detailChannelNum)
                    {
                        case TextureChannel.R:
                            density = v.r;
                            break;
                        case TextureChannel.G:
                            density = v.g;
                            break;
                        case TextureChannel.B:
                            density = v.b;
                            break;
                        case TextureChannel.A:
                            density = v.a;
                            break;
                    }
                    if (density >= _unityDetailData.DetailThreshold)
                    {
                        Vector2 position = Vector2.Scale(new Vector2(pixelX, pixelY), _pixelToTerrain);
                        var normalizedPos = new Vector2(position.x / _terrainSize.x, position.y / _terrainSize.z);
                        //var terrainData = _massiveGrass.UnityTerrain.terrainData;
                        //var height = terrainData.GetInterpolatedHeight(normalizedPos.x, normalizedPos.y);
                        var height = EasyGrassUtility.GetTerrainHeight(normalizedPos.x, normalizedPos.y,
                            _easyGrass.TerrainData.HeightmapResolution, _easyGrass.TerrainData.HeightmapResolution,
                            _easyGrass.TerrainData.TerrainSize.y);
                        var worldPos = _terrainPos + new Vector3(position.x, height + _unityDetailData.HeightOffset, position.y);
                        var direction = worldPos - cameraPos;
                        var linearDensity = 1.0f - Mathf.Clamp01(direction.magnitude / _unityDetailData.CullDistance);

                        int instanceCount = Mathf.CeilToInt(density * linearDensity * _unityDetailData.ShowDensity * _unityTerrainData.DetailMaxDensity);
                        if (instanceCount > 0)
                        {
                            positionList.Add(new Tuple<Vector2, int>(position, instanceCount));
                        }
                    }
                }
            }

            return positionList;
        }

        public Mesh BuildMesh(List<Element> cellEmentList)
        {
            var cameraDir = _easyGrass.RenderCamera.transform.forward;
            cameraDir.y = 0;
            Quaternion camRotation = Quaternion.LookRotation(cameraDir);
            var elementCout = cellEmentList.Count;
            var meshData = new MeshData(elementCout, 2);

            for (int i = 0; i < elementCout; i++)
            {
                var normal = cellEmentList[i].normal;
                Quaternion rotation = camRotation * Quaternion.FromToRotation(Vector3.up, normal);

                var rightVec = rotation * Vector3.right;
                var upVec = rotation * Vector3.up;
                var scale = cellEmentList[i].scale;
                var p1 = scale.x * -rightVec * 0.5f + scale.y * upVec;
                var p2 = scale.x * rightVec * 0.5f + scale.y * upVec;
                var p3 = scale.x * rightVec * 0.5f;
                var p4 = scale.x * -rightVec * 0.5f;

                var color = Color.black;
                var vOrigin = i * 4;
                var iOrigin = i * 6;
                var worldPos = cellEmentList[i].position + _terrainPos;

                meshData.vertices[vOrigin + 0] = worldPos + p1;
                meshData.vertices[vOrigin + 1] = worldPos + p2;
                meshData.vertices[vOrigin + 2] = worldPos + p3;
                meshData.vertices[vOrigin + 3] = worldPos + p4;
                meshData.normals[vOrigin + 0] = rotation * Vector3.up; 
                meshData.normals[vOrigin + 1] = rotation * Vector3.up; 
                meshData.normals[vOrigin + 2] = rotation * Vector3.forward;
                meshData.normals[vOrigin + 3] = rotation * Vector3.forward;
                meshData.uvs[vOrigin + 0] = new Vector4(0f, 1f, _unityDetailData.CullDistance, _unityDetailData.CullDistance);
                meshData.uvs[vOrigin + 1] = new Vector4(1f, 1f, _unityDetailData.CullDistance, _unityDetailData.CullDistance);
                meshData.uvs[vOrigin + 2] = new Vector4(1f, 0f, _unityDetailData.CullDistance, _unityDetailData.CullDistance);
                meshData.uvs[vOrigin + 3] = new Vector4(0f, 0f, _unityDetailData.CullDistance, _unityDetailData.CullDistance);
                //meshData.colors[vOrigin + 0] = color;
                //meshData.colors[vOrigin + 1] = color;
                //meshData.colors[vOrigin + 2] = color;
                //meshData.colors[vOrigin + 3] = color;
                meshData.triangles[iOrigin + 0] = vOrigin + 0;
                meshData.triangles[iOrigin + 1] = vOrigin + 1;
                meshData.triangles[iOrigin + 2] = vOrigin + 2;
                meshData.triangles[iOrigin + 3] = vOrigin + 2;
                meshData.triangles[iOrigin + 4] = vOrigin + 3;
                meshData.triangles[iOrigin + 5] = vOrigin + 0;
            }


            var mesh = new Mesh();
            mesh.vertices = meshData.vertices;
            mesh.normals = meshData.normals;
            mesh.triangles = meshData.triangles;
            //mesh.colors = meshData.colors;
            mesh.SetUVs(0, meshData.uvs);
            return mesh;
        }

        public async Task<List<Element>> BuildElement(Rect cellRect)
        {
            var positionList = GetPositionList(cellRect);
            var instanceTotalCount = 0;
            var positionCount = positionList.Count;
            for (int i = 0; i < positionCount; i++)
            {
                instanceTotalCount += positionList[i].Item2;
            }

            var actualCount = 0;
            List<Element> elementList = new List<Element>(1024);
            await Task.Run(() =>
            {
                for (int i = 0; i < positionCount; i++)
                {
                    var position = positionList[i].Item1;
                    var instanceCount = positionList[i].Item2;
                    for (int j = 0; j < instanceCount; j++)
                    {
                        var element = BuildElement(position, actualCount++);
                        elementList.Add(element);
                    }
                }
            });

            return elementList;
        }

        public async Task<List<Matrix4x4>> BuildInstance(Rect cellRect)
        {
            var positionList = GetPositionList(cellRect);

            List<Matrix4x4> instanceMatrixList = new List<Matrix4x4>(1023);
            await Task.Run(() =>
            {
                for (int i = 0; i < positionList.Count; i++)
                {
                    var position = positionList[i].Item1;
                    var instanceCount = positionList[i].Item2;
                    for (int j = 0; j < instanceCount; j++)
                    {
                        var instanceMatrix = AddInstance(position, j);
                        instanceMatrix = Matrix4x4.Translate(_terrainPos) * instanceMatrix;
                        instanceMatrixList.Add(instanceMatrix);
                    }
                }
            });

            return instanceMatrixList;
        }
        
        private Vector2 GetRandPosition(Vector2 centerPos, int seed)
        {
            var randPos = centerPos;
            int x = (int)(centerPos.x * 1000f);
            int z = (int)(centerPos.y * 1000f);
            int seedX = (x ^ 2 * z + 2 * z ^ 2 * x + seed) % _randomCache.Length;
            int seedZ = (x ^ 2 * z + 2 * z ^ 2 * x + seed + x * 13) % _randomCache.Length;
            float randomNoiseX = _randomCache[seedX];
            float randomNoiseZ = _randomCache[seedZ];
            randomNoiseX = (randomNoiseX * 2f - 1);
            randomNoiseZ = (randomNoiseZ * 2f - 1);
            randPos.x += (_pixelToTerrain.x * 0.5f) * (1f + randomNoiseX);
            randPos.y += (_pixelToTerrain.y * 0.5f) * (1f + randomNoiseZ);
            return randPos;
        }

        private Element BuildElement(Vector2 centerPos, int index)
        {
            centerPos = GetRandPosition(centerPos, index);
            var normalizedPos = new Vector2(centerPos.x / _terrainSize.x, centerPos.y / _terrainSize.z);
            //var terrainData = _massiveGrass.UnityTerrain.terrainData;
            //var height = terrainData.GetInterpolatedHeight(normalizedPos.x, normalizedPos.y);
            var height = EasyGrassUtility.GetTerrainHeight(normalizedPos.x, normalizedPos.y,
                _easyGrass.TerrainData.HeightmapResolution, _easyGrass.TerrainData.HeightmapResolution,
                _easyGrass.TerrainData.TerrainSize.y);
            //var normal = terrainData.GetInterpolatedNormal(normalizedPos.x, normalizedPos.y);
            var normal = EasyGrassUtility.GetTerrainNormal(normalizedPos.x, normalizedPos.y,
                _easyGrass.TerrainData.HeightmapResolution, _easyGrass.TerrainData.HeightmapResolution);
            float perlinNoise = Mathf.PerlinNoise(_terrainPos.x + centerPos.x * _unityDetailData.NoiseSpread,
                _terrainPos.z + centerPos.y * _unityDetailData.NoiseSpread);
            Vector3 scale = Vector3.one;
            scale.x = Mathf.Lerp(_unityDetailData.WidthScale.x, _unityDetailData.WidthScale.y, perlinNoise);
            scale.y = Mathf.Lerp(_unityDetailData.HeightScale.x, _unityDetailData.HeightScale.y, perlinNoise);
            scale.z = scale.x;

            Element element = new Element(
                new Vector3(centerPos.x, height + _unityDetailData.HeightOffset, centerPos.y),
                normal,
                scale);
            return element;
        }

        private Matrix4x4 AddInstance(Vector2 centerPos, int seed)
        {
            centerPos = GetRandPosition(centerPos, seed);
            var normalizedPos = new Vector2(centerPos.x / _terrainSize.x, centerPos.y / _terrainSize.z);
            //var terrainData = _massiveGrass.UnityTerrain.terrainData;
            //var height = terrainData.GetInterpolatedHeight(normalizedPos.x, normalizedPos.y);
            var height = EasyGrassUtility.GetTerrainHeight(normalizedPos.x, normalizedPos.y,
                _easyGrass.TerrainData.HeightmapResolution, _easyGrass.TerrainData.HeightmapResolution,
                _easyGrass.TerrainData.TerrainSize.y);
            //var normal = terrainData.GetInterpolatedNormal(normalizedPos.x, normalizedPos.y);
            var normal = EasyGrassUtility.GetTerrainNormal(normalizedPos.x, normalizedPos.y,
                _easyGrass.TerrainData.HeightmapResolution, _easyGrass.TerrainData.HeightmapResolution);
            float perlinNoise = Mathf.PerlinNoise(_terrainPos.x + centerPos.x * _unityDetailData.NoiseSpread,
                _terrainPos.z + centerPos.y * _unityDetailData.NoiseSpread);
            Vector3 scale = Vector3.one;
            scale.x = Mathf.Lerp(_unityDetailData.WidthScale.x, _unityDetailData.WidthScale.y, perlinNoise);
            scale.y = Mathf.Lerp(_unityDetailData.HeightScale.x, _unityDetailData.HeightScale.y, perlinNoise);
            scale.z = scale.x;
            return Matrix4x4.TRS(new Vector3(centerPos.x, height + _unityDetailData.HeightOffset, centerPos.y),
                Quaternion.FromToRotation(Vector3.up, normal),
                scale);
        }
    }
}
