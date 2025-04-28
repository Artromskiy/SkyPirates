using DVG.MathsOld;
using DVG.SkyPirates.Client.IViews;
using UnityEngine;

namespace DVG.SkyPirates.Client.Views
{
    internal class CameraView : MonoBehaviour, ICameraView
    {
        [SerializeField]
        private Camera _camera;
        [SerializeField]
        private AudioListener _listener;

        public void SetData(float3 camPosition, float xRotation, float camFov, float3 listenerPosition)
        {
            transform.SetPositionAndRotation(camPosition, Quaternion.Euler(xRotation, 0, 0));
            _camera.fieldOfView = Camera.HorizontalToVerticalFieldOfView(camFov, _camera.aspect);
            _listener.transform.position = listenerPosition;
        }
    }
}