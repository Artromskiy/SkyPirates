using System;
using System.Collections.Generic;
using System.Runtime.CompilerServices;
using UnityEngine;
using Random = UnityEngine.Random;

namespace DVG.Maths
{
    /// <summary>
    /// Vector Extensions
    /// </summary>
    public static class VectorExtensions
    {

        private static readonly System.Random _random = new();

        public static float randomSingle => Random.value;// (float)_random.Next() / int.MaxValue;

        public static vec3 insideUnitSphere
        {
            get
            {
                float u = randomSingle;
                float v = randomSingle;
                float theta = u * math.tau;
                float r = math.cbrt(randomSingle);
                float phi = math.acos((2 * v) - 1);

                float sinTheta = math.sin(theta);
                float sinPhi = math.sin(phi);
                float cosPhi = math.cos(phi);
                float cosTheta = math.cos(theta);
                float x = r * sinPhi * cosTheta;
                float y = r * sinPhi * sinTheta;
                float z = r * cosPhi;
                return new vec3(x, y, z);
            }
        }
        /// <summary>
        /// To XZ
        /// </summary>
        /// <param name="vec"></param>
        /// <returns></returns>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static Vector2 NoY(this Vector3 vec)
        {
            return new Vector2(vec.x, vec.z);
        }
        /// <summary>
        /// Set Y zero
        /// </summary>
        /// <param name="vec"></param>
        /// <returns></returns>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static Vector3 ToY0(this Vector3 vec)
        {
            return new Vector3(vec.x, 0, vec.z);
        }

        /// <summary>
        /// From XZ
        /// </summary>
        /// <param name="vec"></param>
        /// <returns></returns>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static Vector3 ToY0(this Vector2 vec)
        {
            return new Vector3(vec.x, 0f, vec.y);
        }

        /// <summary>
        /// To XZ Normalized
        /// </summary>
        /// <param name="vec"></param>
        /// <returns></returns>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static Vector2 NoYNorm(this Vector3 vec)
        {
            return vec.NoY().normalized;
        }

        /// <summary>
        /// To XZ 3D Normalized
        /// </summary>
        /// <param name="vec"></param>
        /// <returns></returns>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static Vector3 ToY0Norm(this Vector3 vec)
        {
            return vec.ToY0().normalized;
        }
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static bool IsZero(this Vector3 vec)
        {
            return vec.x == 0 && vec.y == 0 && vec.z == 0;
        }
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static bool IsZero(this Vector2 vec)
        {
            return vec.x == 0 && vec.y == 0;
        }
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static bool IsOne(this Vector3 vec)
        {
            return vec.x == 1 && vec.y == 1 && vec.z == 1;
        }
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static bool IsOne(this Vector2 vec)
        {
            return vec.x == 1 && vec.y == 1;
        }

        public static void SetLeft(this RectTransform rt, float left)
        {
            rt.offsetMin = new Vector2(left, rt.offsetMin.y);
        }

        public static void SetRight(this RectTransform rt, float right)
        {
            rt.offsetMax = new Vector2(-right, rt.offsetMax.y);
        }

        public static void SetTop(this RectTransform rt, float top)
        {
            rt.offsetMax = new Vector2(rt.offsetMax.x, -top);
        }

        public static void SetBottom(this RectTransform rt, float bottom)
        {
            rt.offsetMin = new Vector2(rt.offsetMin.x, bottom);
        }

        public static bool All<T>(this List<T> list, Predicate<T> mathc)
        {
            int count = list.Count;
            for (int i = 0; i < count; i++)
                if (!mathc(list[i]))
                    return false;
            return true;
        }
        public static bool Any<T>(this List<T> list, Predicate<T> mathc)
        {
            int count = list.Count;
            for (int i = 0; i < count; i++)
                if (mathc(list[i]))
                    return true;
            return false;
        }

        public static bool All<T>(this T[] array, Predicate<T> mathc)
        {
            int count = array.Length;
            for (int i = 0; i < count; i++)
                if (!mathc(array[i]))
                    return false;
            return true;
        }
        public static bool Any<T>(this T[] array, Predicate<T> mathc)
        {
            int count = array.Length;
            for (int i = 0; i < count; i++)
                if (mathc(array[i]))
                    return true;
            return false;
        }

        public static T FindLowest<T>(this List<T> list, Func<T, float> comparer, Func<T, bool> match = null)
        {
            match ??= (_) => true;
            float minDistance = float.MaxValue;
            T result = default;
            bool found = false;
            foreach (var item in list)
            {
                if (match(item))
                {
                    var distance = comparer(item);
                    if (!found || distance < minDistance)
                    {
                        found = true;
                        minDistance = distance;
                        result = item;
                    }
                }
            }
            return result;
        }

        /// <summary>
        /// Distance Sqrd
        /// </summary>
        /// <param name="a"></param>
        /// <param name="b"></param>
        /// <returns></returns>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static float DistanceSqrdXZ(this Vector3 a, Vector3 b)
        {
            float x = a.x - b.x;
            float z = a.z - b.z;
            return (x * x) + (z * z);
        }
        /// <summary>
        /// Distance Sqrd
        /// </summary>
        /// <param name="a"></param>
        /// <param name="b"></param>
        /// <returns></returns>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static float DistanceSqrdXZ(this Vector2 a, Vector2 b)
        {
            float x = a.x - b.x;
            float z = a.y - b.y;
            return (x * x) + (z * z);
        }
        /// <summary>
        /// Distance Sqrd
        /// </summary>
        /// <param name="a"></param>
        /// <param name="b"></param>
        /// <returns></returns>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static float DistanceSqrdXZ(this Vector2 a, Vector3 b)
        {
            float x = a.x - b.x;
            float z = a.y - b.z;
            return (x * x) + (z * z);
        }

        /// <summary>
        /// Distance Sqrd
        /// </summary>
        /// <param name="a"></param>
        /// <param name="b"></param>
        /// <returns></returns>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static float DistanceSqrdXZ(this vec3 a, vec3 b)
        {
            float x = a.x - b.x;
            float z = a.z - b.z;
            return (x * x) + (z * z);
        }
        /// <summary>
        /// Distance Sqrd
        /// </summary>
        /// <param name="a"></param>
        /// <param name="b"></param>
        /// <returns></returns>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static float DistanceSqrdXZ(this vec2 a, vec3 b)
        {
            float x = a.x - b.x;
            float z = a.y - b.z;
            return (x * x) + (z * z);
        }
        /// <summary>
        /// Shuffle
        /// </summary>
        /// <param name="list"></param>
        /// <typeparam name="T"></typeparam>
        public static void Shuffle<T>(this IList<T> list)
        {
            for (int i = 0; i < list.Count; i++)
            {
                T temp = list[i];
                int randomIndex = Random.Range(i, list.Count);
                list[i] = list[randomIndex];
                list[randomIndex] = temp;
            }
        }

        /// <summary>
        /// Shuffle
        /// </summary>
        /// <param name="list"></param>
        /// <typeparam name="T"></typeparam>
        public static T GetRandom<T>(this IList<T> list)
        {
            return list[Random.Range(0, list.Count)];
        }
        /// <summary>
        /// Shuffle
        /// </summary>
        /// <param name="list"></param>
        /// <typeparam name="T"></typeparam>
        public static T GetRandomNoFirst<T>(this IList<T> list)
        {
            return list[Random.Range(1, list.Count)];
        }
        /// <summary>
        /// Gets random element from set;
        /// </summary>
        /// <param name="set"></param>
        /// <typeparam name="T"></typeparam>
        public static T GetRandom<T>(this HashSet<T> set)
        {
            var current = 0;
            var random = Random.Range(0, set.Count);
            T result = default;
            foreach (var item in set)
            {
                result = item;
                if (current == random)
                    break;
                current++;
            }
            return result;
        }
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static void Except<T>(this HashSet<T> set, HashSet<T> except)
        {
            foreach (var item in except)
                set.Remove(item);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static void Union<T>(this HashSet<T> set, HashSet<T> union)
        {
            foreach (var item in union)
                set.Add(item);
        }

        public static void NormalizeVectorArray(this Vector3[] array)
        {
            Vector3 middlePoint = Vector3.zero;
            int count = array.Length;
            for (int i = 0; i < count; i++)
            {
                middlePoint += array[i];
            }
            middlePoint /= count;
            for (int i = 0; i < count; i++)
            {
                array[i] -= middlePoint;
            }
        }

        public static void MultiplyArray(this Vector3[] array, float multiplier)
        {
            int count = array.Length;
            for (int i = 0; i < count; i++)
            {
                array[i] *= multiplier;
            }
        }


        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static Vector3 Right(this Vector3 vec)
        {
            return new Vector3(vec.z, vec.y, -vec.x);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static Vector3 Left(this Vector3 vec)
        {
            return new Vector3(-vec.z, vec.y, vec.x);
        }
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static Vector2 Left(this Vector2 vec)
        {
            return new Vector2(-vec.y, vec.x);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static Quaternion ToY(this Quaternion quaternion)
        {
            return Quaternion.Euler(0, quaternion.eulerAngles.y, 0);
        }
        public static string KiloFormat(this int num)
        {
            if (num >= 100000000)
                return (num / 1000000).ToString("#,0M");

            if (num >= 10000000)
                return (num / 1000000).ToString("0.#") + "M";

            if (num >= 100000)
                return (num / 1000).ToString("#,0K");

            if (num >= 10000)
                return (num / 1000).ToString("0.#") + "K";

            return num.ToString("#,0");
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static Vector2 Rotate(this Vector2 v, float degrees)
        {
            return v.RotateRadians(degrees * Mathf.Deg2Rad);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static Vector2 RotateRadians(this Vector2 v, float radians)
        {
            var ca = math.cos(radians);
            var sa = math.sin(radians);
            return new Vector2((-ca * v.x) + (sa * v.y), (-sa * v.x) - (ca * v.y));
        }



        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static void GetAround(float minRadius, Span<Vector3> around)
        {
            var radius = Mathf.Max(around.Length / (Mathf.PI * 2), minRadius);
            float angle = 360f / around.Length;
            for (int i = 0; i < around.Length; i++)
            {
                around[i] = Quaternion.Euler(0, angle * i, 0) * Vector3.forward * radius;
            }
        }


        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static bool IsNaN(this Vector3 value)
        {
            return float.IsNaN(value.x) || float.IsNaN(value.y) || float.IsNaN(value.z);
        }
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static bool IsNaN(this Vector2 value)
        {
            return float.IsNaN(value.x) || float.IsNaN(value.y);
        }
    }

    public static class CameraHelper
    {
        private static readonly Plane ZeroPlane = new(Vector3.up, Vector3.forward);
        private static Camera _camera;
        private static Matrix4x4 _viewProjection;
        private static Matrix4x4 _projection;
        private static Matrix4x4 _worldToCamera;
        private static vec3 ScreenSize;
        private static float ScreenWidth;
        private static float ScreenHeight;
        private static ref Matrix4x4 ViewProjection => ref _viewProjection;

        public static void UpdateMatrices()
        {
            ScreenWidth = Screen.width;
            ScreenHeight = Screen.height;
            ScreenSize = new vec3(ScreenWidth, ScreenHeight, 1);
            if (Camera(out var cam))
            {
                _projection = cam.projectionMatrix;
                _worldToCamera = cam.worldToCameraMatrix;
                _viewProjection = _projection * _worldToCamera;
            }
        }

        public static bool Camera(out Camera camera)
        {
            if (_camera == null)
                _camera = UnityEngine.Camera.main;
            camera = _camera;
            return _camera != null;
        }

        public static vec3 World2ViewPort(vec3 worldPosition)
        {
            var (x, y, z, w) = MathHelper.Mul(ViewProjection, (worldPosition.x, worldPosition.y, worldPosition.z, 1f));
            return new vec3(((x / w) + 1) * 0.5f, ((y / w) + 1) * 0.5f, (z + 1f) * 0.5f);
        }
        public static vec3 World2Canvas(vec3 worldPosition) => ViewPort2Canvas(World2ViewPort(worldPosition));
        public static vec3 ViewPort2Canvas(vec3 viewportPosition) => viewportPosition * ScreenSize;

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static bool Visible(vec3 position)
        {
            vec3 point = World2ViewPort(position);
            return point.x >= 0 && point.x <= 1 && point.y >= 0 && point.y <= 1;
        }
        public static bool ViewPortVisible(vec3 canvasPosition)
        {
            return canvasPosition.x >= 0 && canvasPosition.x <= 1 && canvasPosition.y >= 0 && canvasPosition.y <= 1;
        }

        public static vec3 FromCanvasSpace(vec3 screenPos)
        {
            Matrix4x4 worldToLocalMatrix = _worldToCamera;
            Matrix4x4 projectionMatrix = _projection;
            vec3 ndcPos = new((screenPos.x / Screen.width * 2) - 1, (screenPos.y / Screen.height * 2) - 1, screenPos.z);
            var (x, y, z, w) = MathHelper.Mul(projectionMatrix.inverse, (ndcPos.x, ndcPos.y, ndcPos.z, 0));
            Vector3 worldPos = worldToLocalMatrix.MultiplyPoint(new vec3(x, y, z) / w);

            return worldPos;
        }

        public static bool LookPoint(out Vector3 res)
        {
            res = Vector3.zero;
            if (!Camera(out var cam))
                return false;
            var transform = cam.transform;
            var camPos = transform.position;
            var camFwd = transform.forward;
            var ray = new Ray(camPos, camFwd);

            if (!ZeroPlane.Raycast(ray, out var distance))
                return false;
            res = camPos + (camFwd * distance);
            return true;
        }

        public static float LookPointSqrDistance(this Vector3 from)
        {
            if (!LookPoint(out var to))
                return float.MaxValue;
            return from.NoY().DistanceSqrdXZ(to.NoY());
        }
    }

    public static class MathHelper
    {
        private static readonly Vector4 kDecodeDot = new(1f, 1f / 255f, 1f / 65025f, 1f / 16581375f);
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static (float x, float y, float z, float w) Mul(in Matrix4x4 lhs, (float x, float y, float z, float w) vector)
        {
            (float x, float y, float z, float w) result;
            result.x = (lhs.m00 * vector.x) + (lhs.m01 * vector.y) + (lhs.m02 * vector.z) + (lhs.m03 * vector.w);
            result.y = (lhs.m10 * vector.x) + (lhs.m11 * vector.y) + (lhs.m12 * vector.z) + (lhs.m13 * vector.w);
            result.z = (lhs.m20 * vector.x) + (lhs.m21 * vector.y) + (lhs.m22 * vector.z) + (lhs.m23 * vector.w);
            result.w = (lhs.m30 * vector.x) + (lhs.m31 * vector.y) + (lhs.m32 * vector.z) + (lhs.m33 * vector.w);
            return result;
        }


        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static Color Float2RGBA(float v)
        {
            Vector4 kEncodeMul = new(1.0f, 255.0f, 65025.0f, 16581375.0f);
            float kEncodeBit = 1.0f / 255.0f;
            Vector4 enc = kEncodeMul * v;
            enc = new Vector4(enc.x % 1, enc.y % 1, enc.z % 1, enc.w % 1);
            enc -= new Vector4(enc.y, enc.z, enc.w, enc.w) * kEncodeBit;
            return enc;
        }
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static float RGBA2Float(Vector4 enc)
        {
            return Vector4.Dot(enc, kDecodeDot);
        }

        public static T[][] MultiCombination<T>(T[] values, int count)
        {
            List<T[]> multiCombo = new();
            MultiCombination(0, 0, values, new T[count], multiCombo.Add);
            return multiCombo.ToArray();
        }

        private static void MultiCombination<T>(int ind, int begin, T[] set, T[] buffer, Action<T[]> onResult)
        {
            int n = set.Length;
            for (int i = begin; i < n; i++)
            {
                buffer[ind] = set[i];
                if (ind + 1 < buffer.Length)
                    MultiCombination(ind + 1, i, set, buffer, onResult);
                else
                    onResult?.Invoke((T[])buffer.Clone());
            }
        }

        public static void SortChances<T>((T enumValue, int chance)[] array)
        {
            Array.Sort(array, (v1, v2) => v2.chance.CompareTo(v1.chance));
            for (int i = 1; i < array.Length; i++)
            {
                array[i].chance += array[i - 1].chance;
            }
        }

        public static T GetChancedValue<T>((T enumValue, int chance)[] chanceT)
        {
            int rnd = Random.Range(0, chanceT[chanceT.Length - 1].chance + 1);
            T cargoType = default;
            for (int i = 0; i < chanceT.Length; i++)
            {
                if (rnd <= chanceT[i].chance)
                {
                    cargoType = chanceT[i].enumValue;
                    break;
                }
            }
            return cargoType;
        }

        public static bool IsPointInPolygon(Span<Vector2> polygon, Vector2 testPoint)
        {
            bool result = false;
            int j = polygon.Length - 1;
            int count = polygon.Length;
            for (int i = 0; i < count; i++)
            {
                if ((polygon[i].y < testPoint.y && polygon[j].y >= testPoint.y) || (polygon[j].y < testPoint.y && polygon[i].y >= testPoint.y))
                {
                    if (polygon[i].x + ((testPoint.y - polygon[i].y) / (polygon[j].y - polygon[i].y) * (polygon[j].x - polygon[i].x)) < testPoint.x)
                    {
                        result = !result;
                    }
                }
                j = i;
            }
            return result;
        }

        public static bool IsPointInPolygon(Vector2[] polygon, Vector2 testPoint)
        {
            bool result = false;
            int j = polygon.Length - 1;
            int count = polygon.Length;
            for (int i = 0; i < count; i++)
            {
                if ((polygon[i].y < testPoint.y && polygon[j].y >= testPoint.y) || (polygon[j].y < testPoint.y && polygon[i].y >= testPoint.y))
                {
                    if (polygon[i].x + ((testPoint.y - polygon[i].y) / (polygon[j].y - polygon[i].y) * (polygon[j].x - polygon[i].x)) < testPoint.x)
                    {
                        result = !result;
                    }
                }
                j = i;
            }
            return result;
        }

        public static bool IsPointInPolygon(List<Vector2> polygon, Vector2 testPoint)
        {
            bool result = false;
            int j = polygon.Count - 1;
            int count = polygon.Count;
            for (int i = 0; i < count; i++)
            {
                if ((polygon[i].y < testPoint.y && polygon[j].y >= testPoint.y) || (polygon[j].y < testPoint.y && polygon[i].y >= testPoint.y))
                {
                    if (polygon[i].x + ((testPoint.y - polygon[i].y) / (polygon[j].y - polygon[i].y) * (polygon[j].x - polygon[i].x)) < testPoint.x)
                    {
                        result = !result;
                    }
                }
                j = i;
            }
            return result;
        }
        public static bool IsPointInPolygon(Span<Vector3> polygon, Vector2 testPoint)
        {
            bool result = false;
            int j = polygon.Length - 1;
            int count = polygon.Length;
            for (int i = 0; i < count; i++)
            {
                if ((polygon[i].z < testPoint.y && polygon[j].z >= testPoint.y) || (polygon[j].z < testPoint.y && polygon[i].z >= testPoint.y))
                {
                    if (polygon[i].x + ((testPoint.y - polygon[i].z) / (polygon[j].z - polygon[i].z) * (polygon[j].x - polygon[i].x)) < testPoint.x)
                    {
                        result = !result;
                    }
                }
                j = i;
            }
            return result;
        }
        public static bool IsPointInPolygon(Vector3[] polygon, Vector2 testPoint)
        {
            bool result = false;
            int j = polygon.Length - 1;
            int count = polygon.Length;
            for (int i = 0; i < count; i++)
            {
                if ((polygon[i].z < testPoint.y && polygon[j].z >= testPoint.y) || (polygon[j].z < testPoint.y && polygon[i].z >= testPoint.y))
                {
                    if (polygon[i].x + ((testPoint.y - polygon[i].z) / (polygon[j].z - polygon[i].z) * (polygon[j].x - polygon[i].x)) < testPoint.x)
                    {
                        result = !result;
                    }
                }
                j = i;
            }
            return result;
        }
        public static bool IsPointInPolygon(List<Vector3> polygon, Vector2 testPoint)
        {
            bool result = false;
            int j = polygon.Count - 1;
            int count = polygon.Count;
            for (int i = 0; i < count; i++)
            {
                if ((polygon[i].z < testPoint.y && polygon[j].z >= testPoint.y) || (polygon[j].z < testPoint.y && polygon[i].z >= testPoint.y))
                {
                    if (polygon[i].x + ((testPoint.y - polygon[i].z) / (polygon[j].z - polygon[i].z) * (polygon[j].x - polygon[i].x)) < testPoint.x)
                    {
                        result = !result;
                    }
                }
                j = i;
            }
            return result;
        }

        /// <summary>
        /// Get Circle Tangents
        /// </summary>
        /// <param name="center"></param>
        /// <param name="r"></param>
        /// <param name="p"></param>
        /// <param name="tanPosA"></param>
        /// <param name="tanPosB"></param>
        /// <returns></returns>
        public static bool CircleTangents_2(Vector2 center, float r, Vector2 p, out Vector2 tanPosA, out Vector2 tanPosB)
        {
            p -= center;

            float P = p.magnitude;
            tanPosA = tanPosB = Vector2.zero;
            // if p is inside the circle, there ain't no tangents.
            if (P <= r)
            {
                return false;
            }

            float a = r * r / P;
            float q = r * (float)System.Math.Sqrt((P * P) - (r * r)) / P;

            Vector2 pN = p / P;
            Vector2 pNP = new(-pN.y, pN.x);
            Vector2 va = pN * a;

            tanPosA = va + (pNP * q);
            tanPosB = va - (pNP * q);

            tanPosA += center;
            tanPosB += center;

            return true;
        }
        public static void ManhattanNear(Vector2Int zone, Span<Vector2Int> near)
        {
            near[0] = zone + Vector2Int.up;
            near[1] = zone + Vector2Int.right;
            near[2] = zone + Vector2Int.down;
            near[3] = zone + Vector2Int.left;
        }

        public static void ManhattanNear(Vector2 zone, Span<Vector2> near)
        {
            near[0] = zone + Vector2.up;
            near[1] = zone + Vector2.right;
            near[2] = zone + Vector2.down;
            near[3] = zone + Vector2.left;
        }

        public static void ChebyshevNear(Vector2Int zone, Span<Vector2Int> near)
        {
            near[0] = zone + Vector2Int.up;
            near[1] = zone + Vector2Int.up + Vector2Int.right;
            near[2] = zone + Vector2Int.right;
            near[3] = zone + Vector2Int.right + Vector2Int.down;
            near[4] = zone + Vector2Int.down;
            near[5] = zone + Vector2Int.down + Vector2Int.left;
            near[6] = zone + Vector2Int.left;
            near[7] = zone + Vector2Int.left + Vector2Int.up;
        }
        public static void ChebyshevNear(Vector2 zone, Span<Vector2> near)
        {
            near[0] = zone + Vector2.up;
            near[1] = zone + Vector2.up + Vector2.right;
            near[2] = zone + Vector2.right;
            near[3] = zone + Vector2.right + Vector2.down;
            near[4] = zone + Vector2.down;
            near[5] = zone + Vector2.down + Vector2.left;
            near[6] = zone + Vector2.left;
            near[7] = zone + Vector2.left + Vector2.up;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static bool DistanceLessXZ(vec3 from, vec3 target1, vec3 target2)
        {
            float x1 = from.x - target1.x;
            float z1 = from.z - target1.z;
            float x2 = from.x - target2.x;
            float z2 = from.z - target2.z;
            return ((x1 * x1) + (z1 * z1)) < ((x2 * x2) + (z2 * z2));
        }
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static bool DistanceLessXZ(vec2 from, vec3 target1, vec3 target2)
        {
            float x1 = from.x - target1.x;
            float z1 = from.y - target1.z;
            float x2 = from.x - target2.x;
            float z2 = from.y - target2.z;
            return ((x1 * x1) + (z1 * z1)) < ((x2 * x2) + (z2 * z2));
        }

        public static bool DistanceLessXZ(vec3 a, vec3 b, float distance)
        {
            float x = a.x - b.x;
            float z = a.z - b.z;
            return (x * x) + (z * z) < distance * distance;
        }
        public static bool DistanceLessXZ(vec2 a, vec3 b, float distance)
        {
            float x = a.x - b.x;
            float z = a.y - b.z;
            return (x * x) + (z * z) < distance * distance;
        }
        public static bool DistanceLessXZ(vec3 a, vec2 b, float distance)
        {
            float x = a.x - b.x;
            float z = a.z - b.y;
            return (x * x) + (z * z) < distance * distance;
        }
        public static bool DistanceLessXZ(vec2 a, vec2 b, float distance)
        {
            float x = a.x - b.x;
            float z = a.y - b.y;
            return (x * x) + (z * z) < distance * distance;
        }
    }
}