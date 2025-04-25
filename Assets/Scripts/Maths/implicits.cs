#if UNITY_5_3_OR_NEWER
using Unity.Netcode;
using UnityEngine;
using UnityEngine.Scripting;

namespace DVG.Maths
{
    [Preserve]
    partial struct vec2: INetworkSerializeByMemcpy
    {
        public static implicit operator Vector2(vec2 v) => new(v.x, v.y);
        public static implicit operator vec2(Vector2 v) => new(v.x, v.y);
    }

    [Preserve]
    partial struct vec3 : INetworkSerializeByMemcpy
    {
        public static implicit operator Vector3(vec3 v) => new(v.x, v.y, v.z);
        public static implicit operator vec3(Vector3 v) => new(v.x, v.y, v.z);

        public static implicit operator Color(vec3 v) => new(v.x, v.y, v.z);
        public static implicit operator vec3(Color c) => new(c.r, c.g, c.b);
    }

    [Preserve]
    partial struct quat : INetworkSerializeByMemcpy
    {
        public static implicit operator Quaternion(quat v) => new(v.x, v.y, v.z, v.w);
        public static implicit operator quat(Quaternion v) => new(v.x, v.y, v.z, v.w);
    }

    [Preserve]
    partial struct angle : INetworkSerializeByMemcpy
    {
    }
}
#endif