#nullable enable
using DVG.SkyPirates.Client.IViewModels;
using DVG.SkyPirates.Client.IViews;
using UnityEngine;
using UnityEngine.EventSystems;

namespace DVG.SkyPirates.Client.Views
{
    public class TouchJoystickView : View<IJoystickVM>,
        IDragHandler,
        IPointerDownHandler,
        IPointerUpHandler
    {
        [SerializeField]
        private RectTransform _outKnob = null!;
        [SerializeField]
        private RectTransform _inKnob = null!;

        private int _pointerId;
        private float2 _start;

        public override void OnInject() { }

        public void OnDrag(PointerEventData eventData)
        {
            if (_pointerId != eventData.pointerId)
                return;

            float maxDelta = (_outKnob.sizeDelta.x * _outKnob.lossyScale.x / 2);
            float2 pos = eventData.position;
            float2 delta = (pos - _start) / maxDelta;
            if (float2.SqrLength(delta) > 1)
                delta = float2.Normalize(delta);

            _inKnob.position = (_start + delta * maxDelta).xy_;

            delta = float2.SqrLength(delta) > 0.1 ? float2.Normalize(delta) : float2.zero;

            ViewModel.Joystick = (delta, true);
        }

        public void OnPointerDown(PointerEventData eventData)
        {
            _pointerId = eventData.pointerId;

            _start = eventData.position;
            ViewModel.Joystick = (float2.zero, true);

            _inKnob.gameObject.SetActive(true);
            _outKnob.gameObject.SetActive(true);
            _outKnob.position = _start.xy_;
            _inKnob.position = _start.xy_;
        }

        public void OnPointerUp(PointerEventData eventData)
        {
            if (_pointerId != eventData.pointerId)
                return;

            _inKnob.gameObject.SetActive(false);
            _outKnob.gameObject.SetActive(false);
            _pointerId = -1;
            ViewModel.Joystick = (float2.zero, false);
        }
    }
}
