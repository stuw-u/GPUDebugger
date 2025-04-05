using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Text;
using UnityEngine;
using UnityEngine.Experimental.Rendering;
using UnityEngine.Rendering;


namespace GPUDebugger.Editor
{
    public static class GPUMemoryUtils
    {
        public static List<(string buffer, string result)> GetGPUMemoryUsage (params object[] objects)
        {

            List<(string name, long count)> entries = new();
            foreach (var obj in objects)
            {
                if (obj == null) continue;

                Type type = obj.GetType();
                FieldInfo[] fields = type.GetFields(BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic);

                foreach (var field in fields)
                {
                    var fieldType = field.FieldType;
                    if (fieldType == typeof(GraphicsBuffer))
                    {
                        var buffer = field.GetValue(obj) as GraphicsBuffer;
                        if (buffer == null) continue;
                        entries.Add(($"{type.Name}.{field.Name}", (long)buffer.stride * buffer.count));
                    }
                    if (fieldType == typeof(ComputeBuffer))
                    {
                        var buffer = field.GetValue(obj) as ComputeBuffer;
                        if (buffer == null) continue;
                        entries.Add(($"{type.Name}.{field.Name}", (long)buffer.stride * buffer.count));
                    }
                    if (fieldType.IsGenericType && fieldType.GetGenericTypeDefinition() == typeof(StructuredBuffer<>))
                    {
                        var innerType = fieldType.GetGenericArguments()[0];
                        var structuredBuffer = field.GetValue(obj);
                        if (structuredBuffer == null) continue;

                        var bufferProperty = fieldType.GetProperty("Buffer");
                        if (bufferProperty != null)
                        {
                            if (bufferProperty.GetValue(structuredBuffer) is GraphicsBuffer graphicsBuffer)
                            {
                                entries.Add(($"{type.Name}.{field.Name}.Buffer", (long)graphicsBuffer.stride * graphicsBuffer.count));
                            }
                        }
                    }
                    if (typeof(Texture).IsAssignableFrom(field.FieldType))
                    {
                        var buffer = field.GetValue(obj) as Texture;
                        if (buffer == null) continue;

                        entries.Add(($"{type.Name}.{field.Name} ({field.FieldType})", EditorTextureUtils.GetRuntimeMemorySize(buffer)));
                        //entries.Add(($"{type.Name}.{field.Name} ({field.FieldType})", RuntimeTextureUtils.GetRuntimeMemorySize(buffer)));
                    }
                }
            }

            entries.Sort((a, b) => b.count.CompareTo(a.count));

            var results = new List<(string buffer, string result)>();

            long total = entries.Select(e => e.count).Sum();
            results.Add(("Total", $"{total/1000000.0:F2} MB (100.00 %)"));
            foreach (var entry in entries)
            {
                double percentage = entry.count / (double)total * 100;
                results.Add((entry.name, $"{entry.count/1000000.0:F2} MB ({percentage:F2} %)"));
            }
            return results;
        }
    }
}