using Delta;
using Delta.ECS;
using DVG.Components;
using DVG.SkyPirates.Shared.Components.Config;
using DVG.SkyPirates.Shared.Components.Framed;
using DVG.SkyPirates.Shared.Components.Runtime;
using DVG.SkyPirates.Shared.Data;
using DVG.SkyPirates.Shared.Ecs;
using DVG.SkyPirates.Shared.IFactories;
using DVG.SkyPirates.Shared.Ids;
using DVG.SkyPirates.Shared.IServices.TickableExecutors;
using System.Collections.Generic;

namespace DVG.SkyPirates.Shared.Systems
{
    public sealed class RangedImpactSystem : IDeltaTickableExecutor
    {
        private Query? _descCache;
        private Query _desc => _descCache ??= _world.
            WhereAll<BehaviourState, Damage, ImpactDistance, Position, Target, SyncIdReserve, Ranged>().Alive().NotDisabled();

        private readonly World _world;
        private readonly IEntityFactory _entityFactory;
        private readonly List<Attack> _attacks = new();

        public RangedImpactSystem(World world, IEntityFactory entityFactory)
        {
            _world = world;
            _entityFactory = entityFactory;
        }

        public void Tick(int tick, fix deltaTime)
        {
            _attacks.Clear();
            var state = _attacks;
            var desc = _desc;
            _world.ForEachEntity<List<Attack>, BehaviourState, Damage, ImpactDistance, Position, Target, Ranged>(in desc, ref state,
                static (ref List<Attack> attacks, EntityRef entity, in BehaviourState behaviour, in Damage damage,
                    in ImpactDistance impactDistance, in Position position, in Target target, in Ranged ranged) =>
                {
                    if (behaviour.State != StateId.Constants.PreAttack || behaviour.Percent != 1 || !target.Entity.HasValue)
                        return;

                    attacks.Add(new Attack((Entity)entity, position.Value, target.Entity.Value, damage, impactDistance,
                        (fix)ranged.ProjectileSpeed, ranged.ProjectileViewToken));
                }).Invoke(ref state);

            for (int i = 0; i < _attacks.Count; i++)
            {
                var attack = _attacks[i];
                if (!_world.IsAlive(attack.Target) || !_world.TryGet(attack.Target, out Position targetPosition))
                    continue;

                fix impactDistance = (fix)attack.ImpactDistance;
                if (fix3.SqrDistance(targetPosition, attack.Position) > impactDistance * impactDistance)
                    continue;

                ref var idReserve = ref _world.GetRef<SyncIdReserve>(attack.Attacker);
                if (idReserve.RemainingCount() <= 0)
                    continue;

                var projectileSyncId = idReserve.GetNext();
                var projectileEntity = _entityFactory.Create(new EntityParameters(projectileSyncId, default, default));
                _world.Add(projectileEntity,
                    new Projectile { Target = attack.Target, StartPosition = attack.Position },
                    new Source { Entity = attack.Attacker },
                    new Position { Value = attack.Position },
                    new DesiredPosition { Value = targetPosition.Value },
                    new Speed { Value = attack.ProjectileSpeed },
                    attack.Damage,
                    new ViewToken { Value = attack.ProjectileViewToken });
            }
        }

        private readonly struct Attack
        {
            public readonly Entity Attacker;
            public readonly fix3 Position;
            public readonly Entity Target;
            public readonly Damage Damage;
            public readonly ImpactDistance ImpactDistance;
            public readonly fix ProjectileSpeed;
            public readonly string ProjectileViewToken;

            public Attack(Entity attacker, fix3 position, Entity target, Damage damage, ImpactDistance impactDistance,
                fix projectileSpeed, string projectileViewToken)
            {
                Attacker = attacker;
                Position = position;
                Target = target;
                Damage = damage;
                ImpactDistance = impactDistance;
                ProjectileSpeed = projectileSpeed;
                ProjectileViewToken = projectileViewToken;
            }
        }
    }
}
