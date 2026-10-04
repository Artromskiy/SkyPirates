#nullable enable
using Delta;
using DVG.SkyPirates.Client.IViewModels;
using DVG.SkyPirates.Client.IViews;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;

namespace DVG.SkyPirates.Client.Views
{
    internal class CameraView : View<ICameraVM>
    {
        [SerializeField]
        private Camera _camera = null!;

        public override void OnInject()
        {
            ForceUpdate();
        }

        private float _distance;
        private float _distanceVelocity;
        private float3 _position;
        private float3 _positionVelocity;
        private float _fov;
        private float _fovVelocity;
        private float _xAngle;
        private float _xAngleVelocity;
        private float _yaw;
        private float _pitchOffset;

        private const float MinimumPitch = 35f;
        private const float MaximumPitch = 85f;


        public void ForceUpdate()
        {
            _distance = ViewModel.TargetDistance;
            _fov = ViewModel.TargetFov;
            _xAngle = Mathf.Clamp(ViewModel.TargetAngle + _pitchOffset, MinimumPitch, MaximumPitch);
            _position = ViewModel.TargetPosition;

            UpdateCameraTransform();
            _camera.fieldOfView = Camera.HorizontalToVerticalFieldOfView(_fov, _camera.aspect);
        }

        private void Update()
        {
            float deltaTime = Time.deltaTime;
            float smooth = ViewModel.SmoothMoveTime;
            _distance = Maths.SmoothDamp(_distance, ViewModel.TargetDistance, ref _distanceVelocity, smooth, deltaTime);
            _fov = Maths.SmoothDamp(_fov, ViewModel.TargetFov, ref _fovVelocity, smooth, deltaTime);
            float targetPitch = Mathf.Clamp(ViewModel.TargetAngle + _pitchOffset, MinimumPitch, MaximumPitch);
            _xAngle = Maths.SmoothDamp(_xAngle, targetPitch, ref _xAngleVelocity, smooth, deltaTime);
            _position = float3.SmoothDamp(_position, ViewModel.TargetPosition, ref _positionVelocity, smooth, deltaTime);

            UpdateCameraTransform();
            _camera.fieldOfView = Camera.HorizontalToVerticalFieldOfView(_fov, _camera.aspect);

            SetDynamicShadowDistance();
        }

        private void UpdateCameraTransform()
        {
            float2 angleDir = new float2(1, 0).Rotate(_xAngle);
            var currentPosition = _position - (angleDir * _distance)._yx;
            currentPosition = _position + (float3)(Quaternion.AngleAxis(_yaw, Vector3.up) * (currentPosition - _position));
            var currentRotation = Quaternion.AngleAxis(_yaw, Vector3.up) * Quaternion.Euler(_xAngle, 0, 0);
            transform.SetPositionAndRotation(currentPosition, currentRotation);
        }

        internal void AddYaw(float degrees)
        {
            _yaw = Mathf.Repeat(_yaw + degrees, 360f);
        }

        internal void AddPitch(float degrees)
        {
            float targetPitch = Mathf.Clamp(ViewModel.TargetAngle + _pitchOffset + degrees, MinimumPitch, MaximumPitch);
            _pitchOffset = targetPitch - ViewModel.TargetAngle;
        }

        private void SetDynamicShadowDistance()
        {
            float minHeight = -5;
            var ray = Camera.main.ScreenPointToRay(new Vector3(Screen.width / 2, Screen.height));
            new Plane(Vector3.down, minHeight).Raycast(ray, out float enter);
            QualitySettings.shadowDistance = enter;
            UniversalRenderPipelineAsset urp = (UniversalRenderPipelineAsset)GraphicsSettings.currentRenderPipeline;
            urp.shadowDistance = enter;
        }
    }
}
