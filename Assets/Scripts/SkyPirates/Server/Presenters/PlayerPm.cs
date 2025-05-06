using DVG.Core;
using DVG.SkyPirates.Server.IFactories;
using DVG.SkyPirates.Shared.Models;


namespace DVG.SkyPirates.Server.Presenters
{
    public class PlayerPm : Presenter, ITickable
    {
        private readonly InputPm _inputPm;
        private readonly SquadPm _squadPm;
        private readonly SquadModel _squadModel;
        private readonly IUnitFactory _unitFactory;

        public PlayerPm(InputPm inputPm, SquadPm squadPm, SquadModel squadModel, IUnitFactory unitFactory)
        {
            _inputPm = inputPm;
            _squadPm = squadPm;
            _squadModel = squadModel;
            _unitFactory = unitFactory;
            _inputPm.OnSpawnUnit += SpawnUnit;
        }

        public void Tick()
        {
            _squadPm.Tick();
        }

        private void SpawnUnit(int index)
        {
            var data = _squadModel.cards[index];
            var unit = _unitFactory.Create((data.unitId, data.level, 1));
            _squadPm.AddUnit(unit);
        }
    }
}
