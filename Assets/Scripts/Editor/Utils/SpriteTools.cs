using System.IO;
using UnityEditor;
using UnityEngine;

namespace DVG.Editor.Tools
{
    public static class SpriteTools
    {
        private const int Padding = 5;

        [MenuItem("Assets/DVG/Crop Icon", false, 0)]
        public static void Crop()
        {
            Object[] selected = Selection.objects;
            if (selected == null || selected.Length == 0)
                return;

            foreach (Object obj in selected)
            {
                if (obj is not Texture2D texture)
                    continue;

                Vector2Int min = new(int.MaxValue, int.MaxValue);
                Vector2Int max = new(int.MinValue, int.MinValue);

                var path = AssetDatabase.GetAssetPath(texture);
                texture = new Texture2D(2, 2);
                texture.LoadImage(File.ReadAllBytes(path));

                var width = texture.width;
                var height = texture.height;

                for (int y = 0; y < height; y++)
                {
                    for (int x = 0; x < width; x++)
                    {
                        if (texture.GetPixel(x, y).a != 0)
                        {
                            Vector2Int coord = new(x, y);
                            min = Vector2Int.Min(min, coord);
                            max = Vector2Int.Max(max, coord);
                        }
                    }
                }

                min -= new Vector2Int(Padding, Padding);
                max += new Vector2Int(Padding, Padding);
                min = Vector2Int.Max(min, new Vector2Int(0, 0));
                max = Vector2Int.Min(max, new Vector2Int(width, height));

                var newWidth = max.x - min.x;
                var newHeight = max.y - min.y;

                Texture2D cropped = new(newWidth, newHeight, TextureFormat.ARGB32, false);

                var newPixels = texture.GetPixels(min.x, min.y, newWidth, newHeight, 0);
                cropped.SetPixels(0, 0, cropped.width, cropped.height, newPixels, 0);
                cropped.Apply();

                File.WriteAllBytes(path, cropped.EncodeToPNG());

            }
            AssetDatabase.Refresh();
        }
    }
}