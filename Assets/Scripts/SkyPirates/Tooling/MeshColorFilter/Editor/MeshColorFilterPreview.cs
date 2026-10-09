using System;
using UnityEditor;
using UnityEngine;
using UnityEngine.Rendering;

namespace DVG.SkyPirates.Tooling.MeshColorFilter.Editor
{
    internal enum MeshColorFilterPreviewMode
    {
        [InspectorName("Original")]
        Original,
        [InspectorName("Original + zones")]
        OriginalWithZones,
        [InspectorName("Zones only")]
        ZonesOnly,
        [InspectorName("New shading")]
        NewShading
    }

    internal sealed class MeshColorFilterPreview : IDisposable
    {
        private const float RotationSpeed = 0.5f;
        private const float MinimumZoom = 0.2f;
        private const float MaximumZoom = 8f;

        private PreviewRenderUtility _renderUtility;
        private GameObject _sourceObject;
        private GameObject _processedObject;
        private GameObject _overlayObject;
        private Mesh _processedMesh;
        private Mesh _overlayMesh;
        private Material[] _processedMaterials;
        private Material[] _zoneMaterials;
        private Material[] _overlayMaterials;
        private Renderer _sourceRenderer;
        private Renderer _processedRenderer;
        private Renderer _overlayRenderer;
        private readonly Action _requestRepaint;
        private float _baseSize;
        private float _yaw;
        private float _pitch;
        private float _zoom = 1f;
        private Vector2 _lastMousePosition;
        private bool _hasCameraView;
        private MeshColorFilterPreviewMode _mode;

        public MeshColorFilterPreview(Action requestRepaint)
        {
            _requestRepaint = requestRepaint;
        }

        public bool IsReady => _renderUtility != null && _sourceObject != null;

        public void SetMesh(Mesh sourceMesh, Material[] sourceMaterials, Mesh processedMesh, Material[] processedMaterials,
            int[] submeshRuleIndices, string[] ruleNames, MeshColorFilterPreviewMode mode)
        {
            bool preserveCameraView = _hasCameraView;
            Dispose();
            if (sourceMesh == null) throw new ArgumentNullException(nameof(sourceMesh));
            _processedMesh = processedMesh;
            _processedMaterials = processedMaterials ?? Array.Empty<Material>();
            _mode = mode;

            _renderUtility = new PreviewRenderUtility();
            _renderUtility.ambientColor = new Color(0.35f, 0.35f, 0.35f, 1f);
            _renderUtility.camera.clearFlags = CameraClearFlags.Color;
            _renderUtility.camera.backgroundColor = new Color(0.16f, 0.17f, 0.19f, 1f);
            _renderUtility.camera.orthographic = true;
            _renderUtility.camera.nearClipPlane = 0.01f;
            _renderUtility.camera.farClipPlane = 10000f;

            Bounds bounds = sourceMesh.bounds;
            _baseSize = Mathf.Max(bounds.extents.x, bounds.extents.y, bounds.extents.z, 0.25f);
            if (!preserveCameraView)
            {
                _zoom = 1f;
                Vector3 direction = new Vector3(1.3f, 0.85f, -1.7f).normalized;
                _yaw = Mathf.Atan2(direction.x, direction.z) * Mathf.Rad2Deg;
                _pitch = Mathf.Asin(direction.y) * Mathf.Rad2Deg;
            }
            _hasCameraView = true;
            UpdateCameraOrbit();
            _renderUtility.lights[0].intensity = 1.2f;
            _renderUtility.lights[1].intensity = 0.7f;

            _sourceObject = CreatePreviewObject("Original mesh", sourceMesh, sourceMaterials ?? Array.Empty<Material>(), -bounds.center);
            if (processedMesh != null && submeshRuleIndices != null)
            {
                Shader shader = Shader.Find("Universal Render Pipeline/Unlit");
                if (shader == null) shader = Shader.Find("Unlit/Color");
                if (shader == null) throw new InvalidOperationException("Could not find an unlit shader for the preview.");

                _zoneMaterials = CreateZoneMaterials(shader, submeshRuleIndices, ruleNames, false);
                _overlayMaterials = CreateZoneMaterials(shader, submeshRuleIndices, ruleNames, true);
                _processedObject = CreatePreviewObject("Processed mesh", processedMesh, _zoneMaterials, -bounds.center);
                _processedObject.GetComponent<Renderer>().sharedMaterials = _processedMaterials;

                _overlayMesh = CreateOverlayMesh(processedMesh);
                _overlayObject = CreatePreviewObject("Zone overlay", _overlayMesh, _overlayMaterials, -bounds.center);
            }

            SetMode(mode);
        }

        public void SetMode(MeshColorFilterPreviewMode mode)
        {
            _mode = mode;
            if (_sourceRenderer != null)
                _sourceRenderer.enabled = mode == MeshColorFilterPreviewMode.Original || mode == MeshColorFilterPreviewMode.OriginalWithZones;
            if (_processedRenderer != null)
            {
                _processedRenderer.enabled = mode == MeshColorFilterPreviewMode.ZonesOnly || mode == MeshColorFilterPreviewMode.NewShading;
                _processedRenderer.sharedMaterials = mode == MeshColorFilterPreviewMode.NewShading
                    ? _processedMaterials
                    : _zoneMaterials;
            }
            if (_overlayRenderer != null)
                _overlayRenderer.enabled = mode == MeshColorFilterPreviewMode.OriginalWithZones;
            _requestRepaint?.Invoke();
        }

        public void Draw(Rect rect)
        {
            if (!IsReady || rect.width <= 1f || rect.height <= 1f) return;

            Event currentEvent = Event.current;
            int controlId = GUIUtility.GetControlID(FocusType.Passive);
            switch (currentEvent.GetTypeForControl(controlId))
            {
                case EventType.MouseDown:
                    if (currentEvent.button == 0 && rect.Contains(currentEvent.mousePosition))
                    {
                        GUIUtility.hotControl = controlId;
                        _lastMousePosition = currentEvent.mousePosition;
                        currentEvent.Use();
                    }
                    break;
                case EventType.MouseDrag:
                    if (GUIUtility.hotControl == controlId)
                    {
                        Vector2 delta = currentEvent.mousePosition - _lastMousePosition;
                        _lastMousePosition = currentEvent.mousePosition;
                        _yaw += delta.x * RotationSpeed;
                        _pitch = Mathf.Clamp(_pitch - delta.y * RotationSpeed, -85f, 85f);
                        UpdateCameraOrbit();
                        currentEvent.Use();
                        _requestRepaint?.Invoke();
                    }
                    break;
                case EventType.MouseUp:
                    if (GUIUtility.hotControl == controlId)
                    {
                        GUIUtility.hotControl = 0;
                        currentEvent.Use();
                    }
                    break;
                case EventType.ScrollWheel:
                    if (rect.Contains(currentEvent.mousePosition))
                    {
                        _zoom = Mathf.Clamp(_zoom * Mathf.Exp(-currentEvent.delta.y * 0.08f), MinimumZoom, MaximumZoom);
                        UpdateCameraOrbit();
                        currentEvent.Use();
                        _requestRepaint?.Invoke();
                    }
                    break;
            }

            if (currentEvent.type == EventType.Repaint)
            {
                _renderUtility.BeginPreview(rect, GUIStyle.none);
                _renderUtility.camera.aspect = rect.width / rect.height;
                _renderUtility.camera.Render();
                Texture texture = _renderUtility.EndPreview();
                GUI.DrawTexture(rect, texture, ScaleMode.StretchToFill, false);
            }
        }

        private GameObject CreatePreviewObject(string objectName, Mesh mesh, Material[] materials, Vector3 position)
        {
            var previewObject = new GameObject(objectName) { hideFlags = HideFlags.HideAndDontSave };
            previewObject.transform.position = position;
            previewObject.AddComponent<MeshFilter>().sharedMesh = mesh;
            Renderer renderer = previewObject.AddComponent<MeshRenderer>();
            renderer.sharedMaterials = materials;
            renderer.shadowCastingMode = ShadowCastingMode.Off;
            renderer.receiveShadows = false;
            renderer.lightProbeUsage = LightProbeUsage.Off;
            renderer.reflectionProbeUsage = ReflectionProbeUsage.Off;
            _renderUtility.AddSingleGO(previewObject);

            if (objectName == "Original mesh") _sourceRenderer = renderer;
            else if (objectName == "Processed mesh") _processedRenderer = renderer;
            else _overlayRenderer = renderer;
            return previewObject;
        }

        private static Material[] CreateZoneMaterials(Shader shader, int[] ruleIndices, string[] ruleNames, bool overlay)
        {
            var materials = new Material[ruleIndices.Length];
            for (int i = 0; i < materials.Length; i++)
            {
                int ruleIndex = ruleIndices[i];
                string ruleName = ruleIndex >= 0 && ruleIndex < (ruleNames?.Length ?? 0) ? ruleNames[ruleIndex] : null;
                Color color = ruleIndex >= 0
                    ? RuleColor(ruleName)
                    : new Color(0.22f, 0.24f, 0.28f, 1f);
                if (overlay) color.a = ruleIndex >= 0 ? 0.58f : 0f;

                var material = new Material(shader)
                {
                    name = "Mesh Color Filter Preview " + i,
                    hideFlags = HideFlags.HideAndDontSave
                };
                if (material.HasProperty("_BaseColor")) material.SetColor("_BaseColor", color);
                if (material.HasProperty("_Color")) material.SetColor("_Color", color);
                if (overlay) ConfigureTransparent(material);
                materials[i] = material;
            }
            return materials;
        }

        private static void ConfigureTransparent(Material material)
        {
            if (material.HasProperty("_Surface")) material.SetFloat("_Surface", 1f);
            if (material.HasProperty("_Blend")) material.SetFloat("_Blend", 0f);
            if (material.HasProperty("_SrcBlend")) material.SetInt("_SrcBlend", (int)BlendMode.SrcAlpha);
            if (material.HasProperty("_DstBlend")) material.SetInt("_DstBlend", (int)BlendMode.OneMinusSrcAlpha);
            if (material.HasProperty("_ZWrite")) material.SetInt("_ZWrite", 0);
            material.SetOverrideTag("RenderType", "Transparent");
            material.EnableKeyword("_SURFACE_TYPE_TRANSPARENT");
            material.renderQueue = (int)RenderQueue.Transparent;
        }

        private static Mesh CreateOverlayMesh(Mesh source)
        {
            Mesh mesh = UnityEngine.Object.Instantiate(source);
            mesh.name = "Mesh Color Filter Zone Overlay";
            mesh.hideFlags = HideFlags.HideAndDontSave;
            Vector3[] vertices = mesh.vertices;
            Vector3[] normals = mesh.normals;
            if (normals.Length != vertices.Length)
            {
                mesh.RecalculateNormals();
                normals = mesh.normals;
            }
            float offset = Mathf.Max(mesh.bounds.extents.magnitude * 0.001f, 0.0001f);
            for (int i = 0; i < vertices.Length; i++)
                vertices[i] += normals[i].normalized * offset;
            mesh.vertices = vertices;
            mesh.RecalculateBounds();
            return mesh;
        }

        private void UpdateCameraOrbit()
        {
            float yawRadians = _yaw * Mathf.Deg2Rad;
            float pitchRadians = _pitch * Mathf.Deg2Rad;
            float pitchCosine = Mathf.Cos(pitchRadians);
            Vector3 direction = new Vector3(
                Mathf.Sin(yawRadians) * pitchCosine,
                Mathf.Sin(pitchRadians),
                Mathf.Cos(yawRadians) * pitchCosine);

            _renderUtility.camera.orthographicSize = _baseSize * 1.35f / _zoom;
            _renderUtility.camera.transform.position = direction * _baseSize * 4f;
            _renderUtility.camera.transform.LookAt(Vector3.zero, Vector3.up);
            _renderUtility.lights[0].transform.position = direction * _baseSize * 2f;
            _renderUtility.lights[0].transform.LookAt(Vector3.zero);
        }

        public void Dispose()
        {
            if (_renderUtility != null)
            {
                _renderUtility.Cleanup();
                _renderUtility = null;
            }
            DestroyPreviewObject(ref _sourceObject);
            DestroyPreviewObject(ref _processedObject);
            DestroyPreviewObject(ref _overlayObject);
            DestroyMaterialArray(ref _zoneMaterials);
            DestroyMaterialArray(ref _overlayMaterials);
            if (_overlayMesh != null)
            {
                UnityEngine.Object.DestroyImmediate(_overlayMesh);
                _overlayMesh = null;
            }
            if (_processedMesh != null)
            {
                UnityEngine.Object.DestroyImmediate(_processedMesh);
                _processedMesh = null;
            }
            _sourceRenderer = null;
            _processedRenderer = null;
            _overlayRenderer = null;
            _processedMaterials = null;
        }

        private static void DestroyPreviewObject(ref GameObject previewObject)
        {
            if (previewObject == null) return;
            UnityEngine.Object.DestroyImmediate(previewObject);
            previewObject = null;
        }

        private static void DestroyMaterialArray(ref Material[] materials)
        {
            if (materials == null) return;
            foreach (Material material in materials)
                if (material != null) UnityEngine.Object.DestroyImmediate(material);
            materials = null;
        }

        private static Color RuleColor(string zoneName)
        {
            uint hash = 2166136261;
            string normalizedName = (zoneName ?? string.Empty).Trim().ToUpperInvariant();
            unchecked
            {
                foreach (char character in normalizedName)
                {
                    hash ^= character;
                    hash *= 16777619;
                }
            }
            float hue = (hash & 0xFFFFu) / 65535f;
            float saturationVariation = ((hash >> 16) & 0xFFu) / 255f;
            float valueVariation = ((hash >> 24) & 0xFFu) / 255f;
            float saturation = 0.5001f + saturationVariation * 0.4999f;
            float value = 0.5001f + valueVariation * 0.4999f;
            return Color.HSVToRGB(hue, saturation, value);
        }
    }
}
