using System;

namespace SlyPirates.Rendering.RoundRect
{
	[Serializable]
	public struct RoundRectCornerRadii
	{
		public RoundRectCornerRadii(float radius)
		{
			TopLeft = radius;
			TopRight = radius;
			BottomRight = radius;
			BottomLeft = radius;
		}

		public float TopLeft;
		public float TopRight;
		public float BottomRight;
		public float BottomLeft;
	}

	[Serializable]
	public struct RoundRectBorderWidths
	{
		public RoundRectBorderWidths(float width)
		{
			Left = width;
			Bottom = width;
			Right = width;
			Top = width;
		}

		[UnityEngine.Min(0f), UnityEngine.Tooltip("Width of the left edge.")]
		public float Left;
		[UnityEngine.Min(0f), UnityEngine.Tooltip("Width of the bottom edge.")]
		public float Bottom;
		[UnityEngine.Min(0f), UnityEngine.Tooltip("Width of the right edge.")]
		public float Right;
		[UnityEngine.Min(0f), UnityEngine.Tooltip("Width of the top edge.")]
		public float Top;
	}
}
