using DVG.Core;
using DVG.SkyPirates.Client.Entry;
using DVG.SkyPirates.Client.IFactories;
using DVG.SkyPirates.Client.IServices;
using DVG.SkyPirates.Client.Factories;
using DVG.SkyPirates.Client.IViewModels;
using DVG.SkyPirates.Client.Services;
using DVG.SkyPirates.Client.Systems;
using DVG.SkyPirates.Client.ViewModels.UI;
using DVG.SkyPirates.Local.Services;
using DVG.SkyPirates.Shared.DI;
using DVG.SkyPirates.Shared.IServices;
using DVG.SkyPirates.Shared.IServices.TickableExecutors;
using DVG.SkyPirates.Shared.Services;
using Delta.ECS.Unity;
using UnityEngine;
using SimpleInjector;
using System;

namespace DVG.SkyPirates.Client.DI
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
