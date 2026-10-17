using SlyPirates.Shared.Core;
using SlyPirates.Shared;
using SlyPirates.Client.Entry;
using SlyPirates.Client.IFactories;
using SlyPirates.Client.IServices;
using SlyPirates.Client.Factories;
using SlyPirates.Client.IViewModels;
using SlyPirates.Client.Services;
using SlyPirates.Client.Systems;
using SlyPirates.Client.ViewModels.UI;
using SlyPirates.Local.Services;
using SlyPirates.Shared.DI;
using SlyPirates.Shared.IServices;
using SlyPirates.Shared.IServices.TickableExecutors;
using Delta.ECS.Unity;
using UnityEngine;
using SimpleInjector;
using System;
using SlyPirates.Client;

namespace SlyPirates.Client.DI
{
    public class LocalContainer : SharedContainer
    {
        public LocalContainer(SceneWorld sceneWorld, UnityEngine.Object owner, int historyCapacity = Constants.MaxHistoryTicks)
            : base(Delta.Netcode.SessionMode.Local, true, sceneWorld.World, historyCapacity)
        {
            RegisterInstance(sceneWorld);
            RegisterSingleton(() => new AddressableViewBindingSystem(sceneWorld, owner));
            Delta.Diagnostics.Trace.Info("[DI] LocalContainer creation start");
            RegisterSingleton<IClientService, FakeClient>();
            RegisterSingleton<IPlayer, Player>();
            RegisterSingleton<ICommandSendScheduler, CommandSendScheduler>();

            RegisterSingleton<LocalGameLoop>();

            RegisterSingleton(typeof(IPathFactory<>), typeof(ResourcesFactory<>));

            RegisterSingleton<IHealthbarCanvasVM, HealthbarCanvasVM>();
            RegisterSingleton<IJoystickVM, JoystickVM>();
            RegisterSingleton<ICardsVM, CardsVM>();
            RegisterSingleton<ICameraVM, SquadCameraVM>();
            RegisterSingleton<IGoodsVM, GoodsVM>();

            RegisterSingleton<GameStartController>();

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
        };

        private static readonly Type[] InTickables = new Type[]
        {
        };

    }
}
