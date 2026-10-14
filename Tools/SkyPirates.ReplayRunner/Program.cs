using Delta.Netcode;
using SkyPirates.Shared.Core;
using SkyPirates.Shared;
using SkyPirates.Shared.Data;
using SkyPirates.Shared.DI;
using SkyPirates.Shared.IServices;
using SkyPirates.Shared.IServices.TickableExecutors;
using SkyPirates.Shared.Services.Netcode;
using SkyPirates.Shared.Services.Replay;
using SkyPirates.Shared.Tools.Json;
using SimpleInjector;
using System;
using System.IO;

namespace SkyPirates.ReplayRunner
{
    internal static class Program
    {
        private static int Main(string[] args)
        {
            if (args.Length == 0 || args[0] == "--help" || args[0] == "-h")
            {
                Console.WriteLine("Usage: SkyPirates.ReplayRunner <replay.json> [--to <step>]");
                return args.Length == 0 ? 2 : 0;
            }

            try
            {
                long? requestedStep = ReadTargetStep(args);
                SkyPiratesReplayData replay = SkyPiratesReplayFile.Load(args[0]);
                using var container = new ReplayContainer();

                IHistorySystem history = container.GetInstance<IHistorySystem>();
                WorldData initialMap = container.GetInstance<IPathFactory<WorldData>>().Create("Configs/Maps/Map1");
                history.ApplySnapshot(initialMap);
                history.SaveBaseline();

                var session = container.GetInstance<SkyPiratesSessionProvider>();
                var playback = new SkyPiratesReplayPlayback(history, session);
                playback.Load(replay);
                // Load rebuilds a fresh session and applies the recorded outcomes to it.
                long targetStep = requestedStep ?? playback.LastStep;
                if (targetStep < replay.InitialStep || targetStep > playback.LastStep)
                    throw new ArgumentOutOfRangeException(nameof(requestedStep), $"Replay range is {replay.InitialStep}..{playback.LastStep}.");

                playback.Seek(targetStep);
                Console.WriteLine($"Replay reached step {targetStep}; commands={replay.Commands.Count}.");
                if (targetStep == replay.EndHashCheck.Step)
                {
                    bool matches = playback.VerifyHashAt(targetStep, out ulong expected, out ulong actual);
                    Console.WriteLine($"Hash at step {targetStep}: expected=0x{expected:X16}, actual=0x{actual:X16}.");
                    return matches ? 0 : 1;
                }

                Console.WriteLine("No hash check is stored for this step.");
                return 0;
            }
            catch (Exception exception)
            {
                Console.Error.WriteLine(exception);
                return 1;
            }
        }

        private static long? ReadTargetStep(string[] args)
        {
            for (int i = 1; i < args.Length; i++)
            {
                if (args[i] == "--to" && i + 1 < args.Length)
                    return long.Parse(args[++i]);
            }

            return null;
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
