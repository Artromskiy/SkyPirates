using Delta;
using Delta.ECS;
using Delta.ECS.Unity;
using SkyPirates.Shared.Components.Framed;
using SkyPirates.Shared.Components.Runtime;
using UnityEngine;

namespace SkyPirates.Client.Views.Components
{
    [RequireComponent(typeof(LineRenderer))]
    public sealed class ProjectileLineView : MonoBehaviour, IEntityView
    {
        [SerializeField] private LineRenderer _lineRenderer;

        private EntityContext _context;
        private Vector3 _previousPosition;
        private Entity _source;
        private Entity _target;
        private Vector3 _startPosition;
        private Vector3 _destination;

        public void OnBind(EntityContext context)
        {
            _context = context;
            Projectile projectile = context.Get<Projectile>();
            _source = context.Get<Source>().Entity;
            _target = projectile.Target;
            _startPosition = (float3)projectile.StartPosition;
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
            if (!EntityViewLifetime.TryEnterUpdate(this, _context))
                return;

            Vector3 currentPosition = GetVisualPosition();
            transform.position = currentPosition;
            _lineRenderer.SetPosition(0, _previousPosition);
            _lineRenderer.SetPosition(1, currentPosition);
            _previousPosition = currentPosition;
        }

        private Vector3 GetVisualPosition()
        {
            World world = _context.World!;
            Vector3 position = (float3)_context.Get<Position>().Value;
            Vector3 start = _startPosition;
            Vector3 destination = _destination;

            if (ProjectileSourceView.TryGetMuzzlePoint(world, _source, out Vector3 muzzlePosition))
                start = muzzlePosition;
            else if (world.TryGet(_source, out Position sourcePosition))
                start = (float3)sourcePosition.Value;
            if (world.TryGet(_target, out Position targetPosition))
            {
                Vector3 target = (float3)targetPosition.Value;
                destination = target + Vector3.up * 0.5f;
            }

            Vector3 flightStart = _startPosition;
            Vector3 flightEnd = _destination;
            Vector3 flight = flightEnd - flightStart;
            float lengthSquared = flight.sqrMagnitude;
            float progress = lengthSquared <= Mathf.Epsilon
                ? 1f
                : Mathf.Clamp01(Vector3.Dot(position - flightStart, flight) / lengthSquared);

            return Vector3.Lerp(start, destination, progress);
        }
    }
}
