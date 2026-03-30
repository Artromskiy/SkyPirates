using Assets.Scripts.SkyPirates.Client.Factories;
using DVG.Core;
using DVG.SkyPirates.Client.Entry;
using DVG.SkyPirates.Client.Factories;
using DVG.SkyPirates.Client.Factories.VMFactories;
using DVG.SkyPirates.Client.IFactories;
using DVG.SkyPirates.Client.IServices;
using DVG.SkyPirates.Client.IViewModels;
using DVG.SkyPirates.Client.Services;
using DVG.SkyPirates.Client.Services.EntityViewProviders;
using DVG.SkyPirates.Client.Systems;
using DVG.SkyPirates.Client.ViewModels.UI;
using DVG.SkyPirates.Shared.DI;
using DVG.SkyPirates.Shared.IServices;
using DVG.SkyPirates.Shared.IServices.TickableExecutors;
using DVG.SkyPirates.Shared.Services;
using Riptide.Transports.Udp;
using SimpleInjector;
using System;

namespace DVG.SkyPirates.Client.DI
{
    public class ClientContainer : SharedContainer
    {
        public ClientContainer()
        {
            RegisterSingleton(() =>
            {
                var client = new Riptide.Client(new UdpClient());
                client.TimeoutTime = Constants.MaxHistoryDurationMs;
                client.HeartbeatInterval = Constants.TickDurationMs;
                return client;
            });
            RegisterSingleton<IClientService, DefaultClient>();
            RegisterSingleton<IPlayer, Player>();
            RegisterSingleton<ICommandSender, CommandSendService>();
            RegisterSingleton<ICommandReciever, CommandReciever>();

            //Register<ICommandSendScheduler, DelayedCommandSendScheduler>(Lifestyle.Singleton);
            RegisterSingleton<ICommandSendScheduler, CommandSendScheduler>();

            RegisterSingleton(typeof(IPathFactory<>), typeof(ResourcesFactory<>));
            RegisterSingleton(typeof(IPathViewFactory<>), typeof(PathViewFactory<>));
            RegisterSingleton<IEntityVMFactory, CachedEntityVMFactory>();

            RegisterSingleton<IHealthbarCanvasVM, HealthbarCanvasVM>();
            RegisterSingleton<IJoystickVM, JoystickVM>();
            RegisterSingleton<ICardsVM, CardsVM>();
            RegisterSingleton<ICameraVM, SquadCameraVM>();
            RegisterSingleton<IGoodsVM, GoodsVM>();

            RegisterSingleton<GameStartController>();
            RegisterSingleton<ITickCounterService, TickCounterService>();
            RegisterSingleton<IHashSumService, HashSumService>();

            Collection.Register<IPreTickable>(PreTickableExecutors, Lifestyle.Singleton);
            Collection.Register<IPostTickable>(PostTickableExecutors, Lifestyle.Singleton);
            Collection.Register<IInTickable>(InTickables, Lifestyle.Singleton);
            Collection.Register<IEntityViewProvider>(ViewProviders, Lifestyle.Singleton);
        }

        private static readonly Type[] PreTickableExecutors = new Type[]
        {
            typeof(ITickCounterService),
            typeof(IClientService), // recieve commands
            typeof(ICommandSendScheduler), // send commands to timeline
        };

        private static readonly Type[] PostTickableExecutors = new Type[]
        {
            typeof(EntityViewSystem),
            //typeof(TimelineWriter)
        };

        private static readonly Type[] InTickables = new Type[]
        {
            //typeof(IHashSumService)
        };

        private static readonly Type[] ViewProviders = new Type[]
        {
            typeof(IdViewProvider),
            typeof(HealthbarViewProvider),
            typeof(HexMapViewProvider),
            typeof(SquadViewProvider)
        };
    }
}
