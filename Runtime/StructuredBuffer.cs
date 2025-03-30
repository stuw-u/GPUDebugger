using System;
using System.Collections.Generic;
using System.Runtime.InteropServices;
using Unity.Burst;
using Unity.Collections;
using Unity.Jobs;
using UnityEngine;

namespace GPUDebugger
{
    public class StructuredBuffer<T> : IDisposable where T : unmanaged
    {
        [Flags]
        public enum Target
        {
            Default = 0,
            Vertex = 1,
            Index = 2,
            CopySource = 4,
            CopyDestination = 8,
            // Structured = 16 (Always used)
            // Raw = 32 (Forbidden)
            Append = 64,
            Counter = 128,
            Indirect = 256,
        }

        GraphicsBuffer buffer;

        private StructuredBuffer () { }

        public StructuredBuffer (int count) : this(count, Target.Default) { }

        public StructuredBuffer (params T[] initialValues) : this(Target.Default, initialValues) { }

        public StructuredBuffer (int count, Target target)
        {
            var fullTarget = GraphicsBuffer.Target.Structured | (GraphicsBuffer.Target)(int)target;
            buffer = new GraphicsBuffer(fullTarget, count, Marshal.SizeOf<T>());
        }

        public StructuredBuffer (Target target, params T[] initialValues)
        {
            var fullTarget = GraphicsBuffer.Target.Structured | (GraphicsBuffer.Target)(int)target;
            buffer = new GraphicsBuffer(fullTarget, initialValues.Length, Marshal.SizeOf<T>());
            buffer.SetData(initialValues);
        }

        public StructuredBuffer (int count, T initalValue) : this(count, Target.Default, initalValue) { }

        public StructuredBuffer (int count, Target target, T initalValue)
        {
            var fullTarget = GraphicsBuffer.Target.Structured | (GraphicsBuffer.Target)(int)target;
            buffer = new GraphicsBuffer(fullTarget, count, Marshal.SizeOf<T>());

            var temp = new NativeArray<T>(count, Allocator.Temp, NativeArrayOptions.UninitializedMemory);
            new MemsetJob() { Data = temp, FillValue = initalValue }.Schedule(count, 64).Complete();
            buffer.SetData(temp);
            temp.Dispose();
        }

        [BurstCompile]
        struct MemsetJob : IJobParallelFor
        {
            public T FillValue;
            public NativeArray<T> Data;
            public void Execute (int index) => Data[index] = FillValue;
        }

        public int count => buffer.count;
        public int Stride => buffer.stride;
        public void SetData (T[] data) => buffer.SetData(data);
        public void SetData (List<T> data) => buffer.SetData(data);
        public void SetData (NativeArray<T> data) => buffer.SetData(data);
        public void SetData (NativeList<T> data) => buffer.SetData(data.AsArray());
        public void GetData (T[] data) => buffer.GetData(data);
        public void SetCounterValue (uint counterValue) => buffer.SetCounterValue(counterValue);

        public static explicit operator GraphicsBuffer (StructuredBuffer<T> buffer) => buffer.buffer;

        public GraphicsBuffer Buffer => buffer;

        public void Dispose () => buffer.Dispose();
    }
}