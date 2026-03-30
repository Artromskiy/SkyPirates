using System;
using System.Collections.Generic;
using UnityEngine;

namespace DVG.SkyPirates.Client.Views
{
    [Obsolete]
    public static class TeamIdToColor
    {
        private static readonly Color[] _colorReplacements = new Color[]
        {
            ToUnityColor(System.Drawing.Color.FromArgb(Convert.ToInt32("FF007F",16))),
            ToUnityColor(System.Drawing.Color.FromArgb(Convert.ToInt32("00AAFF",16))),
            ToUnityColor(System.Drawing.Color.FromArgb(Convert.ToInt32("9700FF",16))),
            ToUnityColor(System.Drawing.Color.FromArgb(Convert.ToInt32("FF00E5",16))),
            ToUnityColor(System.Drawing.Color.FromArgb(Convert.ToInt32("FF5B00",16))),
            ToUnityColor(System.Drawing.Color.FromArgb(Convert.ToInt32("FFA700",16))),
            ToUnityColor(System.Drawing.Color.FromArgb(Convert.ToInt32("C6FF00",16))),
            ToUnityColor(System.Drawing.Color.FromArgb(Convert.ToInt32("7FFF00",16))),
            ToUnityColor(System.Drawing.Color.FromArgb(Convert.ToInt32("00FF97",16))),
            ToUnityColor(System.Drawing.Color.FromArgb(Convert.ToInt32("00FFC9",16))),
            ToUnityColor(System.Drawing.Color.FromArgb(Convert.ToInt32("00F3FF",16))),
        };

        private static readonly Dictionary<int, Material> _materials = new();

        private static readonly int[] _materialHueOffsets = new int[]
        {
            135,
            0,
            40,
            100,
            155,
            170,
            200,
            220,
            290,
            310,
            330
        };

        public static Color GetColor(int teamId)
        {
            return _colorReplacements[teamId % _colorReplacements.Length];
        }

        public static Color RecolorHue(Color from, int teamId)
        {
            Color to = GetColor(teamId);
            Color.RGBToHSV(to, out var toHue, out _, out _);
            Color.RGBToHSV(from, out _, out var s, out var v);

            var c = Color.HSVToRGB(toHue, s, v);
            c.a = from.a;
            return c;
        }

        public static Material GetReplacementMaterial(Material material, int teamId)
        {
            teamId %= _materialHueOffsets.Length;
            if (!_materials.TryGetValue(teamId, out var replacement))
            {
                _materials[teamId] = replacement = new Material(material);
                var hueOffset = _materialHueOffsets[teamId];
                replacement.SetFloat("_Hue", hueOffset);
            }
            return replacement;
        }

        private static Color ToUnityColor(System.Drawing.Color color)
        {
            return new Color(color.R, color.G, color.B, color.A);
        }
    }
}