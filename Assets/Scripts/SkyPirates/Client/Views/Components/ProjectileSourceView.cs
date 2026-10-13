using System.Collections.Generic;
using Delta.ECS;
using Delta.ECS.Unity;
using UnityEngine;

namespace DVG.SkyPirates.Client.Views.Components
{
    public sealed class ProjectileSourceView : MonoBehaviour, IEntityView
    {
        private const string MuzzlePointName = "ProjectileMuzzle";
        private static readonly Dictionary<World, Dictionary<Entity, Transform>> MuzzlePoints = new();

        private EntityContext _context;
        private Transform _muzzlePoint;
        private bool _bound;

        public void OnBind(EntityContext context)
        {
            _context = context;
            _bound = true;
            RegisterMuzzlePoint();
        }

        private void OnDestroy()
        {
            if (!_bound)
                return;

            World world = _context.World!;
            if (!MuzzlePoints.TryGetValue(world, out Dictionary<Entity, Transform> points))
                return;

            points.Remove(_context.Entity);
            if (points.Count == 0)
                MuzzlePoints.Remove(world);
        }

        internal static bool TryGetMuzzlePoint(World world, Entity entity, out Vector3 position)
        {
            if (MuzzlePoints.TryGetValue(world, out Dictionary<Entity, Transform> points)
                && points.TryGetValue(entity, out Transform point)
                && point != null)
            {
                position = point.position;
                return true;
            }

            position = default;
            return false;
        }

        private void RegisterMuzzlePoint()
        {
            Transform[] transforms = GetComponentsInChildren<Transform>(true);
            for (int i = 0; i < transforms.Length; i++)
            {
                if (transforms[i].name != MuzzlePointName)
                    continue;

                _muzzlePoint = transforms[i];
                World world = _context.World!;

                if (!MuzzlePoints.TryGetValue(world, out Dictionary<Entity, Transform> points))
                {
                    points = new Dictionary<Entity, Transform>();
                    MuzzlePoints.Add(world, points);
                }

                points[_context.Entity] = _muzzlePoint;
                return;
            }
        }
    }
}
