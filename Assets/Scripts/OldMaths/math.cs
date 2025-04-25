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
        public static float min(float x, float y, out float res) => res = min(x, y);
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static float max(float x, float y, out float res) => res = max(x, y);
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static float minmax(float x, float y, out float min, out float max) => min = x + y - (max = math.max(x, y));
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static int min(int x, int y) => x <= y ? x : y;
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static int step(float edge, float x) => x < edge ? 0 : 1;
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static int max(int x, int y) => x >= y ? x : y;
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static float clamp01(float value) => value < 0f ? 0f : value > 1f ? 1f : value;
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static float clamp(float value, float min, float max) => value < min ? min : value > max ? max : value;
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static int clamp(int value, int min, int max) => value < min ? min : value > max ? max : value;
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static float autoclamp(float value, float r1, float r2) => value < minmax(r1, r2, out var mi, out var ma) ? mi : value > ma ? ma : value;
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static float floor(float value) => MathF.Floor(value);
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static float repeat(float t, float length) => clamp(t - (floor(t / length) * length), 0f, length);
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static float sin(float radians) => MathF.Sin(radians);
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static float cos(float radians) => MathF.Cos(radians);
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static float acos(float cos) => MathF.Acos(cos);
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static float asin(float sin) => MathF.Asin(sin);
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static float atan2(float y, float x) => MathF.Atan2(y, x);
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static float abs(float value) => MathF.Abs(value);
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static float sign(float value) => value < 0f ? -1f : value > 0f ? 1f : 0f;
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static float sqrt(float value) => MathF.Sqrt(value);
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static float cbrt(float value) => MathF.Pow(value, 1.0f / 3.0f);
        public static float absmin(float x, float y) => abs(x) <= abs(y) ? x : y;
        public static float absmax(float x, float y) => abs(x) >= abs(y) ? x : y;
        public static float tan(float a) => MathF.Tan(a);

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static int MinSqrt(int value1, int value2)
        {
            int m = min(value1, value2);
            return m <= 3 ? m : (int)sqrt(m);
        }
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

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static float unlerp(float a, float b, float value) => a == b ? 0 : clamp01((value - a) / (b - a));
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static float moveto(float current, float target, float maxDelta)
        {
            float delta = target - current;
            return current + (sign(delta) * min(maxDelta, abs(delta)));
        }

        public static float smoothstep(float from, float to, float t)
        {
            t = tocossin(t);
            return (to * t) + (from * (1f - t));
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static float arclerp(float start, float end, float height, float percent) => lerp(start, end, percent) + (height * arc(percent));
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static float remap(float source, float sourceFrom, float sourceTo, float targetFrom, float targetTo) => targetFrom + ((source - sourceFrom) * (targetTo - targetFrom) / (sourceTo - sourceFrom));

        public static vec3 Shoot(vec3 start, vec3 end, float height, float percent) => lerpAs2(start, end, tosin(percent), arclerp(start.y, end.y, height, tosincos(percent)));
        public static vec3 arclerp(vec3 start, vec3 end, float height, float percent) => lerpAs2(start, end, percent, arclerp(start.y, end.y, height, percent));
        private static vec3 lerpAs2(vec3 a, vec3 b, float t, float y) => new(a.x + ((b.x - a.x) * t), y, a.z + ((b.z - a.z) * t));

        public static float smoothdamp(float src, float trg, ref float vel, float smoothTime, float dt, float maxSpeed = float.PositiveInfinity)
        {
            smoothTime = max(0.0001f, smoothTime);
            float maxDelta = maxSpeed * smoothTime;
            float omega = 2f / smoothTime;
            float delta = clamp(src - trg, -maxDelta, maxDelta);
            trg = src - delta;
            float x = dt * omega;
            float exp = 1f / (1f + x + (x * x * ((x * 0.235f) + 0.48f)));
            float temp = (vel * dt) + (x * delta);
            vel = (vel - (omega * temp)) * exp;
            float move = (delta + temp) * exp;
            float finalPosition = trg + move;
            if ((-delta > 0f) == (move > 0))
            {
                finalPosition = trg;
                vel = (finalPosition - trg) / dt;
            }

            return finalPosition;
        }
        public static float smoothdamper(float src, float trg, ref float vel, float smoothTime, float dt)
        {
            float omega = 2f / smoothTime;
            float delta = src - trg;
            float x = dt * omega;
            float exp = 1f / (1f + x + (x * x * ((x * 0.235f) + 0.48f)));
            float temp = (vel * dt) + (x * delta);
            vel = (vel - (omega * temp)) * exp;
            float move = (delta + temp) * exp;
            bool finalized = sign(-delta) == sign(move);
            float final = finalized ? trg : trg + move;
            vel = finalized ? 0 : vel;
            return final;
        }

        public static float SmoothDampold(float current, float target, ref float currentVelocity, float smoothTime, float deltaTime, float maxSpeed = float.PositiveInfinity)
        {
            smoothTime = max(0.0001f, smoothTime);
            float velocityFactor = 2f / smoothTime;
            float deltaTimeFactor = velocityFactor * deltaTime;
            float alpha = 1f / (1f + deltaTimeFactor + (0.48f * deltaTimeFactor * deltaTimeFactor) + (0.235f * deltaTimeFactor * deltaTimeFactor * deltaTimeFactor));
            float distanceToTarget = current - target;
            float clampedDistance = clamp(distanceToTarget, 0f - (maxSpeed * smoothTime), maxSpeed * smoothTime);
            target = current - clampedDistance;
            float velocityTerm = (currentVelocity + (velocityFactor * distanceToTarget)) * deltaTime;
            currentVelocity = (currentVelocity - (velocityFactor * velocityTerm)) * alpha;
            float finalPosition = target + ((distanceToTarget + velocityTerm) * alpha);
            if (((target - current) > 0f) == (finalPosition > target))
            {
                finalPosition = target;
                currentVelocity = (finalPosition - target) / deltaTime;
            }
            return finalPosition;
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