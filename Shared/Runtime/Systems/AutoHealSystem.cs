using Delta;
using Delta.ECS;
using SlyPirates.Shared.Ecs;
using SlyPirates.Shared.Components.Config;
using SlyPirates.Shared.Components.Framed;
using SlyPirates.Shared.Components.Runtime;
using SlyPirates.Shared.IServices.TickableExecutors;

namespace SlyPirates.Shared.Systems
{
    public sealed class AutoHealSystem : IDeltaTickableExecutor
    {
        private Query? _healLoadDescCache;
        private Query _healLoadDesc => _healLoadDescCache ??= _world.
            WhereAll<Health, MaxHealth, AutoHeal, RecivedDamage>().Alive().NotDisabled();

        private Query? _healDescCache;
        private Query _healDesc => _healDescCache ??= _world.
            WhereAll<Health, MaxHealth, AutoHeal>().Alive().NotDisabled();

        private readonly World _world;

        public AutoHealSystem(World world)
        {
            _world = world;
        }

        public void Tick(int tick, fix deltaTime)
        {
            var delta = deltaTime;
            var healLoadDesc = _healLoadDesc;
            _world.ForEach<fix, AutoHeal, RecivedDamage>(in healLoadDesc, ref delta,
                static (ref fix deltaTime, ref AutoHeal autoHeal, in RecivedDamage recivedDamage) =>
                {
                    autoHeal.HealLoadPercent = recivedDamage > fix.Zero ? 0 :
                        Maths.MoveTowards(autoHeal.HealLoadPercent, 1, deltaTime / autoHeal.HealDelay);
                }).Invoke(ref delta);

            var healDesc = _healDesc;
            _world.ForEach<fix, Health, MaxHealth, AutoHeal>(in healDesc, ref delta,
                static (ref fix deltaTime, ref Health health, in MaxHealth maxHealth, ref AutoHeal autoHeal) =>
                {
                    health = autoHeal.HealLoadPercent != 1 ? health :
                        Maths.MoveTowards(health, maxHealth, autoHeal.HealPerSecond * deltaTime);
                }).Invoke(ref delta);
        }
    }
}
