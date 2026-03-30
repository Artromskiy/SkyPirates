#nullable enable
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

        public void ForceUpdate()
        {
            _distance = ViewModel.TargetDistance;
            _fov = ViewModel.TargetFov;
            _xAngle = ViewModel.TargetAngle;
            _position = ViewModel.TargetPosition;

            float2 angleDir = new float2(1, 0).Rotate(_xAngle);

            var currentPosition = _position - (angleDir * _distance)._yx;
            var currentRotation = _xAngle;
            var currentFoV = _fov;

            transform.SetPositionAndRotation(currentPosition, Quaternion.Euler(currentRotation, 0, 0));
            _camera.fieldOfView = Camera.HorizontalToVerticalFieldOfView(currentFoV, _camera.aspect);
        }

        private void Update()
        {
            float deltaTime = Time.deltaTime;
            var smooth = ViewModel.SmoothMoveTime;
            _distance = Maths.SmoothDamp(_distance, ViewModel.TargetDistance, ref _distanceVelocity, smooth, deltaTime);
            _fov = Maths.SmoothDamp(_fov, ViewModel.TargetFov, ref _fovVelocity, smooth, deltaTime);
            _xAngle = Maths.SmoothDamp(_xAngle, ViewModel.TargetAngle, ref _xAngleVelocity, smooth, deltaTime);
            _position = float3.SmoothDamp(_position, ViewModel.TargetPosition, ref _positionVelocity, smooth, deltaTime);

            float2 angleDir = new float2(1, 0).Rotate(_xAngle);

            var currentPosition = _position - (angleDir * _distance)._yx;
            var currentRotation = _xAngle;
            var currentFoV = _fov;

            transform.SetPositionAndRotation(currentPosition, Quaternion.Euler(currentRotation, 0, 0));
            _camera.fieldOfView = Camera.HorizontalToVerticalFieldOfView(currentFoV, _camera.aspect);

            SetDynamicShadowDistance();
        }

        private void SetDynamicShadowDistance()
        {
            float minHeight = -5;
            var ray = Camera.main.ScreenPointToRay(new Vector3(Screen.width / 2, Screen.height));
            new Plane(Vector3.down, minHeight).Raycast(ray, out var enter);
            QualitySettings.shadowDistance = enter;
            UniversalRenderPipelineAsset urp = (UniversalRenderPipelineAsset)GraphicsSettings.currentRenderPipeline;
            urp.shadowDistance = enter;
        }
    }
}