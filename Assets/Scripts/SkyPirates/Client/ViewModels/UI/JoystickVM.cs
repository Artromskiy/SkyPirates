using DVG.Commands;
using DVG.SkyPirates.Client.IServices;
using DVG.SkyPirates.Client.IViewModels;
using DVG.SkyPirates.Shared.Commands;

namespace DVG.SkyPirates.Client.ViewModels.UI
{
    public class JoystickVM : IJoystickVM
    {
        private readonly IPlayer _player;
        private readonly ICommandSendScheduler _sendScheduler;

        public JoystickVM(IPlayer player, ICommandSendScheduler sendScheduler)
        {
            _player = player;
            _sendScheduler = sendScheduler;
        }

        public (float2 direction, bool fixation) Joystick
        {
            set
            {
                if (_player.CurrentEntityId == null)
                    return;

                var cmdData = new JoystickCommand()
                {
                    Direction = (fix2)value.direction,
                    Fixation = value.fixation,
                    Target = _player.CurrentEntityId.Value,
                };
                var cmd = Command.Create(cmdData);
                _sendScheduler.SendCommand(cmd);
            }
        }
    }
}
