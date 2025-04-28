using DVG.Core;
using DVG.MathsOld;
using DVG.SkyPirates.Shared.IViews;
using System;

namespace DVG.SkyPirates.Server.Presenters
{
    public class InputPm : Presenter<IInputView, object>
    {
        public InputPm(IInputView view) : base(view, null) { }

        public float3 Position => View.Position;
        public angle Rotation => View.Rotation;
        public bool Fixation => View.Fixation;

        public event Action<int> OnSpawnUnit
        {
            add => View.OnSpawnUnitCalled += value;
            remove => View.OnSpawnUnitCalled -= value;
        }
    }
}
