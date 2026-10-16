using Delta;
using Delta.ECS;
using SlyPirates.Shared.Ecs;
using SlyPirates.Shared.Components.Config;
using SlyPirates.Shared.Components.Runtime;
using SlyPirates.Shared.Ids;
using SlyPirates.Shared.IServices.TickableExecutors;

namespace SlyPirates.Shared.Systems
{
    public sealed class SimpleBehaviourSystem : IDeltaTickableExecutor
    {
        private Query? _descSwitchCache;
        private Query _descSwitch => _descSwitchCache ??= _world.
            WhereAll<BehaviourState, BehaviourConfig>().Alive().NotDisabled();

        private Query? _descTickCache;
        private Query _descTick => _descTickCache ??= _world.
            WhereAll<BehaviourState>().Alive().NotDisabled();

        private readonly World _world;

        public SimpleBehaviourSystem(World world)
        {
            _world = world;
        }

        public void Tick(int tick, fix deltaTime)
        {
            var switchDesc = _descSwitch;
            _world.ForEach<BehaviourState, BehaviourConfig>(in switchDesc,
                static (ref BehaviourState behaviour, in BehaviourConfig behaviourConfig) =>
                {
                // skip if no force state and we are at none
                if (behaviour.ForceState == null && (
                    behaviour.Percent != 1 || behaviour.State.IsNone))
                {
                    return;
                }

                StateId targetState = behaviour.ForceState ??=
                    behaviourConfig.Scenario[behaviour.State];

                behaviour.ForceState = null;
                behaviour.State = targetState;
                behaviour.Duration = behaviourConfig.Durations[behaviour.State];
                behaviour.Percent = 0;
                }).Invoke();

            var delta = deltaTime;
            var tickDesc = _descTick;
            _world.ForEach<fix, BehaviourState>(in tickDesc, ref delta,
                static (ref fix deltaTime, ref BehaviourState behaviour) =>
                {
                    fix step = behaviour.Duration == 0 ? 1 : deltaTime / behaviour.Duration;
                    behaviour.Percent = Maths.MoveTowards(behaviour.Percent, 1, step);
                }).Invoke(ref delta);
        }
    }
}
