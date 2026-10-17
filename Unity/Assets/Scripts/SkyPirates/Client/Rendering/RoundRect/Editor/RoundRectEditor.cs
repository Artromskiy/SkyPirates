using UnityEditor;
using UnityEngine;
using SlyPirates.Rendering.RoundRect.View;
using SlyPirates.Rendering.RoundRect;

namespace SlyPirates.Rendering.RoundRect.Editor
{
	[CustomEditor(typeof(RoundRect))]
	[CanEditMultipleObjects]
	internal sealed class RoundRectEditor : UnityEditor.Editor
	{
		private static readonly GUIContent[] _cornerLabels = { new("TL"), new("TR"), new("BR"), new("BL") };
		private static readonly GUIContent[] _sideLabels = { new("L"), new("T"), new("R"), new("B") };
		private const float MinFloatFieldWidth = 36f;
		private const float MinFieldLabelWidth = 16f;
		private const float MaxFieldLabelWidth = 24f;
		private const float FieldSpacing = 2f;
		private const float InspectorHorizontalPadding = 48f;

		private SerializedProperty _script = null!;
		private SerializedProperty _sprite = null!;
		private SerializedProperty _spriteFitMode = null!;
		private SerializedProperty _grayscale = null!;
		private SerializedProperty _color = null!;
		private SerializedProperty _raycastTarget = null!;
		private SerializedProperty _maskable = null!;

		private SerializedProperty _individualCorners = null!;
		private SerializedProperty[] _cornerRadii = null!;

		private SerializedProperty _individualBorder = null!;
		private SerializedProperty[] _borderWidths = null!;
		private SerializedProperty _borderColor = null!;

		private SerializedProperty _shadowEnabled = null!;
		private SerializedProperty _shadowColor = null!;
		private SerializedProperty _shadowOffset = null!;
		private SerializedProperty _shadowSpread = null!;
		private SerializedProperty _shadowSoftness = null!;

		private SerializedProperty _glowEnabled = null!;
		private SerializedProperty _glowColor = null!;
		private SerializedProperty _glowExtent = null!;

		private readonly float[] _fieldValues = new float[4];

		private void OnEnable()
		{
			_script = Find("m_Script");
			_sprite = Find("_sprite");
			_spriteFitMode = Find("_spriteFitMode");
			_grayscale = Find("_grayscale");
			_color = Find("m_Color");
			_raycastTarget = Find("m_RaycastTarget");
			_maskable = Find("m_Maskable");

			var corners = Find("_cornerRadii");
			_individualCorners = Find("_useIndividualCornerRadii");
			_cornerRadii = new[] { Child(corners, "TopLeft"), Child(corners, "TopRight"), Child(corners, "BottomRight"), Child(corners, "BottomLeft") };

			var border = Find("_borderWidths");
			_individualBorder = Find("_useIndividualBorderWidths");
			_borderWidths = new[] { Child(border, "Left"), Child(border, "Top"), Child(border, "Right"), Child(border, "Bottom") };
			_borderColor = Find("_borderColor");

			_shadowEnabled = Find("_shadowEnabled");
			_shadowColor = Find("_shadowColor");
			_shadowOffset = Find("_shadowOffset");
			_shadowSpread = Find("_shadowSpread");
			_shadowSoftness = Find("_shadowSoftness");

			_glowEnabled = Find("_glowEnabled");
			_glowColor = Find("_glowColor");
			_glowExtent = Find("_glowExtent");
		}

		public override void OnInspectorGUI()
		{
			serializedObject.Update();
			var originalLabelWidth = EditorGUIUtility.labelWidth;
			var availableLabelWidth = EditorGUIUtility.currentViewWidth
				- InspectorHorizontalPadding
				- _cornerRadii.Length * (MinFloatFieldWidth + MinFieldLabelWidth + FieldSpacing);
			EditorGUIUtility.labelWidth = Mathf.Min(
				originalLabelWidth,
				Mathf.Min(
					Mathf.Clamp(EditorGUIUtility.currentViewWidth * 0.28f, 40f, 150f),
					Mathf.Max(40f, availableLabelWidth)));

			using (new EditorGUI.DisabledScope(true))
			{
				EditorGUILayout.PropertyField(_script);
			}

			EditorGUILayout.PropertyField(_sprite, new GUIContent("Sprite"));
			if (_sprite.objectReferenceValue != null)
			{
				EditorGUILayout.PropertyField(_spriteFitMode, new GUIContent("Sprite Fit Mode"));
				EditorGUILayout.PropertyField(_grayscale, new GUIContent("Grayscale", _grayscale.tooltip));
			}

			EditorGUILayout.PropertyField(_color, new GUIContent(_sprite.objectReferenceValue == null ? "Fill Color" : "Sprite Tint"));
			EditorGUILayout.PropertyField(_raycastTarget, new GUIContent("Raycast Target"));
			EditorGUILayout.PropertyField(_maskable, new GUIContent("Maskable"));

			DrawSeparator();
			DrawValues("Corners", _individualCorners, _cornerRadii, _cornerLabels);
			DrawSeparator();
			DrawValues("Border", _individualBorder, _borderWidths, _sideLabels);
			EditorGUILayout.PropertyField(_borderColor, new GUIContent("Color"));

			DrawSeparator();
			EditorGUILayout.PropertyField(_shadowEnabled, new GUIContent("Shadow"));
			if (_shadowEnabled.boolValue)
			{
				using (new EditorGUI.IndentLevelScope())
				{
					EditorGUILayout.PropertyField(_shadowColor, new GUIContent("Color"));
					EditorGUILayout.PropertyField(_shadowOffset, new GUIContent("Offset"));
					EditorGUILayout.PropertyField(_shadowSpread, new GUIContent("Spread"));
					EditorGUILayout.PropertyField(_shadowSoftness, new GUIContent("Softness"));
				}
			}

			DrawSeparator();
			EditorGUILayout.PropertyField(_glowEnabled, new GUIContent("Glow"));
			if (_glowEnabled.boolValue)
			{
				using (new EditorGUI.IndentLevelScope())
				{
					EditorGUILayout.PropertyField(_glowColor, new GUIContent("Color"));
					EditorGUILayout.PropertyField(_glowExtent, new GUIContent("Extent"));
				}
			}

			serializedObject.ApplyModifiedProperties();
			EditorGUIUtility.labelWidth = originalLabelWidth;
		}

		private static void DrawSeparator()
		{
			EditorGUILayout.Space(2f);
			var rect = EditorGUILayout.GetControlRect(false, 1f, GUILayout.ExpandWidth(true));
			rect.height = 1f;
			rect.xMin -= 20f;
			rect.xMax += 40f;
			var color = new Color(0.18f, 0.18f, 0.18f, 1f);
			EditorGUI.DrawRect(rect, color);
			EditorGUILayout.Space(2f);
		}

		private void DrawValues(string label, SerializedProperty individual, SerializedProperty[] values, GUIContent[] labels)
		{
			var wasIndividual = individual.boolValue;
			EditorGUILayout.PropertyField(individual, new GUIContent(label));

			if (individual.boolValue)
			{
				DrawVector(values, labels);
				return;
			}

			var value = Mathf.Max(0f, values[0].floatValue);
			EditorGUI.BeginChangeCheck();
			value = EditorGUILayout.FloatField("All", value);

			if (!EditorGUI.EndChangeCheck() && wasIndividual == individual.boolValue)
			{
				return;
			}

			value = Mathf.Max(0f, value);
			foreach (var v in values)
			{
				v.floatValue = value;
			}
		}

		private void DrawVector(SerializedProperty[] values, GUIContent[] labels)
		{
			for (var i = 0; i < values.Length; i++)
			{
				_fieldValues[i] = values[i].floatValue;
			}

			var rect = EditorGUI.IndentedRect(EditorGUILayout.GetControlRect());
			rect.xMin += EditorGUIUtility.labelWidth;
			var columnWidth = (rect.width - FieldSpacing * (values.Length - 1)) / values.Length;
			var labelWidth = Mathf.Clamp(
				columnWidth - MinFloatFieldWidth - FieldSpacing,
				MinFieldLabelWidth,
				MaxFieldLabelWidth);
			var floatWidth = columnWidth - labelWidth - FieldSpacing;

			EditorGUI.BeginChangeCheck();
			for (var i = 0; i < values.Length; i++)
			{
				var x = rect.x + i * (columnWidth + FieldSpacing);
				EditorGUI.LabelField(new Rect(x, rect.y, labelWidth, rect.height), labels[i]);
				_fieldValues[i] = EditorGUI.FloatField(
					new Rect(x + labelWidth + FieldSpacing, rect.y, floatWidth, rect.height),
					_fieldValues[i]);
			}

			if (!EditorGUI.EndChangeCheck())
			{
				return;
			}

			for (var i = 0; i < values.Length; i++)
			{
				values[i].floatValue = Mathf.Max(0f, _fieldValues[i]);
			}
		}

		private SerializedProperty Find(string propName) => serializedObject.FindProperty(propName);
		private static SerializedProperty Child(SerializedProperty property, string name) => property.FindPropertyRelative(name);
	}
}
