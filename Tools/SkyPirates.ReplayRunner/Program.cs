using Delta.Netcode;
using SkyPirates.Shared.Core;
using SkyPirates.Shared;
using SkyPirates.Shared.Commands;
using SkyPirates.Shared.Data;
using SkyPirates.Shared.DI;
using SkyPirates.Shared.IServices;
using SkyPirates.Shared.IServices.TickableExecutors;
using SkyPirates.Shared.Services.Netcode;
using SkyPirates.Shared.Services.Replay;
using SkyPirates.Shared.Tools.Json;
using SimpleInjector;
using System;
using System.Diagnostics;
using System.IO;
using System.Threading;

namespace SkyPirates.ReplayRunner
{
    internal static class Program
    {
        private static int Main(string[] args)
        {
            if (args.Length == 0 || args[0] == "--help" || args[0] == "-h")
            {
                PrintUsage();
                return args.Length == 0 ? 2 : 0;
            }

            try
            {
                RunnerOptions options = ParseOptions(args);
                SkyPiratesReplayData replay = SkyPiratesReplayFile.Load(options.ReplayPath);
                using var container = new ReplayContainer();

                IHistorySystem history = container.GetInstance<IHistorySystem>();
                WorldData initialMap = container.GetInstance<IPathFactory<WorldData>>().Create("Configs/Maps/Map1");
                history.ApplySnapshot(initialMap);
                history.SaveBaseline();

                var session = container.GetInstance<SkyPiratesSessionProvider>();
                var playback = new SkyPiratesReplayPlayback(history, session);
                playback.Load(replay);
                var replayRunner = new SkyPiratesReplayFrameRunner(
                    playback,
                    container.GetInstance<ITransientSimulation<JoystickCommand>>())
                {
                    Loop = options.Loop,
                };
                // Load rebuilds a fresh session and applies the recorded outcomes to it.
                long targetStep = options.TargetStep ?? playback.LastStep;
                if (targetStep < replay.InitialStep || targetStep > playback.LastStep)
                    throw new ArgumentOutOfRangeException(nameof(options.TargetStep), $"Replay range is {replay.InitialStep}..{playback.LastStep}.");

                using var cancellation = new CancellationTokenSource();
                ConsoleCancelEventHandler cancelHandler = (_, eventArgs) =>
                {
                    eventArgs.Cancel = true;
                    cancellation.Cancel();
                };
                if (options.Loop)
                {
                    Console.CancelKeyPress += cancelHandler;
                    Console.WriteLine("Looping replay; press Ctrl+C to stop.");
                }

                ReplayRunSummary summary;
                try
                {
                    summary = Run(playback, replayRunner, targetStep, options, cancellation.Token);
                }
                finally
                {
                    if (options.Loop)
                        Console.CancelKeyPress -= cancelHandler;
                }

                Console.WriteLine($"Replay stopped at step {summary.CurrentStep}; commands={replay.Commands.Count}.");
                PrintSummary(summary, options);
                if (summary.CurrentStep == replay.EndHashCheck.Step)
                {
                    bool matches = playback.VerifyHashAt(summary.CurrentStep, out ulong expected, out ulong actual);
                    Console.WriteLine($"Hash at step {summary.CurrentStep}: expected=0x{expected:X16}, actual=0x{actual:X16}.");
                    if (!matches)
                        return 1;
                }
                else
                {
                    Console.WriteLine("No hash check is stored for this step.");
                }

                return summary.Cancelled ? 130 : 0;
            }
            catch (Exception exception)
            {
                Console.Error.WriteLine(exception);
                return 1;
            }
        }

        private static RunnerOptions ParseOptions(string[] args)
        {
            var options = new RunnerOptions(args[0]);
            for (int i = 1; i < args.Length; i++)
            {
                switch (args[i])
                {
                    case "--to":
                        options.TargetStep = long.Parse(ReadOptionValue(args, ref i));
                        break;
                    case "--mode":
                        options.Mode = ParseMode(ReadOptionValue(args, ref i));
                        break;
                    case "--frames-per-tick":
                        options.FramesPerTick = int.Parse(ReadOptionValue(args, ref i));
                        options.FramesPerTickWasSpecified = true;
                        break;
                    case "--loop":
                        options.Loop = true;
                        break;
                    default:
                        throw new ArgumentException($"Unknown option '{args[i]}'.");
                }
            }

            if (options.FramesPerTick <= 0)
                throw new ArgumentOutOfRangeException(nameof(options.FramesPerTick), "Frames per tick must be greater than zero.");
            if (options.Mode != SkyPiratesReplayRunMode.RunFixedFps && options.FramesPerTickWasSpecified)
                throw new ArgumentException("--frames-per-tick can only be used with --mode RunFixedFps.");
            if (options.Loop && options.TargetStep.HasValue)
                throw new ArgumentException("--to cannot be combined with --loop.");

            return options;
        }

        private static string ReadOptionValue(string[] args, ref int index)
        {
            if (index + 1 >= args.Length)
                throw new ArgumentException($"Option '{args[index]}' requires a value.");

            return args[++index];
        }

        private static SkyPiratesReplayRunMode ParseMode(string value)
        {
            string mode = value.Replace("-", string.Empty).Replace("_", string.Empty);
            if (mode.Equals("fast", StringComparison.OrdinalIgnoreCase))
                return SkyPiratesReplayRunMode.Fast;
            if (mode.Equals("fixedfps", StringComparison.OrdinalIgnoreCase) || mode.Equals("runfixedfps", StringComparison.OrdinalIgnoreCase))
                return SkyPiratesReplayRunMode.RunFixedFps;
            if (mode.Equals("realfps", StringComparison.OrdinalIgnoreCase) || mode.Equals("runrealfps", StringComparison.OrdinalIgnoreCase))
                return SkyPiratesReplayRunMode.RunRealFps;

            throw new ArgumentException($"Unknown replay run mode '{value}'. Use Fast, RunFixedFps, or RunRealFps.");
        }

        private static ReplayRunSummary Run(
            SkyPiratesReplayPlayback playback,
            SkyPiratesReplayFrameRunner replayRunner,
            long targetStep,
            RunnerOptions options,
            CancellationToken cancellationToken)
        {
            Stopwatch totalTime = Stopwatch.StartNew();
            long initialStep = playback.CurrentStep;
            replayRunner.ResetStatistics();

            if (options.Mode == SkyPiratesReplayRunMode.Fast && !options.Loop)
            {
                playback.Seek(targetStep);
                totalTime.Stop();
                return new ReplayRunSummary(
                    targetStep - initialStep,
                    0,
                    0,
                    0,
                    playback.CurrentStep,
                    false,
                    totalTime.Elapsed);
            }

            while (!cancellationToken.IsCancellationRequested
                && (options.Loop || playback.CurrentStep < targetStep))
            {
                if (!replayRunner.TryAdvanceOneTick(options.Mode, options.FramesPerTick, out _))
                    break;
            }

            totalTime.Stop();
            SkyPiratesReplayRunStatistics statistics = replayRunner.Statistics;
            return new ReplayRunSummary(
                statistics.Ticks,
                statistics.TransientFrames,
                statistics.MinimumFramesPerTick,
                statistics.MaximumFramesPerTick,
                playback.CurrentStep,
                cancellationToken.IsCancellationRequested,
                totalTime.Elapsed);
        }

        private static void PrintSummary(ReplayRunSummary summary, RunnerOptions options)
        {
            Console.WriteLine($"Mode: {options.Mode}.");
            Console.WriteLine($"Ticks: {summary.TickCount}; frames: {summary.TotalFrames}; elapsed: {summary.Elapsed.TotalSeconds:F3}s.");
            if (summary.TickCount > 0 && options.Mode != SkyPiratesReplayRunMode.Fast)
            {
                double averageFrames = summary.TotalFrames / (double)summary.TickCount;
                Console.WriteLine($"Frames per replay tick: avg={averageFrames:F2}, min={summary.MinFramesPerTick}, max={summary.MaxFramesPerTick}.");
            }

            if (summary.Elapsed.TotalSeconds > 0)
                Console.WriteLine($"Replay tick throughput: {summary.TickCount / summary.Elapsed.TotalSeconds:F2} ticks/s.");
        }

        private static void PrintUsage()
        {
            Console.WriteLine("Usage: SkyPirates.ReplayRunner <replay.json> [--to <step>] [--mode <RunRealFps|RunFixedFps|Fast>] [--frames-per-tick <N>] [--loop]");
            Console.WriteLine("  Fast: seek directly to the requested replay step.");
            Console.WriteLine("  RunFixedFps: run exactly N transient frames per replay tick.");
            Console.WriteLine("  RunRealFps: run transient frames for one real replay-tick duration and report the count (default).");
            Console.WriteLine("  --loop: repeat the full replay until interrupted with Ctrl+C; cannot be combined with --to.");
        }

        private sealed class ReplayContainer : SharedContainer
        {
            public ReplayContainer() : base(SessionMode.Local)
            {
                RegisterSingleton(typeof(IPathFactory<>), typeof(FileResourcesFactory<>));
                Collection.Register<IInTickable>(Array.Empty<Type>(), Lifestyle.Singleton);
                Collection.Register<IPreTickable>(Array.Empty<Type>(), Lifestyle.Singleton);
                Collection.Register<IPostTickable>(Array.Empty<Type>(), Lifestyle.Singleton);
            }
        }

        private sealed class RunnerOptions
        {
            public string ReplayPath { get; }
            public long? TargetStep;
            public SkyPiratesReplayRunMode Mode = SkyPiratesReplayRunMode.RunRealFps;
            public int FramesPerTick = 1;
            public bool FramesPerTickWasSpecified;
            public bool Loop;

            public RunnerOptions(string replayPath) => ReplayPath = replayPath;
        }

        private readonly struct ReplayRunSummary
        {
            public long TickCount { get; }
            public long TotalFrames { get; }
            public int MinFramesPerTick { get; }
            public int MaxFramesPerTick { get; }
            public long CurrentStep { get; }
            public bool Cancelled { get; }
            public TimeSpan Elapsed { get; }

            public ReplayRunSummary(
                long tickCount,
                long totalFrames,
                int minFramesPerTick,
                int maxFramesPerTick,
                long currentStep,
                bool cancelled,
                TimeSpan elapsed)
            {
                TickCount = tickCount;
                TotalFrames = totalFrames;
                MinFramesPerTick = minFramesPerTick;
                MaxFramesPerTick = maxFramesPerTick;
                CurrentStep = currentStep;
                Cancelled = cancelled;
                Elapsed = elapsed;
            }
        }

        private sealed class FileResourcesFactory<T> : IPathFactory<T>
        {
            public T Create(string parameters)
            {
                string relativePath = parameters.Replace('/', Path.DirectorySeparatorChar) + ".json";
                string path = Path.Combine(AppContext.BaseDirectory, "Resources", relativePath);
                return SerializationUTF8.Deserialize<T>(File.ReadAllText(path));
            }
        }
    }
}
