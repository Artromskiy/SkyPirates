using Delta.ECS;
using DVG.SkyPirates.Shared;
using DVG.SkyPirates.Shared.Components;
using DVG.SkyPirates.Shared.Components.Runtime;
using DVG.SkyPirates.Shared.Core;
using DVG.SkyPirates.Shared.Components.Config;
using DVG.SkyPirates.Shared.Components.Framed;
using DVG.SkyPirates.Shared.Commands;
using DVG.SkyPirates.Shared.Data;
using DVG.SkyPirates.Shared.Ecs;
using DVG.SkyPirates.Shared.Factories;
using DVG.SkyPirates.Shared.IFactories;
using DVG.SkyPirates.Shared.IServices;
using DVG.SkyPirates.Shared.IServices.TickableExecutors;
using DVG.SkyPirates.Shared.Ids;
using DVG.SkyPirates.Shared.Services;
using DVG.SkyPirates.Shared.Services.CommandExecutors;
using DVG.SkyPirates.Shared.Services.CommandMutators;
using DVG.SkyPirates.Shared.Services.CommandValidators;
using DVG.SkyPirates.Shared.Services.Netcode;
using DVG.SkyPirates.Shared.Systems;
using DVG.SkyPirates.Shared.Systems.Special;
using SimpleInjector;
using System;
using Delta.Netcode;

namespace DVG.SkyPirates.Shared.DI
{
    public class SharedContainer : Container
    {
        public SharedContainer(SessionMode sessionMode = SessionMode.Local, bool registerCommandReceiver = true, World world = null)
        {
            Delta.Diagnostics.Trace.Info("[DI] SharedContainer Start");
            var currentWorld = world ?? new World(initialEntityCapacity: 1024);
            RegisterAllComponents(currentWorld);
            RegisterSingleton(() => currentWorld);
            RegisterSingleton<IWorldHistory>(() => new ChangeWorldHistory(
                GetInstance<World>(),
                WorldComponentIds.For(GetInstance<World>()).History,
                4,
                Constants.MaxHistoryTicks));

            RegisterSingleton<TimelineWriter>();

            RegisterSingleton(typeof(IEntityConfigFactory<>), typeof(EntityConfigFactory<>));
            RegisterSingleton(typeof(IConfigedEntityFactory<>), typeof(ConfigedEntityFactory<>));
            RegisterFactorySingleton<IGlobalConfigFactory, GlobalConfigFactory, GlobalConfig>();
            RegisterFactorySingleton<IPackedCirclesFactory, PackedCirclesFactory, PackedCirclesConfig>();
            RegisterSingleton<IEntityFactory, EntityFactory>();
            RegisterSingleton<IHexMapFactory, HexMapFactory>();
            RegisterSingleton<ISquadFactory, SquadFactory>();

            RegisterSingleton<IComponentDependenciesService, ComponentDependenciesService>();
            RegisterSingleton<IComponentDefaultsService, ComponentDefaultsService>();
            RegisterSingleton<IEntityRegistry, EntityRegistry>();

            RegisterSingleton<IPooledItemsProvider, PooledItemsProvider>();
            RegisterSingleton<ITargetSearchSystem, TargetSearchSystem>();

            RegisterSingleton<IHistorySystem, HistorySystem>();
            RegisterSingleton<IDisposeSystem, DisposeSystem>();
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
            RegisterSingleton<Delta.Netcode.ITransientSimulation<JoystickCommand>, SkyPiratesTransientSimulation>();

            Collection.Register<IDeltaTickableExecutor>(TickableExecutors, Lifestyle.Singleton);
            Collection.Register<ITransientDeltaTickableExecutor>(new[]
            {
                typeof(CachePositionSystem),
                typeof(DirectionMoveSystem),
                typeof(SquadMemberDestinationSystem),
                typeof(MoveSystem),
                typeof(SeparationSystem),
                typeof(HexMapCollisionSystem),
                typeof(SimpleHeightSystem),
            }, Lifestyle.Singleton);
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
            typeof(SquadGoodsDistributionSystem), // distributes goods across squad members
            typeof(MarkDeadSystem),
            //typeof(LogHashSumSystem),
        };

        private static Type[] CommandExecutors => new Type[]
        {
            typeof(LoadWorldCommandExecutor),
            typeof(SpawnSquadCommandExecutor),
            typeof(SpawnUnitCommandExecutor),
            typeof(SetEntityDataCommandExecutor),
            typeof(JoystickCommandExecutor)
            //typeof(CommandLogger)
        };

        private static Type[] CommandValidators => new Type[]
        {
            typeof(FutureCommandValidator<InvalidateCommand>), typeof(LateCommandValidator<InvalidateCommand>), typeof(ZeroTickCommandValidator<InvalidateCommand>),
            typeof(FutureCommandValidator<JoystickCommand>), typeof(LateCommandValidator<JoystickCommand>), typeof(ZeroTickCommandValidator<JoystickCommand>),
            typeof(FutureCommandValidator<LoadWorldCommand>), typeof(LateCommandValidator<LoadWorldCommand>), typeof(ZeroTickCommandValidator<LoadWorldCommand>),
            typeof(FutureCommandValidator<SetEntityDataCommand>), typeof(LateCommandValidator<SetEntityDataCommand>), typeof(ZeroTickCommandValidator<SetEntityDataCommand>),
            typeof(FutureCommandValidator<SpawnSquadCommand>), typeof(LateCommandValidator<SpawnSquadCommand>), typeof(ZeroTickCommandValidator<SpawnSquadCommand>),
            typeof(FutureCommandValidator<SpawnUnitCommand>), typeof(LateCommandValidator<SpawnUnitCommand>), typeof(ZeroTickCommandValidator<SpawnUnitCommand>),
        };

        private static Type[] CommandMutators => new Type[]
        {
            typeof(SpawnCommandMutator),
        };

        private static void RegisterAllComponents(World world)
        {
            ComponentRegistry layouts = world.Components;
            foreach (var registration in GeneratedComponentCatalog.GetRegistrations())
            {
                layouts.Register(registration);
            }
            _ = WorldComponentIds.For(world);
        }

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
