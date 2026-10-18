using SlyPirates.Client.IServices;
using SlyPirates.Client.IViewModels;
using SlyPirates.Shared.Commands;
using SlyPirates.Shared.Data;
using SlyPirates.Shared.Ids;
using System;

namespace SlyPirates.Client.ViewModels.UI
{
    public class CardsVM : ICardsVM
    {
        private readonly IPlayer _player;
        private readonly ICommandSendScheduler _sendScheduler;

        private readonly UnitId[] _cards = ClientSetupData.TeamCards;
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
            {
                return;
            }

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
