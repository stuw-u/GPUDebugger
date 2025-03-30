using System;

namespace GPUDebugger
{
    public class GPUDebugAttribute : Attribute
    {

    }

    public class GPUDebugAsAttribute : Attribute
    {
        public Type FormatType;
        public GPUDebugAsAttribute (Type formatType) { this.FormatType = formatType; }
    }

    public class GPUDebugRoutineAttribute : Attribute
    {
        public string description;

        public GPUDebugRoutineAttribute () { }
        public GPUDebugRoutineAttribute (string description) { this.description = description; }
    }
}
