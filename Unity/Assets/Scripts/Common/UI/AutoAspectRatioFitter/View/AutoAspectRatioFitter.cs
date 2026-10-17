using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

namespace Common.UI
{
	[ExecuteAlways]
	[DisallowMultipleComponent]
	[RequireComponent(typeof(RectTransform), typeof(Image))]
	public sealed class AutoAspectRatioFitter : UIBehaviour, ILayoutSelfController
	{
		[SerializeField] private AspectRatioFitter.AspectMode _aspectMode;

		private DrivenRectTransformTracker _tracker;
		private RectTransform? _rectTransform;
		private Image? _image;
		private Sprite? _sprite;
		private float _aspectRatio;

		private RectTransform CachedRectTransform =>
			_rectTransform != null ? _rectTransform : _rectTransform = (RectTransform)transform;

		private Image TargetImage => _image != null ? _image : _image = GetComponent<Image>();

		protected override void OnEnable()
		{
			base.OnEnable();
			RefreshAspectRatio();
			SetDirty();
		}

		protected override void OnDisable()
		{
			_tracker.Clear();
			LayoutRebuilder.MarkLayoutForRebuild(CachedRectTransform);
			base.OnDisable();
		}

		protected override void OnRectTransformDimensionsChange()
		{
			SetDirty();
		}

#if UNITY_EDITOR
		protected override void OnValidate()
		{
			base.OnValidate();
			RefreshAspectRatio();
			SetDirty();
		}
#endif

		private void LateUpdate()
		{
			if (TargetImage.sprite == _sprite)
			{
				return;
			}

			RefreshAspectRatio();
			SetDirty();
		}

		public void SetLayoutHorizontal()
		{
		}

		public void SetLayoutVertical() => UpdateRect();

		private void RefreshAspectRatio()
		{
			_sprite = TargetImage.sprite;
			_aspectRatio = _sprite != null && _sprite.rect.height > 0f
				? _sprite.rect.width / _sprite.rect.height
				: 1f;
		}

		private void UpdateRect()
		{
			_tracker.Clear();

			if (_aspectMode == AspectRatioFitter.AspectMode.None)
			{
				return;
			}

			RectTransform rectTransform = CachedRectTransform;

			switch (_aspectMode)
			{
				case AspectRatioFitter.AspectMode.WidthControlsHeight:
					_tracker.Add(this, rectTransform, DrivenTransformProperties.SizeDeltaY);
					rectTransform.SetSizeWithCurrentAnchors(RectTransform.Axis.Vertical, rectTransform.rect.width / _aspectRatio);
					break;

				case AspectRatioFitter.AspectMode.HeightControlsWidth:
					_tracker.Add(this, rectTransform, DrivenTransformProperties.SizeDeltaX);
					rectTransform.SetSizeWithCurrentAnchors(RectTransform.Axis.Horizontal, rectTransform.rect.height * _aspectRatio);
					break;

				case AspectRatioFitter.AspectMode.FitInParent:
				case AspectRatioFitter.AspectMode.EnvelopeParent:
					FitToParent(rectTransform);
					break;
			}
		}

		private void FitToParent(RectTransform rectTransform)
		{
			RectTransform? parent = rectTransform.parent as RectTransform;
			if (parent == null)
			{
				return;
			}

			_tracker.Add(
				this,
				rectTransform,
				DrivenTransformProperties.Anchors |
				DrivenTransformProperties.AnchoredPosition |
				DrivenTransformProperties.SizeDeltaX |
				DrivenTransformProperties.SizeDeltaY);

			rectTransform.anchorMin = Vector2.zero;
			rectTransform.anchorMax = Vector2.one;
			rectTransform.anchoredPosition = Vector2.zero;

			Vector2 parentSize = parent.rect.size;
			Vector2 sizeDelta = Vector2.zero;
			bool controlHeight = parentSize.y * _aspectRatio < parentSize.x;
			if (_aspectMode == AspectRatioFitter.AspectMode.FitInParent)
			{
				controlHeight = !controlHeight;
			}

			if (controlHeight)
			{
				sizeDelta.y = parentSize.x / _aspectRatio - parentSize.y;
			}
			else
			{
				sizeDelta.x = parentSize.y * _aspectRatio - parentSize.x;
			}

			rectTransform.sizeDelta = sizeDelta;
		}

		private void SetDirty()
		{
			if (!IsActive())
			{
				return;
			}

			LayoutRebuilder.MarkLayoutForRebuild(CachedRectTransform);
		}
	}
}
