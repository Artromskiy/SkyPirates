using Delta;
using Delta.ECS;
using SlyPirates.Shared.Ecs;
using SlyPirates.Shared.Components.Framed;
using SlyPirates.Shared.Components.Runtime;
using SlyPirates.Shared.IServices.TickableExecutors;

namespace SlyPirates.Shared.Systems
{
    public sealed class DamageSystem : IDeltaTickableExecutor
    {
        private Query? _descCache;
        private Query _desc => _descCache ??= _world.
            WhereAll<Health, RecivedDamage>().Alive().NotDisabled();

        private readonly World _world;
        private readonly IOperation _applyDamage;

        public DamageSystem(World world)
        {
            _world = world;
            var desc = _desc;
            _applyDamage = world.ForEach<Health, RecivedDamage>(in desc,
                static (ref Health health, ref RecivedDamage recivedDamage) =>
                {
                    health -= (fix)recivedDamage;
                    recivedDamage = fix.Zero;
                });
        }

        public void Tick(int tick, fix deltaTime)
        {
            _applyDamage.Invoke();
        }
    }
}
