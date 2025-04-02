using System.Collections.Generic;
using Unity.Collections;
using UnityEngine;
using UnityEngine.Rendering;

namespace GPUDebugger
{
    public static class StructuredBufferExtensions
    {

        public static void SetComputeBufferParam<T> (this CommandBuffer commandBuffer, ComputeShader computeShader, int kernelIndex, string name, StructuredBuffer<T> structuredBuffer) where T : unmanaged
        {
            commandBuffer.SetComputeBufferParam(computeShader, kernelIndex, name, structuredBuffer.Buffer);
        }

        public static void SetComputeBufferParam<T> (this CommandBuffer commandBuffer, ComputeShader computeShader, int kernelIndex, int nameID, StructuredBuffer<T> structuredBuffer) where T : unmanaged
        {
            commandBuffer.SetComputeBufferParam(computeShader, kernelIndex, nameID, structuredBuffer.Buffer);
        }

        public static void SetBuffer<T> (this ComputeShader computeShader, int kernelIndex, string name, StructuredBuffer<T> structuredBuffer) where T : unmanaged
        {
            computeShader.SetBuffer(kernelIndex, name, structuredBuffer.Buffer);
        }

        public static void SetBuffer<T> (this ComputeShader computeShader, int kernelIndex, int nameID, StructuredBuffer<T> structuredBuffer) where T : unmanaged
        {
            computeShader.SetBuffer(kernelIndex, nameID, structuredBuffer.Buffer);
        }

        public static void SetBuffer<T> (this Material material, string name, StructuredBuffer<T> structuredBuffer) where T : unmanaged
        {
            material.SetBuffer(name, structuredBuffer.Buffer);
        }

        public static void SetBuffer<T> (this Material material, int nameID, StructuredBuffer<T> structuredBuffer) where T : unmanaged
        {
            material.SetBuffer(nameID, structuredBuffer.Buffer);
        }

        public static void SetBuffer<T> (this MaterialPropertyBlock materialPropertyBlock, string name, StructuredBuffer<T> structuredBuffer) where T : unmanaged
        {
            materialPropertyBlock.SetBuffer(name, structuredBuffer.Buffer);
        }

        public static void SetBuffer<T> (this MaterialPropertyBlock materialPropertyBlock, int nameID, StructuredBuffer<T> structuredBuffer) where T : unmanaged
        {
            materialPropertyBlock.SetBuffer(nameID, structuredBuffer.Buffer);
        }

        public static void SetBufferData<T> (this CommandBuffer commandBuffer, StructuredBuffer<T> structuredBuffer, T[] data) where T : unmanaged
        {
            commandBuffer.SetBufferData(structuredBuffer.Buffer, data);
        }

        public static void SetBufferData<T> (this CommandBuffer commandBuffer, StructuredBuffer<T> structuredBuffer, List<T> data) where T : unmanaged
        {
            commandBuffer.SetBufferData(structuredBuffer.Buffer, data);
        }

        public static void SetBufferData<T> (this CommandBuffer commandBuffer, StructuredBuffer<T> structuredBuffer, NativeArray<T> data) where T : unmanaged
        {
            commandBuffer.SetBufferData(structuredBuffer.Buffer, data);
        }

        public static void DispatchIndirect<T> (this ComputeShader computeShader, int kernelIndex, StructuredBuffer<T> indirectArgs, uint argOffset) where T : unmanaged
        {
            computeShader.DispatchIndirect(kernelIndex, indirectArgs.Buffer, argOffset);
        }

        public static void DispatchCompute<T> (this CommandBuffer commandBuffer, ComputeShader computeShader, int kernelIndex, StructuredBuffer<T> indirectArgs, uint argOffset) where T : unmanaged
        {
            commandBuffer.DispatchCompute(computeShader, kernelIndex, indirectArgs.Buffer, argOffset);
        }
    }
}