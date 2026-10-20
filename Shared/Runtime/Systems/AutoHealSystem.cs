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
        private fix _deltaTime;
        private readonly IOperation<fix> _healLoad;
        private readonly IOperation<fix> _heal;

        public AutoHealSystem(World world)
        {
            _world = world;
            var healLoadDesc = _healLoadDesc;
            _healLoad = world.ForEach<fix, AutoHeal, RecivedDamage>(in healLoadDesc, ref _deltaTime,
                static (ref fix deltaTime, ref AutoHeal autoHeal, in RecivedDamage recivedDamage) =>
                {
                    autoHeal.HealLoadPercent = recivedDamage > fix.Zero ? 0 :
                        Maths.MoveTowards(autoHeal.HealLoadPercent, 1, deltaTime / autoHeal.HealDelay);
                });

            var healDesc = _healDesc;
            _heal = world.ForEach<fix, Health, MaxHealth, AutoHeal>(in healDesc, ref _deltaTime,
                static (ref fix deltaTime, ref Health health, in MaxHealth maxHealth, ref AutoHeal autoHeal) =>
                {
                    health = autoHeal.HealLoadPercent != 1 ? health :
                        Maths.MoveTowards(health, maxHealth, autoHeal.HealPerSecond * deltaTime);
                });
        }

        public void Tick(int tick, fix deltaTime)
        {
            _deltaTime = deltaTime;
            _healLoad.Invoke(ref _deltaTime);
            _heal.Invoke(ref _deltaTime);
        }
    }
}
