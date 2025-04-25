using DVG.Maths;
using System;
using Vertx.Debugging;
using Unity.Netcode;
using DVG.SkyPirates.Shared.IViews;
using UnityEngine;

namespace DVG.SkyPirates.Shared.Views
{
    public class InputView : NetworkBehaviour, IInputView
    {
        private readonly NetworkVariable<vec3> _position = new(writePerm: NetworkVariableWritePermission.Owner);
        private readonly NetworkVariable<angle> _rotation = new(writePerm: NetworkVariableWritePermission.Owner);
        private readonly NetworkVariable<bool> _fixation = new(writePerm: NetworkVariableWritePermission.Owner);
        [Rpc(SendTo.Server)]
        private void SpawnUnitRpc(int index) => OnSpawnUnitCalled?.Invoke(index);

        public vec3 Position { get => _position.Value; set => _position.Value = value; }
        public angle Rotation { get => _rotation.Value; set => _rotation.Value = value; }
        public bool Fixation { get => _fixation.Value; set => _fixation.Value = value; }
        public void SpawnUnit(int index) => SpawnUnitRpc(index);
        public event Action<int> OnSpawnUnitCalled;

        private void Update()
        {
            D.raw(new Shape.Sphere(Position, .1f), Color.red);
        }
    }
}
