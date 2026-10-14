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
using SkyPirates.Local.Services;
using SkyPirates.Shared.DI;
using SkyPirates.Shared.IServices;
using SkyPirates.Shared.IServices.TickableExecutors;
using SkyPirates.Shared.Services;
using Delta.ECS.Unity;
using UnityEngine;
using SimpleInjector;
using System;

namespace SkyPirates.Client.DI
{
    public class LocalContainer : SharedContainer
    {
        public LocalContainer(SceneWorld sceneWorld)
            : base(Delta.Netcode.SessionMode.Local, true, sceneWorld.World)
        {
            RegisterInstance(sceneWorld);
            RegisterSingleton(() => Resources.Load<ViewPrefabCatalog>("ViewPrefabCatalog"));
            RegisterSingleton(() => new ViewBindingSystem(sceneWorld, GetInstance<ViewPrefabCatalog>()));
            Delta.Diagnostics.Trace.Info("[DI] LocalContainer creation start");
            RegisterSingleton<IClientService, FakeClient>();
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

            Delta.Diagnostics.Trace.Info("[DI] Collections registration");

            Collection.Register<IPreTickable>(PreTickableExecutors, Lifestyle.Singleton);
            Collection.Register<IPostTickable>(PostTickableExecutors, Lifestyle.Singleton);
            Collection.Register<IInTickable>(InTickables, Lifestyle.Singleton);
        }

        private static readonly Type[] PreTickableExecutors = new Type[]
        {
            typeof(ICommandSendScheduler),
        };

        private static readonly Type[] PostTickableExecutors = new Type[]
        {
            typeof(ViewBindingTickSystem),
            //typeof(TimelineWriter),
            //typeof(IHashSumService),
        };

        private static readonly Type[] InTickables = new Type[]
        {
        };

    }
}
