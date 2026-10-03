#if UNITY_EDITOR
using System;
using System.Collections.Generic;
using UnityEditor;
using UnityEngine;
using UnityEngine.Rendering;

namespace DVG.SkyPirates.Tooling.IslandGeneration.Editor
{
    internal sealed class IslandGeneration3DPreview : IDisposable
    {
        private const float HexRadius = 1f;
        private const float RowSpacing = 1.5f;
        private const float SqrtThree = 1.7320508f;
        private const float CameraFitMargin = 1.25f;
        private static readonly int[] NeighborQ = { 1, 0, -1, -1, 0, 1 };
        private static readonly int[] NeighborR = { 0, 1, 1, 0, -1, -1 };

        private HexIslandMap _map;
        private Gradient _heightGradient;
        private float _verticalScale = 1f;
        private PreviewRenderUtility _renderUtility;
        private Mesh _terrainMesh;
        private Mesh _waterMesh;
        private Material[] _terrainMaterials;
        private Material _waterMaterial;
        private bool[] _hasTerrainSubmesh;
        private float _yaw = -35f;
        private float _pitch = 32f;
        private float _zoom = 1f;
        private Vector3 _pan;
        private int _hotControl;
        private int _dragButton;

        public void SetMap(HexIslandMap map, Gradient heightGradient, float verticalScale, bool forceRebuild = false)
        {
            if (_map == map && _heightGradient == heightGradient && Mathf.Approximately(_verticalScale, verticalScale) && !forceRebuild)
                return;

            _map = map;
            _heightGradient = heightGradient;
            _verticalScale = verticalScale;
            ReleaseMapResources();
            if (_map != null)
            {
                try
                {
                    BuildMapResources(_map);
                }
                catch (Exception exception)
                {
                    ReleaseMapResources();
                    UnityEngine.Debug.LogException(exception);
                }
            }
        }

        public void Draw()
        {
            var rect = GUILayoutUtility.GetRect(180f, 180f, GUILayout.ExpandWidth(true), GUILayout.ExpandHeight(true));
            HandleInput(rect);

            if (_map == null)
            {
                GUI.Box(rect, "Generate an island to see its 3D height map.");
                return;
            }

            if (_renderUtility == null)
                CreateRenderUtility();

            EditorGUIUtility.AddCursorRect(rect, MouseCursor.Pan);
            if (Event.current.type != EventType.Repaint)
                return;

            ConfigureCamera(rect);
            _renderUtility.BeginPreview(rect, GUIStyle.none);
            Texture previewTexture;
            try
            {
                if (_waterMesh != null)
                    _renderUtility.DrawMesh(_waterMesh, Matrix4x4.identity, _waterMaterial, 0);
                if (_terrainMesh != null)
                {
                    for (var i = 0; i < _hasTerrainSubmesh.Length; i++)
                    {
                        if (_hasTerrainSubmesh[i])
                        {
                            var materialIndex = i < _terrainMaterials.Length
                                ? i
                                : i - _terrainMaterials.Length;
                            _renderUtility.DrawMesh(_terrainMesh, Matrix4x4.identity, _terrainMaterials[materialIndex], i);
                        }
                    }
                }
                _renderUtility.Render(true);
            }
            finally
            {
                previewTexture = _renderUtility.EndPreview();
            }

            GUI.DrawTexture(rect, previewTexture, ScaleMode.StretchToFill, false);

            var hintRect = new Rect(rect.x + 8f, rect.yMax - 20f, rect.width - 16f, 16f);
            GUI.Label(hintRect, "Drag to rotate  ·  Right/middle drag to pan  ·  Scroll to zoom", EditorStyles.whiteMiniLabel);
        }

        public void Dispose()
        {
            if (_hotControl != 0 && GUIUtility.hotControl == _hotControl)
                GUIUtility.hotControl = 0;
            _hotControl = 0;
            ReleaseMapResources();
            _renderUtility?.Cleanup();
            _renderUtility = null;
        }

        private void HandleInput(Rect rect)
        {
            var currentEvent = Event.current;
            var controlId = GUIUtility.GetControlID(FocusType.Passive);

            if (currentEvent.type == EventType.MouseDown && currentEvent.button <= 2 && rect.Contains(currentEvent.mousePosition))
            {
                _hotControl = controlId;
                _dragButton = currentEvent.button;
                GUIUtility.hotControl = controlId;
                currentEvent.Use();
            }
            else if (currentEvent.type == EventType.MouseDrag && GUIUtility.hotControl == _hotControl && _hotControl != 0)
            {
                if (_dragButton == 0)
                {
                    _yaw += currentEvent.delta.x * 0.7f;
                    _pitch = Mathf.Clamp(_pitch + currentEvent.delta.y * 0.5f, 8f, 78f);
                }
                else if (_renderUtility != null)
                {
                    var camera = _renderUtility.camera;
                    var scaledHeight = _map.MaximumHeight * _verticalScale;
                    var radius = Mathf.Sqrt(_map.Columns * _map.Columns * 3f + _map.Rows * _map.Rows * 2.25f + scaledHeight * scaledHeight) * 0.5f;
                    var distance = radius / Mathf.Tan(_renderUtility.cameraFieldOfView * 0.5f * Mathf.Deg2Rad) * CameraFitMargin * _zoom;
                    var worldUnitsPerPixel = 2f * distance * Mathf.Tan(_renderUtility.cameraFieldOfView * 0.5f * Mathf.Deg2Rad) / Mathf.Max(1f, rect.height);
                    _pan += (-camera.transform.right * currentEvent.delta.x + camera.transform.up * currentEvent.delta.y) * worldUnitsPerPixel;
                }
                GUI.changed = true;
                currentEvent.Use();
            }
            else if (currentEvent.type == EventType.MouseUp && currentEvent.button == _dragButton && GUIUtility.hotControl == _hotControl)
            {
                GUIUtility.hotControl = 0;
                _hotControl = 0;
                currentEvent.Use();
            }
            else if (currentEvent.type == EventType.ScrollWheel && rect.Contains(currentEvent.mousePosition))
            {
                _zoom *= Mathf.Exp(currentEvent.delta.y * 0.04f);
                GUI.changed = true;
                currentEvent.Use();
            }
        }

        private void CreateRenderUtility()
        {
            _renderUtility = new PreviewRenderUtility();
            _renderUtility.cameraFieldOfView = 28f;

            var lights = _renderUtility.lights;
            lights[0].intensity = 1.2f;
            lights[0].transform.rotation = Quaternion.Euler(45f, -35f, 0f);
            lights[1].intensity = 1f;
            lights[1].transform.rotation = Quaternion.Euler(25f, 145f, 0f);
        }

        private void ConfigureCamera(Rect rect)
        {
            var camera = _renderUtility.camera;
            var width = Mathf.Max(1f, _map.Columns * SqrtThree + 1f);
            var depth = Mathf.Max(1f, (_map.Rows - 1) * RowSpacing + 2f);
            var height = Mathf.Max(1f, _map.MaximumHeight * _verticalScale);
            var target = new Vector3(0f, height * 0.34f, 0f) + _pan;
            var radius = Mathf.Sqrt(width * width + depth * depth + height * height) * 0.5f;
            var distance = radius / Mathf.Tan(_renderUtility.cameraFieldOfView * 0.5f * Mathf.Deg2Rad) * CameraFitMargin * _zoom;
            var pitchRadians = _pitch * Mathf.Deg2Rad;
            var yawRadians = _yaw * Mathf.Deg2Rad;
            var direction = new Vector3(
                Mathf.Cos(pitchRadians) * Mathf.Sin(yawRadians),
                Mathf.Sin(pitchRadians),
                Mathf.Cos(pitchRadians) * Mathf.Cos(yawRadians));

            camera.transform.position = target + direction * distance;
            camera.transform.LookAt(target);
            camera.nearClipPlane = Mathf.Max(0.01f, distance * 0.001f);
            camera.farClipPlane = distance + radius * 4f;
            camera.aspect = Mathf.Max(0.1f, rect.width / Mathf.Max(1f, rect.height));
        }

        private void BuildMapResources(HexIslandMap map)
        {
            var shader = Shader.Find("Universal Render Pipeline/Lit");
            if (shader == null)
                shader = Shader.Find("Standard");
            if (shader == null)
                return;

            _waterMaterial = CreateMaterial(shader, new Color(0.12f, 0.34f, 0.48f, 1f));
            _terrainMaterials = new Material[Mathf.Max(1, map.HeightLevels - 1)];
            for (var index = 0; index < _terrainMaterials.Length; index++)
            {
                var normalizedHeight = (index + 1f) / _terrainMaterials.Length;
                _terrainMaterials[index] = CreateMaterial(shader, GetHeightColor(_heightGradient, normalizedHeight));
            }

            _waterMesh = BuildWaterMesh(map);
            BuildTerrainMesh(map);
        }

        private void BuildTerrainMesh(HexIslandMap map)
        {
            var cells = map.Cells ?? Array.Empty<HexIslandCell>();
            var indicesByCoordinate = new Dictionary<long, int>(cells.Length);
            for (var i = 0; i < cells.Length; i++)
                indicesByCoordinate[CoordinateKey(cells[i].Q, cells[i].R)] = i;

            var vertices = new List<Vector3>();
            var trianglesByLevel = new List<int>[_terrainMaterials.Length * 2];
            for (var i = 0; i < trianglesByLevel.Length; i++)
                trianglesByLevel[i] = new List<int>();

            var centerX = (map.Columns - 1) * SqrtThree * 0.5f + SqrtThree * 0.25f;
            var centerZ = (map.Rows - 1) * RowSpacing * 0.5f;
            for (var cellIndex = 0; cellIndex < cells.Length; cellIndex++)
            {
                var cell = cells[cellIndex];
                if (!cell.IsLand || cell.Height <= 0f)
                    continue;

                var row = cell.R + map.Rows / 2;
                var offset = (cell.R - (cell.R & 1)) / 2;
                var column = cell.Q + offset;
                var x = (column + (row & 1) * 0.5f) * SqrtThree - centerX;
                var z = row * RowSpacing - centerZ;
                var cellHeight = cell.Height * _verticalScale;
                var levelSubmesh = Mathf.Clamp(cell.HeightLevel - 1, 0, _terrainMaterials.Length - 1);
                var cellVertices = vertices.Count;
                vertices.Add(new Vector3(x, cellHeight, z));

                for (var corner = 0; corner < 6; corner++)
                {
                    var angle = (corner * 60f + 30f) * Mathf.Deg2Rad;
                    vertices.Add(new Vector3(
                        x + Mathf.Cos(angle) * HexRadius,
                        cellHeight,
                        z + Mathf.Sin(angle) * HexRadius));
                }

                var topTriangles = trianglesByLevel[levelSubmesh];
                for (var corner = 0; corner < 6; corner++)
                {
                    var nextCorner = (corner + 1) % 6;
                    topTriangles.Add(cellVertices);
                    topTriangles.Add(cellVertices + 1 + nextCorner);
                    topTriangles.Add(cellVertices + 1 + corner);
                }

                for (var direction = 0; direction < 6; direction++)
                {
                    var hasNeighbor = indicesByCoordinate.TryGetValue(
                        CoordinateKey(cell.Q + NeighborQ[direction], cell.R + NeighborR[direction]),
                        out var neighborIndex);
                    var neighbor = hasNeighbor ? cells[neighborIndex] : default;
                    var lowerHeight = neighbor.IsLand ? neighbor.Height * _verticalScale : 0f;
                    if (lowerHeight >= cellHeight - 0.001f)
                        continue;

                    var firstCorner = (direction + 5) % 6;
                    var secondCorner = direction;
                    AddWall(
                        vertices,
                        trianglesByLevel[_terrainMaterials.Length + levelSubmesh],
                        x,
                        z,
                        firstCorner,
                        secondCorner,
                        cellHeight,
                        lowerHeight);
                }
            }

            if (vertices.Count == 0)
                return;

            _terrainMesh = new Mesh
            {
                name = "Island 3D Preview Terrain",
                indexFormat = vertices.Count > 65535 ? IndexFormat.UInt32 : IndexFormat.UInt16,
                hideFlags = HideFlags.HideAndDontSave,
                subMeshCount = trianglesByLevel.Length
            };
            _terrainMesh.SetVertices(vertices);
            _hasTerrainSubmesh = new bool[trianglesByLevel.Length];
            for (var submesh = 0; submesh < trianglesByLevel.Length; submesh++)
            {
                var indices = trianglesByLevel[submesh];
                _hasTerrainSubmesh[submesh] = indices.Count > 0;
                _terrainMesh.SetTriangles(indices, submesh, false);
            }
            _terrainMesh.RecalculateNormals();
            _terrainMesh.RecalculateBounds();
        }

        private static Mesh BuildWaterMesh(HexIslandMap map)
        {
            var halfWidth = map.Columns * SqrtThree * 0.5f + 0.5f;
            var halfDepth = (map.Rows - 1) * RowSpacing * 0.5f + 1f;
            var mesh = new Mesh
            {
                name = "Island 3D Preview Water",
                hideFlags = HideFlags.HideAndDontSave
            };
            mesh.vertices = new[]
            {
                new Vector3(-halfWidth, -0.05f, -halfDepth),
                new Vector3(halfWidth, -0.05f, -halfDepth),
                new Vector3(halfWidth, -0.05f, halfDepth),
                new Vector3(-halfWidth, -0.05f, halfDepth)
            };
            mesh.triangles = new[] { 0, 2, 1, 0, 3, 2 };
            mesh.RecalculateNormals();
            return mesh;
        }

        private static void AddWall(
            List<Vector3> vertices,
            List<int> indices,
            float centerX,
            float centerZ,
            int firstCorner,
            int secondCorner,
            float topHeight,
            float bottomHeight)
        {
            var firstAngle = (firstCorner * 60f + 30f) * Mathf.Deg2Rad;
            var secondAngle = (secondCorner * 60f + 30f) * Mathf.Deg2Rad;
            var firstX = centerX + Mathf.Cos(firstAngle) * HexRadius;
            var firstZ = centerZ + Mathf.Sin(firstAngle) * HexRadius;
            var secondX = centerX + Mathf.Cos(secondAngle) * HexRadius;
            var secondZ = centerZ + Mathf.Sin(secondAngle) * HexRadius;
            var firstIndex = vertices.Count;
            vertices.Add(new Vector3(firstX, topHeight, firstZ));
            vertices.Add(new Vector3(firstX, bottomHeight, firstZ));
            vertices.Add(new Vector3(secondX, bottomHeight, secondZ));
            vertices.Add(new Vector3(secondX, topHeight, secondZ));
            indices.Add(firstIndex);
            indices.Add(firstIndex + 3);
            indices.Add(firstIndex + 2);
            indices.Add(firstIndex);
            indices.Add(firstIndex + 2);
            indices.Add(firstIndex + 1);
        }

        private Material CreateMaterial(Shader shader, Color color)
        {
            var material = new Material(shader)
            {
                hideFlags = HideFlags.HideAndDontSave
            };
            if (material.HasProperty("_BaseColor"))
                material.SetColor("_BaseColor", color);
            if (material.HasProperty("_Color"))
                material.SetColor("_Color", color);
            if (material.HasProperty("_SpecularHighlights"))
            {
                material.SetFloat("_SpecularHighlights", 0f);
                material.EnableKeyword("_SPECULARHIGHLIGHTS_OFF");
            }
            if (material.HasProperty("_EnvironmentReflections"))
            {
                material.SetFloat("_EnvironmentReflections", 0f);
                material.EnableKeyword("_ENVIRONMENTREFLECTIONS_OFF");
            }
            if (material.HasProperty("_GlossyReflections"))
            {
                material.SetFloat("_GlossyReflections", 0f);
                material.EnableKeyword("_GLOSSYREFLECTIONS_OFF");
            }
            if (material.HasProperty("_Cull"))
                material.SetFloat("_Cull", (float)CullMode.Off);
            return material;
        }

        private static Color GetHeightColor(Gradient gradient, float normalizedHeight)
        {
            return gradient != null ? gradient.Evaluate(normalizedHeight) : new Color(0.35f, 0.55f, 0.3f);
        }

        private static long CoordinateKey(int q, int r)
        {
            return ((long)q << 32) | (uint)r;
        }

        private void ReleaseMapResources()
        {
            DestroyResource(_terrainMesh);
            DestroyResource(_waterMesh);
            DestroyResource(_waterMaterial);
            if (_terrainMaterials != null)
            {
                for (var i = 0; i < _terrainMaterials.Length; i++)
                    DestroyResource(_terrainMaterials[i]);
            }

            _terrainMesh = null;
            _waterMesh = null;
            _terrainMaterials = null;
            _waterMaterial = null;
            _hasTerrainSubmesh = null;
        }

        private static void DestroyResource(UnityEngine.Object resource)
        {
            if (resource != null)
                UnityEngine.Object.DestroyImmediate(resource);
        }
    }
}
#endif
