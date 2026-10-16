using SlyPirates.Shared.Core;
using SlyPirates.Shared;
using SlyPirates.Server.Factories;
using SlyPirates.Server.IServices;
using SlyPirates.Server.Services;
using SlyPirates.Shared.DI;
using SlyPirates.Shared.IServices;
using SlyPirates.Shared.IServices.TickableExecutors;
using SlyPirates.Shared.Services;
using Riptide.Transports.Udp;
using SimpleInjector;
using SimpleInjector.Diagnostics;
using System;

namespace SlyPirates.Server
{
    internal class ServerContainer : SharedContainer
    {
        public ServerContainer() : base(Delta.Netcode.SessionMode.Server)
        {
            RegisterSingleton(() =>
            {
                var server = new Riptide.Server(new UdpServer());
                server.Start(7788, 16, useMessageHandlers: false);
                server.HeartbeatInterval = Constants.TickDurationMs;
                server.TimeoutTime = Constants.MaxHistoryDurationMs;
                return server;
            });

            RegisterSingleton<ICheatLoggerService, CheatLoggerService>();
            RegisterSingleton(typeof(IPathFactory<>), typeof(ResourcesFactory<>));

            RegisterSingleton<GameStartController>();

            Collection.Register<IPreTickable>(PreTickables, Lifestyle.Singleton);
            Collection.Register<IPostTickable>(PostTickables, Lifestyle.Singleton);
            Collection.Register<IInTickable>(InTickables, Lifestyle.Singleton);

            Verify(VerificationOption.VerifyAndDiagnose);
            Analyze(this);
        }

        private static void Analyze(Container container)
        {
            foreach (var item in Analyzer.Analyze(container))
                Console.WriteLine(item.Description);
        }

        private static Type[] PreTickables => Array.Empty<Type>();

        private static Type[] PostTickables => new Type[]
        {
            typeof(BroadcastCurrentStepService),
            typeof(TimelineWriter),
            typeof(TimelineSaver),
        };

        private readonly Type[] InTickables = new Type[]
        {
        };
    }
}
