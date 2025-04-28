using DVG.SkyPirates.Server.Factories;
using DVG.SkyPirates.Server.IFactories;
using DVG.SkyPirates.Shared.Factories;
using DVG.SkyPirates.Shared.IFactories;
using DVG.SkyPirates.Shared.Models;
using SimpleInjector;
using Unity.Multiplayer;
using UnityEngine;

namespace DVG.SkyPirates.Server.Entry
{
    public class ServerScope : MonoBehaviour
    {
        protected void Start()
        {
            if (MultiplayerRolesManager.ActiveMultiplayerRoleMask != MultiplayerRoleFlags.Server)
                return;

            var builder = new Container();

            builder.Register<IPathFactory<SquadModel>, ResourcesFactory<SquadModel>>(Lifestyle.Scoped);
            builder.Register<IPathFactory<UnitModel>, ResourcesFactory<UnitModel>>(Lifestyle.Scoped);
            builder.Register<IPathFactory<PackedCirclesModel>, ResourcesFactory<PackedCirclesModel>>(Lifestyle.Scoped);

            builder.Register<IUnitModelFactory, UnitModelFactory>(Lifestyle.Scoped);
            builder.Register<IUnitViewFactory, NetworkedUnitViewFactory>(Lifestyle.Scoped);

            builder.Register<IUnitFactory, UnitFactory>(Lifestyle.Scoped);
            builder.Register<IInputFactory, InputFactory>(Lifestyle.Scoped);

            builder.Register<PresenterServer>(Lifestyle.Scoped);

        }
    }
}
