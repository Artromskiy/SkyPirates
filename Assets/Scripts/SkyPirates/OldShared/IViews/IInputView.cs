using DVG.Core;
using DVG.MathsOld;
using System;

namespace DVG.SkyPirates.OldShared.IViews
{
    public interface IInputView : IView
    {
        public float3 Position { get; set; }
        public angle Rotation { get; set; }
        public bool Fixation { get; set; }
        public void SpawnUnit(int index);
        public event Action<int> OnSpawnUnitCalled;
    }
}
