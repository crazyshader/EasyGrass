using System.Collections;
using System.IO;
using UnityEngine;
using UnityEngine.Networking;
using Unity.Collections;
using System;

namespace EasyFramework.Grass.Runtime
{
    public class DataLoader : IDisposable
    {
        private DataLoader() { }
        private static DataLoader _instance;
        public static DataLoader Instance
        {
            get
            {
                if (_instance == null)
                {
                    _instance = new DataLoader();
                }
                return _instance;
            }
        }

        private NativeArray<ushort> _heightmap;
        //private Vector3[,] _normalmap;
        private NativeArray<byte> _detailDensity;

        public void Dispose()
        {
            if (_heightmap.IsCreated)
            {
                _heightmap.Dispose();
            }
            if (_detailDensity.IsCreated)
            {
                _detailDensity.Dispose();
            }
        }

        public IEnumerator LoadHeightData(string filePath, int width, int height, Action finishCallback)
        {
            void LoadHeightmap(BinaryReader reader)
            {
                for (int y = 0; y < height; y++)
                {
                    for (int x = 0; x < width; x++)
                    {
                        _heightmap[y * width + x] = reader.ReadUInt16();
                    }
                }
            }

            _heightmap = new NativeArray<ushort>(height * width, Allocator.Persistent);
            if (filePath.Contains("://") || filePath.Contains(":///"))
            {
                UnityWebRequest www = UnityWebRequest.Get(filePath);
                yield return www.SendWebRequest();
                if (www.isNetworkError || www.isHttpError)
                {
                    Debug.LogError($"LoadHeightmap:{www.error}");
                }
                else
                {
                    byte[] results = www.downloadHandler.data;
                    using (var stream = new MemoryStream(results))
                    using (var reader = new BinaryReader(stream))
                    {
                        LoadHeightmap(reader);
                    }
                }
            }
            else
            {
                using (var file = File.OpenRead(filePath))
                using (var reader = new BinaryReader(file))
                {
                    LoadHeightmap(reader);
                }
            }

            finishCallback();
            yield return null;
        }

        public float GetTerrainHeight(float x, float y, float width, float length, float height)
        {
            return GetTerrainHeight(Mathf.CeilToInt(x * width), Mathf.CeilToInt(y * length), Mathf.CeilToInt(width), height);
        }

        public float GetTerrainHeight(int xIndex, int yIndex, int width, float height)
        {
            if (_heightmap == null || _heightmap.Length == 0)
            {
                return 0f;
            }
            var terrainHeight = (float)_heightmap[yIndex * width + xIndex] / 0xFFFF * height;
            return terrainHeight;
        }

        /*
        public Vector3[,] LoadNormalmap(Texture2D normalMap)
        {
            _normalmap = new Vector3[normalMap.width, normalMap.height];
            for (int x = 0; x < normalMap.width; x++)
            {
                for (int y = 0; y < normalMap.height; y++)
                {
                    var normal = normalMap.GetPixel(x, y);
                    _normalmap[x, y] = new Vector3(normal.r * 2f - 1f, normal.g * 2f - 1f, normal.b * 2f - 1f);
                }
            }

            return _normalmap;
        }

        public Vector3 GetTerrainNormal(float x, float y, float width, float length)
        {
            return Vector3.up;

            //var xIndex = Mathf.CeilToInt(x * width);
            //var yIndex = Mathf.CeilToInt(y * length);
            //if (_normalmap == null 
            //    || xIndex < 0 || xIndex >= _normalmap.GetLength(0)
            //    || yIndex < 0 || yIndex >= _normalmap.GetLength(1))
            //{
            //    return Vector3.up;
            //}
            //return _normalmap[xIndex, yIndex];
        }
        */

        public IEnumerator LoadDetailData(string filePath, int layerCount, int detailResolution, Action finishCallback)
        {
            void LoadDetailData(BinaryReader reader)
            {
                byte detailDensity = 0;
                for (int i = 0; i < layerCount; i++)
                {
                    var layerOffset = i * detailResolution * detailResolution;
                    for (int y = 0; y < detailResolution; y++)
                    {
                        for (int x = 0; x < detailResolution; x++)
                        {
                            detailDensity = reader.ReadByte();
                            _detailDensity[layerOffset + y * detailResolution + x] = detailDensity;
                        }
                    }
                }
            }

            _detailDensity = new NativeArray<byte>(layerCount * detailResolution * detailResolution, Allocator.Persistent);
            if (filePath.Contains("://") || filePath.Contains(":///"))
            {
                UnityWebRequest www = UnityWebRequest.Get(filePath);
                yield return www.SendWebRequest();
                if (www.isNetworkError || www.isHttpError)
                {
                    Debug.LogError($"LoadDetailmap:{www.error}");
                }
                else
                {
                    byte[] results = www.downloadHandler.data;
                    using (var stream = new MemoryStream(results))
                    using (var reader = new BinaryReader(stream))
                    {
                        LoadDetailData(reader);
                    }
                }
            }
            else
            {
                using (var file = File.OpenRead(filePath))
                using (var reader = new BinaryReader(file))
                {
                    LoadDetailData(reader);
                }
            }

            finishCallback();
            yield return null;
        }

        public float GetDetailDensity(int index, float x, float y, int detailResolution)
        {
            if (_detailDensity == null || _detailDensity.Length == 0)
            {
                return 0f;
            }

            var layerOffset = index * detailResolution * detailResolution;
            var density = _detailDensity[layerOffset + Mathf.RoundToInt(y) * detailResolution + Mathf.RoundToInt(x)];

            return density;
        }
    }
}
