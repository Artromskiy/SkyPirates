#pragma warning disable IDE1006
#if UNITY_5_3_OR_NEWER
using Unity.IL2CPP.CompilerServices;
using Unity.Profiling;
using UnityEngine;

[assembly: Il2CppSetOption(Option.DivideByZeroChecks, true)]

namespace DVG
{
    [IgnoredByDeepProfiler]
    public partial struct float2
    {
        public static implicit operator Vector2(float2 v) => new(v.x, v.y);
        public static implicit operator float2(Vector2 v) => new(v.x, v.y);
    }

    [IgnoredByDeepProfiler]
    public partial struct float3
    {
        public static implicit operator Vector3(float3 v) => new(v.x, v.y, v.z);
        public static implicit operator float3(Vector3 v) => new(v.x, v.y, v.z);

        public static implicit operator Color(float3 v) => new(v.x, v.y, v.z);
        public static implicit operator float3(Color c) => new(c.r, c.g, c.b);
    }

    [IgnoredByDeepProfiler]
    public partial struct fix2
    {
        public static explicit operator Vector2(fix2 v) => new((float)v.x, (float)v.y);
        public static explicit operator fix2(Vector2 v) => new((fix)v.x, (fix)v.y);
        public static explicit operator float2(fix2 v) => new((float)v.x, (float)v.y);
        public static explicit operator fix2(float2 v) => new((fix)v.x, (fix)v.y);
    }

    [IgnoredByDeepProfiler]
    public partial struct fix3
    {
        public static explicit operator Vector3(fix3 v) => new((float)v.x, (float)v.y, (float)v.z);
        public static explicit operator fix3(Vector3 v) => new((fix)v.x, (fix)v.y, (fix)v.z);
        public static explicit operator float3(fix3 v) => new((float)v.x, (float)v.y, (float)v.z);
        public static explicit operator fix3(float3 v) => new((fix)v.x, (fix)v.y, (fix)v.z);
    }

    [IgnoredByDeepProfiler]
    public partial struct float4 { }

    [IgnoredByDeepProfiler]
    public partial struct fix { }


    [IgnoredByDeepProfiler]
    public partial struct fix4 { }

    [IgnoredByDeepProfiler]
    public partial struct int2 { }

    [IgnoredByDeepProfiler]
    public partial struct int3 { }

    [IgnoredByDeepProfiler]
    public partial struct int4 { }

    [IgnoredByDeepProfiler]
    public partial class Maths { }
}
#endif