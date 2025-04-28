using System;
using System.Runtime.CompilerServices;
#pragma warning disable IDE1006

namespace DVG.MathsOld
{
    public static partial class math
    {
        public const float pi = MathF.PI;
        public const float invpi = 1f / pi;
        public const float tau = pi * 2;
        public const float tausqr = tau * tau;
        public const float invtau = 1f / tau;
        public const float hfpi = pi / 2f;
        public const float deg2rad = pi / 180f;
        public const float rad2deg = 180f / pi;
        public const float rad2deg2 = 2 * rad2deg;

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static float min(float x, float y) => x <= y ? x : y;
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static float max(float x, float y) => x >= y ? x : y;
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static float minmax(float x, float y, out float min, out float max) => min = x + y - (max = math.max(x, y));
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static float autoclamp(float value, float r1, float r2) => value < minmax(r1, r2, out var mi, out var ma) ? mi : value > ma ? ma : value;
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static float sin(float radians) => MathF.Sin(radians);
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static float cos(float radians) => MathF.Cos(radians);
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static float acos(float cos) => MathF.Acos(cos);
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static float asin(float sin) => MathF.Asin(sin);
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static float abs(float value) => MathF.Abs(value);
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static float sign(float value) => value < 0f ? -1f : value > 0f ? 1f : 0f;
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static float sqrt(float value) => MathF.Sqrt(value);
        public static float absmax(float x, float y) => abs(x) >= abs(y) ? x : y;
    }


    /// <summary>
    /// Interpolations math
    /// </summary>
    public static partial class math
    {
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static float lerp(float a, float b, float value) => a + ((b - a) * value);
        public static float lerp2(float a, float b, float value)
        {
            var delta = b - a;
            return autoclamp(a + (absmax(sign(delta) * (delta * delta), delta) * value), a, b);
        }
    }

    /// <summary>
    /// Interpolation helpers
    /// </summary>
    public static partial class math
    {
        /// <summary>
        /// https://www.desmos.com/calculator/xbijqd2szc
        /// </summary>
        public static float tocos(float x) => 1 - cos(x * hfpi);
        /// <summary>
        /// https://www.desmos.com/calculator/kqr5q9t3bk
        /// </summary>
        public static float tosin(float x) => sin(x * hfpi);
        /// <summary>
        /// https://www.desmos.com/calculator/evxro3xanq
        /// </summary>
        public static float tocossin(float x) => x * x * (3f - (2f * x)); // same as (1 - cos(x * pi)) * 0.5f but faster
        /// <summary>
        /// https://www.desmos.com/calculator/26f0ys1o0t
        /// </summary>
        public static float tosincos(float x) => x < 0.5f ? sin(x * pi) * 0.5f : 1 - (sin(x * pi) * 0.5f);
        /// <summary>
        /// https://www.desmos.com/calculator/2vao1a2uhx
        /// </summary>
        public static float toasin(float x) => (asin((2 * x) - 1) * invpi) + 0.5f;
        /// <summary>
        /// https://www.desmos.com/calculator/xtluas7pw8
        /// </summary>
        public static float arc(float x) => cos((x - 0.5f) * pi);

        public static float weighted(float x) => x * x;
    }
}