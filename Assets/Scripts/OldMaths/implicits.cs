#if UNITY_5_3_OR_NEWER
using Unity.Netcode;
using UnityEngine;
using UnityEngine.Scripting;

namespace DVG.MathsOld
{

    [Preserve]
    public partial struct quat : INetworkSerializeByMemcpy
    {
        public static implicit operator Quaternion(quat v) => new(v.x, v.y, v.z, v.w);
        public static implicit operator quat(Quaternion v) => new(v.x, v.y, v.z, v.w);
    }

    [Preserve]
    public partial struct angle : INetworkSerializeByMemcpy
    {
    }
}

namespace DVG
{
    [Preserve]
    public partial struct float2 : INetworkSerializeByMemcpy
    {
        public static implicit operator Vector2(float2 v) => new(v.x, v.y);
        public static implicit operator float2(Vector2 v) => new(v.x, v.y);
    }

    [Preserve]
    public partial struct float3 : INetworkSerializeByMemcpy
    {
        public static implicit operator Vector3(float3 v) => new(v.x, v.y, v.z);
        public static implicit operator float3(Vector3 v) => new(v.x, v.y, v.z);

        public static implicit operator Color(float3 v) => new(v.x, v.y, v.z);
        public static implicit operator float3(Color c) => new(c.r, c.g, c.b);
    }

}
#endif