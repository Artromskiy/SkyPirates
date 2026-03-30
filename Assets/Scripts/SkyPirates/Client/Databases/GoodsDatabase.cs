using DVG.SkyPirates.Shared.Ids;
using DVG.UniTools;
using System;
using UnityEngine;

namespace DVG.SkyPirates.Client.Databases
{
    [CreateAssetMenu(
        fileName = nameof(GoodsDatabase),
        menuName = Menu + nameof(GoodsDatabase))]
    public class GoodsDatabase : Database<GoodsId, GoodsData> { }

    [Serializable]
    public class GoodsData
    {
        public Sprite Sprite;
    }
}