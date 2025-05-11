using DVG.SkyPirates.Shared.IViews;
using UnityEngine;

namespace DVG.SkyPirates.Client.Views
{
    public class UnitView : MonoBehaviour, IUnitView
    {
        public float Rotation { get; set; }
        public float3 Position { get; set; }

        private float3 _velocity;
        private float _rotation;

        private void Update()
        {
            transform.position = float3.SmoothDamp(transform.position, Position, ref _velocity, 0.25f, Time.deltaTime);
            var y = Maths.SmoothDampAngle(transform.eulerAngles.y, Rotation, ref _rotation, 0.25f, Time.deltaTime);
            transform.eulerAngles = new float3(0, y, 0);
        }
    }
}