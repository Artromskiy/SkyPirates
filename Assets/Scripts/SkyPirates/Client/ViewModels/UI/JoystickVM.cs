using Delta;
using Delta.Netcode;
using DVG.SkyPirates.Client.IServices;
using DVG.SkyPirates.Client.IViewModels;
using DVG.SkyPirates.Shared.Commands;
using UnityEngine;

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

                float2 direction = value.direction;
                Camera? camera = Camera.main;
                if (camera != null)
                {
                    Vector3 forward = camera.transform.forward;
                    forward.y = 0;
                    forward.Normalize();

                    Vector3 right = camera.transform.right;
                    right.y = 0;
                    right.Normalize();

                    direction = new float2(
                        right.x * value.direction.x + forward.x * value.direction.y,
                        right.z * value.direction.x + forward.z * value.direction.y);
                }

                var cmdData = new JoystickCommand()
                {
                    Direction = (fix2)direction,
                    Fixation = value.fixation,
                    Target = _player.CurrentEntityId.Value,
                };
                _sendScheduler.SetTransientInput(cmdData);
                _sendScheduler.SendCommand(cmdData);
            }
        }
    }
}
