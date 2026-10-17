using SlyPirates.Shared.Ids;
using System;

namespace SlyPirates.Shared.Data
{
    [Serializable]
    public struct UnitDeck
    {
        public const int Capacity = 4;

        public UnitId First;
        public UnitId Second;
        public UnitId Third;
        public UnitId Fourth;

        public UnitDeck(UnitId first, UnitId second, UnitId third, UnitId fourth)
        {
            First = first;
            Second = second;
            Third = third;
            Fourth = fourth;
        }

        public readonly bool IsComplete
            => !First.IsNone && !Second.IsNone && !Third.IsNone && !Fourth.IsNone;

        public readonly UnitId GetCard(int index) => index switch
        {
            0 => First,
            1 => Second,
            2 => Third,
            3 => Fourth,
            _ => throw new ArgumentOutOfRangeException(nameof(index)),
        };

        public readonly bool Contains(UnitId unitId)
            => First == unitId || Second == unitId || Third == unitId || Fourth == unitId;
    }
}
