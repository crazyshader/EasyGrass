using UnityEngine;
using Unity.Collections;
using System;
using Unity.Collections.LowLevel.Unsafe;
using System.Collections.Generic;

namespace EasyFramework.Grass.Runtime
{
    public class EasyGrassUtility
    {
        private EasyGrassUtility() { }
        private static EasyGrassUtility _instance;
        public static EasyGrassUtility Instance
        {
            get
            {
                if (_instance == null)
                {
                    _instance = new EasyGrassUtility();
                }
                return _instance;
            }
        }

        public void Release()
        {
            if (_randomCache.IsCreated)
            {
                _randomCache.Dispose();
            }
        }

        private NativeArray<float> _randomCache;
        public void InitRandomCache()
        {
            if (_randomCache == null || _randomCache.Length == 0)
            {
                _randomCache = new NativeArray<float>(9999, Allocator.Persistent);
                System.Random random = new System.Random(9999);
                for (int i = 0; i < _randomCache.Length; i++)
                {
                    _randomCache[i] = (float)random.NextDouble();
                }
            }
        }

        public Vector2 GetRandomPosition(Vector2 centerPos, Vector2 pixelToTerrain, int seed)
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
            randPos.x += (pixelToTerrain.x * 0.5f) * (1f + randomNoiseX);
            randPos.y += (pixelToTerrain.y * 0.5f) * (1f + randomNoiseZ);

            return randPos;
        }

        public static unsafe void CopyToFast<T>(NativeArray<T> nativeArray, T[] array) where T : struct
        {
            if (array == null)
            {
                throw new NullReferenceException(nameof(array) + " is null");
            }
            int nativeArrayLength = nativeArray.Length;
            if (array.Length < nativeArrayLength)
            {
                throw new IndexOutOfRangeException(
                    nameof(array) + " is shorter than " + nameof(nativeArray));
            }
            int byteLength = nativeArray.Length * UnsafeUtility.SizeOf<T>();
            void* managedBuffer = UnsafeUtility.AddressOf(ref array[0]);
            void* nativeBuffer = nativeArray.GetUnsafePtr();
            UnsafeUtility.MemCpy(managedBuffer, nativeBuffer, byteLength);
        }

        public static unsafe void CopyToFast<T>(T[] array, NativeArray<T> nativeArray) where T : struct
        {
            if (array == null)
            {
                throw new NullReferenceException(nameof(array) + " is null");
            }
            int nativeArrayLength = nativeArray.Length;
            if (array.Length > nativeArrayLength)
            {
                throw new IndexOutOfRangeException(
                    nameof(nativeArray) + " is shorter than " + nameof(array));
            }
            int byteLength = array.Length * UnsafeUtility.SizeOf<T>();
            void* managedBuffer = UnsafeUtility.AddressOf(ref array[0]);
            void* nativeBuffer = nativeArray.GetUnsafePtr();
            UnsafeUtility.MemCpy(nativeBuffer, managedBuffer, byteLength);
        }
    }
}

