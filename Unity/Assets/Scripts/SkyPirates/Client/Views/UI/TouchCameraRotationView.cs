using UnityEngine;
using UnityEngine.EventSystems;

namespace DVG.SkyPirates.Client.Views
{
    public sealed class TouchCameraRotationView : MonoBehaviour, IDragHandler, IPointerDownHandler, IPointerUpHandler
    {
        [SerializeField] private RectTransform _outKnob = null!;
        [SerializeField] private RectTransform _inKnob = null!;

        [SerializeField] private CameraView? _cameraView;
        private int _pointerId = int.MinValue;
        private Vector2 _start;
        private float _horizontalInput;
        private float _verticalInput;

        private const float RotationDegreesPerSecond = 120f;

        private void Update()
        {
            if (_pointerId == int.MinValue || _horizontalInput == 0)
                return;

            if (_cameraView != null)
            {
                _cameraView.AddYaw(_horizontalInput * RotationDegreesPerSecond * Time.deltaTime);
                _cameraView.AddPitch(-_verticalInput * RotationDegreesPerSecond * Time.deltaTime);
            }
        }

        public void OnPointerDown(PointerEventData eventData)
        {
            _pointerId = eventData.pointerId;
            _start = eventData.position;
            _horizontalInput = 0;
            _verticalInput = 0;
            _outKnob.gameObject.SetActive(true);
            _inKnob.gameObject.SetActive(true);
            _outKnob.position = _start;
            _inKnob.position = _start;
        }

        public void OnDrag(PointerEventData eventData)
        {
            if (eventData.pointerId != _pointerId)
                return;

            float maxDelta = _outKnob.sizeDelta.x * _outKnob.lossyScale.x / 2;
            Vector2 input = (eventData.position - _start) / maxDelta;
            if (input.sqrMagnitude > 1)
                input.Normalize();

            _horizontalInput = input.x;
            _verticalInput = input.y;
            _inKnob.position = _start + input * maxDelta;
        }

        public void OnPointerUp(PointerEventData eventData)
        {
            if (eventData.pointerId != _pointerId)
                return;

            _inKnob.gameObject.SetActive(false);
            _outKnob.gameObject.SetActive(false);
            _pointerId = int.MinValue;
            _horizontalInput = 0;
            _verticalInput = 0;
        }
    }
}
