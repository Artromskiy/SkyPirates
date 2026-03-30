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
using DVG.SkyPirates.Local.Services;
using DVG.SkyPirates.Shared.DI;
using DVG.SkyPirates.Shared.IServices;
using DVG.SkyPirates.Shared.IServices.TickableExecutors;
using DVG.SkyPirates.Shared.Services;
using SimpleInjector;
using System;
using System.Diagnostics;

namespace DVG.SkyPirates.Client.DI
{
    public class LocalContainer : SharedContainer
    {
        public LocalContainer()
        {
            Debug.WriteLine("[DI] LocalContainer creation start");
            RegisterSingleton<IClientService, FakeClient>();
            RegisterSingleton<IPlayer, Player>();
            RegisterSingleton<ICommandSender, LocalCommandSendService>();
            RegisterSingleton<ICommandReciever, LocalCommandReciever>();

            Register<ICommandSendScheduler, DelayedCommandSendScheduler>(Lifestyle.Singleton);
            //RegisterSingleton<ICommandSendSchedulerService, CommandSendScheduler>();

            RegisterSingleton(typeof(IPathFactory<>), typeof(ResourcesFactory<>));
            RegisterSingleton(typeof(IPathViewFactory<>), typeof(PathViewFactory<>));

            //RegisterSingleton<IEntityVMFactory<IEntityVM>, EntityVMFactory>();
            RegisterSingleton<IEntityVMFactory, CachedEntityVMFactory>();

            RegisterSingleton<IHealthbarCanvasVM, HealthbarCanvasVM>();
            RegisterSingleton<IJoystickVM, JoystickVM>();
            RegisterSingleton<ICardsVM, CardsVM>();
            RegisterSingleton<ICameraVM, SquadCameraVM>();
            RegisterSingleton<IGoodsVM, GoodsVM>();

            RegisterSingleton<GameStartController>();
            RegisterSingleton<ITickCounterService, TickCounterService>();
            RegisterSingleton<IHashSumService, HashSumService>();

            Debug.WriteLine("[DI] Collections registration");

            Collection.Register<IPreTickable>(PreTickableExecutors, Lifestyle.Singleton);
            Collection.Register<IPostTickable>(PostTickableExecutors, Lifestyle.Singleton);
            Collection.Register<IInTickable>(InTickables, Lifestyle.Singleton);
            Collection.Register<IEntityViewProvider>(ViewProviders, Lifestyle.Singleton);
        }

        private static readonly Type[] PreTickableExecutors = new Type[]
        {
            typeof(ITickCounterService),
            typeof(ICommandSendScheduler),
        };

        private static readonly Type[] PostTickableExecutors = new Type[]
        {
            typeof(EntityViewSystem),
            //typeof(TimelineWriter),
            //typeof(IHashSumService),
        };

        private static readonly Type[] InTickables = new Type[]
        {
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
