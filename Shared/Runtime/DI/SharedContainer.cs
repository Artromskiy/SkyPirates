using Delta.ECS;
using SlyPirates.Shared;
using SlyPirates.Shared.Components;
using SlyPirates.Shared.Components.Runtime;
using SlyPirates.Shared.Core;
using SlyPirates.Shared.Components.Config;
using SlyPirates.Shared.Components.Framed;
using SlyPirates.Shared.Commands;
using SlyPirates.Shared.Data;
using SlyPirates.Shared.Ecs;
using SlyPirates.Shared.Factories;
using SlyPirates.Shared.IFactories;
using SlyPirates.Shared.IServices;
using SlyPirates.Shared.IServices.TickableExecutors;
using SlyPirates.Shared.Ids;
using SlyPirates.Shared.Services;
using SlyPirates.Shared.Services.CommandExecutors;
using SlyPirates.Shared.Services.CommandMutators;
using SlyPirates.Shared.Services.CommandValidators;
using SlyPirates.Shared.Services.Netcode;
using SlyPirates.Shared.Systems;
using SlyPirates.Shared.Systems.Special;
using SimpleInjector;
using System;
using Delta.Netcode;

namespace SlyPirates.Shared.DI
{
    public class SharedContainer : Container
    {
        public SharedContainer(SessionMode sessionMode = SessionMode.Local, bool registerCommandReceiver = true, World world = null,
            int historyCapacity = Constants.MaxHistoryTicks)
        {
            Delta.Diagnostics.Trace.Info("[DI] SharedContainer Start");
            var currentWorld = WorldFactory.Create(world ?? new World(initialEntityCapacity: 1024));
            RegisterSingleton(() => currentWorld);

            currentWorld.GetOrCreateEntityIdMap(1024);

            RegisterSingleton<EntityCacheSystem>();
            RegisterSingleton<IWorldHistory>(() =>
            {
                World historyWorld = GetInstance<World>();
                return new WorldHistory(
                    historyWorld,
                    historyWorld.GetGroup(Group.History).ToArray(),
                    Math.Min(4, historyCapacity),
                    historyCapacity);
            });

            RegisterSingleton<TimelineWriter>();

            RegisterSingleton(typeof(IEntityConfigFactory<>), typeof(EntityConfigFactory<>));
            RegisterSingleton(typeof(IConfigedEntityFactory<>), typeof(ConfigedEntityFactory<>));
            RegisterFactorySingleton<IGlobalConfigFactory, GlobalConfigFactory, GlobalConfig>();
            RegisterFactorySingleton<IPackedCirclesFactory, PackedCirclesFactory, PackedCirclesConfig>();
            RegisterSingleton<IEntityFactory, EntityFactory>();
            RegisterSingleton<ISquadFactory, SquadFactory>();

            RegisterSingleton<IComponentDependenciesService, ComponentDependenciesService>();
            RegisterSingleton<IComponentDefaultsService, ComponentDefaultsService>();
            RegisterSingleton<IEntityRegistry, EntityRegistry>();

            RegisterSingleton<IPooledItemsProvider, PooledItemsProvider>();
            RegisterSingleton<ITargetSearchSystem, TargetSearchSystem>();

            RegisterSingleton<ICommandPayloadHandler, SkyPiratesCommandPayloadHandler>();
            if (registerCommandReceiver)
            {
                RegisterSingleton<ICommandReciever, CommandReceiver>();
            }

            RegisterSingleton<SkyPiratesSessionSetup>();
            RegisterSingleton<SkyPiratesSessionTickLoop>();
            RegisterSingleton<ISessionTransport>(() => CreateSessionTransport(sessionMode));
            RegisterSingleton(() => new SkyPiratesSessionProvider(
                GetInstance<SkyPiratesSessionSetup>(),
                sessionMode,
                GetInstance<ISessionTransport>()));

            RegisterSingleton(typeof(IDeltaTickableService<>), typeof(DeltaTickableService<>));
            RegisterSingleton(typeof(ITickableService<>), typeof(TickableService<>));
            RegisterSingleton<SkyPiratesTransientSimulation>();
            RegisterSingleton<ITransientSimulation<JoystickCommand>>(() => GetInstance<SkyPiratesTransientSimulation>());

            Collection.Register<IDeltaTickableExecutor>(TickableExecutors, Lifestyle.Singleton);
            Collection.Register<ICommandExecutorRegistration>(CommandExecutors, Lifestyle.Singleton);
            Collection.Register<ICommandValidatorRegistration>(CommandValidators, Lifestyle.Singleton);
            Collection.Register<ICommandMutatorRegistration>(CommandMutators, Lifestyle.Singleton);

            var globalConfigType = typeof(GlobalConfig);
            foreach (var item in globalConfigType.GetFields())
            {
                RegisterSingleton(item.FieldType, () => item.GetValue(GetInstance<GlobalConfig>()));
            }
        }

        private static Type[] TickableExecutors => new Type[]
        {
            typeof(EntityCacheSystem),
            typeof(HexMapUpdateSystem),
            typeof(FramedComponentsSystem), // cleanups
            typeof(FlagDisabledSystem),
            typeof(CachePositionSystem),
            typeof(SetDestinationSystem),
            typeof(TargetSearchSystem), // cache target search
            typeof(SearchPositionSyncSystem), // sync Positon and TargetSearchPosition
            typeof(SquadMemberCounterSystem), // set count of members to squad
            typeof(SquadTargetSearchDistanceSystem), // set TargetSearchDistance of Squad
            typeof(SquadMemberDestinationSystem), // positioning of squad members
            typeof(SquadMemberSearchSyncSystem), // copies TargetSearch from squad to members
            typeof(DirectionMoveSystem), // moves entities with Direction (squads)
            typeof(FlyMoveSystem),
            typeof(SetSingleTargetSystem),
            typeof(SetMultiTargetSystem),

            typeof(SetTargetDestinationSystem), // sets destination to target or skips
            typeof(MoveSystem),
            typeof(SeparationSystem),
            typeof(HexMapCollisionSystem),
            typeof(SimpleHeightSystem),
            typeof(SimpleBehaviourSystem),
            typeof(SinglePreAttackSystem),
            typeof(MultiPreAttackSystem),
            typeof(SingleImpactSystem),
            typeof(RangedImpactSystem),
            typeof(ProjectileSystem),
            typeof(MultiImpactSystem),
            typeof(AutoHealSystem),
            typeof(DamageSystem),
            typeof(GoodsDropSystem),
            typeof(GoodsCollectorSystem),
            typeof(MarkDeadSystem),
            typeof(SquadUnitMergeSystem),
            typeof(SquadGoodsDistributionSystem), // distributes goods across active squad members
            typeof(ShipTransportSystem), // boarding and disembarking are applied after gameplay updates
        };

        private static Type[] CommandExecutors => new Type[]
        {
            typeof(LoadWorldCommandExecutor),
            typeof(SpawnSquadCommandExecutor),
            typeof(SpawnUnitCommandExecutor),
            typeof(JoystickCommandExecutor)
            //typeof(CommandLogger)
        };

        private static Type[] CommandValidators => new Type[]
        {
            typeof(FutureCommandValidator<InvalidateCommand>), typeof(LateCommandValidator<InvalidateCommand>), typeof(ZeroTickCommandValidator<InvalidateCommand>),
            typeof(FutureCommandValidator<JoystickCommand>), typeof(LateCommandValidator<JoystickCommand>), typeof(ZeroTickCommandValidator<JoystickCommand>),
            typeof(FutureCommandValidator<LoadWorldCommand>), typeof(LateCommandValidator<LoadWorldCommand>), typeof(ZeroTickCommandValidator<LoadWorldCommand>),
            typeof(FutureCommandValidator<SpawnSquadCommand>), typeof(LateCommandValidator<SpawnSquadCommand>), typeof(ZeroTickCommandValidator<SpawnSquadCommand>),
            typeof(SpawnUnitCommandValidator),
            typeof(FutureCommandValidator<SpawnUnitCommand>), typeof(LateCommandValidator<SpawnUnitCommand>), typeof(ZeroTickCommandValidator<SpawnUnitCommand>),
        };

        private static Type[] CommandMutators => new Type[]
        {
            typeof(SpawnCommandMutator),
        };

        private ISessionTransport CreateSessionTransport(SessionMode mode)
        {
            switch (mode)
            {
                case SessionMode.Client:
                    return new RiptideClientSessionTransport(GetInstance<Riptide.Client>());
                case SessionMode.Server:
                    return new RiptideServerSessionTransport(GetInstance<Riptide.Server>());
                default:
                    return new NullSessionTransport();
            }
        }

        protected void RegisterFactorySingleton<TService, TImplementation, TInstance>()
            where TImplementation : class, TService
            where TService : class, IFactory<TInstance>
        {
            RegisterSingleton<TService, TImplementation>();
            RegisterSingleton(typeof(TInstance), () => GetInstance<TService>().Create()!);
        }
    }
}
