using Delta;
using Delta.ECS;
using Delta.ECS.Unity;
using DVG.SkyPirates.Shared.Components.Framed;
using DVG.SkyPirates.Shared.Components.Runtime;
using DVG.SkyPirates.Shared.Ecs;
using UnityEngine;

namespace DVG.SkyPirates.Client.Views.Components
{
    [RequireComponent(typeof(LineRenderer))]
    public sealed class ProjectileLineView : MonoBehaviour, IEntityView
    {
        [SerializeField] private LineRenderer _lineRenderer;

        private EntityContext _context;
        private Vector3 _previousPosition;
        private Transform? _muzzlePoint;
        private Vector3 _startPosition;
        private Vector3 _muzzlePosition;
        private Vector3 _destination;

        public void OnBind(EntityContext context)
        {
            _context = context;
            Projectile projectile = context.Get<Projectile>();
            World world = context.World!;
            SyncId source = context.Get<Source>().SyncId;
            SceneWorld scene = SceneWorld.GetForScene(gameObject.scene);
            _muzzlePoint = null;
            if (world.GetEntityIdMap().TryGetRetainedEntity(source, out Entity sourceEntity)
                && scene.TryGetView(sourceEntity, out GameObject sourceView))
                _muzzlePoint = sourceView.GetComponent<ProjectileSourceView>()?.MuzzlePoint;

            _startPosition = (float3)projectile.StartPosition;
            _muzzlePosition = _startPosition;
            _destination = (float3)context.Get<DesiredPosition>().Value;
            _previousPosition = GetVisualPosition();
            transform.position = _previousPosition;
            _lineRenderer.SetPosition(0, _previousPosition);
            _lineRenderer.SetPosition(1, _previousPosition);
        }

        private void Awake()
        {
            if (_lineRenderer == null)
                _lineRenderer = GetComponent<LineRenderer>();

            _lineRenderer.useWorldSpace = true;
            _lineRenderer.positionCount = 2;
        }

        private void Update()
        {
            Vector3 currentPosition = GetVisualPosition();
            transform.position = currentPosition;
            _lineRenderer.SetPosition(0, _previousPosition);
            _lineRenderer.SetPosition(1, currentPosition);
            _previousPosition = currentPosition;
        }

        private Vector3 GetVisualPosition()
        {
            Vector3 position = (float3)_context.Get<Position>().Value;
            if (_muzzlePoint != null)
                _muzzlePosition = _muzzlePoint.position;

            Vector3 flightStart = _startPosition;
            Vector3 flightEnd = _destination;
            Vector3 flight = flightEnd - flightStart;
            float lengthSquared = flight.sqrMagnitude;
            float progress = lengthSquared <= Mathf.Epsilon
                ? 1f
                : Mathf.Clamp01(Vector3.Dot(position - flightStart, flight) / lengthSquared);

            return Vector3.Lerp(_muzzlePosition, _destination + Vector3.up * 0.5f, progress);
        }
    }
}
