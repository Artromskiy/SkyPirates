using Delta;
using Delta.ECS;
using SlyPirates.Shared.Components;
using SlyPirates.Shared.Components.Runtime;
using SlyPirates.Shared.Components.Config;
using SlyPirates.Shared.Components.Framed;
using SlyPirates.Shared.Data;
using SlyPirates.Shared.Ecs;
using SlyPirates.Shared.IFactories;
using SlyPirates.Shared.Ids;
using SlyPirates.Shared.IServices.TickableExecutors;
using System.Collections.Generic;

namespace SlyPirates.Shared.Systems
{
    public sealed class RangedImpactSystem : IDeltaTickableExecutor
    {
        private Query? _descCache;
        private Query _desc => _descCache ??= _world.
            WhereAll<BehaviourState, Damage, ImpactDistance, Position, Target, SyncIdReserve, Ranged, SyncId>().Alive().NotDisabled();

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
            _world.ForEach<List<Attack>, BehaviourState, Damage, ImpactDistance, Position, Target, Ranged, SyncId>(in desc, ref state,
                static (ref List<Attack> state, in BehaviourState behaviour, in Damage damage,
                    in ImpactDistance impactDistance, in Position position, in Target target, in Ranged ranged, in SyncId syncId) =>
                {
                    if (behaviour.State != StateId.Constants.PreAttack || behaviour.Percent != 1 || !target.SyncId.HasValue)
                    {
                        return;
                    }

                    state.Add(new Attack(syncId, position.Value,
                        target.SyncId.Value, damage, impactDistance, (fix)ranged.ProjectileSpeed,
                        ranged.ProjectileViewToken));
                }).Invoke(ref state);

            for (int i = 0; i < _attacks.Count; i++)
            {
                var attack = _attacks[i];
                Entity targetEntity = _world.GetEntity(attack.Target);
                Position targetPosition = _world.Get<Position>(targetEntity);

                fix impactDistance = (fix)attack.ImpactDistance;
                if (fix3.SqrDistance(targetPosition, attack.Position) > impactDistance * impactDistance)
                {
                    continue;
                }

                Entity attackerEntity = _world.GetEntity(attack.Attacker);
                ref var idReserve = ref _world.GetRef<SyncIdReserve>(attackerEntity);
                if (idReserve.RemainingCount() <= 0)
                {
                    continue;
                }

                var projectileSyncId = idReserve.GetNext();
                var projectileEntity = _entityFactory.Create(new EntityParameters(projectileSyncId, default, default));
                _world.Add(projectileEntity,
                    new Projectile { Target = attack.Target, StartPosition = attack.Position },
                    new Source { SyncId = attack.Attacker },
                    new Position { Value = attack.Position },
                    new DesiredPosition { Value = targetPosition.Value },
                    new Speed { Value = attack.ProjectileSpeed },
                    attack.Damage,
                    new ViewToken { Value = attack.ProjectileViewToken });
            }
        }

        private readonly struct Attack
        {
            public readonly SyncId Attacker;
            public readonly fix3 Position;
            public readonly SyncId Target;
            public readonly Damage Damage;
            public readonly ImpactDistance ImpactDistance;
            public readonly fix ProjectileSpeed;
            public readonly string ProjectileViewToken;

            public Attack(SyncId attacker, fix3 position, SyncId target, Damage damage,
                ImpactDistance impactDistance, fix projectileSpeed, string projectileViewToken)
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
