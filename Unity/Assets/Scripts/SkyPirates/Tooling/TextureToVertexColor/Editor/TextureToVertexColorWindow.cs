using System.Collections.Generic;
using UnityEditor;
using UnityEditor.UIElements;
using UnityEngine;
using UnityEngine.UIElements;

namespace SlyPirates.Tooling.TextureToVertexColor.Editor
{
    public sealed class TextureToVertexColorWindow : EditorWindow
    {
        private const string DefaultVertexMaterialPath = "Assets/Visual/MainMaterials/VertexColor/Default/VC.mat";

        [SerializeField] private GameObject _source;
        [SerializeField] private Material _vertexColorMaterial;

        private readonly List<Renderer> _renderers = new List<Renderer>();
        private ObjectField _sourceField;
        private ObjectField _materialField;
        private DropdownField _rendererField;
        private HelpBox _status;
        private Renderer _selectedRenderer;

        [MenuItem("Sky Pirates/Tools/Texture To Vertex Color")]
        public static void Open()
        {
            TextureToVertexColorWindow window = GetWindow<TextureToVertexColorWindow>();
            window.titleContent = new GUIContent("Texture To Vertex Color");
            window.minSize = new Vector2(420f, 300f);
        }

        public void CreateGUI()
        {
            rootVisualElement.Clear();
            ScrollView scroll = new ScrollView();
            scroll.horizontalScrollerVisibility = ScrollerVisibility.Hidden;
            scroll.style.flexGrow = 1f;
            rootVisualElement.Add(scroll);

            VisualElement content = new VisualElement();
            content.style.paddingLeft = 12f;
            content.style.paddingRight = 12f;
            content.style.paddingTop = 10f;
            content.style.paddingBottom = 10f;
            scroll.Add(content);

            Label title = new Label("Texture To Vertex Color");
            title.style.unityFontStyleAndWeight = FontStyle.Bold;
            title.style.fontSize = 16f;
            title.style.marginBottom = 8f;
            content.Add(title);

            content.Add(new HelpBox(
                "Samples each renderer's albedo texture at mesh vertices, writes the result to a copied mesh, and creates a new prefab. The source model and prefab are left untouched.",
                HelpBoxMessageType.Info));

            _sourceField = new ObjectField("Source prefab or model")
            {
                objectType = typeof(GameObject),
                allowSceneObjects = false,
            };
            _sourceField.SetValueWithoutNotify(_source);
            _sourceField.RegisterValueChangedCallback(change => SetSource(change.newValue as GameObject));
            content.Add(_sourceField);

            _rendererField = new DropdownField("Renderer");
            _rendererField.RegisterValueChangedCallback(change => SelectRenderer(change.newValue));
            content.Add(_rendererField);

            _materialField = new ObjectField("Vertex color material")
            {
                objectType = typeof(Material),
                allowSceneObjects = false,
            };
            if (_vertexColorMaterial == null)
                _vertexColorMaterial = AssetDatabase.LoadAssetAtPath<Material>(DefaultVertexMaterialPath);
            _materialField.SetValueWithoutNotify(_vertexColorMaterial);
            _materialField.RegisterValueChangedCallback(change => _vertexColorMaterial = change.newValue as Material);
            content.Add(_materialField);

            content.Add(new HelpBox(
                "Uses the mesh's current vertex density. Fine texture details need a sufficiently dense mesh. Meshes must be readable; texture pixels are copied through the GPU without changing texture importer settings.",
                HelpBoxMessageType.Warning));

            Button convert = new Button(Convert) { text = "Create Converted Prefab" };
            convert.style.height = 32f;
            convert.style.marginTop = 8f;
            content.Add(convert);

            _status = new HelpBox("Select a prefab or imported model asset.", HelpBoxMessageType.Info);
            _status.style.marginTop = 8f;
            content.Add(_status);

            if (_source == null && Selection.activeObject is GameObject selected)
                SetSource(selected);
            else
                SetSource(_source);
        }

        private void SetSource(GameObject source)
        {
            _source = source;
            _sourceField?.SetValueWithoutNotify(source);
            _selectedRenderer = null;
            _renderers.Clear();

            if (source != null)
            {
                Renderer[] found = source.GetComponentsInChildren<Renderer>(true);
                foreach (Renderer renderer in found)
                    if (renderer is MeshRenderer || renderer is SkinnedMeshRenderer)
                        _renderers.Add(renderer);
            }

            List<string> labels = BuildRendererLabels();
            if (_rendererField != null)
            {
                _rendererField.choices = labels;
                _rendererField.SetValueWithoutNotify(labels.Count > 0 ? labels[0] : string.Empty);
            }

            _selectedRenderer = _renderers.Count > 0 ? _renderers[0] : null;
            SetStatus(source == null
                ? "Select a prefab or imported model asset."
                : _renderers.Count == 0
                    ? "No MeshRenderer or SkinnedMeshRenderer was found under this asset."
                    : "Choose a renderer, then create a converted prefab.", HelpBoxMessageType.Info);
        }

        private List<string> BuildRendererLabels()
        {
            List<string> labels = new List<string>(_renderers.Count);
            foreach (Renderer renderer in _renderers)
                labels.Add(GetHierarchyPath(renderer.transform, _source.transform));
            return labels;
        }

        private void SelectRenderer(string label)
        {
            int index = BuildRendererLabels().IndexOf(label);
            _selectedRenderer = index >= 0 ? _renderers[index] : null;
        }

        private void Convert()
        {
            if (!TextureToVertexColorProcessor.TryCreatePrefab(
                    _source,
                    _selectedRenderer,
                    _vertexColorMaterial,
                    out string outputPath,
                    out string message))
            {
                SetStatus(message, HelpBoxMessageType.Error);
                return;
            }

            GameObject output = AssetDatabase.LoadAssetAtPath<GameObject>(outputPath);
            Selection.activeObject = output;
            EditorGUIUtility.PingObject(output);
            SetStatus(message + " " + outputPath, HelpBoxMessageType.Info);
        }

        private void SetStatus(string message, HelpBoxMessageType type)
        {
            if (_status == null)
                return;

            _status.text = message;
            _status.messageType = type;
        }

        private static string GetHierarchyPath(Transform target, Transform root)
        {
            List<string> names = new List<string>();
            for (Transform current = target; current != null && current != root; current = current.parent)
                names.Add(current.name);

            names.Reverse();
            return string.Join("/", names);
        }
    }
}
