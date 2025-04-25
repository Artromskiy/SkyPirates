using DVG.Core;
using DVG.MathsOld;
using DVG.SkyPirates.Shared.IViews;
using DVG.SkyPirates.Shared.Models;
using UnityEngine;
using VContainer.Unity;

namespace DVG.SkyPirates.Server.Presenters
{
    public class UnitPm : Presenter<IUnitView, UnitModel>, ITickable
    {
        public vec3 Position => View.Position;
        public vec3 TargetPosition { get; set; }

        private angle _angle;

        public UnitPm(IUnitView view, UnitModel model) : base(view, model) { }

        public void Tick()
        {
            RotateTo(1f / 60);
            MoveRoutine();
        }

        private void MoveRoutine()
        {
            //var distance = vec2.length((TargetPosition - Position).xz);
            //View.Velocity = vec2.clamp(vec2.clamp1((TargetPosition - Position).xz) * Model.speed, distance);
            var velocity= vec2.clamp1((TargetPosition - Position).xz) * Model.speed;
            View.Velocity = velocity;

            Debug.Log(velocity);
        }

        private void RotateTo(float deltaTime)
        {
            var direction = (TargetPosition - Position).xz;
            angle trgRot = direction.isZero ? _angle : new(direction);
            //var delta = angle.distance(_angle, trgRot);
            //var rot = delta > 80 ? trgRot : angle.moveto(_angle, trgRot, 360 * deltaTime * 2);
            _angle = angle.moveto(_angle, trgRot, 360 * deltaTime * 2);
            View.Rotation = _angle.deg;
        }
    }
}