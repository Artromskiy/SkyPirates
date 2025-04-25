using DVG.Maths;
using DVG.SkyPirates.Shared.IViews;
using System;
using Unity.Netcode;
using UnityEngine;

namespace DVG.SkyPirates.Shared.Views
{
    public class UnitView : NetworkBehaviour, IUnitView
    {
        [SerializeField]
        private Rigidbody _rigidbody;

        public vec2 Velocity { set; private get; }
        public angle Rotation { set => transform.eulerAngles = new(0, value.deg, 0); }

        public vec3 Position { get => transform.position; }

        private readonly NetworkVariable<vec2> _position = new();
        private readonly NetworkVariable<angle> _rotation = new();

        private void FixedUpdate()
        {
            if (IsServer)
            {
                _rigidbody.linearVelocity = Velocity.ToY0();
            }
        }

        private void Update()
        {
            if (IsServer)
            {
                float latency = (float)TimeSpan.FromTicks(NetworkManager.Singleton.NetworkTimeSystem.TickLatency).TotalSeconds;
                _position.Value = (vec2)transform.position.NoY() + Velocity * latency;
                _rotation.Value = transform.eulerAngles.y;
            }
            if (IsClient && !IsServer)
            {
                var pos = vec3.lerp(transform.position, _position.Value.x0y, Time.deltaTime * 5);
                var rot = math.lerp(transform.eulerAngles.y, _rotation.Value.deg, Time.deltaTime * 5);
                transform.position = pos;
                transform.eulerAngles = new(0, rot, 0);
            }
        }
    }
}