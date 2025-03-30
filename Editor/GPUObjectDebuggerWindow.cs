using System;
using System.Collections.Generic;
using System.Reflection;
using Unity.Mathematics;
using UnityEditor;
using UnityEngine;

namespace GPUDebugger.Editor
{
    public class GPUObjectDebuggerWindow : EditorWindow
    {
        private class BufferEntry
        {
            public object Target;
            public FieldInfo Field;
            public Type Type;
        }

        private class RoutineEntry
        {
            public string Name;
            public string description;
            public MethodInfo MethodInfo;
        }

        private class TextureEntry
        {
            public string Name;
            public Texture Texture;
        }

        readonly string[] tabNames = { "Buffer Viewer", "Texture Viewer", "Memory Usage", "Debug Routines" };
        
        List<BufferEntry> bufferEntries = new();
        List<RoutineEntry> methodEntries = new();
        List<TextureEntry> textureEntries = new();
        int selectedTab = 0;
        Vector2 scroll;
        Vector2 scroll2;
        UnityEditor.Editor textureEditor;
        Texture selectedTexture;

        GPUObjectDebugger.GPUDebugObject debugObject;

        Type bufferType;
        bool isBufferPrimitive;
        FieldInfo[] bufferTypeFields;
        Array bufferData;
        int page = 0;
        int pageSize = 100;
        List<(string entry, string result)> memoryUsage;

        

        [MenuItem("Window/Analysis/GPU Object Debugger")]
        public static void ShowWindow ()
        {
            var window = GetWindow<GPUObjectDebuggerWindow>("GPU Object Debugger");
        }
        public static void ShowObject (object debugObject)
        {
            ShowObject(new GPUObjectDebugger.GPUDebugObject(debugObject));
        }

        public static void ShowObject (GPUObjectDebugger.GPUDebugObject debugObject)
        {
            var window = GetWindow<GPUObjectDebuggerWindow>("GPU Object Debugger");
            window.debugObject = debugObject;

            var obj = debugObject.obj;
            Type type = obj.GetType();
            window.selectedTab = 0;
            window.methodEntries.Clear();
            window.bufferEntries.Clear();
            window.textureEntries.Clear();
            FieldInfo[] fields = type.GetFields(BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic);
            MethodInfo[] methods = type.GetMethods(BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic);

            foreach (var field in fields)
            {
                var fieldType = field.FieldType;

                var bufferAttribute = field.GetCustomAttribute<GPUDebugAsAttribute>();
                if (bufferAttribute != null && (fieldType == typeof(GraphicsBuffer) || fieldType == typeof(ComputeBuffer)))
                {
                    window.bufferEntries.Add(new BufferEntry
                    {
                        Target = obj,
                        Field = field,
                        Type = bufferAttribute.FormatType
                    });
                }

                var textureAttribute = field.GetCustomAttribute<GPUDebugAttribute>();
                if(textureAttribute != null && typeof(Texture).IsAssignableFrom(fieldType))
                {
                    var texture = field.GetValue(obj) as Texture;
                    string name = string.IsNullOrEmpty(texture.name) ? field.Name : texture.name;
                    window.textureEntries.Add(new TextureEntry
                    {
                        Name = name,
                        Texture = texture,
                    });
                }
            }
            foreach (var method in methods)
            {
                var attribute = method.GetCustomAttribute<GPUDebugRoutineAttribute>();
                if(attribute != null)
                {
                    window.methodEntries.Add(new RoutineEntry
                    {
                        MethodInfo = method,
                        Name = method.Name,
                        description = attribute.description,
                    });
                }
            }

            window.memoryUsage = GPUMemoryUtils.GetGPUMemoryUsage(obj);
        }

        private void OnGUI ()
        {
            if(debugObject != null && debugObject.obj == null)
            {
                debugObject = null;
            }


            // Select a debug object
            if (debugObject == null)
            {
                var debugObjectEntries = GPUObjectDebugger.TrackedObjectList;

                if (debugObjectEntries.Count == 0)
                {
                    EditorGUILayout.LabelField("No object to inspect. Call GPUObjectDebugger.StartTracking(); " +
                        "and GPUObjectDebugger.StopTracking(); on any object to have it listed here.", EditorStyles.wordWrappedLabel);
                    return;
                }

                scroll = EditorGUILayout.BeginScrollView(scroll);
                foreach (var entry in debugObjectEntries)
                {
                    EditorGUILayout.BeginHorizontal();
                    EditorGUILayout.LabelField(entry.name, EditorStyles.boldLabel,
                        GUILayout.ExpandWidth(true));
                    if (GUILayout.Button("Open", new GUIStyle(EditorStyles.miniButtonLeft) { fixedWidth = 80 }))
                    {
                        ShowObject(entry);
                    }
                    EditorGUILayout.EndHorizontal();
                }
                EditorGUILayout.EndScrollView();

                return;
            }
            else
            {
                EditorGUILayout.BeginHorizontal();
                EditorGUILayout.LabelField(debugObject.name, EditorStyles.boldLabel, GUILayout.ExpandWidth(true));
                if (GUILayout.Button("Close", new GUIStyle(EditorStyles.miniButtonLeft) { fixedWidth = 80 }))
                {
                    debugObject = null;
                }
                EditorGUILayout.EndHorizontal();
                selectedTab = GUILayout.Toolbar(selectedTab, tabNames);

                if(debugObject == null) return;
            }

            // Buffer Viewer
            if (selectedTab == 0)
            {
                if (bufferEntries.Count == 0)
                {
                    EditorGUILayout.LabelField("No GraphicsBuffers found with [GPUDebugAs].");
                    return;
                }

                int entries = Mathf.Min(5, bufferEntries.Count);
                float totalHeight = entries * EditorGUIUtility.singleLineHeight + entries * EditorGUIUtility.standardVerticalSpacing;
                scroll2 = EditorGUILayout.BeginScrollView(scroll2, (bufferData == null) ? GUILayout.ExpandHeight(true) : GUILayout.MinHeight(totalHeight));
                foreach (var entry in bufferEntries)
                {
                    EditorGUILayout.BeginHorizontal();
                    EditorGUILayout.LabelField($"{entry.Target.GetType().Name}:{entry.Field.Name} ({entry.Type.Name})", EditorStyles.boldLabel, 
                        GUILayout.ExpandWidth(true));
                    if (GUILayout.Button("Load", new GUIStyle(EditorStyles.miniButtonLeft) { fixedWidth = 80 }))
                    {
                        LoadBufferData(entry);
                    }
                    EditorGUILayout.EndHorizontal();
                }
                EditorGUILayout.EndScrollView();


                DrawBufferData();
            }

            // Texture Viewer
            else if (selectedTab == 1)
            {
                if (textureEntries.Count == 0)
                {
                    EditorGUILayout.LabelField("No Texture found with [GPUDebug].");
                    return;
                }

                int entries = Mathf.Min(5, textureEntries.Count);
                float totalHeight = entries * EditorGUIUtility.singleLineHeight + entries * EditorGUIUtility.standardVerticalSpacing;
                scroll2 = EditorGUILayout.BeginScrollView(scroll2, (bufferData == null) ? GUILayout.ExpandHeight(true) : GUILayout.MinHeight(totalHeight));
                foreach (var entry in textureEntries)
                {
                    EditorGUILayout.BeginHorizontal();
                    EditorGUILayout.LabelField($"{entry.Name} ({entry.Texture.GetType()})", EditorStyles.boldLabel,
                        GUILayout.ExpandWidth(true));
                    if (GUILayout.Button("Open", new GUIStyle(EditorStyles.miniButtonLeft) { fixedWidth = 80 }))
                    {
                        selectedTexture = entry.Texture;
                    }
                    EditorGUILayout.EndHorizontal();
                }
                EditorGUILayout.EndScrollView();

                DrawTexture();
            }

            // Memory Usage
            else if (selectedTab == 2)
            {
                if (memoryUsage == null)
                {
                    EditorGUILayout.LabelField("No entries found");
                }
                else
                {
                    scroll = EditorGUILayout.BeginScrollView(scroll);
                    foreach (var entry in memoryUsage)
                    {
                        EditorGUILayout.BeginHorizontal();
                        EditorGUILayout.LabelField(entry.entry, GUILayout.ExpandWidth(true));
                        EditorGUILayout.LabelField(entry.result, new GUIStyle(EditorStyles.label) { alignment = TextAnchor.MiddleRight }, GUILayout.Width(150));
                        EditorGUILayout.EndHorizontal();
                    }
                    EditorGUILayout.EndScrollView();
                }
            }

            // Debug Routines
            else if (selectedTab == 3)
            {
                if (methodEntries.Count == 0)
                {
                    EditorGUILayout.LabelField("No Methods found with [GPUDebugRoutine].");
                    return;
                }

                scroll = EditorGUILayout.BeginScrollView(scroll);
                foreach (var entry in methodEntries)
                {
                    EditorGUILayout.BeginHorizontal();
                    EditorGUILayout.LabelField(entry.Name, EditorStyles.boldLabel);
                    EditorGUILayout.Space(10);
                    if (entry.description != null)
                    {
                        EditorGUILayout.LabelField(entry.description, EditorStyles.wordWrappedLabel, GUILayout.ExpandWidth(true));
                    }
                    if (GUILayout.Button("Run", new GUIStyle(EditorStyles.miniButtonLeft) { fixedWidth = 80 }))
                    {
                        entry.MethodInfo.Invoke(debugObject.obj, null);
                    }
                    EditorGUILayout.EndHorizontal();
                }
                EditorGUILayout.EndScrollView();
            }
        }

        private void LoadBufferData (BufferEntry entry)
        {
            var bufferAsObject = entry.Field.GetValue(entry.Target);
            if (bufferAsObject is GraphicsBuffer graphicBuffer)
            {
                int elementCount = graphicBuffer.count;
                bufferData = Array.CreateInstance(entry.Type, elementCount);
                bufferType = entry.Type;
                bufferTypeFields = bufferType.GetFields();
                isBufferPrimitive = bufferType.IsPrimitive;

                graphicBuffer.GetData(bufferData);
            }
            else if (bufferAsObject is ComputeBuffer computeBuffer)
            {
                int elementCount = computeBuffer.count;
                bufferData = Array.CreateInstance(entry.Type, elementCount);
                bufferType = entry.Type;
                bufferTypeFields = bufferType.GetFields();
                isBufferPrimitive = bufferType.IsPrimitive;

                computeBuffer.GetData(bufferData);
            }

            page = 0;
        }

        private void DrawTexture ()
        {
            if (selectedTexture == null) return;

            scroll = EditorGUILayout.BeginScrollView(scroll);
            if (textureEditor == null || textureEditor.target != selectedTexture)
            {
                textureEditor = UnityEditor.Editor.CreateEditor(selectedTexture);
            }
            textureEditor.OnInspectorGUI();
            Rect previewRect = EditorGUILayout.GetControlRect(GUILayout.ExpandWidth(true), GUILayout.ExpandHeight(true));
            textureEditor.DrawPreview(previewRect);
            EditorGUILayout.EndScrollView();

            EditorGUILayout.BeginHorizontal();
            if (GUILayout.Button("Close", EditorStyles.miniButtonRight))
            {
                selectedTexture = null;
            }
            EditorGUILayout.EndHorizontal();
        }

        private void DrawBufferData ()
        {
            if (bufferData == null) return;

            int maxPage = Mathf.CeilToInt((float)bufferData.Length / pageSize);

            int fieldCount = isBufferPrimitive ? 1 : bufferTypeFields.Length;
            float itemHeight = EditorGUIUtility.singleLineHeight * fieldCount + EditorGUIUtility.standardVerticalSpacing * (fieldCount - 1) + 10;
            int start = page * pageSize;
            int end = Mathf.Min(start + pageSize, bufferData.Length);
            float totalHeight = (end - start) * itemHeight;

            scroll = EditorGUILayout.BeginScrollView(scroll);

            float viewTop = scroll.y;
            float viewBottom = viewTop + position.height;
            float currentY = 0;
            for (int i = start; i < end; i++)
            {
                float itemYMin = currentY;
                float itemYMax = currentY + itemHeight;
                if (itemYMax < viewTop || itemYMin > viewBottom)
                {
                    GUILayout.Space(itemHeight);
                }
                else
                {
                    object item = bufferData.GetValue(i);
                    EditorGUILayout.BeginHorizontal();
                    EditorGUILayout.LabelField($"[{i}]", GUILayout.Width(50));
                    EditorGUILayout.BeginVertical();
                    if(isBufferPrimitive)
                    {
                        DisplayField("", item);
                    }
                    else
                    {
                        foreach (FieldInfo field in bufferTypeFields)
                        {
                            DisplayField(field, item);
                        }
                    }
                    EditorGUILayout.EndVertical();
                    EditorGUILayout.EndHorizontal();
                    EditorGUILayout.Space(10);
                }
                currentY = itemYMax;
            }

            
            EditorGUILayout.EndScrollView();

            EditorGUILayout.BeginHorizontal();
            EditorGUILayout.LabelField($"Page {page + 1}/{maxPage}. Total Items: {bufferData.Length}", GUILayout.ExpandWidth(true));
            if (GUILayout.Button("Previous") && page > 0)
            {
                page--;
            }
            if (GUILayout.Button("Next") && page < maxPage - 1)
            {
                page++;
            }
            if (GUILayout.Button("Close"))
            {
                bufferData = null;
            }
            EditorGUILayout.EndHorizontal();
        }

        private void DisplayField (FieldInfo field, object obj)
        {
            DisplayField(field.Name, field.GetValue(obj));
        }

        private void DisplayField (string label, object value)
        {
            GUI.enabled = false;

            if (value is int intValue)
            {
                EditorGUILayout.IntField(label, intValue);
            }
            else if (value is float floatValue)
            {
                EditorGUILayout.FloatField(label, floatValue);
            }
            else if (value is double doubleValue)
            {
                EditorGUILayout.DoubleField(label, doubleValue);
            }
            else if (value is uint uintValue)
            {
                EditorGUILayout.LongField(label, uintValue);
            }
            else if (value is int2 int2Value)
            {
                Vector2Field(label, new Vector2(int2Value.x, int2Value.y));
            }
            else if (value is int3 int3Value)
            {
                Vector3Field(label, new Vector4(int3Value.x, int3Value.y, int3Value.z));
            }
            else if (value is int4 int4Value)
            {
                Vector4Field(label, new Vector4(int4Value.x, int4Value.y, int4Value.z, int4Value.w));
            }
            else if (value is uint2 uint2Value)
            {
                Vector2Field(label, new Vector2(uint2Value.x, uint2Value.y));
            }
            else if (value is uint3 uint3Value)
            {
                Vector3Field(label, new Vector4(uint3Value.x, uint3Value.y, uint3Value.z));
            }
            else if (value is uint4 uint4Value)
            {
                Vector4Field(label, new Vector4(uint4Value.x, uint4Value.y, uint4Value.z, uint4Value.w));
            }
            else if (value is float2 float2Value)
            {
                Vector2Field(label, new Vector2(float2Value.x, float2Value.y));
            }
            else if (value is float3 float3Value)
            {
                Vector3Field(label, new Vector4(float3Value.x, float3Value.y, float3Value.z));
            }
            else if (value is float4 float4Value)
            {
                Vector4Field(label, new Vector4(float4Value.x, float4Value.y, float4Value.z, float4Value.w));
            }
            else if (value is Vector2 vector2Value)
            {
                Vector2Field(label, vector2Value);
            }
            else if (value is Vector3 vector3Value)
            {
                Vector3Field(label, vector3Value);
            }
            else if (value is Vector4 vector4Value)
            {
                Vector4Field(label, vector4Value);
            }
            else
            {
                EditorGUILayout.LabelField(label, value != null ? value.ToString() : "null");
            }

            GUI.enabled = true;
        }

        static void Vector2Field (string label, Vector2 value)
        {
            EditorGUI.BeginDisabledGroup(true);
            EditorGUILayout.BeginHorizontal();
            EditorGUILayout.PrefixLabel(label);
            EditorGUILayout.BeginHorizontal();
            EditorGUILayout.LabelField("X", GUILayout.Width(12));
            EditorGUILayout.FloatField(value.x, GUILayout.MinWidth(30));
            EditorGUILayout.LabelField("Y", GUILayout.Width(12));
            EditorGUILayout.FloatField(value.y, GUILayout.MinWidth(30));
            EditorGUILayout.EndHorizontal();
            EditorGUILayout.EndHorizontal();
            EditorGUI.EndDisabledGroup();
        }

        static void Vector3Field (string label, Vector3 value)
        {
            EditorGUI.BeginDisabledGroup(true);
            EditorGUILayout.BeginHorizontal();
            EditorGUILayout.PrefixLabel(label);
            EditorGUILayout.BeginHorizontal();
            EditorGUILayout.LabelField("X", GUILayout.Width(12));
            EditorGUILayout.FloatField(value.x, GUILayout.MinWidth(30));
            EditorGUILayout.LabelField("Y", GUILayout.Width(12));
            EditorGUILayout.FloatField(value.y, GUILayout.MinWidth(30));
            EditorGUILayout.LabelField("Z", GUILayout.Width(12));
            EditorGUILayout.FloatField(value.z, GUILayout.MinWidth(30));
            EditorGUILayout.EndHorizontal();
            EditorGUILayout.EndHorizontal();
            EditorGUI.EndDisabledGroup();
        }

        static void Vector4Field (string label, Vector4 value)
        {
            EditorGUI.BeginDisabledGroup(true);
            EditorGUILayout.BeginHorizontal();
            EditorGUILayout.PrefixLabel(label);
            EditorGUILayout.BeginHorizontal();
            EditorGUILayout.LabelField("X", GUILayout.Width(12));
            EditorGUILayout.FloatField(value.x, GUILayout.MinWidth(30));
            EditorGUILayout.LabelField("Y", GUILayout.Width(12));
            EditorGUILayout.FloatField(value.y, GUILayout.MinWidth(30));
            EditorGUILayout.LabelField("Z", GUILayout.Width(12));
            EditorGUILayout.FloatField(value.z, GUILayout.MinWidth(30));
            EditorGUILayout.LabelField("W", GUILayout.Width(12));
            EditorGUILayout.FloatField(value.w, GUILayout.MinWidth(30));
            EditorGUILayout.EndHorizontal();
            EditorGUILayout.EndHorizontal();
            EditorGUI.EndDisabledGroup();
        }
    }
}