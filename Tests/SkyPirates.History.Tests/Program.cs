using System;
using System.Collections.Generic;
using System.Buffers;
using System.Diagnostics;
using System.IO;
using System.Collections.Immutable;
using SlyPirates.Shared.Components.Framed;
using SlyPirates.Shared.Ids;
using SlyPirates.Shared.Systems;
using Delta;
using Delta.ECS;
using Delta.Netcode;
using SlyPirates.Shared;
using SlyPirates.Shared.Commands;
using SlyPirates.Shared.Components;
using SlyPirates.Shared.Components.Config;
using SlyPirates.Shared.Components.Runtime;
using SlyPirates.Shared.Core;
using SlyPirates.Shared.Data;
using SlyPirates.Shared.DI;
using SlyPirates.Shared.Ecs;
using SlyPirates.Shared.IServices;
using SlyPirates.Shared.IServices.TickableExecutors;
using SlyPirates.Shared.Services.Netcode;
using SlyPirates.Shared.Services.Replay;
using SlyPirates.Shared.Tools.Json;
using SimpleInjector;

namespace SkyPirates.History.Tests;

[ComponentSchema(0x1D6D01E67FBAB901UL), ComponentAlias("HistoryTestResource"), ComponentGroup(Group.All)]
public struct TestResource : IDisposable
{
    public Counter Counter;
    public void Dispose() => Counter.Disposals++;
}
public sealed class Counter { public int Disposals; }

internal static class Program
{
    private static int _checks;
    private static void Check(bool condition, string message)
    {
        _checks++;
        if (!condition) throw new InvalidOperationException(message);
    }
    private static void Throws(Action action, string message)
    {
        try { action(); } catch (InvalidOperationException) { _checks++; return; }
        throw new InvalidOperationException(message);
    }
    private static WorldData Initial(int id = 1, int health = 31)
    {
        var data = new WorldData();
        data.Get<SyncId>()[id] = new SyncId { Value = id };
        data.Get<Alive>()[id] = new Alive();
        data.Get<Health>()[id] = new Health { Value = health };
        data.Get<Position>()[id] = new Position { Value = new fix3(1, 2, 3) };
        return data;
    }
    private static Entity Find(World world, int id)
    {
        Entity result = default;
        Query query = world.WhereAll<SyncId>();
        world.ForEachEntity(in query, (EntityRef entity, in SyncId syncId) =>
        {
            if (syncId.Value == id) result = entity;
        }).Invoke();
        return result;
    }
    private static IWorldHistory History(World world, int capacity)
    {
        var resource = world.Components.Register(GeneratedComponentCatalog.GetRegistration<TestResource>());
        world.Components.AddGroup(Group.All, resource);
        world.Components.BindInterface<TestResource, IDisposable>();
        ComponentId[] ids = world.GetGroup(Group.History).ToArray();
        return new WorldHistory(world, ids, Math.Min(2, capacity), capacity);
    }
    private static void Retire(World world, Entity entity)
    {
        world.Add<PendingDestroy>(entity);
        world.Remove<Alive>(entity);
    }
    private static void Lifecycle()
    {
        using World world = WorldFactory.Create();
        using IWorldHistory history = History(world, 4);
        history.ApplySnapshot(Initial(), 0);
        Entity entity = Find(world, 1);
        var counter = new Counter();
        world.Add(entity, new TestResource { Counter = counter });
        world.GetRef<Health>(entity).Value = 13;
        history.Save(1);
        Retire(world, entity);
        history.Save(2);
        Check(!history.GetSnapshot(2).Get<SyncId>().ContainsKey(1), "Tombstone must not be exported.");
        Check(world.IsAlive(entity) && counter.Disposals == 0, "Retain resources while a pre-deletion frame is reachable.");
        history.GoTo(1);
        Check(!world.Has<PendingDestroy>(entity) && world.Has<Alive>(entity), "Restore lifecycle before deletion.");
        Check(world.Get<Health>(entity).Value == (fix)13, "Restore existing values.");
        Check(history.NewestTick == 2, "GoTo must preserve the future.");
        history.GoTo(2);
        Check(world.Has<PendingDestroy>(entity) && !world.Has<Alive>(entity), "Restore logical absence.");
        history.Save(3);
        Check(world.IsAlive(entity), "Still held by tick 0 or 1.");
        history.Save(4);
        Check(world.IsAlive(entity), "Tick 1 still holds the entity.");
        history.Save(5);
        Check(!world.IsAlive(entity) && counter.Disposals == 1, $"Release exactly once after the last presence expires: alive={world.IsAlive(entity)}, disposals={counter.Disposals}.");
        history.Save(6);
        Check(counter.Disposals == 1, "Resource must not be disposed twice.");
        Entity replacement = world.Create<SyncId>();
        Check(replacement.Index == entity.Index && replacement.Generation != entity.Generation, "ECS reuses index with a new generation.");
        history.ApplySnapshot(Initial(3), 10);
        Check(history.OldestTick == 10 && history.NewestTick == 10, "Import creates exactly one initial frame.");
        Check(!world.IsAlive(replacement), "Import frees entities missing from the snapshot.");
        Check(Find(world, 3).IsValid, "Import into populated world creates missing entities.");
        history.Clear();
        history.Save(11);
        Check(history.GetSnapshot(11).Get<Health>()[3].Value == (fix)31, "Clear followed by Save must seed unchanged data.");
    }
    private static void FutureAndRollback()
    {
        using World world = WorldFactory.Create();
        using IWorldHistory history = History(world, 8);
        history.ApplySnapshot(Initial(), 0);
        Entity existing = Find(world, 1);
        world.GetRef<Health>(existing).Value = 17;
        Entity future = world.Create(new[] { world.Components.GetPrimary<SyncId>(), world.Components.GetPrimary<Alive>(), world.Components.GetPrimary<Health>() });
        world.GetRef<SyncId>(future).Value = 2;
        world.GetRef<Health>(future).Value = 20;
        var resource = new Counter();
        world.Add(future, new TestResource { Counter = resource });
        history.Save(1);
        history.GoTo(0);
        Check(world.IsAlive(future) && world.Has<PendingDestroy>(future), "Future descriptor survives a GoTo.");
        history.GoTo(1);
        Check(world.Get<Health>(future).Value == (fix)20, "Future values survive a GoTo.");
        Check(world.Get<Health>(existing).Value == (fix)17, "Future changes restored.");
        history.Rollback(0);
        Check(history.NewestTick == 0 && !world.IsAlive(future) && resource.Disposals == 1, "Rollback drops future and reclaims its resources.");
        world.GetRef<Health>(existing).Value = 9;
        history.Save(1);
        Check(history.GetSnapshot(1).Get<Health>()[1].Value == (fix)9, "Branch recomputes after Rollback.");
        history.ApplySnapshot(Initial(1, 7), 10);
        Check(Find(world, 1) == existing && world.Get<Health>(existing).Value == (fix)7, "Snapshot reuses descriptors without keeping stale values.");
    }
    private static void GameplayRemoval()
    {
        using World world = WorldFactory.Create();
        world.GetOrCreateEntityIdMap(128);
        var cache = new EntityCacheSystem(world);
        using IWorldHistory history = History(world, 8);
        var data = Initial();
        data.Get<Health>()[1] = new Health { Value = 0 };
        history.ApplySnapshot(data, 0);
        Entity entity = Find(world, 1);
#pragma warning disable CS0612
        new MarkDeadSystem(world).Tick(1, Constants.TickTime);
#pragma warning restore CS0612
        history.Save(1);
        Check(world.Has<PendingDestroy>(entity), "Death system requests logical destruction.");
        history.Rollback(0);
        Check(world.Has<Alive>(entity) && !world.Has<PendingDestroy>(entity), "Death rollback.");

        data = Initial();
        data.Get<Position>()[1] = new Position();
        data.Get<SyncId>()[2] = new SyncId { Value = 2 };
        data.Get<Alive>()[2] = new Alive();
        data.Get<Position>()[2] = new Position();
        data.Get<Projectile>()[2] = new Projectile { Target = new SyncId { Value = 1 } };
        data.Get<DesiredPosition>()[2] = new DesiredPosition();
        data.Get<Speed>()[2] = new Speed { Value = 10 };
        data.Get<Damage>()[2] = new Damage { Value = 3 };
        history.ApplySnapshot(data, 0);
        cache.Synchronize();
        Entity bullet = Find(world, 2);
        var projectiles = new ProjectileSystem(world);
        projectiles.Tick(1, Constants.TickTime);
        history.Save(1);
        Check(world.Has<PendingDestroy>(bullet), "Projectile completion requests destruction.");
        history.Rollback(0);
        cache.Synchronize();
        world.Remove<RecivedDamage>(Find(world, 1));
        projectiles.Tick(1, Constants.TickTime);
        history.Save(1);
        Check(world.Has<PendingDestroy>(bullet), "Projectile completion replays after rollback.");

        data = Initial();
        data.Get<Position>()[1] = new Position();
        data.Get<GoodsDrop>()[1] = new GoodsDrop { Values = ImmutableSortedDictionary<GoodsId, int>.Empty };
        data.Get<GoodsCollectorRadius>()[1] = new GoodsCollectorRadius { Value = 2 };
        data.Get<SyncId>()[2] = new SyncId { Value = 2 };
        data.Get<Alive>()[2] = new Alive();
        data.Get<Position>()[2] = new Position();
        data.Get<GoodsId>()[2] = new GoodsId("HistoryTestGoods");
        data.Get<GoodsAmount>()[2] = new GoodsAmount { Value = 5 };
        data.Get<MaxSpeed>()[2] = new MaxSpeed { Value = 1 };
        history.ApplySnapshot(data, 0);
        cache.Synchronize();
        Entity item = Find(world, 2);
        var collector = new GoodsCollectorSystem(world);
        collector.Tick(1, Constants.TickTime);
        history.Save(1);
        Check(world.Has<PendingDestroy>(item), "Collection requests destruction.");
        history.Rollback(0);
        cache.Synchronize();
        Check(world.Has<Alive>(item) && world.Get<GoodsDrop>(Find(world, 1)).Values.Count == 0, "Collection rollback restores both item and inventory.");
        collector.Tick(1, Constants.TickTime);
        history.Save(1);
        Check(world.Has<PendingDestroy>(item), "Collection replays after rollback.");

        data = new WorldData();
        for (int id = 1; id <= 3; id++)
        {
            data.Get<SyncId>()[id] = new SyncId { Value = id };
            data.Get<Alive>()[id] = new Alive();
            data.Get<Health>()[id] = new Health { Value = 10 };
            data.Get<MaxHealth>()[id] = new MaxHealth { Value = 10 };
            data.Get<Damage>()[id] = new Damage { Value = 2 };
            data.Get<UnitId>()[id] = UnitId.Constants.Buccaneer;
            data.Get<SquadMember>()[id] = new SquadMember { SquadId = new SyncId { Value = 10 } };
        }
        history.ApplySnapshot(data, 0);
        var merge = new SquadUnitMergeSystem(world);
        merge.Tick(1, Constants.TickTime);
        history.Save(1);
        Check(history.GetSnapshot(1).Get<SyncId>().Count == 1, "Merge retires two entities.");
        history.Rollback(0);
        Check(history.GetSnapshot(0).Get<SyncId>().Count == 3, "Merge rollback restores three members.");
        merge.Tick(1, Constants.TickTime);
        history.Save(1);
        Check(history.GetSnapshot(1).Get<SyncId>().Count == 1, "Merge replays after rollback.");
    }

    private static void MassRemoval()
    {
        using World world = WorldFactory.Create();
        using IWorldHistory history = History(world, 16);
        var data = new WorldData();
        for (int i = 1; i <= 2000; i++)
        {
            data.Get<SyncId>()[i] = new SyncId { Value = i };
            data.Get<Alive>()[i] = new Alive();
            data.Get<Health>()[i] = new Health { Value = i };
        }
        history.ApplySnapshot(data, 0);
        var query = world.WhereAll<SyncId>();
        var entities = new List<Entity>();
        world.ForEachEntity(in query, entity => entities.Add(entity)).Invoke();
        world.Add<PendingDestroy>(entities.ToArray());
        world.Remove<Alive>(entities.ToArray());
        var timer = Stopwatch.StartNew();
        for (int tick = 1; tick <= 16; tick++) history.Save(tick);
        timer.Stop();
        Check(!world.IsAlive(entities[0]) && !world.IsAlive(entities[^1]), "Batch retirement expires all descriptors.");
        Console.WriteLine($"WorldHistory: 2000 removals + retention expiry in {timer.Elapsed.TotalMilliseconds:F2} ms.");
    }
    private sealed class GameContainer : SharedContainer
    {
        public GameContainer(int capacity) : base(SessionMode.Local)
        {
            Options.AllowOverridingRegistrations = true;
            RegisterSingleton<IWorldHistory>(() => History(GetInstance<World>(), capacity));
            RegisterSingleton(typeof(IPathFactory<>), typeof(ResourcesFactory<>));
            Collection.Register<IPreTickable>(Array.Empty<Type>());
            Collection.Register<IPostTickable>(Array.Empty<Type>());
            Collection.Register<IInTickable>(Array.Empty<Type>());
        }
    }
    private sealed class ResourcesFactory<T> : IPathFactory<T>
    {
        private readonly World _world;
        public ResourcesFactory(World world) => _world = world;
        public T Create(string path) => SerializationUTF8.Deserialize<T>(File.ReadAllText(Path.Combine(AppContext.BaseDirectory, "Resources", path + ".json")), _world);
    }
    private static SkyPiratesReplayData Record(int capacity)
    {
        using var game = new GameContainer(capacity);
        var session = game.GetInstance<SkyPiratesSessionProvider>();
        var history = game.GetInstance<IWorldHistory>();
        var map = game.GetInstance<IPathFactory<WorldData>>().Create("Configs/Maps/Map1");
        int wounded = 981;
        map.Get<Health>()[wounded] = new Health { Value = 7 };
        session.ApplySnapshot(map, -1);
        session.Start(new AuthorId(1));
        var simulation = game.GetInstance<SkyPiratesTransientSimulation>();
        // Initial snapshot is authoritative: defaults must not turn wounded units into full-health units.
        var world = game.GetInstance<World>();
        Entity unit = Find(world, 981);
        if (unit.IsValid && world.Has<Health>(unit))
            Check(world.Get<Health>(unit).Value == map.Get<Health>()[981].Value, "Defaulting must preserve imported Health.");
        simulation.Tick(0);
        var recorder = new SkyPiratesReplayRecorder(session.Session, history, history.GetSnapshot(0), 0);
        session.Send(new SpawnSquadCommand(), 1);
        var input = default(JoystickCommand);
        for (int tick = 0; tick <= 20; tick++)
        {
            simulation.AdvanceFrame(tick, 0, in input);
            simulation.AdvanceFrame(tick, 0.025, in input);
            simulation.AdvanceFrame(tick, 0.05, in input);
        }
        var result = recorder.Capture();
        Check(result.Commands.Count == 1, "The recording must contain an accepted spawn command.");
        return result;
    }
    private static void Replay(SkyPiratesReplayData data)
    {
        using var game = new GameContainer(checked((int)(data.EndHashCheck.Step - data.InitialStep + 1)));
        var session = game.GetInstance<SkyPiratesSessionProvider>();
        var history = game.GetInstance<IWorldHistory>();
        var replay = new SkyPiratesReplayPlayback(session, game.GetInstance<SkyPiratesTransientSimulation>());
        replay.Load(data);
        replay.Seek(replay.LastStep);
        Check(replay.VerifyHashAt(replay.LastStep, out var expected, out var actual), $"Replay hash: {expected:X16} != {actual:X16}");
        replay.Seek(replay.FirstStep);
        Check(history.NewestTick == replay.LastStep, "Replay seek preserves computed future.");
        replay.Seek(replay.LastStep);
        Check(replay.VerifyHashAt(replay.LastStep, out _, out _), "Second traversal hash.");
        Throws(() => session.Send(new SpawnSquadCommand(), replay.LastStep + 1), "Replay must reject additional commands.");
        replay.Seek(replay.FirstStep);
        replay.Mode = ReplayRunMode.RunFixedFps;
        replay.FramesPerTick = 4;
        replay.Playing = true;
        int frames = 0;
        while (replay.Playing && frames++ < 1000) replay.Update(0.001);
        Check(frames == (replay.LastStep - replay.FirstStep) * 4, "Fixed FPS must produce N microframes per fixed tick.");
        Check(replay.VerifyHashAt(replay.LastStep, out _, out _), "Fixed FPS hash.");
        replay.Seek(replay.FirstStep);
        replay.Mode = ReplayRunMode.RunRealFps;
        replay.Playing = true;
        while (replay.Playing) replay.Update(1d / 73);
        Check(replay.VerifyHashAt(replay.LastStep, out _, out _), "Real FPS hash.");
        replay.Loop = true;
        replay.Playing = true;
        replay.Update(1);
        Check(replay.CompletedLoops == 1 && replay.CurrentStep == replay.FirstStep && history.NewestTick == replay.LastStep, "Loop uses the initial cached frame.");
        replay.Playing = false;
        long paused = replay.CurrentStep;
        replay.Update(1);
        Check(replay.CurrentStep == paused, "Pause must not simulate.");
    }
    private static void FirstPassModes(SkyPiratesReplayData data)
    {
        foreach (ReplayRunMode mode in Enum.GetValues<ReplayRunMode>())
        {
            using var game = new GameContainer(checked((int)(data.EndHashCheck.Step - data.InitialStep + 1)));
            var replay = new SkyPiratesReplayPlayback(game.GetInstance<SkyPiratesSessionProvider>(),
                game.GetInstance<SkyPiratesTransientSimulation>());
            replay.Load(data);
            replay.Mode = mode;
            replay.FramesPerTick = 7;
            replay.Playing = true;
            int frames = 0;
            while (replay.Playing && frames++ < 10000) replay.Update(1d / 73);
            Check(!replay.Playing && replay.VerifyHashAt(replay.LastStep, out _, out _), $"First pass {mode} produces the ordinary simulation hash.");
            if (mode == ReplayRunMode.RunFixedFps)
                Check(frames == (replay.LastStep - replay.FirstStep) * 7, "Non-binary frame fractions must not produce an extra final frame.");
            replay.Load(data);
            replay.Seek(replay.LastStep);
            Check(replay.VerifyHashAt(replay.LastStep, out _, out _), "Loading another recording replaces the sealed session.");
        }
    }

    private static void CommandRollback()
    {
        using var game = new GameContainer(64);
        var session = game.GetInstance<SkyPiratesSessionProvider>();
        var history = game.GetInstance<IWorldHistory>();
        var simulation = game.GetInstance<SkyPiratesTransientSimulation>();
        var map = game.GetInstance<IPathFactory<WorldData>>().Create("Configs/Maps/Map1");
        session.ApplySnapshot(map, -1);
        session.Start(new AuthorId(1));
        simulation.Tick(1);
        var recorder = new SkyPiratesReplayRecorder(session.Session, history, history.GetSnapshot(1), 1);
        var key = session.Send(new SpawnSquadCommand(), 2);
        simulation.Tick(8);
        var recording = recorder.Capture();
        Check(recording.Commands.Count == 1, "Rollback test requires an accepted command.");
        ulong withCommand = SerializationUTF8.GetHashSum(session.GetSnapshot(8));
        Check(session.Session.Cancel(key) == CommandResult.Cancelled, "Live commands can be cancelled.");
        simulation.Tick(8);
        ulong withoutCommand = SerializationUTF8.GetHashSum(session.GetSnapshot(8));
        Check(withCommand != withoutCommand, "Cancelling a past command recomputes the branch.");
        Check(session.ComputedStep == 8 && session.CurrentStep == 8, "Recomputation advances the fixed frontier.");
        var command = recording.Commands[0];
        var header = new CommandHeader(new CommandKey(session.Session.Start.SessionId, new AuthorId(command.AuthorId), 999), command.TypeId, command.Step, command.Order);
        session.ApplyReplayCommand(header, command.Payload);
        simulation.Tick(8);
        Check(SerializationUTF8.GetHashSum(session.GetSnapshot(8)) == withCommand, "Re-adding the same command reproduces its fixed state.");
        simulation.Tick(0);
        Check(session.ComputedStep == 8, "Moving the display cursor preserves the frontier.");
        simulation.Tick(8);
        Check(SerializationUTF8.GetHashSum(session.GetSnapshot(8)) == withCommand, "Cached forward traversal keeps the result.");
        int restores = 0;
        session.StateRestored += () => restores++;
        var state = new ArrayBufferWriter<byte>();
        var model = game.GetInstance<SkyPiratesSessionSetup>().Model;
        model.Save(state);
        model.Load(state.WrittenSpan);
        Check(restores == 1 && game.GetInstance<IWorldHistory>().OldestTick == 8 && SerializationUTF8.GetHashSum(session.GetSnapshot(8)) == withCommand,
            "Network model snapshot loading prepares gameplay and notifies the view binder.");
    }

    private static void ArbitraryNavigation()
    {
        using var world = WorldFactory.Create();
        using var history = History(world, 5);
        history.ApplySnapshot(Initial(), 0);
        var random = new Random(417);
        int nextId = 2;
        for (int tick = 1; tick <= 60; tick++)
        {
            if (tick % 3 == 0)
            {
                Entity born = world.Create(new[] { world.Components.GetPrimary<SyncId>(), world.Components.GetPrimary<Alive>(), world.Components.GetPrimary<Health>() });
                world.GetRef<SyncId>(born).Value = nextId++;
                world.GetRef<Health>(born).Value = tick;
            }
            var entities = new List<Entity>();
            var query = world.WhereAll<SyncId>().WhereNone<PendingDestroy>();
            world.ForEachEntity(in query, (EntityRef entity) => entities.Add(entity)).Invoke();
            foreach (Entity entity in entities)
            {
                int action = random.Next(6);
                if (action == 0) world.Remove<Health>(entity);
                else if (action == 1) { world.Add<Health>(entity); world.GetRef<Health>(entity).Value = random.Next(50); }
                else if (action == 2) Retire(world, entity);
            }
            history.Save(tick);
            if (tick % 4 == 0 && entities.Count > 0 && world.IsAlive(entities[0]) && !world.Has<PendingDestroy>(entities[0]))
            {
                world.Add<Health>(entities[0]);
                world.GetRef<Health>(entities[0]).Value = 73;
                history.Save(tick); // Replacing the newest frame must preserve its original before-values.
            }
            for (int visit = 0; visit < 4; visit++)
            {
                int target = random.Next(history.OldestTick, tick + 1);
                WorldData expected = history.GetSnapshot(target);
                history.GoTo(target);
                AssertCurrent(world, expected);
            }
            history.GoTo(tick);
            AssertCurrent(world, history.GetSnapshot(tick));
        }
        history.GoTo(60);
        AssertCurrent(world, history.GetSnapshot(60));
    }

    private static void AssertCurrent(World world, WorldData expected)
    {
        int count = 0;
        var query = world.WhereAll<SyncId>().WhereNone<PendingDestroy>();
        world.ForEachEntity(in query, (EntityRef entity, in SyncId id) =>
        {
            count++;
            Check(expected.Get<SyncId>().ContainsKey(id.Value), "Restored presence matches the saved frame.");
            bool hasHealth = expected.Get<Health>().TryGetValue(id.Value, out Health health);
            Check(entity.Has<Health>() == hasHealth && (!hasHealth || world.Get<Health>(entity).Value == health.Value), "Restored component membership and value.");
        }).Invoke();
        Check(count == expected.Get<SyncId>().Count, "Restoration does not lose logical entities.");
    }

    private static void HistoricalResources()
    {
        using var world = WorldFactory.Create();
        world.Components.AddGroup(Group.History, world.Components.GetPrimary<TestResource>());
        using var history = History(world, 4);
        history.ApplySnapshot(Initial(), 0);
        Entity entity = Find(world, 1);
        var first = new Counter();
        var second = new Counter();
        world.Add(entity, new TestResource { Counter = first });
        history.Save(0);
        world.GetRef<TestResource>(entity) = new TestResource { Counter = second };
        history.Save(1);
        history.GoTo(0);
        Check(ReferenceEquals(world.Get<TestResource>(entity).Counter, first) && first.Disposals == 0 && second.Disposals == 0, "Resource ownership survives GoTo and component replacement.");
        history.GoTo(1);
        history.Clear();
        Check(first.Disposals == 1 && second.Disposals == 0, $"Clear releases obsolete resource values, keeping the attached resource: {first.Disposals}/{second.Disposals}.");
        history.Save(3);
        world.Remove<TestResource>(entity);
        history.Save(4);
        history.GoTo(3);
        Check(world.Has<TestResource>(entity) && second.Disposals == 0, "Removed resource component can be restored.");
        history.GoTo(4);
        Retire(world, entity);
        for (int tick = 5; tick <= 8; tick++) history.Save(tick);
        Check(!world.IsAlive(entity) && first.Disposals == 1 && second.Disposals == 1, "A removed historical resource is disposed once at final entity release.");
    }

    private static void DerivedCaches()
    {
        using var game = new GameContainer(8);
        var session = game.GetInstance<SkyPiratesSessionProvider>();
        var world = game.GetInstance<World>();
        session.ApplySnapshot(game.GetInstance<IPathFactory<WorldData>>().Create("Configs/Maps/Map1"), -1);
        session.Start(new AuthorId(1));
        var simulation = game.GetInstance<SkyPiratesTransientSimulation>();
        simulation.Tick(0);
        Entity tileEntity = world.WhereAll<HexTile>().First().Invoke();
        HexTile tile = world.Get<HexTile>(tileEntity);
        SyncId id = world.Get<SyncId>(tileEntity);
        var coordinate = new int3(tile.Coordinates.x, tile.Elevation, tile.Coordinates.y);
        HexMap map = world.Get<HexMap>(world.WhereAll<HexMap>().First().Invoke());
        Check(map.Data.ContainsKey(coordinate), "Map projection is ready after import.");
        Retire(world, tileEntity);
        game.GetInstance<IWorldHistory>().Save(1);
        game.GetInstance<EntityCacheSystem>().Synchronize();
        foreach (IDeltaTickableExecutor system in game.GetAllInstances<IDeltaTickableExecutor>())
            if (system is HexMapUpdateSystem) system.Tick(1, Constants.TickTime);
        Check(!world.GetEntityIdMap().TryGetEntity(id, out _) && !map.Data.ContainsKey(coordinate), "Pending entities leave both derived caches.");
        simulation.Tick(0);
        Check(world.GetEntityIdMap().GetEntity(id) == tileEntity && map.Data.ContainsKey(coordinate), "GoTo restores ID and map projections using the same descriptor.");
        long old = game.GetInstance<IWorldHistory>().OldestTick;
        var model = game.GetInstance<SkyPiratesSessionSetup>().Model;
        long allowed = old + 1;
        Check(!model.TrySchedule(ref old) && model.TrySchedule(ref allowed), "Command scheduling follows actual retained history.");
    }

    private static void RestoredGameplayProjections()
    {
        using var game = new GameContainer(8);
        var session = game.GetInstance<SkyPiratesSessionProvider>();
        var world = game.GetInstance<World>();
        WorldData data = Initial();
        data.Get<Squad>()[1] = new Squad();
        data.Get<ActivityRange>()[1] = new ActivityRange { Value = 1000 };
        for (int i = 2; i <= 3; i++)
        {
            data.Get<SyncId>()[i] = new SyncId { Value = i };
            data.Get<Alive>()[i] = new Alive();
            data.Get<SquadMember>()[i] = new SquadMember { SquadId = new SyncId { Value = 1 } };
        }
        data.Get<Target>()[2] = new Target { SyncId = new SyncId { Value = 3 } };
        session.ApplySnapshot(data, -1);
        session.Start(new AuthorId(1));
        Entity squad = Find(world, 1);
        Entity member = Find(world, 2);
        Check(world.Get<SquadMemberCount>(squad).Value == 2 && world.Get<Target>(member).SyncId?.Value == 3,
            "Import rebuilds the nonhistorical squad count without clearing a historical temporary target.");
        var simulation = game.GetInstance<SkyPiratesTransientSimulation>();
        simulation.Tick(0);
        Retire(world, Find(world, 3));
        simulation.Tick(1);
        world.GetRef<SquadMemberCount>(squad) = 999;
        world.Add(member, new RecivedDamage { Value = 17 });
        simulation.Tick(-1);
        Check(world.Get<SquadMemberCount>(squad).Value == 2 && world.Get<RecivedDamage>(member).Value == fix.Zero,
            "Rewind clears temporary effects and recomputes the restored squad membership.");
        Check(world.Get<Target>(member).SyncId?.Value == 3, "Restore preserves components belonging to both History and Temporary.");
        simulation.Tick(1);
        Check(world.Get<SquadMemberCount>(squad).Value == 1, "Cached forward navigation recomputes the later squad membership.");
    }

    private static void Main()
    {
        Lifecycle();
        HistoricalResources();
        ArbitraryNavigation();
        FutureAndRollback();
        GameplayRemoval();
        DerivedCaches();
        RestoredGameplayProjections();
        MassRemoval();
        var shortWindow = Record(4);
        var longWindow = Record(64);
        Check(shortWindow.EndHashCheck.Hash == longWindow.EndHashCheck.Hash, "Retention must not affect the game result.");
        Replay(longWindow);
        FirstPassModes(longWindow);
        CommandRollback();
        SkyPiratesReplayFile.Save(Path.Combine(Path.GetTempPath(), "skypirates-history-validation-replay.json"), longWindow);
        Console.WriteLine($"PASS: {_checks} checks for WorldHistory.");
    }
}
