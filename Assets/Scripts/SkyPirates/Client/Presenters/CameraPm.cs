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
        private float _fov;
        private float _fovVelocity;
        private float _xAngle;
        private float _xAngleVelocity;

        public CameraPm(ICameraView view, CameraModel model) : base(view, model) { }

        public float3 TargetPosition { get; set; }
        public float TargetVisibleZone { get; set; }
        public float TargetNormalizedDistance { get; set; }

        public void UpdateCamera(float deltaTime)
        {
            float targetDistance = Maths.Lerp(Model.minDistance, Model.maxDistance, TargetNormalizedDistance);
            float targetFov = Maths.Lerp(Model.minFov, Model.maxFov, TargetNormalizedDistance);
            float targetAngle = Maths.Lerp(Model.minXAngle, Model.maxXAngle, TargetNormalizedDistance);

            _distance = Maths.SmoothDamp(_distance, targetDistance, ref _distanceVelocity, 1, deltaTime);
            _fov = Maths.SmoothDamp(_fov, targetFov, ref _fovVelocity, 1, deltaTime);
            _xAngle = Maths.SmoothDamp(_xAngle, targetAngle, ref _xAngleVelocity, 1, deltaTime);
            _position = float3.SmoothDamp(_position, TargetPosition, ref _positionVelocity, 1, deltaTime);

            float2 angleDir = angle.rotate(new float2(1, 0), _xAngle);

            var currentPosition = _position - (angleDir * _distance)._yx;
            var currentRotation = _xAngle;
            var currentFoV = _fov;

            View.SetData(currentPosition, currentRotation, currentFoV, currentPosition);
        }

        public void SetCameraFast()
        {
            ZeroVelocity();
            float targetDistance = Maths.Lerp(Model.minDistance, Model.maxDistance, TargetNormalizedDistance);
            float targetFov = Maths.Lerp(Model.minFov, Model.maxFov, TargetNormalizedDistance);
            float targetAngle = Maths.Lerp(Model.minXAngle, Model.maxXAngle, TargetNormalizedDistance);

            _distance = targetDistance;
            _fov = targetFov;
            _xAngle = targetAngle;
            _position = TargetPosition;

            float2 angleDir = angle.rotate(new float2(1, 0), _xAngle);

            var currentPosition = _position - (angleDir * _distance)._yx;
            var currentRotation = _xAngle;
            var currentFoV = _fov;

            View.SetData(currentPosition, currentRotation, currentFoV, currentPosition);
        }

        private void ZeroVelocity()
        {
            _distanceVelocity = 0;
            _fovVelocity = 0;
            _xAngleVelocity = 0;
            _positionVelocity = new(0);
        }

        public void Tick()
        {
            UpdateCamera(1f / 60);
        }
    }
}