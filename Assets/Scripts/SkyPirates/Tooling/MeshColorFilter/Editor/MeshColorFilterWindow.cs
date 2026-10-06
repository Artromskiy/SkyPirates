using System.Collections.Generic;
using System.IO;
using UnityEditor;
using UnityEditor.UIElements;
using UnityEngine;
using UnityEngine.UIElements;

namespace DVG.SkyPirates.Tooling.MeshColorFilter.Editor
{
    public sealed class MeshColorFilterWindow : EditorWindow
    {
        private const string UxmlPath = "Assets/Scripts/SkyPirates/Tooling/MeshColorFilter/Editor/MeshColorFilterWindow.uxml";
        private const float MinimumPreviewHeight = 160f;
        private const float MaximumPreviewHeight = 900f;
        [SerializeField] private MeshColorFilterProfile _profile;
        private GameObject _source;
        private Renderer _selectedRenderer;
        private readonly List<Renderer> _renderers = new List<Renderer>();
        private ObjectField _profileField;
        private ObjectField _sourceField;
        private DropdownField _rendererField;
        private VisualElement _rulesHost;
        private EnumField _triangleMode;
        private IntegerField _splitDepth;
        private HelpBox _status;
        private Foldout _highlightFoldout;
        private VisualElement _highlightPreviewHost;
        private IMGUIContainer _highlightPreviewContainer;
        private HelpBox _highlightPreviewMessage;
        private MeshColorFilterPreview _highlightPreview;
        private IVisualElementScheduledItem _highlightRefreshSchedule;
        private float _previewHeight = 300f;
        private float _resizeStartY;
        private float _resizeStartHeight;
        private int _resizePointerId = -1;
        private SerializedObject _serializedProfile;

        [MenuItem("Tools/Sky Pirates/Mesh Color Filter")]
        public static void Open()
        {
            var window = GetWindow<MeshColorFilterWindow>();
            window.titleContent = new GUIContent("Mesh Color Filter");
            window.minSize = new Vector2(560f, 640f);
        }

        public void CreateGUI()
        {
            VisualTreeAsset tree = AssetDatabase.LoadAssetAtPath<VisualTreeAsset>(UxmlPath);
            if (tree == null)
            {
                rootVisualElement.Add(new HelpBox("UI layout not found: " + UxmlPath, HelpBoxMessageType.Error));
                return;
            }
            tree.CloneTree(rootVisualElement);
            _profileField = rootVisualElement.Q<ObjectField>("profile-field");
            _sourceField = rootVisualElement.Q<ObjectField>("source-field");
            _rendererField = rootVisualElement.Q<DropdownField>("renderer-field");
            ScrollView contentScroll = rootVisualElement.Q<ScrollView>("content-scroll");
            contentScroll.horizontalScrollerVisibility = ScrollerVisibility.Hidden;
            _rulesHost = rootVisualElement.Q<VisualElement>("rules-host");
            _triangleMode = rootVisualElement.Q<EnumField>("triangle-mode");
            _splitDepth = rootVisualElement.Q<IntegerField>("split-depth");
            _status = rootVisualElement.Q<HelpBox>("status");
            _highlightFoldout = rootVisualElement.Q<Foldout>("highlight-foldout");
            _highlightPreviewHost = rootVisualElement.Q<VisualElement>("highlight-preview-host");
            _profileField.objectType = typeof(MeshColorFilterProfile);
            _profileField.allowSceneObjects = false;
            _sourceField.objectType = typeof(GameObject);
            _sourceField.allowSceneObjects = false;
            _triangleMode.Init(MeshTriangleAssignmentMode.AverageColor);
            _profileField.RegisterValueChangedCallback(change => SetProfile(change.newValue as MeshColorFilterProfile));
            _sourceField.RegisterValueChangedCallback(change => SetSource(change.newValue as GameObject));
            _rendererField.RegisterValueChangedCallback(change => SelectRenderer(change.newValue));
            _highlightFoldout.RegisterValueChangedCallback(change =>
            {
                if (change.newValue) ScheduleHighlightRefresh();
            });
            _triangleMode.RegisterValueChangedCallback(change =>
            {
                if (_profile == null) return;
                Undo.RecordObject(_profile, "Change triangle assignment mode");
                _profile.TriangleMode = (MeshTriangleAssignmentMode)change.newValue;
                EditorUtility.SetDirty(_profile);
                UpdateSplitDepth();
                ScheduleHighlightRefresh();
            });
            _splitDepth.RegisterValueChangedCallback(change =>
            {
                if (_profile == null) return;
                Undo.RecordObject(_profile, "Change adaptive split depth");
                _profile.AdaptiveSplitDepth = change.newValue;
                EditorUtility.SetDirty(_profile);
                _splitDepth.SetValueWithoutNotify(_profile.AdaptiveSplitDepth);
                ScheduleHighlightRefresh();
            });
            rootVisualElement.Q<Button>("create-profile").clicked += CreateProfile;
            rootVisualElement.Q<Button>("highlight-button").clicked += HighlightAreas;
            rootVisualElement.Q<Button>("process-button").clicked += Process;
            _highlightPreviewContainer = new IMGUIContainer(() =>
            {
                Rect rect = GUILayoutUtility.GetRect(100f, _previewHeight, GUILayout.ExpandWidth(true));
                _highlightPreview?.Draw(rect);
            });
            _highlightPreviewContainer.style.height = _previewHeight;
            _highlightPreviewHost.Add(_highlightPreviewContainer);
            _highlightPreviewMessage = new HelpBox(string.Empty, HelpBoxMessageType.Warning);
            _highlightPreviewMessage.style.display = DisplayStyle.None;
            _highlightPreviewHost.Add(_highlightPreviewMessage);
            SetupPreviewResize(rootVisualElement.Q<VisualElement>("preview-resize-handle"));
            _highlightFoldout.style.display = DisplayStyle.None;
            SetProfile(_profile);
        }

        private void OnDisable()
        {
            ClearHighlightPreview(true);
        }

        private void CreateProfile()
        {
            string path = EditorUtility.SaveFilePanelInProject("Create mesh color filter profile", "MeshColorFilterProfile", "asset", "Choose profile path.");
            if (string.IsNullOrEmpty(path)) return;
            var profile = CreateInstance<MeshColorFilterProfile>();
            AssetDatabase.CreateAsset(profile, path);
            AssetDatabase.SaveAssets();
            SetProfile(profile);
        }

        private void SetProfile(MeshColorFilterProfile profile)
        {
            ClearPreviewMesh();
            _profile = profile;
            _profileField?.SetValueWithoutNotify(profile);
            if (_profile == null)
            {
                _rulesHost?.Clear();
                _serializedProfile = null;
                _rulesHost?.Add(new HelpBox("Select a filter profile to edit its rules.", HelpBoxMessageType.Info));
                _status.text = "Create or select a filter profile.";
                ScheduleHighlightRefresh();
                return;
            }
            _triangleMode.SetValueWithoutNotify(_profile.TriangleMode);
            _splitDepth.SetValueWithoutNotify(_profile.AdaptiveSplitDepth);
            UpdateSplitDepth();
            BuildRuleList();
            ScheduleHighlightRefresh();
        }

        private void SetSource(GameObject source)
        {
            ClearPreviewMesh();
            _source = source;
            _sourceField.SetValueWithoutNotify(source);
            _renderers.Clear();
            _rendererField.choices = new List<string>();
            _rendererField.SetValueWithoutNotify(string.Empty);
            _selectedRenderer = null;
            if (source != null)
            {
                Renderer[] found = source.GetComponentsInChildren<Renderer>(true);
                foreach (Renderer renderer in found)
                    if (renderer is MeshRenderer || renderer is SkinnedMeshRenderer) _renderers.Add(renderer);
                _rendererField.choices = RendererLabels();
                if (_renderers.Count > 0)
                {
                    _rendererField.SetValueWithoutNotify(_rendererField.choices[0]);
                    SelectRenderer(_rendererField.choices[0]);
                }
            }
            _status.text = _source == null ? "Select a prefab or imported model asset." : _renderers.Count == 0 ? "No MeshRenderer or SkinnedMeshRenderer was found." : "Select the renderer to process.";
            ScheduleHighlightRefresh();
        }

        private List<string> RendererLabels()
        {
            var labels = new List<string>(_renderers.Count);
            foreach (Renderer renderer in _renderers) labels.Add(renderer.GetType().Name + " — " + GetPath(renderer.transform, _source.transform));
            return labels;
        }

        private void SelectRenderer(string label)
        {
            ClearPreviewMesh();
            List<string> labels = RendererLabels();
            int index = labels.IndexOf(label);
            _selectedRenderer = index >= 0 ? _renderers[index] : null;
            if (_selectedRenderer != null) _status.text = "Renderer selected. Source assets are read-only; output is saved as a new prefab and mesh.";
            ScheduleHighlightRefresh();
        }

        private static string GetPath(Transform target, Transform root)
        {
            var indices = new Stack<int>();
            for (Transform current = target; current != null && current != root; current = current.parent) indices.Push(current.GetSiblingIndex());
            return indices.Count == 0 ? root.name : root.name + "/" + string.Join("/", indices);
        }

        private void BuildRuleList()
        {
            _rulesHost.Clear();
            _serializedProfile = new SerializedObject(_profile);
            SerializedProperty rules = _serializedProfile.FindProperty("rules");
            var rulesField = new PropertyField(rules, "Rules");
            rulesField.AddToClassList("rules-field");
            _rulesHost.Add(rulesField);
            rulesField.Bind(_serializedProfile);
            rulesField.TrackSerializedObjectValue(_serializedProfile, _ =>
            {
                EditorUtility.SetDirty(_profile);
                ScheduleHighlightRefresh();
            });
        }

        private void UpdateSplitDepth()
        {
            _splitDepth?.SetEnabled(_profile != null && _profile.TriangleMode == MeshTriangleAssignmentMode.AdaptiveSubdivision);
        }

        private void Process()
        {
            if (!MeshColorFilterProcessor.TryCreatePrefab(_source, _selectedRenderer, _profile, out string path, out string message))
            {
                _status.text = message;
                return;
            }
            _status.text = message + " " + path;
            Selection.activeObject = AssetDatabase.LoadAssetAtPath<GameObject>(path);
            EditorGUIUtility.PingObject(Selection.activeObject);
        }

        private void HighlightAreas()
        {
            _highlightRefreshSchedule?.Pause();
            _highlightFoldout.style.display = DisplayStyle.Flex;
            _highlightFoldout.value = true;

            Mesh sourceMesh = _selectedRenderer is SkinnedMeshRenderer skinned
                ? skinned.sharedMesh
                : _selectedRenderer != null ? _selectedRenderer.GetComponent<MeshFilter>()?.sharedMesh : null;
            if (sourceMesh == null)
            {
                ClearPreviewMesh();
                SetHighlightMessage("Select a renderer with a mesh before highlighting.");
                _status.text = "Select a renderer with a mesh before highlighting.";
                return;
            }

            if (!MeshColorFilterProcessor.TryBuildMesh(sourceMesh, _selectedRenderer.sharedMaterials, _profile,
                    out Mesh previewMesh, out _, out int[] submeshRuleIndices, out string message))
            {
                ClearPreviewMesh();
                SetHighlightMessage(message);
                _status.text = message;
                return;
            }

            try
            {
                if (_highlightPreview == null)
                    _highlightPreview = new MeshColorFilterPreview(Repaint);
                _highlightPreview.SetMesh(previewMesh, submeshRuleIndices, _profile.Rules.Count);
                _highlightPreviewContainer.style.display = DisplayStyle.Flex;
                _highlightPreviewMessage.style.display = DisplayStyle.None;
                _status.text = "Matched zones use distinct colors; unmatched triangles are gray. Drag to rotate, scroll to zoom. Drag the grip below the preview to resize it.";
            }
            catch (System.Exception exception)
            {
                if (_highlightPreview != null)
                {
                    _highlightPreview.Dispose();
                    _highlightPreview = null;
                }
                else if (previewMesh != null)
                {
                    DestroyImmediate(previewMesh);
                }
                SetHighlightMessage(exception.Message);
                _status.text = "Could not create the highlight preview: " + exception.Message;
            }
        }

        private bool IsHighlightPreviewOpen => _highlightFoldout != null &&
            _highlightFoldout.style.display == DisplayStyle.Flex && _highlightFoldout.value;

        private void ScheduleHighlightRefresh()
        {
            if (!IsHighlightPreviewOpen || rootVisualElement == null || rootVisualElement.panel == null)
                return;

            _highlightRefreshSchedule?.Pause();
            _highlightRefreshSchedule = rootVisualElement.schedule.Execute(HighlightAreas).StartingIn(120);
        }

        private void ClearHighlightPreview(bool hideFoldout)
        {
            _highlightRefreshSchedule?.Pause();
            ClearPreviewMesh();
            if (hideFoldout && _highlightFoldout != null)
                _highlightFoldout.style.display = DisplayStyle.None;
        }

        private void ClearPreviewMesh()
        {
            if (_highlightPreview != null)
            {
                _highlightPreview.Dispose();
                _highlightPreview = null;
            }
        }

        private void SetHighlightMessage(string message)
        {
            _highlightPreviewContainer.style.display = DisplayStyle.None;
            _highlightPreviewMessage.text = message;
            _highlightPreviewMessage.style.display = DisplayStyle.Flex;
        }

        private void SetupPreviewResize(VisualElement handle)
        {
            handle.RegisterCallback<PointerDownEvent>(change =>
            {
                if (change.button != 0) return;
                _resizePointerId = change.pointerId;
                _resizeStartY = change.position.y;
                _resizeStartHeight = _previewHeight;
                handle.CapturePointer(_resizePointerId);
                change.StopPropagation();
            });
            handle.RegisterCallback<PointerMoveEvent>(change =>
            {
                if (_resizePointerId != change.pointerId || !handle.HasPointerCapture(change.pointerId)) return;
                _previewHeight = Mathf.Clamp(_resizeStartHeight + change.position.y - _resizeStartY, MinimumPreviewHeight, MaximumPreviewHeight);
                _highlightPreviewContainer.style.height = _previewHeight;
                change.StopPropagation();
            });
            handle.RegisterCallback<PointerUpEvent>(change =>
            {
                if (_resizePointerId != change.pointerId) return;
                handle.ReleasePointer(_resizePointerId);
                _resizePointerId = -1;
                change.StopPropagation();
            });
        }
    }
}
