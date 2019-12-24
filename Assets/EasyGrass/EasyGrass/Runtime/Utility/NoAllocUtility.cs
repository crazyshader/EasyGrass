using System;
using System.Collections.Generic;
using System.Reflection;
using Unity.Collections.LowLevel.Unsafe;
using Unity.Collections;
using UnityEngine;
using Unity.Mathematics;

namespace EasyFramework.Grass.Runtime
{
    /// <summary>
    /// Provides access to the internal UnityEngine.NoAllocHelpers methods.
    /// 
    /// Taken from https://forum.unity.com/threads/nativearray-and-mesh.522951/#post-3842671
    /// </summary>
    public static class NoAllocHelpers
    {
        private static readonly Dictionary<Type, Delegate> ExtractArrayFromListTDelegates = new Dictionary<Type, Delegate>();
        private static readonly Dictionary<Type, Delegate> ResizeListDelegates = new Dictionary<Type, Delegate>();

        /// <summary>
        /// Extract the internal array from a list.
        /// </summary>
        /// <typeparam name="T"><see cref="List{T}"/>.</typeparam>
        /// <param name="list">The <see cref="List{T}"/> to extract from.</param>
        /// <returns>The internal array of the list.</returns>
        public static T[] ExtractArrayFromListT<T>(List<T> list)
        {
            Delegate obj;
            if (!ExtractArrayFromListTDelegates.TryGetValue(typeof(T), out obj))
            {
                var ass = Assembly.GetAssembly(typeof(Mesh)); // any class in UnityEngine
                var type = ass.GetType("UnityEngine.NoAllocHelpers");
                var methodInfo = type.GetMethod("ExtractArrayFromListT", BindingFlags.Static | BindingFlags.Public)
                    .MakeGenericMethod(typeof(T));

                obj = ExtractArrayFromListTDelegates[typeof(T)] = Delegate.CreateDelegate(typeof(Func<List<T>, T[]>), methodInfo);
            }

            var func = (Func<List<T>, T[]>)obj;
            return func.Invoke(list);
        }

        /// <summary>
        /// Resize a list.
        /// </summary>
        /// <typeparam name="T"><see cref="List{T}"/>.</typeparam>
        /// <param name="list">The <see cref="List{T}"/> to resize.</param>
        /// <param name="size">The new length of the <see cref="List{T}"/>.</param>
        public static void ResizeList<T>(List<T> list, int size)
        {
            Delegate obj;
            if (!ResizeListDelegates.TryGetValue(typeof(T), out obj))
            {
                var ass = Assembly.GetAssembly(typeof(Mesh)); // any class in UnityEngine
                var type = ass.GetType("UnityEngine.NoAllocHelpers");
                var methodInfo = type.GetMethod("ResizeList", BindingFlags.Static | BindingFlags.Public)
                    .MakeGenericMethod(typeof(T));
                obj = ResizeListDelegates[typeof(T)] =
                    Delegate.CreateDelegate(typeof(Action<List<T>, int>), methodInfo);
            }

            var action = (Action<List<T>, int>)obj;
            action.Invoke(list, size);
        }

        private static unsafe void NativeAddRange<T>(this List<T> list, void* arrayBuffer, int length)
            where T : struct
        {
            var index = list.Count;
            var newLength = index + length;

            // Resize our list if we require
            if (list.Capacity < newLength)
            {
                list.Capacity = newLength;
            }

            var items = NoAllocHelpers.ExtractArrayFromListT(list);
            var size = UnsafeUtility.SizeOf<T>();

            // Get the pointer to the end of the list
            var bufferStart = (IntPtr)UnsafeUtility.AddressOf(ref items[0]);
            var buffer = (byte*)(bufferStart + (size * index));

            UnsafeUtility.MemCpy(buffer, arrayBuffer, length * (long)size);

            NoAllocHelpers.ResizeList(list, newLength);
        }

        public unsafe static void DataConvert<T>(NativeList<T> nativeArray, ref List<T> list) where T : struct
        {
            list.NativeAddRange(nativeArray.GetUnsafePtr(), nativeArray.Length);
        }

        public unsafe static void DataConvert<T>(NativeArray<T> matrixArray, int maxInstanceSize, ref List<T>[] matrixList, ref int totalBatch) where T : struct
        {
            totalBatch = 0;
            var length = matrixArray.Length;
            for (int beginIndex = 0; beginIndex < length; beginIndex = beginIndex + maxInstanceSize)
            {
                var endIndex = Mathf.Min(beginIndex + maxInstanceSize, length - 1);
                var instanceCount = endIndex - beginIndex + 1;
                var instanceArray = matrixArray.GetSubArray(beginIndex, instanceCount);
                matrixList[totalBatch].Clear();
                matrixList[totalBatch].NativeAddRange(instanceArray.GetUnsafePtr(), instanceCount);
                ++totalBatch;
            }
        }
    }
}
