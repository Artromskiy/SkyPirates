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

        public void SetData(float3 camPosition, quat camRotation, float camFov, float3 listenerPosition)
        {
            transform.SetPositionAndRotation(camPosition, camRotation);
            _camera.fieldOfView = Camera.HorizontalToVerticalFieldOfView(camFov, _camera.aspect);
            _listener.transform.position = listenerPosition;
        }
    }
}