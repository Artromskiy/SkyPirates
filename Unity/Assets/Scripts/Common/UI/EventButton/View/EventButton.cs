using System;
using UnityEngine;
using UnityEngine.EventSystems;

namespace Common.UI
{
	[DisallowMultipleComponent]
	public class EventButton : MonoBehaviour, IPointerDownHandler, IPointerUpHandler, IPointerEnterHandler, IPointerExitHandler, IDisposable
	{
		[field: SerializeField]
		public virtual bool IsInteractable { get; set; } = true;

		public event Action<PointerEventData> OnClickPointer;
		public event Action OnClick;
		public event Action OnDown;
		public event Action OnUp;

		public bool IsPressed => _isPressed;
		public bool IsPointerInside => _isPointerInside;

		private bool _isPressed;
		private bool _isPointerInside;
		private int _activePointerId = -1;

		public virtual void OnDisable() => ResetPointerState();
		public virtual void OnEnable() => ResetPointerState();

		public virtual void OnPointerDown(PointerEventData eventData)
		{
			if (!IsInteractable || _activePointerId != -1)
			{
				return;
			}

			_activePointerId = eventData.pointerId;
			_isPressed = true;
			_isPointerInside = true;
			OnDown?.Invoke();
		}

		public virtual void OnPointerUp(PointerEventData eventData)
		{
			if (_activePointerId == -1 || eventData.pointerId != _activePointerId)
			{
				return;
			}

			_isPressed = false;
			OnUp?.Invoke();

			if (_isPointerInside && IsInteractable)
			{
				OnClick?.Invoke();
				OnClickPointer?.Invoke(eventData);
			}

			_activePointerId = -1;
		}

		public virtual void OnPointerEnter(PointerEventData eventData)
		{
			if (_activePointerId != -1 && eventData.pointerId != _activePointerId)
			{
				return;
			}

			_isPointerInside = true;
		}

		public virtual void OnPointerExit(PointerEventData eventData)
		{
			if (_activePointerId != -1 && eventData.pointerId != _activePointerId)
			{
				return;
			}

			_isPointerInside = false;
		}

		public void Dispose()
		{
			OnClickPointer = null;
			OnClick = null;
			OnDown = null;
			OnUp = null;
		}

		private void ResetPointerState()
		{
			_activePointerId = -1;
			_isPressed = false;
			_isPointerInside = false;
		}
	}
}
