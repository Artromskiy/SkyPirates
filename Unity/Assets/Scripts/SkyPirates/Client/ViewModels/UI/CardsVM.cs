using Delta.Netcode;
using SkyPirates.Client.IServices;
using SkyPirates.Client.IViewModels;
using SkyPirates.Shared.Commands;
using SkyPirates.Shared.Data;
using SkyPirates.Shared.Ids;
using System;

namespace SkyPirates.Client.ViewModels.UI
{
    public class CardsVM : ICardsVM
    {
        private readonly IPlayer _player;
        private readonly ICommandSendScheduler _sendScheduler;

        private readonly UnitId[] _cards = new UnitId[]
        {
            UnitId.Constants.Rogue,
            UnitId.Constants.Militia,
            UnitId.Constants.Buccaneer,
        };
        public ReadOnlySpan<UnitId> Cards => _cards;
        public UnitsInfoConfig UnitsConfig { get; }

        public CardsVM(IPlayer player, ICommandSendScheduler sendScheduler, UnitsInfoConfig unitsConfig)
        {
            Delta.Diagnostics.Trace.Info("[CardsVM] created");
            _player = player;
            _sendScheduler = sendScheduler;
            UnitsConfig = unitsConfig;
        }


        public void UseCard(int index)
        {
            if (_player.SquadEntityId == null)
                return;

            var selected = _cards[index];
            var cmdData = new SpawnUnitCommand()
            {
                UnitId = selected,
                SquadId = _player.SquadEntityId.Value,
            };
            _sendScheduler.SendCommand(cmdData);
        }
    }
}
