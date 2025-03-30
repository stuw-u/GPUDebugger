using System.Collections.Generic;
using System.Collections.ObjectModel;

namespace GPUDebugger
{
    public static class GPUObjectDebugger
    {
        public class GPUDebugObject
        {
            public string name;
            public object obj;
            public GPUDebugObject (string name, object obj)
            {
                this.name=name;
                this.obj=obj;
            }
            public GPUDebugObject (object obj)
            {
                this.name=obj.GetType().Name;
                this.obj=obj;
            }
        }

        static List<GPUDebugObject> trackedObjects = new();

        public static ReadOnlyCollection<GPUDebugObject> TrackedObjectList => trackedObjects.AsReadOnly();

        public static void StartTracking (object obj)
        {
#if UNITY_EDITOR
            trackedObjects.Add(new GPUDebugObject(obj));
#endif
        }

        public static void StartTracking (object obj, string name)
        {
#if UNITY_EDITOR
            trackedObjects.Add(new GPUDebugObject(name, obj));
#endif
        }

        public static void StopTracking (object obj)
        {
#if UNITY_EDITOR
            trackedObjects.RemoveAll((entry) => entry.obj == obj);
#endif
        }
    }
}