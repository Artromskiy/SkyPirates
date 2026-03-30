using DVG.SkyPirates.Shared.Ids;
using DVG.UniTools;
using System;
using UnityEngine;

namespace DVG.SkyPirates.Client.Databases
{
    [CreateAssetMenu(
        fileName = nameof(UnitsDatabase),
        menuName = Menu + nameof(UnitsDatabase))]
    public class UnitsDatabase : Database<UnitId, UnitData> { }

    [Serializable]
    public class UnitData
    {
        public Sprite Sprite;
    }
}