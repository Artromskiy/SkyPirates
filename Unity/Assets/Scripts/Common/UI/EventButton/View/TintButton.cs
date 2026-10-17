using System;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

namespace Common.UI
{
	[DisallowMultipleComponent]
	public sealed class TintButton : EventButton
	{
		[SerializeField] private Graphic _targetGraphic = null!;
		[SerializeField] private Color _normalColor = Color.white;
		[SerializeField] private Color _pressedColor = new(0.8f, 0.8f, 0.8f);
		[SerializeField] private Color _disabledColor = new(0.5f, 0.5f, 0.5f);

		public override bool IsInteractable
		{
			get => base.IsInteractable;
			set
			{
				base.IsInteractable = value;
				RefreshVisuals();
			}
		}

        private void OnValidate()
        {
            if(_targetGraphic == null)
            {
                TryGetComponent(out _targetGraphic);
            }
        }

        public override void OnEnable()
		{
			base.OnEnable();
			RefreshVisuals();
		}

		public override void OnPointerDown(PointerEventData eventData)
		{
			base.OnPointerDown(eventData);
			RefreshVisuals();
		}

		public override void OnPointerUp(PointerEventData eventData)
		{
			base.OnPointerUp(eventData);
			RefreshVisuals();
		}

		public override void OnPointerEnter(PointerEventData eventData)
		{
			base.OnPointerEnter(eventData);
			RefreshVisuals();
		}

		public override void OnPointerExit(PointerEventData eventData)
		{
			base.OnPointerExit(eventData);
			RefreshVisuals();
		}

		private void Awake() => RefreshVisuals();

        private void RefreshVisuals()
		{
			if (_targetGraphic == null)
			{
				return;
			}

			var tint = !IsInteractable ? _disabledColor :
				IsPressed ? _pressedColor :
				_normalColor;

			_targetGraphic.CrossFadeColor(tint, 0f, true, true);
		}
	}
}
