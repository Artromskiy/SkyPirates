using System;
using UnityEditor;
using UnityEngine;
using UnityEngine.Rendering;

namespace DVG.SkyPirates.Tooling.MeshColorFilter.Editor
{
    internal sealed class MeshColorFilterPreview : IDisposable
    {
        private const float RotationSpeed = 0.5f;
        private const float MinimumZoom = 0.2f;
        private const float MaximumZoom = 8f;

        private PreviewRenderUtility _renderUtility;
        private GameObject _previewObject;
        private Mesh _mesh;
        private Material[] _materials;
        private readonly Action _requestRepaint;
        private float _baseSize;
        private float _yaw;
        private float _pitch;
        private float _zoom = 1f;
        private Vector2 _lastMousePosition;
        private bool _hasCameraView;

        public MeshColorFilterPreview(Action requestRepaint)
        {
            _requestRepaint = requestRepaint;
        }

        public bool IsReady => _renderUtility != null && _mesh != null;

        public void SetMesh(Mesh mesh, int[] submeshRuleIndices, int ruleCount)
        {
            bool preserveCameraView = _hasCameraView;
            Dispose();
            _mesh = mesh;
            if (_mesh == null) throw new ArgumentNullException(nameof(mesh));

            Shader shader = Shader.Find("Universal Render Pipeline/Unlit");
            if (shader == null) shader = Shader.Find("Unlit/Color");
            if (shader == null) throw new InvalidOperationException("Could not find an unlit shader for the preview.");

            _materials = new Material[submeshRuleIndices.Length];
            for (int submesh = 0; submesh < _materials.Length; submesh++)
            {
                int ruleIndex = submeshRuleIndices[submesh];
                Color color = ruleIndex >= 0 ? RuleColor(ruleIndex, ruleCount) : new Color(0.22f, 0.24f, 0.28f, 1f);
                var material = new Material(shader)
                {
                    name = "Mesh Color Filter Preview " + submesh,
                    hideFlags = HideFlags.HideAndDontSave
                };
                if (material.HasProperty("_BaseColor")) material.SetColor("_BaseColor", color);
                if (material.HasProperty("_Color")) material.SetColor("_Color", color);
                _materials[submesh] = material;
            }

            _previewObject = new GameObject("Mesh Color Filter Preview") { hideFlags = HideFlags.HideAndDontSave };
            var filter = _previewObject.AddComponent<MeshFilter>();
            filter.sharedMesh = _mesh;
            var renderer = _previewObject.AddComponent<MeshRenderer>();
            renderer.sharedMaterials = _materials;
            renderer.shadowCastingMode = ShadowCastingMode.Off;
            renderer.receiveShadows = false;
            renderer.lightProbeUsage = LightProbeUsage.Off;
            renderer.reflectionProbeUsage = ReflectionProbeUsage.Off;

            Bounds bounds = _mesh.bounds;
            _previewObject.transform.position = -bounds.center;
            _renderUtility = new PreviewRenderUtility();
            _renderUtility.ambientColor = new Color(0.35f, 0.35f, 0.35f, 1f);
            _renderUtility.AddSingleGO(_previewObject);
            _renderUtility.camera.clearFlags = CameraClearFlags.Color;
            _renderUtility.camera.backgroundColor = new Color(0.16f, 0.17f, 0.19f, 1f);
            _renderUtility.camera.orthographic = true;
            _renderUtility.camera.nearClipPlane = 0.01f;
            _renderUtility.camera.farClipPlane = 10000f;
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
            if (_previewObject != null)
            {
                UnityEngine.Object.DestroyImmediate(_previewObject);
                _previewObject = null;
            }
            if (_materials != null)
            {
                foreach (Material material in _materials)
                    if (material != null) UnityEngine.Object.DestroyImmediate(material);
                _materials = null;
            }
            if (_mesh != null)
            {
                UnityEngine.Object.DestroyImmediate(_mesh);
                _mesh = null;
            }
        }

        private static Color RuleColor(int index, int count)
        {
            float hue = count <= 1 ? 0f : Mathf.Repeat(index * 0.61803398875f, 1f);
            return Color.HSVToRGB(hue, 0.78f, 1f);
        }
    }
}
