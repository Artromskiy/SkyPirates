using DVG.Core;
using DVG.SkyPirates.Client.Factories;
using DVG.SkyPirates.Client.IFactories;
using DVG.SkyPirates.Client.Presenters;
using DVG.SkyPirates.Shared.Factories;
using DVG.SkyPirates.Shared.IFactories;
using DVG.SkyPirates.Shared.Models;

using Unity.Multiplayer;
using UnityEngine;
using VContainer;
using VContainer.Unity;

namespace DVG.SkyPirates.Client.Entry
{

    public class ClientScope : LifetimeScope
    {
        [SerializeField]
        private GameObject[] _views;

        protected override void Configure(IContainerBuilder builder)
        {
            if (MultiplayerRolesManager.ActiveMultiplayerRoleMask != MultiplayerRoleFlags.Client)
                return;

            builder.Register<IInputFactory, InputFactory>(Lifetime.Scoped);
            builder.Register<IPathFactory<CameraModel>, ResourcesFactory<CameraModel>>(Lifetime.Scoped);


            builder.Register<JoystickPm>(Lifetime.Scoped);
            builder.Register<MoveTargetPm>(Lifetime.Scoped);
            builder.Register<CardsPm>(Lifetime.Scoped);

            builder.Register<CameraPm>(Lifetime.Scoped).WithParameter((r) =>
                r.Resolve<IPathFactory<CameraModel>>().Create("Configs/Camera/LandCamera"));


            foreach (var item in _views)
                builder.RegisterComponent(item.GetComponent(typeof(IView))).AsImplementedInterfaces();

            //builder.RegisterComponent<ICardsView>(_cardsView);
            //builder.RegisterComponent<IJoystickView>(_joystickView);
            //builder.RegisterComponent<ICameraView>(_cameraView);
            //builder.RegisterComponent<IMoveTargetView>(_moveTargetView);

            builder.RegisterEntryPoint<PresenterClient>(Lifetime.Scoped);
        }
    }
}
