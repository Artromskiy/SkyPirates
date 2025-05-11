using DVG.Core;
using DVG.SkyPirates.Client.Entry;
using DVG.SkyPirates.Client.Factories;
using DVG.SkyPirates.Client.IServices;
using DVG.SkyPirates.Client.Presenters;
using DVG.SkyPirates.Client.Services;
using DVG.SkyPirates.Shared.IFactories;
using DVG.SkyPirates.Shared.IServices;
using DVG.SkyPirates.Shared.Models;
using DVG.SkyPirates.Shared.Services;
using SimpleInjector;
using SimpleInjector.Diagnostics;
using UnityEngine;

namespace DVG
{
    public class ClientContainer : Container
    {
        public ClientContainer(GameObject[] views)
        {
            Register(() => new Riptide.Client(), Lifestyle.Singleton);
            Register<ICommandSerializer, CommandSerializer>(Lifestyle.Singleton);
            Register<ICommandSendService, CommandSendService>(Lifestyle.Singleton);
            Register<ICommandRecieveService, CommandRecieveService>(Lifestyle.Singleton);

            Register<IUnitViewSyncer, NetworkedUnitViewSyncer>(Lifestyle.Singleton);
            Register<IUnitViewFactory, UnitViewFactory>(Lifestyle.Singleton);

            Register<IPlayerLoopSystem, PlayerLoopSystem>(Lifestyle.Singleton);
            RegisterInitializer<IPlayerLoopItem>((item) => GetInstance<IPlayerLoopSystem>().Add(item));

            Register<IPathFactory<CameraModel>, ResourcesFactory<CameraModel>>(Lifestyle.Singleton);
            Register<CameraModel>(() => GetInstance<IPathFactory<CameraModel>>().Create("Configs/Camera/SeaCamera"), Lifestyle.Singleton);

            Register<JoystickPm>(Lifestyle.Singleton);
            Register<MoveTargetPm>(Lifestyle.Singleton);
            Register<CardsPm>(Lifestyle.Singleton);
            Register<CameraPm>(Lifestyle.Singleton);

            Register<PresenterClient>(Lifestyle.Singleton);

            foreach (var item in views)
                if (item.TryGetComponent<IView>(out var instance))
                    foreach (var type in instance.GetType().GetInterfaces())
                        if (type != typeof(IView))
                            RegisterInstance(type, instance);

            Verify();
            Analyze(this);
        }


        private void Analyze(Container container)
        {
            foreach (var item in Analyzer.Analyze(container))
            {
                if (item.Severity == DiagnosticSeverity.Information)
                    Debug.Log(item.Description);
                else
                    Debug.LogWarning(item.Description);
            }
        }
    }
}
