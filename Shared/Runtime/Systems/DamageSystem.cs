using Delta;
using Delta.ECS;
using DVG.SkyPirates.Shared.Ecs;
using DVG.SkyPirates.Shared.Components.Framed;
using DVG.SkyPirates.Shared.Components.Runtime;
using DVG.SkyPirates.Shared.IServices.TickableExecutors;

namespace DVG.SkyPirates.Shared.Systems
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
