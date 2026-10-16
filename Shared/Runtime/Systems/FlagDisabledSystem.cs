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
		private readonly List<fix4> _activeQuads = new();

        private Query? _activityRangesCache;
        private Query _activityRanges => _activityRangesCache ??= _world.WhereAll<ActivityRange, Position>().Alive();

        private Query? _enabledCache;
        private Query _enabled => _enabledCache ??= _world.WhereAll<Position>().WhereNone<Disabled>().Alive();
        private Query? _disabledCache;
        private Query _disabled => _disabledCache ??= _world.WhereAll<Position, Disabled>().Alive();

		public FlagDisabledSystem(World world)
		{
			_world = world;
		}

		public void Tick(int tick, fix deltaTime)
		{
			_toDisable.Clear();
			_toEnable.Clear();
			_activeQuads.Clear();

			var activeQuads = _activeQuads;
			var activityRanges = _activityRanges;
			_world.ForEach<List<fix4>, ActivityRange, Position>(in activityRanges, ref activeQuads, static (ref List<fix4> quads, in ActivityRange range, in Position position) =>
			{
				var pos = position.Value.xz;
				fix4 minMax = default;
				minMax.xy = pos - new fix2(range.Value);
				minMax.zw = pos + new fix2(range.Value);
				quads.Add(minMax);
			}).Invoke(ref activeQuads);

			var activeRegions = _activeQuads;
			var toDisable = _toDisable;
			var enabled = _enabled;
			(List<fix4> Regions, List<Entity> Entities) disableState = (activeRegions, toDisable);
			_world.ForEachEntity<(List<fix4> Regions, List<Entity> Entities), Position>(in enabled,
				ref disableState,
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
				}).Invoke(ref disableState);

			var toEnable = _toEnable;
			var disabled = _disabled;
			(List<fix4> Regions, List<Entity> Entities) enableState = (activeRegions, toEnable);
			_world.ForEachEntity<(List<fix4> Regions, List<Entity> Entities), Position>(in disabled, ref enableState,
				static (ref (List<fix4> Regions, List<Entity> Entities) state, EntityRef entity, in Position position) =>
				{
					var xz = position.Value.xz;
					for (int i = 0; i < state.Regions.Count; i++)
                    {
                        if (Inside(xz, state.Regions[i]))
                        {
                            state.Entities.Add(entity);
                        }
                    }
                }).Invoke(ref enableState);

			foreach (var item in _toEnable)
            {
                _world.Remove<Disabled>(item);
            }

            foreach (var item in _toDisable)
            {
                _world.Add<Disabled>(item);
            }
        }

		private static bool Inside(fix2 point, fix4 minMax)
		{
			return point.x >= minMax.x && point.y >= minMax.y && point.x <= minMax.z && point.y <= minMax.w;
		}
	}
}
