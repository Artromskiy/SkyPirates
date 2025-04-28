using DVG.Core;
using DVG.SkyPirates.Client.Factories;
using DVG.SkyPirates.Client.IFactories;
using DVG.SkyPirates.Client.Presenters;
using DVG.SkyPirates.Shared.Factories;
using DVG.SkyPirates.Shared.IFactories;
using DVG.SkyPirates.Shared.Models;
using SimpleInjector;
using Unity.Multiplayer;
using UnityEngine;



namespace DVG.SkyPirates.Client.Entry
{

    public class ClientScope : MonoBehaviour
    {
        [SerializeField]
        private GameObject[] _views;

        protected void Start()
        {
            if (MultiplayerRolesManager.ActiveMultiplayerRoleMask != MultiplayerRoleFlags.Client)
                return;

            var builder = new Container();

            builder.Register<IInputFactory, InputFactory>(Lifestyle.Scoped);
            builder.Register<IPathFactory<CameraModel>, ResourcesFactory<CameraModel>>(Lifestyle.Scoped);


            builder.Register<JoystickPm>(Lifestyle.Scoped);
            builder.Register<MoveTargetPm>(Lifestyle.Scoped);
            builder.Register<CardsPm>(Lifestyle.Scoped);
            builder.Register<CameraPm>(Lifestyle.Scoped);
            RegisterIPathFactoryMethod<CameraModel>(builder, "Configs/Camera/SeaCamera");

            foreach (var item in _views)
                builder.RegisterInstance(item.GetComponent<IView>());

            //builder.RegisterComponent<ICardsView>(_cardsView);
            //builder.RegisterComponent<IJoystickView>(_joystickView);
            //builder.RegisterComponent<ICameraView>(_cameraView);
            //builder.RegisterComponent<IMoveTargetView>(_moveTargetView);

            builder.Register<PresenterClient>(Lifestyle.Scoped);
        }

        private void RegisterIPathFactoryMethod<T>(Container builder, string parameter)
        {
            builder.ResolveUnregisteredType += (s, e) =>
            {
                if (e.UnregisteredServiceType.IsClosedTypeOf(typeof(T)))
                    e.Register(() => builder.GetInstance<IPathFactory<T>>().Create(parameter));
            };
        }
    }
}
