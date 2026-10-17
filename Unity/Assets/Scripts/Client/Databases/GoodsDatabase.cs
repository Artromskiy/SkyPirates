using SlyPirates.Shared.Ids;
using DVG.UniTools;
using System;
using UnityEngine;

namespace SlyPirates.Client.Databases
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