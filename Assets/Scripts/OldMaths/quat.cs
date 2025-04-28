using System;
#pragma warning disable IDE1006

namespace DVG.MathsOld
{
    [Serializable]
    public partial struct quat
    {
        public float x, y, z, w;
        public readonly quat normalized => normalize(this);

        private static readonly quat identity = new(0f, 0f, 0f, 1f);
        public quat(float x, float y, float z, float w)
        {
            this.x = x;
            this.y = y;
            this.z = z;
            this.w = w;
        }
        public static quat operator *(quat lhs, quat rhs)
        {
            return new quat((lhs.w * rhs.x) + (lhs.x * rhs.w) + (lhs.y * rhs.z) - (lhs.z * rhs.y), (lhs.w * rhs.y) + (lhs.y * rhs.w) + (lhs.z * rhs.x) - (lhs.x * rhs.z), (lhs.w * rhs.z) + (lhs.z * rhs.w) + (lhs.x * rhs.y) - (lhs.y * rhs.x), (lhs.w * rhs.w) - (lhs.x * rhs.x) - (lhs.y * rhs.y) - (lhs.z * rhs.z));
        }

        public static float3 operator *(quat rotation, float3 point)
        {
            float x = rotation.x;
            float y = rotation.y;
            float z = rotation.z;
            float w = rotation.w;

            float num2 = x * 2f;
            float num3 = y * 2f;
            float num4 = z * 2f;
            float num5 = x * num2;
            float num6 = y * num3;
            float num7 = z * num4;
            float num8 = x * num3;
            float num9 = x * num4;
            float num10 = y * num4;
            float num11 = w * num2;
            float num12 = w * num3;
            float num13 = w * num4;

            float3 result;
            result.x = (1f - (num6 + num7)) * point.x + (num8 - num13) * point.y + (num9 + num12) * point.z;
            result.y = (num8 + num13) * point.x + (1f - (num5 + num7)) * point.y + (num10 - num11) * point.z;
            result.z = (num9 - num12) * point.x + (num10 + num11) * point.y + (1f - (num5 + num6)) * point.z;

            return result;
        }

        public static quat euler(float x, float y, float z)
        {
            var yaw = Maths.Radians(x);
            var pitch = Maths.Radians(y);
            var roll = Maths.Radians(z);
            float rollOver2 = roll * 0.5f;
            float sinRollOver2 = Maths.Sin(rollOver2);
            float cosRollOver2 = Maths.Cos(rollOver2);
            float pitchOver2 = pitch * 0.5f;
            float sinPitchOver2 = Maths.Sin(pitchOver2);
            float cosPitchOver2 = Maths.Cos(pitchOver2);
            float yawOver2 = yaw * 0.5f;
            float sinYawOver2 = Maths.Sin(yawOver2);
            float cosYawOver2 = Maths.Cos(yawOver2);
            quat result;
            result.x = cosYawOver2 * cosPitchOver2 * cosRollOver2 + sinYawOver2 * sinPitchOver2 * sinRollOver2;
            result.y = cosYawOver2 * cosPitchOver2 * sinRollOver2 - sinYawOver2 * sinPitchOver2 * cosRollOver2;
            result.z = cosYawOver2 * sinPitchOver2 * cosRollOver2 + sinYawOver2 * cosPitchOver2 * sinRollOver2;
            result.w = sinYawOver2 * cosPitchOver2 * cosRollOver2 - cosYawOver2 * sinPitchOver2 * sinRollOver2;
            return result;
        }

        public static quat look(float3 forward, float3 up)
        {
            forward = float3.Normalize(forward);
            float3 right = float3.Normalize(float3.Cross(up, forward));
            up = float3.Cross(forward, right);
            var m00 = right.x;
            var m01 = right.y;
            var m02 = right.z;
            var m10 = up.x;
            var m11 = up.y;
            var m12 = up.z;
            var m20 = forward.x;
            var m21 = forward.y;
            var m22 = forward.z;


            float num8 = (m00 + m11) + m22;
            var quaternion = new quat();
            if (num8 > 0f)
            {
                var num = Maths.Sqrt(num8 + 1f);
                quaternion.w = num * 0.5f;
                num = 0.5f / num;
                quaternion.x = (m12 - m21) * num;
                quaternion.y = (m20 - m02) * num;
                quaternion.z = (m01 - m10) * num;
                return quaternion;
            }
            if ((m00 >= m11) && (m00 >= m22))
            {
                var num7 = Maths.Sqrt(((1f + m00) - m11) - m22);
                var num4 = 0.5f / num7;
                quaternion.x = 0.5f * num7;
                quaternion.y = (m01 + m10) * num4;
                quaternion.z = (m02 + m20) * num4;
                quaternion.w = (m12 - m21) * num4;
                return quaternion;
            }
            if (m11 > m22)
            {
                var num6 = Maths.Sqrt(((1f + m11) - m00) - m22);
                var num3 = 0.5f / num6;
                quaternion.x = (m10 + m01) * num3;
                quaternion.y = 0.5f * num6;
                quaternion.z = (m21 + m12) * num3;
                quaternion.w = (m20 - m02) * num3;
                return quaternion;
            }
            var num5 = Maths.Sqrt(((1f + m22) - m00) - m11);
            var num2 = 0.5f / num5;
            quaternion.x = (m20 + m02) * num2;
            quaternion.y = (m21 + m12) * num2;
            quaternion.z = 0.5f * num5;
            quaternion.w = (m01 - m10) * num2;
            return quaternion;
        }
    }

    public partial struct quat
    {
        private const float DotEqualityValue = 0.999999f;
        public static float dot(quat a, quat b) => (a.x * b.x) + (a.y * b.y) + (a.z * b.z) + (a.w * b.w);
        private static bool isEqualUsingDot(float dot) => dot > DotEqualityValue;
        public static float angle(quat a, quat b)
        {
            float num = dot(a, b);
            return isEqualUsingDot(num) ? 0f : Maths.Degrees(Maths.Acos(Maths.Min(Maths.Abs(num), 1f))) * 2;
        }
        public static quat normalize(quat q)
        {
            float num = Maths.Sqrt(dot(q, q));
            return num < float.Epsilon ? identity : new quat(q.x / num, q.y / num, q.z / num, q.w / num);
        }
    }
}