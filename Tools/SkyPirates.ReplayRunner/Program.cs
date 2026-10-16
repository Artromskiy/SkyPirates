using Delta.Netcode;
using Delta.ECS;
using SlyPirates.Shared.Core;
using SlyPirates.Shared;
using SlyPirates.Shared.DI;
using SlyPirates.Shared.IServices.TickableExecutors;
using SlyPirates.Shared.Services.Netcode;
using SlyPirates.Shared.Services.Replay;
using SlyPirates.Shared.Tools.Json;
using System;
using System.IO;
using System.Diagnostics;
using System.Globalization;

namespace SlyPirates.ReplayRunner
{
    internal static class Program
    {
        private static int Main(string[] args)
        {
            if (args.Length == 0 || args[0] == "--help" || args[0] == "-h")
            {
                Console.WriteLine("Usage: SkyPirates.ReplayRunner <replay.json> [--to <step>] [--mode RunRealFps|RunFixedFps|Fast] [--frames-per-tick N] [--runs N] [--loop] [--speed N]");
                return args.Length == 0 ? 2 : 0;
            }
            try
            {
                var replay = SkyPiratesReplayFile.Load(args[0]);
                using var container = new ReplayContainer(checked((int)(replay.EndHashCheck.Step - replay.InitialStep + 1)));
                var session = container.GetInstance<SkyPiratesSessionProvider>();
                var playback = new SkyPiratesReplayPlayback(session,
                    container.GetInstance<SkyPiratesTransientSimulation>());
                playback.Load(replay);
                int runs = 1;
                bool loop = false;
                long? target = null;
                for (int i = 1; i < args.Length; i++)
                {
                    switch (args[i])
                    {
                        case "--to": target = long.Parse(args[++i], CultureInfo.InvariantCulture); break;
                        case "--mode": playback.Mode = Enum.Parse<ReplayRunMode>(args[++i], true); break;
                        case "--frames-per-tick": playback.FramesPerTick = int.Parse(args[++i], CultureInfo.InvariantCulture); break;
                        case "--runs": runs = int.Parse(args[++i], CultureInfo.InvariantCulture); break;
                        case "--loop": loop = true; break;
                        case "--speed": playback.Speed = double.Parse(args[++i], CultureInfo.InvariantCulture); break;
                        default: throw new ArgumentException($"Unknown argument: {args[i]}");
                    }
                }
                if (runs <= 0) throw new ArgumentOutOfRangeException(nameof(runs));
                if (target.HasValue)
                {
                    playback.Seek(target.Value);
                    return Verify(playback, target.Value);
                }
                bool cancelled = false;
                Console.CancelKeyPress += (_, e) => { e.Cancel = true; cancelled = true; };
                playback.Playing = true;
                playback.Loop = loop || runs > 1;
                var clock = Stopwatch.StartNew();
                double previous = 0;
                int completed = 0;
                long frames = 0;
                while (playback.Playing && !cancelled)
                {
                    double now = clock.Elapsed.TotalSeconds;
                    playback.Update(now - previous);
                    previous = now;
                    frames++;
                    int finished = playback.CompletedLoops + (playback.Playing ? 0 : 1);
                    if (finished <= completed) continue;
                    completed = finished;
                    if (Verify(playback, playback.LastStep) != 0) return 1;
                    Console.WriteLine($"Run {completed}: mode={playback.Mode}, frames={frames}, elapsed={clock.Elapsed.TotalMilliseconds:F2} ms.");
                    if (!loop && completed == runs - 1) playback.Loop = false;
                }
                return 0;
            }
            catch (Exception exception)
            {
                Console.Error.WriteLine(exception);
                return 1;
            }
        }

        private static int Verify(SkyPiratesReplayPlayback playback, long step)
        {
            bool matches = playback.VerifyHashAt(step, out ulong expected, out ulong actual);
            if (step != playback.LastStep)
            {
                Console.WriteLine($"Replay reached step {step}; no final hash check at this step.");
                return 0;
            }
            Console.WriteLine($"Hash at step {step}: expected=0x{expected:X16}, actual=0x{actual:X16}.");
            return matches ? 0 : 1;
        }

        private sealed class ReplayContainer : SharedContainer
        {
            public ReplayContainer(int historyCapacity) : base(SessionMode.Local, historyCapacity: historyCapacity)
            {
                RegisterSingleton(typeof(IPathFactory<>), typeof(FileResourcesFactory<>));
                Collection.Register<IPreTickable>(Array.Empty<Type>());
                Collection.Register<IPostTickable>(Array.Empty<Type>());
                Collection.Register<IInTickable>(Array.Empty<Type>());
            }
        }

        private sealed class FileResourcesFactory<T> : IPathFactory<T>
        {
            private readonly World _world;
            public FileResourcesFactory(World world) => _world = world;
            public T Create(string parameters)
            {
                string relativePath = parameters.Replace('/', Path.DirectorySeparatorChar) + ".json";
                string path = Path.Combine(AppContext.BaseDirectory, "Resources", relativePath);
                return SerializationUTF8.Deserialize<T>(File.ReadAllText(path), _world);
            }
        }
    }
}
