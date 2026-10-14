using Delta;
using Delta.ECS;
using DVG.SkyPirates.Shared.Ecs;
using DVG.SkyPirates.Shared.Components.Config;
using DVG.SkyPirates.Shared.Components.Runtime;
using DVG.SkyPirates.Shared.IServices.TickableExecutors;
using System.Collections.Generic;

namespace DVG.SkyPirates.Shared.Systems
{
    public class FlyMoveSystem : IDeltaTickableExecutor
    {
        private Query? _descCache;
        private Query _desc => _descCache ??= _world.
            WhereAll<Position, FlyDestination, MaxSpeed>().Alive().NotDisabled();

        private readonly List<Entity> _finished = new();

        private readonly World _world;

        public FlyMoveSystem(World world)
        {
            _world = world;
        }

        public void Tick(int tick, fix deltaTime)
        {
            _finished.Clear();
            var desc = _desc;
            var delta = deltaTime;
            _world.ForEach<fix, Position, FlyDestination, MaxSpeed>(in desc, ref delta,
                static (ref fix deltaTime, ref Position position, in FlyDestination fly, in MaxSpeed maxSpeed) =>
                {
                    const int ArcHeight = 4;

                    var end = fly.EndPosition;
                    var start = fly.StartPosition;
                    var endXZ = end.xz;
                    var startXZ = start.xz;
                    var currentXZ = fix2.MoveTowards(((fix3)position).xz, endXZ, deltaTime * maxSpeed);
                    var totalDistXZ = fix2.Distance(startXZ, endXZ);
                    var currentDistXZ = fix2.Distance(startXZ, currentXZ);
                    var percent = totalDistXZ == 0 ? 1 : Maths.InvLerp(0, totalDistXZ, currentDistXZ);
                    var currentY = Maths.Lerp(start.y, end.y, percent);

                    var arc = 4 * percent * (1 - percent);
                    position = new fix3(currentXZ.x, currentY + arc * ArcHeight, currentXZ.y);
                }).Invoke(ref delta);

            var finished = _finished;
            _world.ForEachEntity<List<Entity>, Position, FlyDestination, MaxSpeed>(in desc, ref finished,
                static (ref List<Entity> entities, EntityRef entity, in Position position, in FlyDestination fly, in MaxSpeed maxSpeed) =>
                {
                    if (position == fly.EndPosition)
                    {
                        entities.Add(entity);
                    }
                }).Invoke(ref finished);
            foreach (var item in _finished)
            {
                _world.Remove<FlyDestination>(item);
            }
        }
    }
}
