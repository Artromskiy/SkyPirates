using DVG.MathsOld;
using DVG.SkyPirates.Shared.IViews;
using System;
using Unity.Netcode;
using UnityEngine;

namespace DVG.SkyPirates.OldShared.Views
{
    public class UnitView : NetworkBehaviour, IUnitView
    {
        [SerializeField]
        private Rigidbody _rigidbody;

        public float2 Velocity { set; private get; }
        public float Rotation { set => transform.eulerAngles = new(0, value, 0); }

        public float3 Position { get => transform.position; }

        private readonly NetworkVariable<float2> _position = new();
        private readonly NetworkVariable<angle> _rotation = new();

        private void FixedUpdate()
        {
            if (IsServer)
            {
                _rigidbody.linearVelocity = Velocity.x_y;
            }
        }

        private void Update()
        {
            if (IsServer)
            {
                float latency = (float)TimeSpan.FromTicks(NetworkManager.Singleton.NetworkTimeSystem.TickLatency).TotalSeconds;
                _position.Value = new float2(transform.position.x, transform.position.z) + Velocity * latency;
                _rotation.Value = transform.eulerAngles.y;
            }
            if (IsClient && !IsServer)
            {
                var pos = float3.Lerp(transform.position, _position.Value.x_y, Time.deltaTime * 5);
                var rot = Maths.Lerp(transform.eulerAngles.y, _rotation.Value.deg, Time.deltaTime * 5);
                transform.position = pos;
                transform.eulerAngles = new(0, rot, 0);
            }
        }
    }
}