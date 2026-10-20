using Delta;
using Delta.ECS;
using SlyPirates.Shared.Ecs;
using SlyPirates.Shared.Components.Config;
using SlyPirates.Shared.Components.Framed;
using SlyPirates.Shared.Components.Runtime;
using SlyPirates.Shared.IServices.TickableExecutors;
using System;
using System.Collections.Generic;

namespace SlyPirates.Shared.Systems
{
	public class FlagDisabledSystem : IDeltaTickableExecutor
	{
		private readonly World _world;
		private readonly List<Entity> _toDisable = new();
		private readonly List<Entity> _toEnable = new();
		private List<fix4> _activeQuads = new();
        private Entity[] _entityBatch = Array.Empty<Entity>();
        private (List<fix4> Regions, List<Entity> Entities) _disableState;
        private (List<fix4> Regions, List<Entity> Entities) _enableState;
        private readonly IOperation<List<fix4>> _collectRegions;
        private readonly IOperation<(List<fix4> Regions, List<Entity> Entities)> _collectToDisable;
        private readonly IOperation<(List<fix4> Regions, List<Entity> Entities)> _collectToEnable;

        private Query? _activityRanges;
        private Query ActivityRanges => _activityRanges ??= _world.WhereAll<ActivityRange, Position>().Alive();
        private Query? _enabled;
        private Query Enabled => _enabled ??= _world.WhereAll<Position>().WhereNone<Disabled>().Alive();
        private Query? _disabled;
        private Query Disabled => _disabled ??= _world.WhereAll<Position, Disabled>().Alive();

		public FlagDisabledSystem(World world)
		{
			_world = world;
            _disableState = (_activeQuads, _toDisable);
            _enableState = (_activeQuads, _toEnable);

            var activityRanges = ActivityRanges;
            _collectRegions = world.ForEach<List<fix4>, ActivityRange, Position>(in activityRanges, ref _activeQuads,
                static (ref List<fix4> quads, in ActivityRange range, in Position position) =>
                {
                    var pos = position.Value.xz;
                    fix4 minMax = default;
                    minMax.xy = pos - new fix2(range.Value);
                    minMax.zw = pos + new fix2(range.Value);
                    quads.Add(minMax);
                });

            var enabled = Enabled;
            _collectToDisable = world.ForEachEntity<(List<fix4> Regions, List<Entity> Entities), Position>(in enabled,
                ref _disableState,
                static (ref (List<fix4> Regions, List<Entity> Entities) state, EntityRef entity, in Position position) =>
                {
                    var xz = position.Value.xz;
                    for (int i = 0; i < state.Regions.Count; i++)
                    {
                        if (Inside(xz, state.Regions[i]))
                        {
                            return;
                        }
                    }

                    state.Entities.Add(entity);
                });

            var disabled = Disabled;
            _collectToEnable = world.ForEachEntity<(List<fix4> Regions, List<Entity> Entities), Position>(in disabled,
                ref _enableState,
                static (ref (List<fix4> Regions, List<Entity> Entities) state, EntityRef entity, in Position position) =>
                {
                    var xz = position.Value.xz;
                    for (int i = 0; i < state.Regions.Count; i++)
                    {
                        if (Inside(xz, state.Regions[i]))
                        {
                            state.Entities.Add(entity);
                            return;
                        }
                    }
                });
		}

		public void Tick(int tick, fix deltaTime)
		{
			_toDisable.Clear();
			_toEnable.Clear();
			_activeQuads.Clear();

			_collectRegions.Invoke(ref _activeQuads);
			_disableState.Regions = _activeQuads;
			_disableState.Entities = _toDisable;
			_collectToDisable.Invoke(ref _disableState);
			_enableState.Regions = _activeQuads;
			_enableState.Entities = _toEnable;
			_collectToEnable.Invoke(ref _enableState);

            if (_toEnable.Count != 0)
            {
                _world.Remove<Disabled>(Batch(_toEnable));
            }

            if (_toDisable.Count != 0)
            {
                _world.Add<Disabled>(Batch(_toDisable));
            }
        }

        private ReadOnlySpan<Entity> Batch(List<Entity> entities)
        {
            if (_entityBatch.Length < entities.Count)
            {
                Array.Resize(ref _entityBatch, Math.Max(entities.Count, _entityBatch.Length * 2));
            }

            entities.CopyTo(_entityBatch);
            return _entityBatch.AsSpan(0, entities.Count);
        }

		private static bool Inside(fix2 point, fix4 minMax) =>
            point.x >= minMax.x && point.y >= minMax.y && point.x <= minMax.z && point.y <= minMax.w;
    }
}
