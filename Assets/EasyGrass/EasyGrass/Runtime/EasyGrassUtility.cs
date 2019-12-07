using UnityEngine;

namespace EasyGrass
{
    public class EasyGrassUtility
    {
        private static float[,] _heightmap;
        static public float[,] LoadHeightmap(string filePath, int width, int height)
        {
            _heightmap = new float[height, width];
            using (var file = System.IO.File.OpenRead(filePath))
            using (var reader = new System.IO.BinaryReader(file))
            {
                for (int y = 0; y < height; y++)
                {
                    for (int x = 0; x < width; x++)
                    {
                        float v = (float)reader.ReadUInt16() / 0xFFFF;
                        _heightmap[y, x] = v;
                    }
                }
            }

            return _heightmap;
        }

        static public float[,] LoadHeightmap(Texture2D heightMap)
        {
            _heightmap = new float[heightMap.width, heightMap.height];
            for (int x = 0; x < heightMap.width; x++)
            {
                for (int y = 0; y < heightMap.height; y++)
                {
                    var height = heightMap.GetPixel(x, y).r;
                    _heightmap[x, y] = height;
                }
            }

            return _heightmap;
        }

        static public float GetTerrainHeight(float x, float y, int width, int length, float height)
        {
            return GetTerrainHeight(Mathf.CeilToInt(x * (float)width), Mathf.CeilToInt(y * (float)length), height);
        }

        static public float GetTerrainHeight(int xIndex, int yIndex, float height)
        {
            if (xIndex < 0 || xIndex >= _heightmap.GetLength(0)
                || yIndex < 0 || yIndex >= _heightmap.GetLength(1))
            {
                return 0f;
            }
            return _heightmap[yIndex, xIndex] * height;
        }

        private static Vector3[,] _normalmap;
        static public Vector3[,] LoadNormalmap(Texture2D normalMap)
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

        static public Vector3 GetTerrainNormal(float x, float y, int width, int length)
        {
            var xIndex = Mathf.CeilToInt(x * (float)width);
            var yIndex = Mathf.CeilToInt(y * (float)length);
            if (xIndex < 0 || xIndex >= _heightmap.GetLength(0)
                || yIndex < 0 || yIndex >= _heightmap.GetLength(1))
            {
                return Vector3.up;
            }
            return _normalmap[xIndex, yIndex];
        }
    }
}

