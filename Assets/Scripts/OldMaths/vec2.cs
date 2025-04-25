#pragma warning disable IDE1006
using System;
using static DVG.MathsOld.math;

namespace DVG.MathsOld
{
    /// <summary>
    /// Main vec2 struct
    /// </summary>
    [Serializable]
    public partial struct vec2 : IEquatable<vec2>
    {
        public float x, y;

        private const float EqualityPrecision = 9.99999944E-11f;
        private const float NormalizePrecision = 1E-05f;

        public vec2(float x, float y)
        {
            this.x = x;
            this.y = y;
        }
        public vec2(float value) : this(value, value) { }
        public vec2(angle a) : this(angle.sin(a), -angle.cos(a)) { }

        public static vec2 operator +(vec2 a, vec2 b) => new(a.x + b.x, a.y + b.y);
        public static vec2 operator -(vec2 a, vec2 b) => new(a.x - b.x, a.y - b.y);
        public static vec2 operator *(vec2 a, vec2 b) => new(a.x * b.x, a.y * b.y);
        public static vec2 operator /(vec2 a, vec2 b) => new(a.x / b.x, a.y / b.y);
        public static vec2 operator *(vec2 a, float d) => new(a.x * d, a.y * d);
        public static vec2 operator *(float d, vec2 a) => new(a.x * d, a.y * d);
        public static vec2 operator /(vec2 a, float d) => new(a.x / d, a.y / d);
        public static vec2 operator -(vec2 a) => new(-a.x, -a.y);
        public static bool operator !=(vec2 lhs, vec2 rhs) => !(lhs == rhs);
        public static bool operator ==(vec2 lhs, vec2 rhs)
        {
            float num = lhs.x - rhs.x;
            float num2 = lhs.y - rhs.y;
            return (num * num) + (num2 * num2) < EqualityPrecision;
        }
        public override readonly int GetHashCode() => x.GetHashCode() ^ (y.GetHashCode() << 2);
        public override readonly bool Equals(object other) => other is vec2 vec && Equals(vec);
        public readonly bool Equals(vec2 other) => x == other.x && y == other.y;
        public override readonly string ToString() => $"({x}, {y})";
    }
    /// <summary>
    /// vec2 properties
    /// </summary>
    public partial struct vec2
    {
        public readonly bool isZero => sqrlength(this) < EqualityPrecision;
        public readonly vec3 x0y => new(x, 0, y);
        public readonly vec3 ToY0() => x0y;
    }
    /// <summary>
    /// vec2 static methods
    /// </summary>
    public partial struct vec2
    {
        public static readonly vec2 zero = new(0f, 0f);
        public static readonly vec2 one = new(1f, 1f);
        public static readonly vec2 up = new(0f, 1f);
        public static readonly vec2 down = new(0f, -1f);
        public static readonly vec2 left = new(-1f, 0f);
        public static readonly vec2 right = new(1f, 0f);

        public static vec2 lerp(vec2 a, vec2 b, float t) => new(math.lerp(a.x, b.x, t), math.lerp(a.y, b.y, t));
        public static vec2 lerp2(vec2 a, vec2 b, float t) => new(math.lerp2(a.x, b.x, t), math.lerp2(a.y, b.y, t));
        public static float sqrlength(vec2 vec) => (vec.x * vec.x) + (vec.y * vec.y);
        public static float sqrlength(vec2 lhs, vec2 rhs)
        {
            var x = lhs.x - rhs.x;
            var y = lhs.y - rhs.y;
            return (x * x) + (y * y);
        }
        public static float length(vec2 vec) => sqrt(sqrlength(vec));
        public static float length(vec2 lhs, vec2 rhs) => sqrt(sqrlength(lhs, rhs));
        public static float dot(vec2 lhs, vec2 rhs) => (lhs.x * rhs.x) + (lhs.y * rhs.y);
        public static float cross(vec2 lhs, vec2 rhs) => (lhs.x * rhs.y) - (rhs.x * lhs.y);
        public static vec2 min(vec2 lhs, vec2 rhs) => new(math.min(lhs.x, rhs.x), math.min(lhs.y, rhs.y));
        public static vec2 max(vec2 lhs, vec2 rhs) => new(math.max(lhs.x, rhs.x), math.max(lhs.y, rhs.y));
        public static vec2 clamp(vec2 vec, float len)
        {
            var sqlen = sqrlength(vec);
            if (sqlen > len * len)
            {
                len /= sqrt(sqlen);
                vec.x *= len;
                vec.y *= len;
            }
            return vec;
        }
        public static vec2 clamp1(vec2 vec) => sqrlength(vec) > 1 ? normalize(vec) : vec;
        public static vec2 normalize(vec2 vec)
        {
            float num = length(vec);
            return num > NormalizePrecision ? vec /= num : zero;
        }

        public static vec2 moveto(vec2 src, vec2 trg, float t)
        {
            float dx = trg.x - src.x;
            float dy = trg.y - src.y;
            float d = sqrt((dx * dx) + (dy * dy));
            float move = t / math.max(t, d);
            return new vec2(src.x + (dx * move), src.y + (dy * move));
        }
    }
}