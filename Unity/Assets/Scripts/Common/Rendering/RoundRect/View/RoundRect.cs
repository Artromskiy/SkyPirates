using UnityEngine;
using UnityEngine.Sprites;
using UnityEngine.UI;
using Common.UI.Internals;

namespace Common.UI
{
	public enum RoundRectSpriteFitMode { Fit, Overlap, Fill }

	[DisallowMultipleComponent]
	[AddComponentMenu("Common/UI/RoundRect", 0)]
	[RequireComponent(typeof(RectTransform), typeof(CanvasRenderer))]
	public sealed class RoundRect : MaskableGraphic, ICanvasRaycastFilter
	{
		private const string ShaderResourcePath = "RoundRect";
		private const string ShaderName = "Common/UI/RoundRect";
		private const string ExternalAlphaPropertyName = "_EnableExternalAlpha";

		private const AdditionalCanvasShaderChannels RequiredCanvasChannels =
			AdditionalCanvasShaderChannels.Normal |
			AdditionalCanvasShaderChannels.Tangent |
			AdditionalCanvasShaderChannels.TexCoord1 |
			AdditionalCanvasShaderChannels.TexCoord2 |
			AdditionalCanvasShaderChannels.TexCoord3;

		[SerializeField] private RoundRectCornerRadii _cornerRadii;
		[SerializeField] private bool _useIndividualCornerRadii;
		[SerializeField, Tooltip("Sprite rendered inside the rounded rectangle. The Graphic color tints the sprite.")]
		private Sprite _sprite;
		[SerializeField, Tooltip("Fit preserves the full sprite, Overlap preserves aspect and crops to fill, and Fill stretches to the rectangle.")]
		private RoundRectSpriteFitMode _spriteFitMode;
		[SerializeField, Tooltip("Render the sprite in grayscale while preserving its alpha and the rectangle's border and effects.")]
		private bool _grayscale;
		[SerializeField] private RoundRectBorderWidths _borderWidths;
		[SerializeField] private bool _useIndividualBorderWidths;
		[SerializeField] private Color _borderColor = Color.black;
		[SerializeField] private bool _shadowEnabled;
		[SerializeField] private Color _shadowColor = new(0f, 0f, 0f, 0.35f);
		[SerializeField] private Vector2 _shadowOffset = new(0f, -5f);
		[SerializeField, Min(0f)] private float _shadowSpread;
		[SerializeField, Min(0f)] private float _shadowSoftness;
		[SerializeField] private bool _glowEnabled;
		[SerializeField] private Color _glowColor = Color.black;
		[SerializeField, Min(0f)] private float _glowExtent;

		private static Material _sharedMaterial = null!;
		private static Material _sharedAlphaMaterial = null!;
		private static readonly int _enableExternalAlpha = Shader.PropertyToID(ExternalAlphaPropertyName);

		public RoundRectCornerRadii CornerRadii { get => _cornerRadii; set => SetVertices(ref _cornerRadii, value); }
		public Sprite Sprite { get => _sprite; set => SetAll(ref _sprite, value); }
		public RoundRectSpriteFitMode SpriteFitMode { get => _spriteFitMode; set => SetVertices(ref _spriteFitMode, value); }
		public bool Grayscale { get => _grayscale; set => SetVertices(ref _grayscale, value); }
		public RoundRectBorderWidths BorderWidths { get => _borderWidths; set => SetVertices(ref _borderWidths, value); }
		public Color BorderColor { get => _borderColor; set => SetVertices(ref _borderColor, value); }
		public Color ShadowColor { get => _shadowColor; set => SetVertices(ref _shadowColor, value); }
		public Vector2 ShadowOffset { get => _shadowOffset; set => SetVertices(ref _shadowOffset, value); }
		public float ShadowSpread { get => _shadowSpread; set => SetVertices(ref _shadowSpread, value); }
		public float ShadowSoftness { get => _shadowSoftness; set => SetVertices(ref _shadowSoftness, value); }
		public float GlowExtent { get => _glowExtent; set => SetVertices(ref _glowExtent, value); }
		public Color GlowColor { get => _glowColor; set => SetVertices(ref _glowColor, value); }

		public override Texture mainTexture => _sprite != null ? _sprite.texture : base.mainTexture;

		public override Material defaultMaterial
		{
			get
			{
				var sharedMaterial = GetSharedMaterial();
				if (sharedMaterial == null)
				{
					return base.defaultMaterial;
				}

				if (_sprite == null || _sprite.associatedAlphaSplitTexture == null)
				{
					return sharedMaterial;
				}

				if (_sharedAlphaMaterial != null)
				{
					return _sharedAlphaMaterial;
				}

				_sharedAlphaMaterial = new Material(sharedMaterial) { hideFlags = HideFlags.HideAndDontSave };
				_sharedAlphaMaterial.SetFloat(_enableExternalAlpha, 1f);
				return _sharedAlphaMaterial;
			}
		}

#if UNITY_EDITOR
		// UIBehaviour.Reset and OnValidate exist only in the editor, so an unguarded override fails the player script compile.
		protected override void Reset()
		{
			base.Reset();
			raycastTarget = false;
			maskable = false;
		}
#endif

		protected override void OnEnable()
		{
			base.OnEnable();
			canvasRenderer.cullTransparentMesh = false;
			EnableRequiredCanvasChannels();
		}

		protected override void OnTransformParentChanged()
		{
			base.OnTransformParentChanged();
			EnableRequiredCanvasChannels();
		}

		protected override void UpdateMaterial()
		{
			base.UpdateMaterial();
			canvasRenderer.SetAlphaTexture(_sprite != null ? _sprite.associatedAlphaSplitTexture : null);
		}

#if UNITY_EDITOR
		protected override void OnValidate()
		{
			base.OnValidate();
			SetAllDirty();
		}
#endif

		protected override void OnPopulateMesh(VertexHelper vh)
		{
			vh.Clear();

			var rect = GetPixelAdjustedRect();
			if (rect.width <= 0f || rect.height <= 0f)
			{
				return;
			}

			var radii = GetNormalizedRadii(rect.size);
			var borderWidths = Vector4.Max(Pack(_borderWidths), Vector4.zero);

			var shadowSpread = _shadowEnabled ? Mathf.Max(0f, _shadowSpread) : 0f;
			var shadowSoftness = _shadowEnabled ? Mathf.Max(0f, _shadowSoftness) : 0f;
			var shadowOffset = _shadowEnabled ? _shadowOffset : Vector2.zero;
			var glowExtent = _glowEnabled ? Mathf.Max(0f, _glowExtent) : 0f;

			var shadowExtent = Vector2.one * (shadowSpread + shadowSoftness);
			var glowExtent2 = Vector2.one * glowExtent;
			var minExtent = Vector2.Max(glowExtent2, shadowExtent - shadowOffset);
			var maxExtent = Vector2.Max(glowExtent2, shadowExtent + shadowOffset);

			var effectsRect = rect;
			effectsRect.min -= minExtent;
			effectsRect.max += maxExtent;

			var borderOpacity = _borderColor.a;
			var borderColor = Rgb(_borderColor);
			var fillColor = ShaderVertexColor(color);

			var effects = Pack(shadowOffset, new Vector2(glowExtent, -borderOpacity - 1f));
			var shadowColor = _shadowEnabled ? Rgba(_shadowColor, color.a) : Vector4.zero;

			AddEffectsQuad(vh, effectsRect, rect, radii, effects, Rgb(_glowColor), _glowColor.a * color.a, shadowColor, shadowSoftness, shadowSpread);

			if (_sprite != null)
			{
				AddSpriteQuad(vh, rect, radii, _sprite, borderWidths, fillColor, borderColor, borderOpacity);
			}
			else
			{
				AddSolidQuad(vh, rect, radii, borderWidths, fillColor, borderColor, borderOpacity);
			}
		}

		public bool IsRaycastLocationValid(Vector2 screenPoint, Camera eventCamera)
		{
			if (!RectTransformUtility.ScreenPointToLocalPointInRectangle(rectTransform, screenPoint, eventCamera, out var point))
			{
				return false;
			}

			var rect = GetPixelAdjustedRect();
			if (!rect.Contains(point))
			{
				return false;
			}

			var radii = GetNormalizedRadii(rect.size);
			var local = point - rect.center;
			var halfSize = rect.size * 0.5f;
			var radius = local.y > 0f ? local.x > 0f ? radii.y : radii.x : local.x > 0f ? radii.z : radii.w;
			var corner = Abs(local) - (halfSize - Vector2.one * radius);

			return corner.x <= 0f || corner.y <= 0f || corner.sqrMagnitude <= radius * radius;
		}

		private void AddSpriteQuad(VertexHelper vh, Rect rect, Vector4 radii, Sprite sprite, Vector4 borderWidths, Color fillColor, Vector3 borderColor, float borderOpacity)
		{
			var spriteRect = GetSpriteRect(rect, sprite);
			var outerUv = DataUtility.GetOuterUV(sprite);
			var uvMin = new Vector2(outerUv.x, outerUv.y);
			var uvMax = new Vector2(outerUv.z, outerUv.w);

			if (_spriteFitMode == RoundRectSpriteFitMode.Overlap)
			{
				var spriteAspect = sprite.rect.width / sprite.rect.height;
				var rectAspect = rect.width / rect.height;
				var horizontal = spriteAspect > rectAspect;
				var visible = horizontal ? rectAspect / spriteAspect : spriteAspect / rectAspect;
				var crop = Vector2.Scale(uvMax - uvMin, horizontal ? Vector2.right : Vector2.up) * ((1f - visible) * 0.5f);

				uvMin += crop;
				uvMax -= crop;
			}

			var fitCenter = spriteRect.center - rect.center;
			var fitHalfSize = spriteRect.size * 0.5f;
			// TEXCOORD3.w: 2 = colored sprite, 3 = grayscale sprite.
			var spriteLayer = Pack(fitCenter, new Vector2(borderOpacity, _grayscale ? 3f : 2f));
			var uvScale = new Vector2(1f / spriteRect.width, 1f / spriteRect.height);
			var start = vh.currentVertCount;

			for (var i = 0; i < 4; i++)
			{
				var position = Corner(rect, i);
				var t = Vector2.Scale(position - spriteRect.min, uvScale);
				var uv = uvMin + Vector2.Scale(uvMax - uvMin, t);

				vh.AddVert(new Vector3(position.x, position.y), fillColor, Pack(uv, fitHalfSize), Shape(position, rect), radii, spriteLayer, borderColor, borderWidths);
			}

			AddQuadTriangles(vh, start);
		}

		private void AddSolidQuad(VertexHelper vh, Rect rect, Vector4 radii, Vector4 borderWidths, Color fillColor, Vector3 borderColor, float borderOpacity)
		{
			var effects = new Vector4(0f, 0f, borderOpacity, 0f);
			var start = vh.currentVertCount;

			for (var i = 0; i < 4; i++)
			{
				var position = Corner(rect, i);
				vh.AddVert(new Vector3(position.x, position.y), fillColor, Shape(position, rect), radii, Vector4.zero, effects, borderColor, borderWidths);
			}

			AddQuadTriangles(vh, start);
		}

		private static void AddEffectsQuad(VertexHelper vh, Rect drawRect, Rect shapeRect, Vector4 radii, Vector4 effects, Vector3 glowColor, float glowOpacity, Vector4 shadowColor, float shadowSoftness, float shadowSpread)
		{
			var style = Pack(new Vector2(glowOpacity, 0f), new Vector2(shadowSoftness, shadowSpread));
			var start = vh.currentVertCount;

			for (var i = 0; i < 4; i++)
			{
				var position = Corner(drawRect, i);
				vh.AddVert(new Vector3(position.x, position.y), Color.clear, Shape(position, shapeRect), radii, style, effects, glowColor, shadowColor);
			}

			AddQuadTriangles(vh, start);
		}

		private Rect GetSpriteRect(Rect rect, Sprite sprite)
		{
			if (_spriteFitMode != RoundRectSpriteFitMode.Fit)
			{
				return rect;
			}

			var scale = Mathf.Min(rect.width / sprite.rect.width, rect.height / sprite.rect.height);
			var size = sprite.rect.size * scale;
			return new Rect(rect.min + Vector2.Scale(rect.size - size, rectTransform.pivot), size);
		}

		private Vector4 GetNormalizedRadii(Vector2 size)
		{
			var r = Pack(_cornerRadii);
			if (!(size.x > 0f && size.y > 0f && r.x >= 0f && r.y >= 0f && r.z >= 0f && r.w >= 0f))
			{
				return default;
			}

			var horizontal = new Vector2(r.x, r.w) + new Vector2(r.y, r.z);
			var vertical = new Vector2(r.x, r.y) + new Vector2(r.w, r.z);
			var maxSums = new Vector2(Mathf.Max(horizontal.x, horizontal.y), Mathf.Max(vertical.x, vertical.y));
			return r * Mathf.Min(1f, Mathf.Min(size.x / maxSums.x, size.y / maxSums.y));
		}

		private static Material GetSharedMaterial()
		{
			if (_sharedMaterial != null)
			{
				return _sharedMaterial;
			}

			var shader = Resources.Load<Shader>(ShaderResourcePath);
			if (shader == null)
			{
				shader = Shader.Find(ShaderName);
			}

			if (shader == null)
			{
				return null!;
			}

			return _sharedMaterial = new Material(shader) { hideFlags = HideFlags.HideAndDontSave };
		}

		private void EnableRequiredCanvasChannels()
		{
			if (canvas != null)
			{
				canvas.additionalShaderChannels |= RequiredCanvasChannels;
			}
		}

		private void SetVertices<T>(ref T field, T value)
		{
			field = value;
			SetVerticesDirty();
		}

		private void SetAll<T>(ref T field, T value)
		{
			field = value;
			SetAllDirty();
		}

		private static Vector2 Corner(Rect rect, int index) => new(index < 2 ? rect.xMin : rect.xMax, index is 0 or 3 ? rect.yMin : rect.yMax);
		private static Vector4 Shape(Vector2 point, Rect rect) => Pack(point - rect.center, rect.size * 0.5f);
		private static Vector2 Abs(Vector2 value) => new(Mathf.Abs(value.x), Mathf.Abs(value.y));
		private static Vector4 Pack(Vector2 xy, Vector2 zw) => new(xy.x, xy.y, zw.x, zw.y);
		private static Vector4 Pack(RoundRectCornerRadii r) => new(r.TopLeft, r.TopRight, r.BottomRight, r.BottomLeft);
		private static Vector4 Pack(RoundRectBorderWidths w) => new(w.Left, w.Bottom, w.Right, w.Top);

		private Color ShaderVertexColor(Color color)
		{
			if (QualitySettings.activeColorSpace != ColorSpace.Linear || (canvas != null && !canvas.vertexColorAlwaysGammaSpace))
			{
				return color;
			}

			return color.linear;
		}

		private static Color ShaderCustomColor(Color color) => QualitySettings.activeColorSpace == ColorSpace.Linear ? color.linear : color;
		private static Vector3 Rgb(Color color) { color = ShaderCustomColor(color); return new(color.r, color.g, color.b); }
		private static Vector4 Rgba(Color color, float alphaMultiplier = 1f) { color = ShaderCustomColor(color); return new(color.r, color.g, color.b, color.a * alphaMultiplier); }

		private static void AddQuadTriangles(VertexHelper vh, int i)
		{
			vh.AddTriangle(i, i + 1, i + 2);
			vh.AddTriangle(i + 2, i + 3, i);
		}
	}
}
