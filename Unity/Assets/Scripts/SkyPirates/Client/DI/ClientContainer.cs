using SkyPirates.Shared.Core;
using SkyPirates.Shared;
using SkyPirates.Client.Entry;
using SkyPirates.Client.IFactories;
using SkyPirates.Client.IServices;
using SkyPirates.Client.Factories;
using SkyPirates.Client.IViewModels;
using SkyPirates.Client.Services;
using SkyPirates.Client.Systems;
using SkyPirates.Client.ViewModels.UI;
using SkyPirates.Shared.DI;
using SkyPirates.Shared.IServices;
using SkyPirates.Shared.IServices.TickableExecutors;
using SkyPirates.Shared.Services;
using Riptide.Transports.Udp;
using SimpleInjector;
using System;
using Delta.ECS.Unity;
using UnityEngine;

namespace SkyPirates.Client.DI
{
    public class ClientContainer : SharedContainer
    {
        public ClientContainer(SceneWorld sceneWorld) : base(Delta.Netcode.SessionMode.Client, true, sceneWorld.World)
        {
            RegisterInstance(sceneWorld);
            RegisterSingleton(() => Resources.Load<ViewPrefabCatalog>("ViewPrefabCatalog"));
            RegisterSingleton(() => new ViewBindingSystem(sceneWorld, GetInstance<ViewPrefabCatalog>()));
            RegisterSingleton(() =>
            {
                var client = new Riptide.Client(new UdpClient());
                client.TimeoutTime = Constants.MaxHistoryDurationMs;
                client.HeartbeatInterval = Constants.TickDurationMs;
                return client;
            });
            RegisterSingleton<IClientService, DefaultClient>();
            RegisterSingleton<IPlayer, Player>();
            RegisterSingleton<ICommandSendScheduler, CommandSendScheduler>();

            RegisterSingleton(typeof(IPathFactory<>), typeof(ResourcesFactory<>));

            RegisterSingleton<IHealthbarCanvasVM, HealthbarCanvasVM>();
            RegisterSingleton<IJoystickVM, JoystickVM>();
            RegisterSingleton<ICardsVM, CardsVM>();
            RegisterSingleton<ICameraVM, SquadCameraVM>();
            RegisterSingleton<IGoodsVM, GoodsVM>();

            RegisterSingleton<GameStartController>();
            RegisterSingleton<IHashSumService, HashSumService>();

            Collection.Register<IPreTickable>(PreTickableExecutors, Lifestyle.Singleton);
            Collection.Register<IPostTickable>(PostTickableExecutors, Lifestyle.Singleton);
            Collection.Register<IInTickable>(InTickables, Lifestyle.Singleton);
        }

        private static readonly Type[] PreTickableExecutors = new Type[]
        {
            typeof(IClientService), // recieve commands
            typeof(ICommandSendScheduler), // send commands to session
        };

        private static readonly Type[] PostTickableExecutors = new Type[]
        {
            typeof(ViewBindingTickSystem),
            //typeof(TimelineWriter)
        };

        private static readonly Type[] InTickables = new Type[]
        {
            //typeof(IHashSumService)
        };

    }
}
