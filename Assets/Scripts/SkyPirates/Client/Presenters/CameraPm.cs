using DVG.Core;
using DVG.MathsOld;
using DVG.SkyPirates.Client.IViews;
using DVG.SkyPirates.Shared.Models;
using VContainer.Unity;

namespace DVG.SkyPirates.Client.Presenters
{
    public class CameraPm : Presenter<ICameraView, CameraModel>, ITickable
    {
        private float _distance;
        private float _distanceVelocity;
        private float3 _position;
        private float3 _positionVelocity;

        public float3 ListenerPosition;

        public CameraPm(ICameraView view, CameraModel model) : base(view, model) { }

        public float3 TargetPosition { get; set; }
        public float TargetVisibleZone { get; set; }
        public float TargetNormalizedDistance { get; set; }
        public bool AudioOnTarget { get; set; }

        public void UpdateCamera(float deltaTime)
        {
            float value = TargetNormalizedDistance;
            float distance = Maths.SmoothStep(Model.minDistance, Model.maxDistance, value);
            _distance = Maths.SmoothDamp(_distance, distance, ref _distanceVelocity, 1, deltaTime);
            value = Model.maxDistance == Model.minDistance ? TargetNormalizedDistance :
                Maths.Clamp(Maths.InvLerp(Model.minDistance, Model.maxDistance, _distance), 0, 1);
            float xAngle = Maths.Lerp(Model.minXAngle, Model.maxXAngle, value);
            float3 angleDir = angle.rotate(new float2(-1, 0), xAngle)._yx;
            float3 pos = (Model.yAxis ? TargetPosition : TargetPosition.x_z) + (angleDir * _distance);
            _position = float3.SmoothDamp(_position, pos, ref _positionVelocity, Model.smoothMoveTime, deltaTime);

            distance = float3.Distance(_position, Model.yAxis ? TargetPosition : TargetPosition.x_z);
            value = Model.minDistance == Model.maxDistance ? TargetNormalizedDistance :
                Maths.Clamp(Maths.InvLerp(Model.minDistance, Model.maxDistance, distance), 0, 1);

            float fovRemaped = Maths.Lerp(Model.minFov, MaxFov(TargetVisibleZone, Model.maxDistance), value);
            var offsetDir = angle.rotate(new float2(0, Maths.Tan(Maths.Radians(fovRemaped)) * distance), xAngle - 90);
            float3 offset = new(0, offsetDir.y, offsetDir.x);
            offset *= Maths.Lerp(Model.minOffset, Model.maxOffset, 1 - value);
            quat rot = quat.look(-angleDir, new(0, 1, 0)); // TODO simplify

            var currentPosition = _position + offset;
            var currentRotation = rot;
            var currentFoV = fovRemaped;
            var currentNormalizedDistance = value;

            View.SetData(currentPosition, currentRotation, currentFoV, currentPosition);
        }

        public void SetCameraFast()
        {
            var value = TargetNormalizedDistance;
            float distance = Maths.SmoothStep(Model.minDistance, Model.maxDistance, value);
            _distance = distance;
            float xAngle = Maths.SmoothStep(Model.minXAngle, Model.maxXAngle, value);
            float3 angleDir = angle.rotate(new float2(-1, 0), xAngle)._yx;
            float3 pos = (Model.yAxis ? TargetPosition : TargetPosition.x_z) + (angleDir * _distance);
            _position = pos;

            distance = float3.Distance(_position, Model.yAxis ? TargetPosition : TargetPosition.x_z);
            value = Model.minDistance == Model.maxDistance ? 0 : Maths.Clamp(Maths.Remap(distance, Model.minDistance, Model.maxDistance, 0, 1), 0, 1);

            float fovRemaped = Maths.Lerp(Model.minFov, MaxFov(TargetVisibleZone, Model.maxDistance), value);
            var offsetDir = angle.rotate(new float2(0, Maths.Tan(Maths.Radians(fovRemaped)) * distance), xAngle - 90);
            float3 offset = new(0, offsetDir.y, offsetDir.x);
            offset *= Maths.Lerp(Model.minOffset, Model.maxOffset, 1 - value);
            quat rot = quat.look(-angleDir, new(0, 1, 0)); // TODO simplify

            var currentPosition = _position + offset;
            var currentRotation = rot;
            var currentFoV = fovRemaped;
            var currentNormalizedDistance = value;

            View.SetData(currentPosition, currentRotation, currentFoV, currentPosition);
        }

        private float MaxFov(float horizontal, float distance)
        {
            var fov = Maths.Degrees(Maths.Atan2(horizontal, distance));
            return Maths.Max(Model.maxFov, fov);
        }

        public void Tick()
        {
            UpdateCamera(1f / 60);
        }
    }
}