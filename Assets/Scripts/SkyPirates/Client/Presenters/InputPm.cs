using DVG.Core;
using DVG.MathsOld;
using DVG.SkyPirates.OldShared.IViews;

namespace DVG.SkyPirates.Client.Presenters
{
    public class InputPm : Presenter<IInputView, object>
    {
        public InputPm(IInputView view) : base(view, null) { }

        public float3 Position { set => View.Position = value; }
        public angle Rotation { set => View.Rotation = value; }
        public bool Fixation { set => View.Fixation = value; }
        public void SpawnUnit(int index) => View.SpawnUnit(index);
    }
}
