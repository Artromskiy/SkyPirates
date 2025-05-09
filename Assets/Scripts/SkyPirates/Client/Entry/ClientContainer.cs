using DVG.Core;
using DVG.SkyPirates.Client.Entry;
using DVG.SkyPirates.Shared.Models;
using DVG.SkyPirates.Client.Presenters;
using DVG.SkyPirates.OldShared.Factories;
using SimpleInjector;
using SimpleInjector.Diagnostics;
using UnityEngine;
using DVG.SkyPirates.Client.IServices;
using DVG.SkyPirates.Client.Services;

namespace DVG
{
    public class ClientContainer : Container
    {
        public ClientContainer(GameObject[] views)
        {
            Options.DefaultScopedLifestyle = ScopedLifestyle.Flowing;

            Register<Riptide.Client>(() => new Riptide.Client(), Lifestyle.Scoped);
            Register<ICommandSendService, CommandSendService>(Lifestyle.Scoped);
            Register<ICommandRecieveService, CommandRecieveService>(Lifestyle.Scoped);

            Register<IPlayerLoopSystem, PlayerLoopSystem>(Lifestyle.Scoped);
            RegisterInitializer<IPlayerLoopItem>((item) => GetInstance<IPlayerLoopSystem>().Add(item));

            Register<IPathFactory<CameraModel>, ResourcesFactory<CameraModel>>(Lifestyle.Scoped);
            Register<CameraModel>(() => GetInstance<IPathFactory<CameraModel>>().Create("Configs/Camera/SeaCamera"), Lifestyle.Scoped);

            Register<JoystickPm>(Lifestyle.Scoped);
            Register<MoveTargetPm>(Lifestyle.Scoped);
            Register<CardsPm>(Lifestyle.Scoped);
            Register<CameraPm>(Lifestyle.Scoped);

            Register<PresenterClient>(Lifestyle.Scoped);

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
