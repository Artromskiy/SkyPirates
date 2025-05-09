using DVG.Core;
using DVG.SkyPirates.Client.IServices;
using DVG.SkyPirates.Client.Presenters;
using DVG.SkyPirates.Shared.Commands;

namespace DVG.SkyPirates.Client.Entry
{
    internal class PresenterClient : ITickable
    {
        private readonly MoveTargetPm _moveTargetPm;
        private readonly JoystickPm _joystickPm;
        private readonly CardsPm _cardsPm;
        private readonly CameraPm _cameraPm;
        private readonly ICommandSendService _messageSendService;

        public PresenterClient(ICommandSendService messageSendService, MoveTargetPm moveTargetPm, JoystickPm joystickPm, CardsPm cardsPm, CameraPm cameraPm)
        {
            _messageSendService = messageSendService;
            _moveTargetPm = moveTargetPm;
            _joystickPm = joystickPm;
            _cardsPm = cardsPm;
            _cameraPm = cameraPm;
            _cardsPm.OnSpawnUnit += id => _messageSendService.SendCommand(new SpawnUnitCommand() { id = id });
        }

        public void Tick()
        {
            _moveTargetPm.Direction = _joystickPm.Direction;

            _messageSendService.SendCommand(new UpdateInputCommand()
            {
                position = _moveTargetPm.Position,
                rotation = _moveTargetPm.Rotation.deg,
                fixation = _joystickPm.Fixation
            });


            _cameraPm.TargetPosition = _moveTargetPm.Position;
            _cameraPm.TargetVisibleZone = 10;
            _cameraPm.TargetNormalizedDistance = _joystickPm.Fixation ? 1 : 0;
            _cameraPm.Tick();
        }
    }
}