using System;
using System.Collections.Generic;
using UnityEngine;

namespace DVG.SkyPirates.Tooling
{
    public enum DsvSeparator
    {
        Comma,
        Tab,
    }

    [Serializable]
    public sealed class GoogleSheetSource
    {
        public string Name = string.Empty;
        public string Url = string.Empty;
        [Min(1)] public int HeaderRows = 1;
        public DsvSeparator Separator = DsvSeparator.Tab;
    }

    [CreateAssetMenu(fileName = "GoogleSheetsConfigLoader", menuName = "DVG/Configs/Google Sheets Loader")]
    public sealed class GoogleSheetsConfigLoader : ScriptableObject
    {
        public List<GoogleSheetSource> Sheets = new();
    }
}
