using Delta;
using Delta.ECS;
using Common.Rendering;
using Common.Rendering.HexTerrainStencilShadow;
using Common.Rendering.SphereOverlay;
using SlyPirates.Shared.Components.Config;
using SlyPirates.Shared.Components.Runtime;
using SlyPirates.Shared.Core;
using SlyPirates.Shared.Ids;
using SlyPirates.Shared.IServices.TickableExecutors;
using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.SceneManagement;

namespace SlyPirates.Client.Systems
{
    /// <summary>
    /// Keeps HexTile entities as the source of truth and renders their exposed geometry in chunk meshes.
    /// </summary>
    public sealed class HexMapChunkRenderSystem : IPostTickable
    {
        private const string TilePrefabPath = "Prefabs/Visual/Tile/Land";
        private const int ChunkSize = 16;
        private const int ChunkHeight = 4;
        private const int ChunksPerTick = 16;
        private const float TileVerticalScale = 0.51f;
        private const float SideShade = 0.78f;

        private static readonly int[] NeighborQ = { 0, 1, 1, 0, -1, -1 };
        private static readonly int[] NeighborR = { 1, 0, -1, -1, 0, 1 };

        private readonly World _world;
        private readonly Scene _scene;
        private readonly GameObject _root;
        private readonly Mesh _sourceMesh;
        private readonly Material _material;
        private readonly Vector3[] _sourceVertices;
        private readonly float _sourceMinY;
        private readonly float _sourceHeight;
        private readonly Mesh _shadowTemplate;
        private readonly Vector3[] _sourceNormals;
        private readonly Vector2[] _sourceUvs;
        private readonly FaceTriangle[] _sourceTriangles;
        private readonly ComponentChangeOperation<HexTile> _tileChanges;
        private readonly ComponentChangeOperation<TileId> _tileIdChanges;
        private readonly ComponentChangeOperation<PendingDestroy> _lifetimeChanges;
        private readonly List<ChunkKey> _dirtyQueue = new();
        private readonly HashSet<ChunkKey> _dirty = new();
        private readonly Dictionary<ChunkKey, ChunkData> _chunks = new();
        private readonly Dictionary<Entity, ChunkKey> _chunkByEntity = new();
        private readonly HashSet<TileCoordinate> _shadowRunsProcessed = new();
        private readonly Dictionary<TileCoordinate, int> _occupancy = new();
        private readonly Dictionary<AxialCoordinate, int> _lowestElevation = new();
        private readonly Dictionary<AxialCoordinate, int> _highestElevation = new();
        private readonly List<Vector3> _vertices = new();
        private readonly List<Vector3> _normals = new();
        private readonly List<Vector2> _uvs = new();
        private readonly List<Color> _colors = new();
        private readonly List<int> _indices = new();
        private readonly List<Matrix4x4> _shadowInstances = new();
        private readonly int[] _topVertexIndices;
        private readonly int[] _sideVertexIndices;
        private readonly Vector3[] _neighborDirections = new Vector3[6];


        public HexMapChunkRenderSystem(World world)
        {
            _world = world;
            _scene = SceneManager.GetActiveScene();

            GameObject tilePrefab = Resources.Load<GameObject>(TilePrefabPath);
            MeshFilter tileFilter = tilePrefab.GetComponent<MeshFilter>();
            MeshRenderer tileRenderer = tilePrefab.GetComponent<MeshRenderer>();
            _sourceMesh = tileFilter.sharedMesh;
            _material = tileRenderer.sharedMaterial;
            if (!_sourceMesh.isReadable)
            {
                throw new InvalidOperationException($"Tile mesh '{_sourceMesh.name}' must be readable to build chunk geometry.");
            }

            _sourceVertices = _sourceMesh.vertices;
            Bounds sourceBounds = _sourceMesh.bounds;
            _sourceMinY = sourceBounds.min.y;
            _sourceHeight = sourceBounds.size.y;
            Vector3[] importedNormals = _sourceMesh.normals;
            _sourceNormals = new Vector3[importedNormals.Length];
            for (int i = 0; i < importedNormals.Length; i++)
            {
                Vector3 normal = importedNormals[i];
                normal.y /= TileVerticalScale;
                normal.Normalize();
                _sourceNormals[i] = normal;
            }

            _sourceUvs = _sourceMesh.uv;
            BuildNeighborDirections();
            _sourceTriangles = BuildFaceTriangles(_sourceMesh);
            _shadowTemplate = HexTerrainShadowMesh.Create(_sourceMesh);
            _topVertexIndices = new int[_sourceVertices.Length];
            _sideVertexIndices = new int[_sourceVertices.Length];

            _root = new GameObject("Hex Map Chunks");
            _root.AddComponent<SphereOverlayReceiverView>();

            _tileChanges = world.ForEachChange<HexTile>(world.Components.GetPrimary<HexTile>(), OnHexTileChanged);
            _tileIdChanges = world.ForEachChange<TileId>(world.Components.GetPrimary<TileId>(), OnTileIdChanged);
            _lifetimeChanges = world.ForEachChange<PendingDestroy>(world.Components.GetPrimary<PendingDestroy>(), OnLifetimeChanged);

            SceneManager.sceneUnloaded += OnSceneUnloaded;
        }

        public void Tick(int tick)
        {
            _tileChanges.Invoke();
            _tileIdChanges.Invoke();
            _lifetimeChanges.Invoke();

            int count = Math.Min(ChunksPerTick, _dirtyQueue.Count);
            for (int i = 0; i < count; i++)
            {
                ChunkKey key = _dirtyQueue[i];
                _dirty.Remove(key);
                RebuildChunk(key);
            }

            if (count != 0)
            {
                _dirtyQueue.RemoveRange(0, count);
            }
        }

        private void OnHexTileChanged(Entity entity, ChangeKind change, ref HexTile tile)
        {
            if (change == ChangeKind.Removed || _world.Has<PendingDestroy>(entity)
                || !_world.TryGet(entity, out TileId tileId) || tileId.IsNone)
            {
                RemoveTile(entity);
                return;
            }

            UpsertTile(entity, tile);
        }

        private void OnTileIdChanged(Entity entity, ChangeKind change, ref TileId tileId)
        {
            if (change != ChangeKind.Removed && !tileId.IsNone && _chunkByEntity.ContainsKey(entity))
            {
                return;
            }

            if (change == ChangeKind.Removed || tileId.IsNone || _world.Has<PendingDestroy>(entity)
                || !_world.TryGet(entity, out HexTile tile))
            {
                RemoveTile(entity);
                return;
            }

            AddTile(entity, tile);
        }

        private void OnLifetimeChanged(Entity entity, ChangeKind change, ref PendingDestroy pending)
        {
            if (change == ChangeKind.Removed)
            {
                if (_world.TryGet(entity, out HexTile tile) && _world.TryGet(entity, out TileId tileId) && !tileId.IsNone)
                {
                    UpsertTile(entity, tile);
                }
            }
            else
            {
                RemoveTile(entity);
            }
        }

        private void UpsertTile(Entity entity, in HexTile tile)
        {
            RemoveTile(entity);
            AddTile(entity, tile);
        }

        private void AddTile(Entity entity, in HexTile tile)
        {
            TileCoordinate coordinate = new(tile.Coordinates.x, tile.Elevation, tile.Coordinates.y);
            ChunkKey key = ToChunk(coordinate);
            if (!_chunks.TryGetValue(key, out ChunkData chunk))
            {
                chunk = new ChunkData();
                _chunks.Add(key, chunk);
            }

            chunk.Tiles.Add(entity, tile);
            _chunkByEntity.Add(entity, key);
            AddOccupancy(coordinate);
            MarkAffected(coordinate);
        }

        private void RemoveTile(Entity entity)
        {
            if (!_chunkByEntity.TryGetValue(entity, out ChunkKey key))
            {
                return;
            }

            ChunkData chunk = _chunks[key];
            HexTile tile = chunk.Tiles[entity];
            chunk.Tiles.Remove(entity);
            _chunkByEntity.Remove(entity);
            TileCoordinate coordinate = new(tile.Coordinates.x, tile.Elevation, tile.Coordinates.y);
            RemoveOccupancy(coordinate);
            MarkAffected(coordinate);
        }

        private void AddOccupancy(TileCoordinate coordinate)
        {
            _occupancy.TryGetValue(coordinate, out int count);
            _occupancy[coordinate] = count + 1;

            AxialCoordinate column = new(coordinate.Q, coordinate.R);
            if (!_highestElevation.TryGetValue(column, out int highest))
            {
                _lowestElevation[column] = coordinate.Elevation;
                _highestElevation[column] = coordinate.Elevation;
                return;
            }

            if (coordinate.Elevation < _lowestElevation[column])
            {
                _lowestElevation[column] = coordinate.Elevation;
            }

            if (coordinate.Elevation > highest)
            {
                _highestElevation[column] = coordinate.Elevation;
            }
        }

        private void RemoveOccupancy(TileCoordinate coordinate)
        {
            int count = _occupancy[coordinate];
            if (count == 1)
            {
                _occupancy.Remove(coordinate);
                AxialCoordinate column = new(coordinate.Q, coordinate.R);
                int lowest = _lowestElevation[column];
                int highest = _highestElevation[column];
                if (lowest == coordinate.Elevation && highest == coordinate.Elevation)
                {
                    _lowestElevation.Remove(column);
                    _highestElevation.Remove(column);
                }
                else if (lowest == coordinate.Elevation)
                {
                    for (int elevation = lowest + 1; elevation <= highest; elevation++)
                    {
                        if (_occupancy.ContainsKey(new TileCoordinate(coordinate.Q, elevation, coordinate.R)))
                        {
                            _lowestElevation[column] = elevation;
                            break;
                        }
                    }
                }
                else if (highest == coordinate.Elevation)
                {
                    for (int elevation = highest - 1; elevation >= lowest; elevation--)
                    {
                        if (_occupancy.ContainsKey(new TileCoordinate(coordinate.Q, elevation, coordinate.R)))
                        {
                            _highestElevation[column] = elevation;
                            break;
                        }
                    }
                }
            }
            else
            {
                _occupancy[coordinate] = count - 1;
            }
        }

        private void MarkAffected(TileCoordinate coordinate)
        {
            MarkDirty(ToChunk(coordinate));
            MarkDirty(ToChunk(new TileCoordinate(coordinate.Q, coordinate.Elevation - 1, coordinate.R)));
            MarkDirty(ToChunk(new TileCoordinate(coordinate.Q, coordinate.Elevation + 1, coordinate.R)));
            MarkColumnChunks(coordinate.Q, coordinate.R);
            for (int i = 0; i < NeighborQ.Length; i++)
            {
                int neighborQ = coordinate.Q + NeighborQ[i];
                int neighborR = coordinate.R + NeighborR[i];
                MarkDirty(ToChunk(new TileCoordinate(neighborQ, coordinate.Elevation, neighborR)));


            }
        }

        private void MarkColumnChunks(int q, int r)
        {
            var column = new AxialCoordinate(q, r);
            if (!_lowestElevation.TryGetValue(column, out int lowest) ||
                !_highestElevation.TryGetValue(column, out int highest))
            {
                return;
            }

            int chunkQ = FloorDiv(q, ChunkSize);
            int chunkR = FloorDiv(r, ChunkSize);
            int firstElevationChunk = FloorDiv(lowest, ChunkHeight);
            int lastElevationChunk = FloorDiv(highest, ChunkHeight);
            for (int elevationChunk = firstElevationChunk; elevationChunk <= lastElevationChunk; elevationChunk++)
            {
                MarkDirty(new ChunkKey(chunkQ, chunkR, elevationChunk));
            }
        }

        private void MarkDirty(ChunkKey key)
        {
            if (_dirty.Add(key))
            {
                _dirtyQueue.Add(key);
            }
        }

        private void RebuildChunk(ChunkKey key)
        {
            if (!_chunks.TryGetValue(key, out ChunkData chunk) || chunk.Tiles.Count == 0)
            {
                if (chunk != null)
                {
                    ReleaseChunk(chunk);
                    _chunks.Remove(key);
                }

                return;
            }

            EnsureChunkObject(key, chunk);
            BuildMesh(key, chunk);
        }

        private void EnsureChunkObject(ChunkKey key, ChunkData chunk)
        {
            if (chunk.GameObject != null)
            {
                return;
            }

            chunk.GameObject = new GameObject($"Hex Chunk {key.Q},{key.R},{key.Elevation}");
            chunk.Mesh = new Mesh
            {
                name = chunk.GameObject.name
            };

            MeshFilter filter = chunk.GameObject.AddComponent<MeshFilter>();
            filter.sharedMesh = chunk.Mesh;
            MeshRenderer renderer = chunk.GameObject.AddComponent<MeshRenderer>();
            renderer.sharedMaterial = _material;
            renderer.shadowCastingMode = ShadowCastingMode.Off;
            renderer.receiveShadows = true;
            renderer.lightProbeUsage = LightProbeUsage.Off;
            renderer.reflectionProbeUsage = ReflectionProbeUsage.Off;
            renderer.motionVectorGenerationMode = MotionVectorGenerationMode.ForceNoMotion;
            chunk.GameObject.transform.SetParent(_root.transform, false);
            chunk.GameObject.transform.position = ChunkOrigin(key);
        }

        private void BuildMesh(ChunkKey key, ChunkData chunk)
        {
            _vertices.Clear();
            _normals.Clear();
            _uvs.Clear();
            _colors.Clear();
            _indices.Clear();
            _shadowInstances.Clear();
            _shadowRunsProcessed.Clear();
            fix3 origin = Hex.AxialToWorld(new int3(
                key.Q * ChunkSize, key.Elevation * ChunkHeight, key.R * ChunkSize));
            float originX = (float)origin.x;
            float originY = (float)origin.y;
            float originZ = (float)origin.z;
            foreach (HexTile tile in chunk.Tiles.Values)
            {
                TileCoordinate coordinate = new(tile.Coordinates.x, tile.Elevation, tile.Coordinates.y);
                Color tileColor = ToColor(tile.Color);
                fix2 worldXZ = Hex.AxialToWorld(new int2(coordinate.Q, coordinate.R));
                Vector3 tileOrigin = new(
                    (float)worldXZ.x - originX,
                    (float)Hex.AxialToWorldY(coordinate.Elevation) - originY,
                    (float)worldXZ.y - originZ);
                ResetVertexIndices(_topVertexIndices);
                ResetVertexIndices(_sideVertexIndices);
                if (!_occupancy.ContainsKey(new TileCoordinate(coordinate.Q, coordinate.Elevation + 1, coordinate.R)) &&
                    _shadowRunsProcessed.Add(coordinate))
                {
                    int lowestElevation = coordinate.Elevation;
                    while (_occupancy.ContainsKey(new TileCoordinate(coordinate.Q, lowestElevation - 1, coordinate.R)))
                    {
                        lowestElevation--;
                    }

                    Vector3 shadowOrigin = tileOrigin;
                    shadowOrigin.y += (float)(Hex.AxialToWorldY(lowestElevation) - Hex.AxialToWorldY(coordinate.Elevation)) +
                                      _sourceMinY * TileVerticalScale;
                    float height = (coordinate.Elevation - lowestElevation) * (float)Hex.AxialToWorldY(1) +
                                   _sourceHeight * TileVerticalScale;
                    float scaleY = height / _sourceHeight;
                    shadowOrigin.y -= _sourceMinY * scaleY;
                    _shadowInstances.Add(chunk.GameObject.transform.localToWorldMatrix *
                        Matrix4x4.TRS(shadowOrigin, Quaternion.identity, new Vector3(1f, scaleY, 1f)));
                }

                for (int faceIndex = 0; faceIndex < _sourceTriangles.Length; faceIndex++)
                {
                    FaceTriangle face = _sourceTriangles[faceIndex];
                    if (!IsExposed(coordinate, face))
                    {
                        continue;
                    }

                    float shade = face.Kind == FaceKind.Side ? SideShade : 1f;
                    int[] vertexIndices = face.Kind == FaceKind.Side ? _sideVertexIndices : _topVertexIndices;
                    AppendVertex(tileOrigin, tileColor, shade, vertexIndices, face.A);
                    AppendVertex(tileOrigin, tileColor, shade, vertexIndices, face.B);
                    AppendVertex(tileOrigin, tileColor, shade, vertexIndices, face.C);
                }
            }

            chunk.Mesh.Clear(false);
            chunk.Mesh.indexFormat = GetIndexFormat(_vertices.Count);
            chunk.Mesh.SetVertices(_vertices);
            chunk.Mesh.SetNormals(_normals);
            chunk.Mesh.SetUVs(0, _uvs);
            chunk.Mesh.SetColors(_colors);
            chunk.Mesh.SetTriangles(_indices, 0, false);
            chunk.Mesh.RecalculateBounds();

            HexTerrainStencilShadowRegistry.Set(chunk, _shadowTemplate, _shadowInstances);
        }

        private void AppendVertex(Vector3 tileOrigin, Color tileColor, float shade, int[] vertexIndices, int sourceIndex)
        {
            int vertexIndex = vertexIndices[sourceIndex];
            if (vertexIndex == -1)
            {
                Vector3 position = new(
                    tileOrigin.x + _sourceVertices[sourceIndex].x,
                    tileOrigin.y + _sourceVertices[sourceIndex].y * TileVerticalScale,
                    tileOrigin.z + _sourceVertices[sourceIndex].z);

                vertexIndex = _vertices.Count;
                vertexIndices[sourceIndex] = vertexIndex;
                _vertices.Add(position);
                _normals.Add(_sourceNormals[sourceIndex]);
                _uvs.Add(_sourceUvs.Length == _sourceVertices.Length ? _sourceUvs[sourceIndex] : Vector2.zero);
                _colors.Add(new Color(tileColor.r * shade, tileColor.g * shade, tileColor.b * shade, tileColor.a));
            }

            _indices.Add(vertexIndex);
        }

        private static void ResetVertexIndices(int[] indices)
        {
            for (int i = 0; i < indices.Length; i++)
            {
                indices[i] = -1;
            }
        }

        private bool IsExposed(TileCoordinate coordinate, FaceTriangle face)
        {
            if (face.Kind == FaceKind.Bottom)
            {
                return false;
            }

            if (face.Kind == FaceKind.Top)
            {
                return !_occupancy.ContainsKey(new TileCoordinate(coordinate.Q, coordinate.Elevation + 1, coordinate.R));
            }

            return !_occupancy.ContainsKey(new TileCoordinate(
                coordinate.Q + NeighborQ[face.Neighbor], coordinate.Elevation,
                coordinate.R + NeighborR[face.Neighbor]));
        }

        private void ReleaseChunk(ChunkData chunk)
        {
            HexTerrainStencilShadowRegistry.Remove(chunk);
            if (chunk.GameObject != null)
            {
                UnityEngine.Object.Destroy(chunk.GameObject);
            }

            if (chunk.Mesh != null)
            {
                UnityEngine.Object.Destroy(chunk.Mesh);
            }
        }

        private void OnSceneUnloaded(Scene scene)
        {
            if (scene.handle != _scene.handle)
            {
                return;
            }

            SceneManager.sceneUnloaded -= OnSceneUnloaded;
            foreach (ChunkData chunk in _chunks.Values)
            {
                ReleaseChunk(chunk);
            }

            _chunks.Clear();
            _chunkByEntity.Clear();
            _occupancy.Clear();
            _lowestElevation.Clear();
            _highestElevation.Clear();
            _dirty.Clear();
            _dirtyQueue.Clear();
            UnityEngine.Object.Destroy(_root);
            UnityEngine.Object.Destroy(_shadowTemplate);
        }

        private Vector3 ChunkOrigin(ChunkKey key)
        {
            fix3 origin = Hex.AxialToWorld(new int3(
                key.Q * ChunkSize, key.Elevation * ChunkHeight, key.R * ChunkSize));
            return new Vector3((float)origin.x, (float)origin.y, (float)origin.z);
        }

        private void BuildNeighborDirections()
        {
            for (int i = 0; i < NeighborQ.Length; i++)
            {
                fix2 direction = Hex.AxialToWorld(new int2(NeighborQ[i], NeighborR[i]));
                _neighborDirections[i] = new Vector3((float)direction.x, 0f, (float)direction.y).normalized;
            }
        }

        private FaceTriangle[] BuildFaceTriangles(Mesh mesh)
        {
            int[] triangles = mesh.triangles;
            var faces = new List<FaceTriangle>(triangles.Length / 3);
            for (int i = 0; i < triangles.Length; i += 3)
            {
                int a = triangles[i];
                int b = triangles[i + 1];
                int c = triangles[i + 2];
                Vector3 vertexA = _sourceVertices[a];
                Vector3 vertexB = _sourceVertices[b];
                Vector3 vertexC = _sourceVertices[c];
                vertexA.y *= TileVerticalScale;
                vertexB.y *= TileVerticalScale;
                vertexC.y *= TileVerticalScale;
                Vector3 normal = Vector3.Cross(vertexB - vertexA, vertexC - vertexA).normalized;

                if (normal.y > 0.75f)
                {
                    faces.Add(new FaceTriangle(a, b, c, FaceKind.Top, 0));
                }
                else if (normal.y < -0.75f)
                {
                    faces.Add(new FaceTriangle(a, b, c, FaceKind.Bottom, 0));
                }
                else
                {
                    int neighbor = FindNearestNeighbor(normal);
                    faces.Add(new FaceTriangle(a, b, c, FaceKind.Side, neighbor));
                }
            }

            return faces.ToArray();
        }

        private int FindNearestNeighbor(Vector3 normal)
        {
            Vector2 face = new Vector2(normal.x, normal.z).normalized;
            int nearest = 0;
            float best = float.NegativeInfinity;
            for (int i = 0; i < _neighborDirections.Length; i++)
            {
                Vector3 direction = _neighborDirections[i];
                float dot = Vector2.Dot(face, new Vector2(direction.x, direction.z));
                if (dot > best)
                {
                    best = dot;
                    nearest = i;
                }
            }

            return nearest;
        }

        private static ChunkKey ToChunk(TileCoordinate coordinate)
            => new(
                FloorDiv(coordinate.Q, ChunkSize),
                FloorDiv(coordinate.R, ChunkSize),
                FloorDiv(coordinate.Elevation, ChunkHeight));

        private static IndexFormat GetIndexFormat(int vertexCount)
            => vertexCount > ushort.MaxValue ? IndexFormat.UInt32 : IndexFormat.UInt16;

        private static int FloorDiv(int value, int divisor)
        {
            int quotient = value / divisor;
            if (value % divisor < 0)
            {
                quotient--;
            }

            return quotient;
        }

        private static Color ToColor(fix4 color)
        {
            if (color == default)
            {
                return Color.white;
            }

            return new Color((float)color.x, (float)color.y, (float)color.z, (float)color.w);
        }

        private sealed class ChunkData
        {
            public readonly Dictionary<Entity, HexTile> Tiles = new();
            public GameObject GameObject;
            public Mesh Mesh;
        }

        private enum FaceKind : byte
        {
            Top,
            Side,
            Bottom
        }

        private readonly struct FaceTriangle
        {
            public readonly int A;
            public readonly int B;
            public readonly int C;
            public readonly FaceKind Kind;
            public readonly int Neighbor;

            public FaceTriangle(int a, int b, int c, FaceKind kind, int neighbor)
            {
                A = a;
                B = b;
                C = c;
                Kind = kind;
                Neighbor = neighbor;
            }
        }

        private readonly struct ChunkKey : IEquatable<ChunkKey>
        {
            public readonly int Q;
            public readonly int R;
            public readonly int Elevation;

            public ChunkKey(int q, int r, int elevation)
            {
                Q = q;
                R = r;
                Elevation = elevation;
            }

            public bool Equals(ChunkKey other) => Q == other.Q && R == other.R && Elevation == other.Elevation;
            public override bool Equals(object obj) => obj is ChunkKey other && Equals(other);
            public override int GetHashCode() => HashCode.Combine(Q, R, Elevation);
        }

        private readonly struct TileCoordinate : IEquatable<TileCoordinate>
        {
            public readonly int Q;
            public readonly int Elevation;
            public readonly int R;

            public TileCoordinate(int q, int elevation, int r)
            {
                Q = q;
                Elevation = elevation;
                R = r;
            }

            public bool Equals(TileCoordinate other) => Q == other.Q && Elevation == other.Elevation && R == other.R;
            public override bool Equals(object obj) => obj is TileCoordinate other && Equals(other);
            public override int GetHashCode() => HashCode.Combine(Q, Elevation, R);
        }

        private readonly struct AxialCoordinate : IEquatable<AxialCoordinate>
        {
            public readonly int Q;
            public readonly int R;

            public AxialCoordinate(int q, int r)
            {
                Q = q;
                R = r;
            }

            public bool Equals(AxialCoordinate other) => Q == other.Q && R == other.R;
            public override bool Equals(object obj) => obj is AxialCoordinate other && Equals(other);
            public override int GetHashCode() => HashCode.Combine(Q, R);
        }
    }
}
