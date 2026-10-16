using UnityEngine;

namespace SlyPirates.Client.Views
{
    public sealed class MouseCameraRotationView : MonoBehaviour
    {
        [SerializeField] private CameraView? _cameraView;

        private const float MouseAxisToDegrees = 2f;

        private void Update()
        {
            if (_cameraView == null)
                return;

            float mouseDeltaX = Input.GetAxisRaw("Mouse X");
            float mouseDeltaY = Input.GetAxisRaw("Mouse Y");
            _cameraView.AddYaw(mouseDeltaX * MouseAxisToDegrees);
            _cameraView.AddPitch(-mouseDeltaY * MouseAxisToDegrees);
        }
    }
}
