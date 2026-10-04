using System;
using UnityEngine;

namespace DVG.SkyPirates.Tooling.IslandGeneration
{
    [Serializable]
    public struct HexIslandCell
    {
        [SerializeField] private int q;
        [SerializeField] private int r;
        [SerializeField] private bool isLand;
        [SerializeField] private int heightLevel;
        [SerializeField] private float rawHeight;
        [SerializeField] private float height;

        public int Q => q;
        public int R => r;
        public bool IsLand => isLand;
        public int HeightLevel => heightLevel;
        public float RawHeight => rawHeight;
        public float Height => height;

        public HexIslandCell(
            int q,
            int r,
            bool isLand,
            int heightLevel,
            float rawHeight,
            float height)
        {
            this.q = q;
            this.r = r;
            this.isLand = isLand;
            this.heightLevel = heightLevel;
            this.rawHeight = rawHeight;
            this.height = height;
        }
    }

    public sealed class HexIslandMap : ScriptableObject
    {
        [SerializeField] private int columns;
        [SerializeField] private int rows;
        [SerializeField] private int seed;
        [SerializeField] private int heightLevels;
        [SerializeField] private float maximumHeight;
        [SerializeField] private HexIslandCell[] cells = Array.Empty<HexIslandCell>();
        [NonSerialized] private Texture2D preview;

        public int Columns => columns;
        public int Rows => rows;
        public int Seed => seed;
        public int HeightLevels => heightLevels;
        public float MaximumHeight => maximumHeight;
        public HexIslandCell[] Cells => cells;
        public Texture2D Preview => preview;

        public int LandCellCount
        {
            get
            {
                int count = 0;
                for (int i = 0; i < cells.Length; i++)
                {
                    if (cells[i].IsLand)
                    {
                        count++;
                    }
                }

                return count;
            }
        }

        public void SetGeneratedData(
            int columns,
            int rows,
            int seed,
            int heightLevels,
            float maximumHeight,
            HexIslandCell[] cells,
            Texture2D preview)
        {
            this.columns = columns;
            this.rows = rows;
            this.seed = seed;
            this.heightLevels = heightLevels;
            this.maximumHeight = maximumHeight;
            this.cells = cells ?? Array.Empty<HexIslandCell>();
            this.preview = preview;
        }

        internal void SetPreviewTexture(Texture2D preview)
        {
            this.preview = preview;
        }
    }
}
