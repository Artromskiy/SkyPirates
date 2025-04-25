using DVG.Core;
using DVG.MathsOld;
using System;

namespace DVG.SkyPirates.Shared.IViews
{
    public interface IInputView : IView
    {
        public vec3 Position { get; set; }
        public angle Rotation { get; set; }
        public bool Fixation { get; set; }
        public void SpawnUnit(int index);
        public event Action<int> OnSpawnUnitCalled;
    }
}
