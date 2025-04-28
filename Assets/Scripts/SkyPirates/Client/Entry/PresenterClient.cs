using DVG.Core;
using DVG.SkyPirates.Client.IFactories;
using DVG.SkyPirates.Client.Presenters;
using Unity.Netcode;
using UnityEngine;


namespace DVG.SkyPirates.Client.Entry
{
    public class PresenterClient : IStartable, ITickable
    {
        private readonly IInputFactory _inputFactory;
        private readonly MoveTargetPm _moveTargetPm;
        private readonly JoystickPm _joystickPm;
        private readonly CardsPm _cardsPm;
        private readonly CameraPm _cameraPm;

        private InputPm _input;

        public PresenterClient(IInputFactory inputFactory, MoveTargetPm moveTargetPm, JoystickPm joystickPm, CardsPm cardsPm, CameraPm cameraPm)
        {
            _inputFactory = inputFactory;
            _moveTargetPm = moveTargetPm;
            _joystickPm = joystickPm;
            _cardsPm = cardsPm;
            _cameraPm = cameraPm;
        }

        public void Start()
        {
            Debug.Log("Start");
            NetworkManager.Singleton.OnClientConnectedCallback += OnClientStarted;
            NetworkManager.Singleton.StartClient();
        }

        public void Tick()
        {
            if (_input == null)
                return;

            _input.Position = _moveTargetPm.Position;
            _input.Rotation = _moveTargetPm.Rotation;
            _input.Fixation = _joystickPm.Fixation;

            _moveTargetPm.Direction = _joystickPm.Direction;

            _cameraPm.TargetPosition = _moveTargetPm.Position;
            _cameraPm.TargetVisibleZone = 10;
            _cameraPm.TargetNormalizedDistance = _joystickPm.Fixation ? 1 : 0;
            _cameraPm.Tick();
        }

        private void OnClientStarted(ulong _)
        {
            _input = _inputFactory.Create();
            _cardsPm.OnSpawnUnit += _input.SpawnUnit;
        }
    }
}