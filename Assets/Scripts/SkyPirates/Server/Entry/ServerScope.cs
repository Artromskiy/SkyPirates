using DVG.SkyPirates.Server.Factories;
using DVG.SkyPirates.Server.IFactories;
using DVG.SkyPirates.Shared.Factories;
using DVG.SkyPirates.Shared.IFactories;
using DVG.SkyPirates.Shared.Models;
using Unity.Multiplayer;
using VContainer;
using VContainer.Unity;

namespace DVG.SkyPirates.Server.Entry
{
    public class ServerScope : LifetimeScope
    {
        protected override void Configure(IContainerBuilder builder)
        {
            if (MultiplayerRolesManager.ActiveMultiplayerRoleMask != MultiplayerRoleFlags.Server)
                return;

            builder.Register<IPathFactory<SquadModel>, ResourcesFactory<SquadModel>>(Lifetime.Scoped);
            builder.Register<IPathFactory<UnitModel>, ResourcesFactory<UnitModel>>(Lifetime.Scoped);
            builder.Register<IPathFactory<PackedCirclesModel>, ResourcesFactory<PackedCirclesModel>>(Lifetime.Scoped);

            builder.Register<IUnitModelFactory, UnitModelFactory>(Lifetime.Scoped);
            builder.Register<IUnitViewFactory, NetworkedUnitViewFactory>(Lifetime.Scoped);

            builder.Register<IUnitFactory, UnitFactory>(Lifetime.Scoped);
            builder.Register<IInputFactory, InputFactory>(Lifetime.Scoped);

            builder.RegisterEntryPoint<PresenterServer>(Lifetime.Scoped);

        }
    }
}
