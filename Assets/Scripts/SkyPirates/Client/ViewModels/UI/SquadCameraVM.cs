#nullable enable
using Arch.Core;
using DVG.Components;
using DVG.SkyPirates.Client.IServices;
using DVG.SkyPirates.Client.IViewModels;
using DVG.SkyPirates.Shared.Components.Config;
using DVG.SkyPirates.Shared.Components.Runtime;
using DVG.SkyPirates.Shared.Data;
using DVG.SkyPirates.Shared.IServices;

namespace DVG.SkyPirates.Client.ViewModels.UI
{
    public class SquadCameraVM : ICameraVM
    {
        private readonly CameraConfig _config = null!;
        private readonly World _world = null!;
        private readonly IPlayer _player;
        private readonly IEntityRegistry _entityRegistry;

        private const int LerpValue = 0;

        public SquadCameraVM(CameraConfig cameraConfig, World world, IPlayer player, IEntityRegistry entityRegistry)
        {
            _config = cameraConfig;
            _world = world;
            _player = player;
            _entityRegistry = entityRegistry;
        }

        public float3 TargetPosition
        {
            get
            {
                if (_player.CurrentEntityId == null)
                    return float3.zero;
                if (!_entityRegistry.TryGet(_player.CurrentEntityId.Value, out var entity))
                    return float3.zero;
                if (!_world.IsAlive(entity))
                    return float3.zero;

                SyncId syncId = new() { Value = _player.CurrentEntityId.Value };
                _entityRegistry.TryGet(syncId, out var squad);
                if (_world.IsAlive(squad) && _world.Has<Position>(squad))
                    return (float3)_world.Get<Position>(squad).Value;
                else
                    return float3.zero;
            }
        }

        public float TargetDistance
        {
            get
            {
                if (_player.CurrentEntityId == null)
                    return (float)_config.MinDistance;
                if (!_entityRegistry.TryGet(_player.CurrentEntityId.Value, out var entity))
                    return (float)_config.MinDistance;
                if (!_world.IsAlive(entity))
                    return (float)_config.MinDistance;

                return GetHorizontalRange() / Maths.Tan(Maths.Radians(TargetFov) / 2);
            }
        }


        public float TargetFov => (float)Maths.Lerp(_config.MinFov, _config.MaxFov, LerpValue);
        public float TargetAngle => (float)Maths.Lerp(_config.MinXAngle, _config.MaxXAngle, LerpValue);
        public float SmoothMoveTime => (float)_config.SmoothMoveTime;

        private float GetHorizontalRange()
        {
            float range = 4;
            if (!_player.CurrentEntityId.HasValue)
                return range;
            if (!_entityRegistry.TryGet(_player.CurrentEntityId.Value, out var squad))
                return range;

            var searchDistance = (float)_world.Get<TargetSearchDistance>(squad).Value;
            return Maths.Max(range, searchDistance + 1);
        }
    }
}
