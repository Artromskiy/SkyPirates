#pragma warning disable IDE1006 // Стили именования
using System;
using System.Runtime.CompilerServices;
using static DVG.MathsOld.math;

namespace DVG.MathsOld
{
    /// <summary>
    /// Main vec3 struct
    /// </summary>
    [Serializable]
    public partial struct vec3 : IEquatable<vec3>
    {
        public float x, y, z;
        private const float EqualityPrecision = 9.99999944E-11f;
        private const float NormalizePrecision = 1E-05f;

        public vec3(float x, float y, float z)
        {
            this.x = x;
            this.y = y;
            this.z = z;
        }
        public vec3(float value)
        {
            x = y = z = value;
        }
        public vec3(angle a)
        {
            x = angle.sin(a);
            y = 0;
            z = angle.cos(a);
        }

        public static vec3 operator *(in vec3 lhs, in vec3 rhs) => new(lhs.x * rhs.x, lhs.y * rhs.y, lhs.z * rhs.z);
        public static vec3 operator *(in vec3 lhs, float rhs) => new(lhs.x * rhs, lhs.y * rhs, lhs.z * rhs);
        public static vec3 operator *(float rhs, in vec3 lhs) => new(lhs.x * rhs, lhs.y * rhs, lhs.z * rhs);
        public static vec3 operator /(in vec3 lhs, in vec3 rhs) => new(lhs.x / rhs.x, lhs.y / rhs.y, lhs.z / rhs.z);
        public static vec3 operator /(in vec3 lhs, float rhs) => new(lhs.x / rhs, lhs.y / rhs, lhs.z / rhs);
        public static vec3 operator +(in vec3 lhs, in vec3 rhs) => new(lhs.x + rhs.x, lhs.y + rhs.y, lhs.z + rhs.z);
        public static vec3 operator -(in vec3 lhs, in vec3 rhs) => new(lhs.x - rhs.x, lhs.y - rhs.y, lhs.z - rhs.z);
        public static vec3 operator -(vec3 a) => new(-a.x, -a.y, -a.z);
        public static bool operator !=(vec3 lhs, vec3 rhs) => !(lhs == rhs);
        public static bool operator ==(vec3 lhs, vec3 rhs)
        {
            float deltaX = lhs.x - rhs.x;
            float deltaY = lhs.y - rhs.y;
            float deltaZ = lhs.z - rhs.z;
            float sqrDistance = (deltaX * deltaX) + (deltaY * deltaY) + (deltaZ * deltaZ);
            return sqrDistance < EqualityPrecision;
        }
        public readonly override int GetHashCode() => x.GetHashCode() ^ (y.GetHashCode() << 2) ^ (z.GetHashCode() >> 2);
        public readonly override bool Equals(object other) => other is vec3 vec && Equals(vec);
        public readonly bool Equals(vec3 other) => x == other.x && y == other.y && z == other.z;
        public readonly override string ToString() => $"({x}, {y}, {z})";

    }
    /// <summary>
    /// vec3 properties
    /// </summary>
    public partial struct vec3
    {
        public vec3 normalized => normalize(this);
        public bool isZero => sqrlength(this) < EqualityPrecision;
        public vec2 xz => new(x, z);
        public vec2 xy => new(x, y);
        public vec2 noY => xz;
        public vec3 zeroY() => xz.x0y;
    }


    /// <summary>
    /// vec3 static methods
    /// </summary>
    public partial struct vec3
    {
        public static readonly vec3 zero = new(0f, 0f, 0f);
        public static readonly vec3 one = new(1f, 1f, 1f);
        public static readonly vec3 up = new(0f, 1f, 0f);
        public static readonly vec3 down = new(0f, -1f, 0f);
        public static readonly vec3 left = new(-1f, 0f, 0f);
        public static readonly vec3 right = new(1f, 0f, 0f);
        public static readonly vec3 forward = new(0f, 0f, 1f);
        public static readonly vec3 back = new(0f, 0f, -1f);


        public static bool xzequal(vec3 lhs, vec3 rhs)
        {
            float deltaX = lhs.x - rhs.x;
            float deltaZ = lhs.z - rhs.z;
            float sqrDistance = (deltaX * deltaX) + (deltaZ * deltaZ);
            return sqrDistance < EqualityPrecision;
        }

        public static vec3 lerp(vec3 a, vec3 b, float t) => new(math.lerp(a.x, b.x, t), math.lerp(a.y, b.y, t), math.lerp(a.z, b.z, t));
        public static vec3 cross(vec3 lhs, vec3 rhs) => new((lhs.y * rhs.z) - (lhs.z * rhs.y), (lhs.z * rhs.x) - (lhs.x * rhs.z), (lhs.x * rhs.y) - (lhs.y * rhs.x));
        public static float dot(vec3 lhs, vec3 rhs) => (lhs.x * rhs.x) + (lhs.y * rhs.y) + (lhs.z * rhs.z);
        public static float length(vec3 v) => sqrt((v.x * v.x) + (v.y * v.y) + (v.z * v.z));
        public static float length(vec3 lhs, vec3 rhs) => sqrt(sqrlength(lhs, rhs));
        public static float sqrlength(vec3 v) => (v.x * v.x) + (v.y * v.y) + (v.z * v.z);
        public static vec3 min(vec3 lhs, vec3 rhs) => new(math.min(lhs.x, rhs.x), math.min(lhs.y, rhs.y), math.min(lhs.z, rhs.z));
        public static vec3 max(vec3 lhs, vec3 rhs) => new(math.max(lhs.x, rhs.x), math.max(lhs.y, rhs.y), math.max(lhs.z, rhs.z));
        public static float sqrlength(vec3 lhs, vec3 rhs)
        {
            var x = lhs.x - rhs.x;
            var y = lhs.y - rhs.y;
            var z = lhs.z - rhs.z;
            return (x * x) + (y * y) + (z * z);
        }
        public static vec3 clamp(vec3 vector, float len)
        {
            var sqlen = sqrlength(vector);
            if (sqlen > len * len)
            {
                len /= sqrt(sqlen);
                vector.x *= len;
                vector.y *= len;
                vector.z *= len;
            }
            return vector;
        }
        public static vec3 normalize(vec3 value)
        {
            float num = length(value);
            return num > NormalizePrecision ? value / num : zero;
        }
        public static vec3 moveto(vec3 src, vec3 trg, float t)
        {
            float dx = trg.x - src.x;
            float dy = trg.y - src.y;
            float dz = trg.z - src.z;
            float d = sqrt((dx * dx) + (dy * dy) + (dz * dz));
            float move = t / d <= t ? t : d;
            return new vec3(src.x + (dx * move), src.y + (dy * move), src.z + (dz * move));
        }


        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static vec3 smoothDamp(vec3 current, vec3 target, ref vec3 currentVelocity, float smoothTime, float deltaTime, float maxSpeed = float.PositiveInfinity)
        {
            float num = 0f;
            float num2 = 0f;
            float num3 = 0f;
            smoothTime = math.max(0.0001f, smoothTime);
            float num4 = 2f / smoothTime;
            float num5 = num4 * deltaTime;
            float num6 = 1f / (1f + num5 + 0.48f * num5 * num5 + 0.235f * num5 * num5 * num5);
            float num7 = current.x - target.x;
            float num8 = current.y - target.y;
            float num9 = current.z - target.z;
            vec3 vector = target;
            float num10 = maxSpeed * smoothTime;
            float num11 = num10 * num10;
            float num12 = num7 * num7 + num8 * num8 + num9 * num9;
            if (num12 > num11)
            {
                float num13 = math.sqrt(num12);
                num7 = num7 / num13 * num10;
                num8 = num8 / num13 * num10;
                num9 = num9 / num13 * num10;
            }

            target.x = current.x - num7;
            target.y = current.y - num8;
            target.z = current.z - num9;
            float num14 = (currentVelocity.x + num4 * num7) * deltaTime;
            float num15 = (currentVelocity.y + num4 * num8) * deltaTime;
            float num16 = (currentVelocity.z + num4 * num9) * deltaTime;
            currentVelocity.x = (currentVelocity.x - num4 * num14) * num6;
            currentVelocity.y = (currentVelocity.y - num4 * num15) * num6;
            currentVelocity.z = (currentVelocity.z - num4 * num16) * num6;
            num = target.x + (num7 + num14) * num6;
            num2 = target.y + (num8 + num15) * num6;
            num3 = target.z + (num9 + num16) * num6;
            float num17 = vector.x - current.x;
            float num18 = vector.y - current.y;
            float num19 = vector.z - current.z;
            float num20 = num - vector.x;
            float num21 = num2 - vector.y;
            float num22 = num3 - vector.z;
            if (num17 * num20 + num18 * num21 + num19 * num22 > 0f)
            {
                num = vector.x;
                num2 = vector.y;
                num3 = vector.z;
                currentVelocity.x = (num - vector.x) / deltaTime;
                currentVelocity.y = (num2 - vector.y) / deltaTime;
                currentVelocity.z = (num3 - vector.z) / deltaTime;
            }

            return new vec3(num, num2, num3);
        }

    }

    /// <summary>
    /// Gameplay and dev specific functions
    /// </summary>
    public partial struct vec3
    {
        public static vec3 rgb2hsv(vec3 c)
        {
            const float x = 0f;
            const float y = -1f / 3f;
            const float z = 2f / 3f;
            const float w = -1f;
            (float x, float y, float z, float w) p = c.y < c.z ? (c.z, c.y, w, z) : (c.y, c.z, x, y);
            (float x, float y, float z, float w) q = c.x < p.x ? (p.x, p.y, p.w, c.x) : (c.x, p.y, p.z, p.x);
            float d = q.x - math.min(q.w, q.y);
            float e = 1.0e-10f;
            return new vec3(abs(q.z + ((q.w - q.y) / ((6.0f * d) + e))), d / (q.x + e), q.x);
        }
        public static vec3 hsv2rgb(vec3 c)
        {
            const float x = 1f;
            const float y = 2f / 3f;
            const float z = 1f / 3f;
            const float w = 3f;
            (float x, float y, float z) p = (abs(((c.x + x) % 1 * 6) - w) - x, abs(((c.x + y) % 1 * 6) - w) - x, abs(((c.x + z) % 1 * 6) - w) - x);
            return new vec3(c.z * math.lerp(x, clamp01(p.x), c.y), c.z * math.lerp(x, clamp01(p.y), c.y), c.z * math.lerp(x, clamp01(p.z), c.y));
        }
    }
}