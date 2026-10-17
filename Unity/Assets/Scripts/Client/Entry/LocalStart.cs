using Delta.Netcode;
using Delta.ECS.Unity;
using SlyPirates.Shared.Core;
using SlyPirates.Shared;
using SlyPirates.Client.DI;
using SlyPirates.Client.Entry;
using SlyPirates.Client.IServices;
using SlyPirates.Shared.Data;
using SlyPirates.Shared.IServices;
using SlyPirates.Shared.IServices.TickableExecutors;
using SlyPirates.Shared.Services.Netcode;
using SlyPirates.Client.Systems;
using SimpleInjector;
using System;
using System.Linq;
using UnityEngine;

namespace SlyPirates.Local.Entry
{
    public class LocalStart : MonoBehaviour
    {
        [SerializeField, Min(1)]
        [Tooltip("Number of fixed states retained by the local session history.")]
        private int _historyCapacity = Constants.MaxHistoryTicks;

        private Container _container = null!;
        private LocalGameLoop _gameLoop = null!;

        private void Start()
        {
            try
            {
                Delta.Diagnostics.Trace.Info("[LocalStart] Container creation");
                var sceneWorld = SceneWorld.GetForScene(gameObject.scene);
                _container = new LocalContainer(sceneWorld, _historyCapacity);
                Delta.Diagnostics.Trace.Info("[LocalStart] Container register and inject ViewModels");
                _container.RegisterAndInjectViewModels();
                _gameLoop = _container.GetInstance<LocalGameLoop>();

                Delta.Diagnostics.Trace.Info("[LocalStart] Container get instances");
                var client = _container.GetInstance<IClientService>();
                var worldData = _container.GetInstance<IPathFactory<WorldData>>().Create("Configs/Maps/Map1");
                Delta.Diagnostics.Trace.Info("[LocalStart] Load map");
                var session = _container.GetInstance<SkyPiratesSessionProvider>();
                session.ApplySnapshot(worldData, -1);
                _container.GetAllInstances<IPostTickable>().OfType<ViewBindingTickSystem>().Single().Tick(0);
                session.Start(new AuthorId((uint)client.Id));
            }
            catch (Exception e)
            {
                Delta.Diagnostics.Debug.Error(e);
            }
        }

        private void Update()
        {
            if (_container == null || _gameLoop == null)
            {
                return;
            }

            _gameLoop.Update();
        }
    }
}
