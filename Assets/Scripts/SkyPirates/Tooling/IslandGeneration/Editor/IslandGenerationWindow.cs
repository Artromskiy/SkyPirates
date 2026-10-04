#if UNITY_EDITOR
using System;
using System.IO;
using UnityEditor;
using UnityEditor.UIElements;
using UnityEngine;
using UnityEngine.UIElements;

namespace DVG.SkyPirates.Tooling.IslandGeneration.Editor
{
    public sealed class IslandGenerationWindow : EditorWindow
    {
        private const string UxmlPath = "Assets/Scripts/SkyPirates/Tooling/IslandGeneration/Editor/IslandGenerationWindow.uxml";
        private const string ShapeShaderResourceName = "IslandShapeField";
        private const double RealtimeGenerationDebounceSeconds = 0.2d;

        [SerializeField] private IslandGenerationProfile profile;
        [SerializeField] private bool realtimeGeneration;

        private ObjectField _profileField;
        private ScrollView _settingsScroll;
        private Toggle _realtimeGenerationToggle;
        private Label _shaderStatus;
        private Label _status;
        private Image _preview;
        private VisualElement _preview3dHost;
        private IMGUIContainer _preview3dContainer;
        private IslandGeneration3DPreview _preview3d;
        private Foldout _preview3dFoldout;
        private Foldout _preview2dFoldout;
        private TwoPaneSplitView _mainSplitView;
        private Button _generateButton;
        private Button _resetSettingsButton;
        private SerializedObject _serializedProfile;
        private bool _generationQueued;
        private double _generationDueTime;
        private IslandGenerationGenerator.GenerationOperation _activeGeneration;

        [MenuItem("Tools/Sky Pirates/Island Generation")]
        public static void Open()
        {
            var window = GetWindow<IslandGenerationWindow>();
            window.titleContent = new GUIContent("Island Generation");
            window.minSize = new Vector2(560f, 720f);
        }

        private void OnEnable()
        {
            EditorApplication.update += UpdateRealtimeGeneration;
        }

        private void OnDisable()
        {
            EditorApplication.update -= UpdateRealtimeGeneration;
            _generationQueued = false;
            _activeGeneration?.Cancel();
            _preview3d?.Dispose();
            _preview3d = null;
        }

        public void CreateGUI()
        {
            var tree = AssetDatabase.LoadAssetAtPath<VisualTreeAsset>(UxmlPath);
            if (tree == null)
            {
                rootVisualElement.Add(new HelpBox($"Island generation UI was not found: {UxmlPath}", HelpBoxMessageType.Error));
                return;
            }

            tree.CloneTree(rootVisualElement);
            _profileField = rootVisualElement.Q<ObjectField>("profile-field");
            _settingsScroll = rootVisualElement.Q<ScrollView>("settings-scroll");
            _realtimeGenerationToggle = rootVisualElement.Q<Toggle>("realtime-generation-toggle");
            _shaderStatus = rootVisualElement.Q<Label>("shader-status");
            _status = rootVisualElement.Q<Label>("status-label");
            _preview = rootVisualElement.Q<Image>("preview-image");
            _preview3dHost = rootVisualElement.Q<VisualElement>("preview-3d-host");
            _preview3dFoldout = rootVisualElement.Q<Foldout>("preview-3d-foldout");
            _preview2dFoldout = rootVisualElement.Q<Foldout>("preview-2d-foldout");
            _mainSplitView = rootVisualElement.Q<TwoPaneSplitView>("main-split-view");
            _generateButton = rootVisualElement.Q<Button>("generate-button");
            _resetSettingsButton = rootVisualElement.Q<Button>("reset-settings-button");
            var createProfileButton = rootVisualElement.Q<Button>("create-profile-button");

            if (_profileField == null || _settingsScroll == null || _realtimeGenerationToggle == null ||
                _shaderStatus == null || _status == null || _preview == null || _preview3dHost == null ||
                _preview3dFoldout == null || _preview2dFoldout == null || _mainSplitView == null || _generateButton == null ||
                _resetSettingsButton == null || createProfileButton == null)
            {
                rootVisualElement.Clear();
                rootVisualElement.Add(new HelpBox(
                    "Island generation UI is missing required elements. Check IslandGenerationWindow.uxml element names.",
                    HelpBoxMessageType.Error));
                return;
            }

            _profileField.objectType = typeof(IslandGenerationProfile);
            _profileField.allowSceneObjects = false;
            _settingsScroll.mode = ScrollViewMode.Vertical;
            _settingsScroll.horizontalScrollerVisibility = ScrollerVisibility.Hidden;
            _settingsScroll.contentContainer.style.minWidth = 0f;
            _settingsScroll.contentContainer.style.width = Length.Percent(100f);
            _mainSplitView.viewDataKey = "sky-pirates-island-generation-split";
            _profileField.RegisterValueChangedCallback(change => SetProfile(change.newValue as IslandGenerationProfile));
            createProfileButton.clicked += CreateProfile;
            _resetSettingsButton.clicked += ResetProfileSettings;
            _realtimeGenerationToggle.SetValueWithoutNotify(realtimeGeneration);
            _realtimeGenerationToggle.RegisterValueChangedCallback(change =>
            {
                realtimeGeneration = change.newValue;
                if (realtimeGeneration)
                {
                    ScheduleRealtimeGeneration();
                }
                else
                {
                    _generationQueued = false;
                    _activeGeneration?.Cancel();
                    _status.text = profile == null
                        ? "Create or select a profile to start."
                        : "Realtime generation is off. Press Generate Island to refresh the preview.";
                }
            });
            _preview3dFoldout.RegisterValueChangedCallback(_ => UpdatePreviewFoldoutLayout());
            _preview2dFoldout.RegisterValueChangedCallback(_ => UpdatePreviewFoldoutLayout());
            UpdatePreviewFoldoutLayout();
            _generateButton.clicked += Generate;
            _preview.scaleMode = ScaleMode.ScaleToFit;
            _preview3d?.Dispose();
            _preview3d = new IslandGeneration3DPreview();
            _preview3dContainer = new IMGUIContainer(_preview3d.Draw);
            _preview3dContainer.style.width = Length.Percent(100f);
            _preview3dContainer.style.flexGrow = 1f;
            _preview3dContainer.style.flexShrink = 1f;
            _preview3dHost.Add(_preview3dContainer);

            if (profile == null)
            {
                profile = FindSingleProfile();
            }

            SetProfile(profile);
        }

        private void CreateProfile()
        {
            const string profileFolder = "Assets/GeneratedHexIslands/Profiles";
            EnsureAssetFolder(profileFolder);
            string path = EditorUtility.SaveFilePanelInProject(
                "Create island generation profile",
                "IslandGenerationProfile",
                "asset",
                "Choose where to save the island generation profile.",
                profileFolder);
            if (string.IsNullOrEmpty(path))
            {
                return;
            }

            var newProfile = CreateInstance<IslandGenerationProfile>();
            AssetDatabase.CreateAsset(newProfile, path);
            AssetDatabase.SaveAssets();
            SetProfile(newProfile);
        }

        private void ResetProfileSettings()
        {
            if (profile == null)
            {
                return;
            }

            _activeGeneration?.Cancel();
            _generationQueued = false;
            _serializedProfile.ApplyModifiedProperties();
            Undo.RecordObject(profile, "Reset island generation settings");
            profile.ResetSettingsToDefaults();
            EditorUtility.SetDirty(profile);
            _serializedProfile.Update();
            RebuildSettings();
            RefreshStatus();

            if (realtimeGeneration)
            {
                ScheduleRealtimeGeneration();
            }
            else
            {
                _status.text = "Settings reset. Press Generate Island to refresh the preview.";
            }
        }

        private void SetProfile(IslandGenerationProfile newProfile)
        {
            if (profile != newProfile)
            {
                _activeGeneration?.Cancel();
                _generationQueued = false;
            }

            _serializedProfile?.ApplyModifiedProperties();
            profile = newProfile;
            _profileField.SetValueWithoutNotify(profile);
            _resetSettingsButton.SetEnabled(profile != null);
            _serializedProfile = profile == null ? null : new SerializedObject(profile);
            RebuildSettings();
            RefreshStatus();
            if (realtimeGeneration && profile != null)
            {
                ScheduleRealtimeGeneration();
            }
        }

        private void RebuildSettings()
        {
            _settingsScroll.Clear();
            if (_serializedProfile == null)
            {
                _settingsScroll.Add(new HelpBox("Create or select an island generation profile.", HelpBoxMessageType.Info));
                return;
            }

            _serializedProfile.Update();

            var grid = AddFoldout("Hex grid");
            AddField(grid, "columns", "Columns");
            AddField(grid, "rows", "Rows");
            AddField(grid, "seed", "Seed");

            var shape = AddFoldout("Island shape");
            AddField(shape, "islandScale", "Island scale");
            AddField(shape, "outlinePower", "Outline roundness");
            AddField(shape, "boundaryWarp", "Boundary warp");
            AddField(shape, "boundaryFrequency", "Boundary frequency");
            AddField(shape, "centerOffsetX", "Center offset X");
            AddField(shape, "centerOffsetY", "Center offset Y");

            var elevation = AddFoldout("Elevation");
            AddField(elevation, "maximumHeight", "Maximum height");
            AddField(elevation, "heightLevels", "Height levels");
            AddField(elevation, "coastExponent", "Coast profile");
            AddField(elevation, "heightNoiseStrength", "Height variation");
            AddField(elevation, "heightNoiseFrequency", "Height frequency");

            var erosion = AddFoldout("Thermal erosion");
            AddField(erosion, "thermalErosionIterations", "Erosion iterations");
            AddField(erosion, "talusHeight", "Talus height");
            AddField(erosion, "erosionRate", "Erosion rate");

            var appearance = AddFoldout("Preview appearance");
            AddField(appearance, "heightGradient", "Height color gradient", false);
            AddField(appearance, "previewVerticalScale", "3D hex height scale", false);
        }

        private Foldout AddFoldout(string title)
        {
            var foldout = new Foldout { text = title, value = true };
            _settingsScroll.Add(foldout);
            return foldout;
        }

        private void AddField(VisualElement parent, string propertyName, string label, bool affectsGeneration = true)
        {
            var property = _serializedProfile.FindProperty(propertyName);
            if (property == null)
            {
                parent.Add(new HelpBox($"Profile field was not found: {propertyName}", HelpBoxMessageType.Error));
                return;
            }

            var field = new PropertyField(property, label);
            field.style.minWidth = 0f;
            field.style.flexShrink = 1f;
            field.RegisterValueChangeCallback(affectsGeneration ? OnProfileSettingsChanged : OnPreviewAppearanceChanged);
            parent.Add(field);
            field.Bind(_serializedProfile);
        }

        private void OnPreviewAppearanceChanged(SerializedPropertyChangeEvent changeEvent)
        {
            _serializedProfile.ApplyModifiedProperties();
            if (profile != null)
            {
                var map = profile.LastGeneratedMap;
                IslandGenerationGenerator.RefreshPreviewAppearance(map, profile.HeightGradient);
                _preview.image = map != null ? map.Preview : null;
                _preview.MarkDirtyRepaint();
                _preview3d?.SetMap(map, profile.HeightGradient, profile.PreviewVerticalScale, true);
            }
        }

        private void UpdatePreviewFoldoutLayout()
        {
            _preview3dFoldout.style.flexGrow = _preview3dFoldout.value ? 1f : 0f;
            _preview2dFoldout.style.flexGrow = _preview2dFoldout.value ? 1f : 0f;
            _preview3dFoldout.contentContainer.style.flexGrow = _preview3dFoldout.value ? 1f : 0f;
            _preview2dFoldout.contentContainer.style.flexGrow = _preview2dFoldout.value ? 1f : 0f;
            _preview3dContainer?.MarkDirtyRepaint();
        }

        private void Generate()
        {
            if (profile == null || _activeGeneration != null)
            {
                return;
            }

            try
            {
                _serializedProfile.ApplyModifiedProperties();
                var shader = FindShapeShader();
                if (shader == null)
                {
                    throw new InvalidOperationException("IslandShapeField.compute could not be found in the project.");
                }

                _generationQueued = false;
                var generationProfile = profile;
                IslandGenerationGenerator.GenerationOperation operation = null;
                operation = IslandGenerationGenerator.CreateOperation(
                    generationProfile,
                    map =>
                    {
                        if (this == null || _activeGeneration != operation)
                        {
                            return;
                        }

                        Undo.RecordObject(generationProfile, "Generate hex island");
                        generationProfile.LastGeneratedMap = map;
                        EditorUtility.SetDirty(generationProfile);
                        AssetDatabase.SaveAssets();
                        RefreshStatus(true);
                        FinishGeneration(operation);
                    },
                    exception =>
                    {
                        if (this == null || _activeGeneration != operation)
                        {
                            return;
                        }

                        _status.text = exception.Message;
                        UnityEngine.Debug.LogException(exception);
                    },
                    finished =>
                    {
                        if (this != null)
                        {
                            FinishGeneration(finished);
                        }
                    });
                _activeGeneration = operation;
                _generateButton.SetEnabled(false);
                _status.text = "Generating island… GPU work is running asynchronously.";
                operation.Start(shader);
            }
            catch (Exception exception)
            {
                if (_activeGeneration != null && _activeGeneration.IsFinished)
                {
                    _activeGeneration = null;
                }

                if (_generateButton != null)
                {
                    _generateButton.SetEnabled(profile != null && FindShapeShader() != null);
                }

                _status.text = exception.Message;
                UnityEngine.Debug.LogException(exception);
            }
        }

        private void OnProfileSettingsChanged(SerializedPropertyChangeEvent changeEvent)
        {
            _activeGeneration?.Cancel();
            if (realtimeGeneration)
            {
                ScheduleRealtimeGeneration();
                return;
            }

            _status.text = profile == null
                ? "Create or select a profile to start."
                : "Settings changed. Press Generate Island to refresh the preview.";
        }

        private void ScheduleRealtimeGeneration()
        {
            if (!realtimeGeneration || profile == null)
            {
                return;
            }

            _generationQueued = true;
            _activeGeneration?.Cancel();
            _generationDueTime = EditorApplication.timeSinceStartup + RealtimeGenerationDebounceSeconds;
            _status.text = "Realtime generation pending…";
        }

        private void UpdateRealtimeGeneration()
        {
            if (!_generationQueued || _activeGeneration != null || EditorApplication.timeSinceStartup < _generationDueTime)
            {
                return;
            }

            _generationQueued = false;
            if (realtimeGeneration && profile != null)
            {
                Generate();
            }
        }

        private void FinishGeneration(IslandGenerationGenerator.GenerationOperation operation)
        {
            if (_activeGeneration != operation)
            {
                return;
            }

            _activeGeneration = null;
            _generateButton.SetEnabled(profile != null && FindShapeShader() != null);
            if (_generationQueued)
            {
                UpdateRealtimeGeneration();
            }
        }

        private void RefreshStatus(bool force3DRefresh = false)
        {
            var shader = FindShapeShader();
            _shaderStatus.text = shader == null
                ? "Compute shader: IslandShapeField.compute was not found."
                : "Compute shader: IslandShapeField.compute (automatic)";
            _generateButton.SetEnabled(profile != null && shader != null);

            if (profile == null)
            {
                _status.text = "Create or select a profile to start.";
                _preview.image = null;
                _preview3d?.SetMap(null, null, 1f);
                _resetSettingsButton.SetEnabled(false);
                return;
            }

            _resetSettingsButton.SetEnabled(true);

            var map = profile.LastGeneratedMap;
            if (map == null)
            {
                _status.text = "Adjust the settings, then generate an island.";
                _preview.image = null;
                _preview3d?.SetMap(null, profile.HeightGradient, profile.PreviewVerticalScale);
                return;
            }

            if (map.Preview == null)
            {
                IslandGenerationGenerator.RefreshPreviewAppearance(map, profile.HeightGradient);
            }

            _status.text = $"{map.Columns} × {map.Rows} hexes · {map.LandCellCount} land hexes · seed {map.Seed}";
            _preview.image = map.Preview;
            _preview3d?.SetMap(map, profile.HeightGradient, profile.PreviewVerticalScale, force3DRefresh);
        }

        private static IslandGenerationProfile FindSingleProfile()
        {
            string[] profileGuids = AssetDatabase.FindAssets("t:IslandGenerationProfile");
            if (profileGuids.Length != 1)
            {
                return null;
            }

            string path = AssetDatabase.GUIDToAssetPath(profileGuids[0]);
            return AssetDatabase.LoadAssetAtPath<IslandGenerationProfile>(path);
        }

        private static ComputeShader FindShapeShader()
        {
            return Resources.Load<ComputeShader>(ShapeShaderResourceName);
        }

        private static void EnsureAssetFolder(string folderPath)
        {
            if (string.IsNullOrEmpty(folderPath) || AssetDatabase.IsValidFolder(folderPath))
            {
                return;
            }

            string? parent = Path.GetDirectoryName(folderPath)?.Replace('\\', '/');
            string name = Path.GetFileName(folderPath);
            if (string.IsNullOrEmpty(parent) || string.IsNullOrEmpty(name))
            {
                throw new InvalidOperationException($"Cannot create project asset folder: {folderPath}");
            }

            EnsureAssetFolder(parent);
            if (!AssetDatabase.IsValidFolder(folderPath))
            {
                AssetDatabase.CreateFolder(parent, name);
            }
        }
    }
}
#endif
