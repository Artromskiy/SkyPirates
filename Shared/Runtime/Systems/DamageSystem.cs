using Delta;
using Delta.ECS;
using SkyPirates.Shared.Ecs;
using SkyPirates.Shared.Components.Framed;
using SkyPirates.Shared.Components.Runtime;
using SkyPirates.Shared.IServices.TickableExecutors;

namespace SkyPirates.Shared.Systems
{
    public sealed class DamageSystem : IDeltaTickableExecutor
    {
        private Query? _descCache;
        private Query _desc => _descCache ??= _world.
            WhereAll<Health, RecivedDamage>().Alive().NotDisabled();

        private readonly World _world;

        public DamageSystem(World world)
        {
            _world = world;
        }

        public void Tick(int tick, fix deltaTime)
        {
            var desc = _desc;
            _world.ForEach<Health, RecivedDamage>(in desc,
                static (ref Health health, ref RecivedDamage recivedDamage) =>
                {
                    health -= (fix)recivedDamage;
                    recivedDamage = fix.Zero;
                }).Invoke();
        }
    }
}
