using UnityEngine;

namespace DVG.SkyPirates.Client.Views
{
    public sealed class MouseCameraRotationView : MonoBehaviour
    {
        [SerializeField] private CameraView? _cameraView;

        private Vector2 _lastMousePosition;

        private void OnEnable()
        {
            _lastMousePosition = Input.mousePosition;
        }

        private void Update()
        {
            if (_cameraView == null)
                return;

            Vector2 mousePosition = Input.mousePosition;
            Vector2 mouseDelta = mousePosition - _lastMousePosition;
            _cameraView.AddYaw(mouseDelta.x * 0.2f);
            _cameraView.AddPitch(-mouseDelta.y * 0.2f);
            _lastMousePosition = mousePosition;
        }
    }
}
