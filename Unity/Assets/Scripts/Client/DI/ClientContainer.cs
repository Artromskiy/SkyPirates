using SlyPirates.Shared.Core;
using SlyPirates.Shared;
using SlyPirates.Client.Entry;
using SlyPirates.Client.IServices;
using SlyPirates.Client.Factories;
using SlyPirates.Client.IViewModels;
using SlyPirates.Client.Services;
using SlyPirates.Client.Systems;
using SlyPirates.Client.ViewModels.UI;
using SlyPirates.Shared.DI;
using SlyPirates.Shared.IServices.TickableExecutors;
using Riptide.Transports.Udp;
using SimpleInjector;
using System;
using Delta.ECS.Unity;
using UnityEngine;

namespace SlyPirates.Client.DI
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
            typeof(HexMapChunkRenderSystem),
            //typeof(TimelineWriter)
        };

        private static readonly Type[] InTickables = new Type[]
        {
        };

    }
}
