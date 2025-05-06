using DVG.Core;
using DVG.SkyPirates.Client.Factories;
using DVG.SkyPirates.Client.IFactories;
using DVG.SkyPirates.Client.Presenters;
using DVG.SkyPirates.OldShared.Factories;
using DVG.SkyPirates.OldShared.Models;
using SimpleInjector;
using SimpleInjector.Diagnostics;
using SimpleInjector.Lifestyles;
using Unity.Multiplayer;
using UnityEngine;

namespace DVG.SkyPirates.Client.Entry
{
    public class ClientScope : MonoBehaviour
    {
        private Scope scope;
        [SerializeField]
        private GameObject[] _views;
        protected void Start()
        {
            if (MultiplayerRolesManager.ActiveMultiplayerRoleMask != MultiplayerRoleFlags.Client)
                return;

            var container = new Container();
            container.Options.DefaultScopedLifestyle = ScopedLifestyle.Flowing;
            container.Register<IPlayerLoopSystem, PlayerLoopSystem>(Lifestyle.Scoped);
            container.RegisterInitializer<IPlayerLoopItem>((item) => container.GetInstance<IPlayerLoopSystem>().Add(item));

            container.Register<IInputFactory, InputFactory>(Lifestyle.Scoped);
            container.Register<IPathFactory<CameraModel>, ResourcesFactory<CameraModel>>(Lifestyle.Scoped);

            container.Register<JoystickPm>(Lifestyle.Scoped);
            container.Register<MoveTargetPm>(Lifestyle.Scoped);
            container.Register<CardsPm>(Lifestyle.Scoped);
            container.Register<CameraPm>(Lifestyle.Scoped);

            RegisterIPathFactoryMethod<CameraModel>(container, "Configs/Camera/SeaCamera");

            foreach (var item in _views)
            {
                if (item.TryGetComponent<IView>(out var instance))
                    foreach (var type in instance.GetType().GetInterfaces())
                        if (type != typeof(IView))
                            container.RegisterInstance(type, instance);
            }

            container.Register<PresenterClient>(Lifestyle.Scoped);
            container.Verify();

            scope = AsyncScopedLifestyle.BeginScope(container);
            scope.GetInstance<PresenterClient>();
            Analyze(container);
        }

        private void RegisterIPathFactoryMethod<T>(Container builder, string parameter)
        {
            builder.ResolveUnregisteredType += (s, e) =>
            {
                if (e.UnregisteredServiceType == typeof(T) || e.UnregisteredServiceType.IsClosedTypeOf(typeof(T)))
                    e.Register(() => builder.GetInstance<IPathFactory<T>>().Create(parameter));
            };
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

        private void Update()
        {
            scope?.GetInstance<IPlayerLoopSystem>().Start();
            scope?.GetInstance<IPlayerLoopSystem>().Tick();
        }
        private void FixedUpdate()
        {
            scope?.GetInstance<IPlayerLoopSystem>().FixedTick();
        }
    }
}
