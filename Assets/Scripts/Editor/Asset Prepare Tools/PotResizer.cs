using System;
using System.IO;
using UnityEditor;
using UnityEngine;
using DVG.MathsOld;
using static DVG.MathsOld.math;

namespace DVG.Editor.AssetPrepareTools
{
    [CreateAssetMenu(fileName = "PotResizer", menuName = "Tools/PotResizer")]
    public class PotResizer : ScriptableObject
    {
        [SerializeField]
        private Sprite[] _toResize;

        private Color32[] _clearArray = new Color32[0];

        [ContextMenu("Group Crop")]
        public void GroupCrop()
        {
            int4 minmax = new(int.MaxValue, int.MaxValue, int.MinValue, int.MinValue);
            foreach (var item in _toResize)
            {
                Texture2D texture = Copy(item.texture);
                var minmaxT = GetMinMax(texture);
                minmax = minmax.minMax(minmaxT);
                DestroyImmediate(texture);
            }

            minmax.x = max(minmax.x - 1, 0);
            minmax.y = max(minmax.y - 1, 0);
            minmax.z = min(minmax.z + 1, _toResize[0].texture.width);
            minmax.w = min(minmax.w + 1, _toResize[0].texture.height);

            int width = minmax.z - minmax.x;
            int height = minmax.w - minmax.y;

            foreach (var item in _toResize)
            {
                if (width == item.texture.width && height == item.texture.height)
                    continue;
                Texture2D texture = Crop(item.texture, minmax.x, minmax.y, width, height);
                var encoded = ImageConversion.EncodeToPNG(texture);
                var path = AssetDatabase.GetAssetPath(item);
                File.WriteAllBytes(path, encoded);
                DestroyImmediate(texture);
            }
            AssetDatabase.Refresh();
        }

        [ContextMenu("Group PoT")]
        public void GroupPot()
        {
            foreach (var item in _toResize)
            {
                if (Mathf.IsPowerOfTwo(item.texture.width) && Mathf.IsPowerOfTwo(item.texture.height))
                    continue;
                Texture2D texture = PoT(item.texture);
                var encoded = ImageConversion.EncodeToPNG(texture);
                var path = AssetDatabase.GetAssetPath(item);
                File.WriteAllBytes(path, encoded);
                DestroyImmediate(texture);
            }
            AssetDatabase.Refresh();
        }

        [ContextMenu("Group Square")]
        public void GroupSquare()
        {
            foreach (var item in _toResize)
            {
                if (item.texture.width == item.texture.height)
                    continue;
                Texture2D texture = Square(item.texture);
                var encoded = ImageConversion.EncodeToPNG(texture);
                var path = AssetDatabase.GetAssetPath(item);
                File.WriteAllBytes(path, encoded);
                DestroyImmediate(texture);
            }
            AssetDatabase.Refresh();
        }

        [ContextMenu("Group PoT Aspect")]
        public void GroupPoTAspect()
        {
            foreach (var item in _toResize)
            {
                if (item.texture.width == item.texture.height)
                    continue;
                Texture2D texture = PoTAspect(item.texture);
                var encoded = ImageConversion.EncodeToPNG(texture);
                var path = AssetDatabase.GetAssetPath(item);
                File.WriteAllBytes(path, encoded);
                DestroyImmediate(texture);
            }
            AssetDatabase.Refresh();
        }

        public Texture2D PoT(Texture2D originalTexture)
        {
            originalTexture = Copy(originalTexture);
            var prevWidth = originalTexture.width;
            var prevHeight = originalTexture.height;

            int width = Mathf.IsPowerOfTwo(prevWidth) ? prevWidth : Mathf.NextPowerOfTwo(prevWidth);
            int height = Mathf.IsPowerOfTwo(prevHeight) ? prevHeight : Mathf.NextPowerOfTwo(prevHeight);
            Texture2D newTexture = CreateTexture(width, height);
            var newx = (width - prevWidth) / 2;
            var newy = (height - prevHeight) / 2;
            Graphics.CopyTexture(originalTexture, 0, 0, 0, 0, prevWidth, prevHeight, newTexture, 0, 0, newx, newy);
            newTexture.Apply();

            return newTexture;
        }

        public Texture2D PoTAspect(Texture2D originalTexture)
        {
            originalTexture = Copy(originalTexture);
            var prevWidth = originalTexture.width;
            var prevHeight = originalTexture.height;
            var widthK = prevWidth + ToPoTRatio(prevWidth, prevHeight);
            var heightK = prevHeight + ToPoTRatio(prevHeight, prevWidth);
            bool selectWidth = widthK * prevHeight < prevWidth * heightK;
            var width = selectWidth ? widthK : prevWidth;
            var height = selectWidth ? prevHeight : heightK;
            Texture2D newTexture = CreateTexture(width, height);
            var newx = (width - prevWidth) / 2;
            var newy = (height - prevHeight) / 2;
            Graphics.CopyTexture(originalTexture, 0, 0, 0, 0, prevWidth, prevHeight, newTexture, 0, 0, newx, newy);
            newTexture.Apply();

            return newTexture;
        }

        private static int ToPoTRatio(int x, int y)
        {
            float ratio = (float)x / y;
            int n = Mathf.CeilToInt(Mathf.Log(ratio, 2));
            int powerOfTwo = 1 << Math.Abs(n);
            int k = n < 0 ?
                y / powerOfTwo :
                y * powerOfTwo;
            k -= x;
            return k;
        }

        public Texture2D Square(Texture2D originalTexture)
        {
            originalTexture = Copy(originalTexture);
            var prevWidth = originalTexture.width;
            var prevHeight = originalTexture.height;

            int size = max(prevHeight, prevWidth);
            Texture2D newTexture = CreateTexture(size, size);

            var newx = (size - prevWidth) / 2;
            var newy = (size - prevHeight) / 2;
            Graphics.CopyTexture(originalTexture, 0, 0, 0, 0, prevWidth, prevHeight, newTexture, 0, 0, newx, newy);
            newTexture.Apply();

            return newTexture;
        }


        private Texture2D Copy(Texture2D originalTexture)
        {
            var width = originalTexture.width;
            var height = originalTexture.height;
            RenderTexture renderTexture = new(width, height, 32);
            Texture2D newTexture = CreateTexture(width, height);

            var active = RenderTexture.active;
            RenderTexture.active = renderTexture;

            Graphics.Blit(originalTexture, renderTexture);
            newTexture.ReadPixels(new(0, 0, width, height), 0, 0);
            newTexture.Apply();

            RenderTexture.active = active;
            renderTexture.Release();

            return newTexture;
        }

        private Texture2D Crop(Texture2D originalTexture, int x, int y, int width, int height)
        {
            var prevWidth = originalTexture.width;
            var prevHeight = originalTexture.height;
            RenderTexture renderTexture = new(prevWidth, prevHeight, 32);
            Texture2D newTexture = CreateTexture(width, height);

            var active = RenderTexture.active;
            RenderTexture.active = renderTexture;

            Graphics.Blit(originalTexture, renderTexture);
            newTexture.ReadPixels(new(x, y, width, height), 0, 0);
            newTexture.Apply();

            RenderTexture.active = active;
            renderTexture.Release();

            return newTexture;
        }


        private int4 GetMinMax(Texture2D item)
        {
            int4 minmax = new(int.MaxValue, int.MaxValue, int.MinValue, int.MinValue);
            int width = item.width;
            int height = item.height;
            var pixels = item.GetPixels32();
            int length = pixels.Length;
            bool[] vals = new bool[length];
            for (int i = 0; i < length; i++)
            {
                vals[i] = pixels[i].a == 0;
            }
            for (int y = 0, i = 0; y < height; y++)
            {
                for (int x = 0; x < width; x++, i++)
                {
                    if (vals[i])
                        continue;
                    minmax[0] = min(minmax[0], x);
                    minmax[1] = min(minmax[1], y);
                    minmax[2] = max(minmax[2], x);
                    minmax[3] = max(minmax[3], y);
                }
            }
            return minmax;
        }

        private Texture2D CreateTexture(int width, int height)
        {
            Texture2D tex = new(width, height, TextureFormat.RGBA32, false);
            int length = width * height;
            Array.Resize(ref _clearArray, length);
            tex.SetPixels32(_clearArray);
            tex.Apply();
            return tex;
        }
    }
}