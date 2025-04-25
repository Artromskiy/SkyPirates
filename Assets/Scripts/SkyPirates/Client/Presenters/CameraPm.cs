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
        private vec3 _position;
        private vec3 _positionVelocity;

        public vec3 ListenerPosition;

        public CameraPm(ICameraView view, CameraModel model) : base(view, model) { }

        public vec3 TargetPosition { get; set; }
        public float TargetVisibleZone { get; set; }
        public float TargetNormalizedDistance { get; set; }
        public bool AudioOnTarget { get; set; }

        public void UpdateCamera(float deltaTime)
        {
            float value = TargetNormalizedDistance;
            float distance = math.smoothstep(Model.minDistance, Model.maxDistance, value);
            _distance = math.smoothdamper(_distance, distance, ref _distanceVelocity, 1, deltaTime);
            value = Model.maxDistance == Model.minDistance ? TargetNormalizedDistance :
                math.clamp01(math.unlerp(Model.minDistance, Model.maxDistance, _distance));
            float xAngle = math.lerp(Model.minXAngle, Model.maxXAngle, value);
            var rotatedDir = angle.rotate(vec2.left, xAngle);
            vec3 angleDir = new(0, rotatedDir.y, rotatedDir.x);
            vec3 pos = (Model.yAxis ? TargetPosition : TargetPosition.zeroY()) + (angleDir * _distance);
            _position = vec3.smoothDamp(_position, pos, ref _positionVelocity, Model.smoothMoveTime, deltaTime);

            distance = vec3.length(_position, Model.yAxis ? TargetPosition : TargetPosition.zeroY());
            value = Model.minDistance == Model.maxDistance ? TargetNormalizedDistance :
                math.clamp01(math.unlerp(Model.minDistance, Model.maxDistance, distance));

            float fovRemaped = math.lerp(Model.minFov, MaxFov(TargetVisibleZone, Model.maxDistance), value);
            var offsetDir = angle.rotate(new vec2(0, math.tan(fovRemaped * math.deg2rad) * distance), xAngle - 90);
            vec3 offset = new (0, offsetDir.y, offsetDir.x);
            offset *= math.lerp(Model.minOffset, Model.maxOffset, 1 - value);
            quat rot = quat.look(-angleDir, vec3.up); // TODO simplify

            var currentPosition = _position + offset;
            var currentRotation = rot;
            var currentFoV = fovRemaped;
            var currentNormalizedDistance = value;

            View.SetData(currentPosition, currentRotation, currentFoV, currentPosition);
        }

        public void SetCameraFast()
        {
            var value = TargetNormalizedDistance;
            float distance = math.smoothstep(Model.minDistance, Model.maxDistance, value);
            _distance = distance;
            float xAngle = math.smoothstep(Model.minXAngle, Model.maxXAngle, value);
            var rotatedDir = angle.rotate(vec2.left, xAngle);
            vec3 angleDir = new(0, rotatedDir.y, rotatedDir.x);
            vec3 pos = (Model.yAxis ? TargetPosition : TargetPosition.zeroY()) + (angleDir * _distance);
            _position = pos;

            distance = vec3.length(_position, Model.yAxis ? TargetPosition : TargetPosition.zeroY());
            value = Model.minDistance == Model.maxDistance ? 0 : math.clamp01(math.remap(distance, Model.minDistance, Model.maxDistance, 0, 1));

            float fovRemaped = math.lerp(Model.minFov, MaxFov(TargetVisibleZone, Model.maxDistance), value);
            var offsetDir = angle.rotate(new vec2(0, math.tan(fovRemaped * math.deg2rad) * distance), xAngle - 90);
            vec3 offset = new(0, offsetDir.y, offsetDir.x);
            offset *= math.lerp(Model.minOffset, Model.maxOffset, 1 - value);
            quat rot = quat.look(-angleDir, vec3.up); // TODO simplify

            var currentPosition = _position + offset;
            var currentRotation = rot;
            var currentFoV = fovRemaped;
            var currentNormalizedDistance = value;

            View.SetData(currentPosition, currentRotation, currentFoV, currentPosition);
        }

        private float MaxFov(float horizontal, float distance)
        {
            var fov = math.atan2(horizontal, distance) * math.rad2deg;
            return math.max(Model.maxFov, fov);
        }

        public void Tick()
        {
            UpdateCamera(1f / 60);
        }
    }
}