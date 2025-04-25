using DVG.Maths;
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

        public void SetData(vec3 camPosition, quat camRotation, float camFov, vec3 listenerPosition)
        {
            transform.SetPositionAndRotation(camPosition, camRotation);
            _camera.fieldOfView = Camera.HorizontalToVerticalFieldOfView(camFov, _camera.aspect);
            _listener.transform.position = listenerPosition;
        }
    }
}