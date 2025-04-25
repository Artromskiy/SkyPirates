#pragma warning disable IDE1006
using System;
using static DVG.MathsOld.math;
using static DVG.MathsOld.vec2;

namespace DVG.MathsOld
{
    public partial struct angle : IEquatable<angle>
    {
        public readonly float turn;
        public readonly float deg => turn * 360;
        public readonly float rad => turn * tau;

        private const float EqualityPrecision = 1e-6f;
        private const float Div = 1f - EqualityPrecision;
        private const float InverseDegreeTurn = 1f / 360f;

        public static implicit operator angle(float degrees) => new(wrap(degrees * InverseDegreeTurn));

        public angle(vec2 vec) => turn = wrap(-atan2(-vec.x, vec.y) * invtau);
        public angle(vec2 vec1, vec2 vec2) => turn = wrap(-atan2(cross(vec1, vec2), dot(vec1, vec2)) * invtau);
        private angle(float turn)
        {
            this.turn = turn;
        }

        private static float wrap(float turn)
        {
            float res = ((turn % 1f) + 1f) % Div; //  faster than
            return res;
        }
        /// <summary>
        /// returns clockwise difference angle from source to target
        /// </summary>
        public static angle operator -(angle source, angle target) => new(wrap(target.turn - source.turn));
        public static angle operator -(angle source) => new(wrap(-source.turn));
        public static angle operator +(angle left, angle right) => new(wrap(left.turn + right.turn));
        public static angle operator *(angle source, float scale) => new(wrap(source.turn * scale));
        public static bool operator <(angle left, angle right) => left != right && left.turn < right.turn;
        public static bool operator >(angle left, angle right) => left != right && left.turn > right.turn;
        public static bool operator <=(angle left, angle right) => left == right || left.turn < right.turn;
        public static bool operator >=(angle left, angle right) => left == right || left.turn > right.turn;
        public static bool operator !=(angle left, angle right) => !(left == right);
        public static bool operator ==(angle left, angle right) => distance(left.turn, right.turn) <= EqualityPrecision;
        public override int GetHashCode() => turn.GetHashCode();
        public override bool Equals(object other) => other is angle vec && Equals(vec);
        public bool Equals(angle other) => turn == other.turn;
        public override string ToString() => turn.ToString("0.000");
        public static vec2 rotate(vec2 vec, angle a)
        {
            var cs = cos(a);
            var sn = sin(a);
            float x = vec.x * cs + vec.y * sn;
            float y = -vec.x * sn + vec.y * cs;
            return new vec2(x, y);
        }
        public static vec3 rotate(vec3 vec, angle a)
        {
            var cs = cos(a);
            var sn = sin(a);
            float x = vec.x * cs + vec.z * sn;
            float z = -vec.x * sn + vec.z * cs;
            return new vec3(x, vec.y, z);
        }

        private static float direction(float src, float trg) => ((trg - src + 1.5f) % 1f) - 0.5f;
        public static float distance(float src, float trg) => abs(direction(src, trg));
        public static float direction(angle src, angle trg) => direction(src.turn, trg.turn);
        public static angle distance(angle src, angle trg) => new(distance(src.turn, trg.turn));
        public static float sin(float t) => math.sin(t * tau);
        public static float sin(angle a) => math.sin(a.rad);
        public static float cos(angle a) => math.cos(a.rad);
        public static angle lerp(angle current, angle target, float t)
        {
            var delta = 0.5f - current.turn;
            return new angle(wrap(math.lerp(0.5f, wrap(target.turn + delta), t) - delta));
        }
        public static angle moveto(angle current, angle target, angle t)
        {
            float delta = 0.5f - current.turn;
            return new angle(wrap(math.moveto(0.5f, wrap(target.turn + delta), t.turn) - delta));
        }
        private static angle nearest(angle current, angle first, angle second) => distance(current.turn, first.turn) <= distance(current.turn, second.turn) ? first : second;
        public static angle clamp(angle current, angle min, angle max)
        {
            float hfrange = wrap(min.turn - max.turn) * 0.5f;
            return distance(current.turn, wrap(min.turn + hfrange)) > hfrange ? nearest(current, min, max) : current;
        }
    }
}