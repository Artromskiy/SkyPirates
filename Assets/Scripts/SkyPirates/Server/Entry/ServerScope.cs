using DVG.Core;
using DVG.SkyPirates.Server.Factories;
using DVG.SkyPirates.Server.IFactories;
using DVG.SkyPirates.Shared.Factories;
using DVG.SkyPirates.Shared.IFactories;
using DVG.SkyPirates.Shared.Models;
using SimpleInjector;
using SimpleInjector.Diagnostics;
using SimpleInjector.Lifestyles;
using Unity.Multiplayer;
using UnityEngine;

namespace DVG.SkyPirates.Server.Entry
{
    public class ServerScope : MonoBehaviour
    {
        private Scope scope;
        protected void Start()
        {
            if (MultiplayerRolesManager.ActiveMultiplayerRoleMask != MultiplayerRoleFlags.Server)
                return;

            var container = new Container();
            container.Options.DefaultScopedLifestyle = new AsyncScopedLifestyle();

            container.Register<IPlayerLoopSystem, PlayerLoopSystem>(Lifestyle.Scoped);
            container.RegisterInitializer<IPlayerLoopItem>((item) => container.GetInstance<IPlayerLoopSystem>().Add(item));

            container.Register<IPathFactory<SquadModel>, ResourcesFactory<SquadModel>>(Lifestyle.Scoped);
            container.Register<IPathFactory<UnitModel>, ResourcesFactory<UnitModel>>(Lifestyle.Scoped);
            container.Register<IPathFactory<PackedCirclesModel>, ResourcesFactory<PackedCirclesModel>>(Lifestyle.Scoped);
            container.Register<IUnitModelFactory, UnitModelFactory>(Lifestyle.Scoped);
            container.Register<IUnitViewFactory, NetworkedUnitViewFactory>(Lifestyle.Scoped);
            container.Register<IUnitFactory, UnitFactory>(Lifestyle.Scoped);
            container.Register<IInputFactory, InputFactory>(Lifestyle.Scoped);
            container.Register<PresenterServer>(Lifestyle.Scoped);

            container.Verify(VerificationOption.VerifyAndDiagnose);

            scope = AsyncScopedLifestyle.BeginScope(container);
            scope.GetInstance<PresenterServer>();
            Analyze(container);
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
