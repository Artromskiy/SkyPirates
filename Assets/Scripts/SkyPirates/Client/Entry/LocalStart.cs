using Delta.Netcode;
using DVG.Core;
using DVG.SkyPirates.Client.DI;
using DVG.SkyPirates.Client.Entry;
using DVG.SkyPirates.Client.IServices;
using DVG.SkyPirates.Shared.Commands;
using DVG.SkyPirates.Shared.Data;
using DVG.SkyPirates.Shared.IServices;
using DVG.SkyPirates.Shared.IServices.TickableExecutors;
using DVG.SkyPirates.Shared.Services.Netcode;
using SimpleInjector;
using System;
using UnityEngine;

namespace DVG.SkyPirates.Local.Entry
{
    public class LocalStart : MonoBehaviour
    {
        private Container _container = null!;
        private SkyPiratesSessionProvider _session = null!;
        private bool _spawnSquadSent;

        private void Start()
        {
            try
            {
                Delta.Diagnostics.Trace.Info("[LocalStart] Container creation");
                _container = new LocalContainer();
                Delta.Diagnostics.Trace.Info("[LocalStart] Container register and inject ViewModels");
                _container.RegisterAndInjectViewModels();

                Delta.Diagnostics.Trace.Info("[LocalStart] Container get instances");
                var client = _container.GetInstance<IClientService>();
                var worldData = _container.GetInstance<IPathFactory<WorldData>>().Create("Configs/Maps/Map1");
                Delta.Diagnostics.Trace.Info("[LocalStart] Load map");
                var history = _container.GetInstance<IHistorySystem>();
                history.ApplySnapshot(worldData);
                history.SaveBaseline();
                _session = _container.GetInstance<SkyPiratesSessionProvider>();
                _session.Start(new AuthorId((uint)client.Id));
            }
            catch (Exception e)
            {
                Delta.Diagnostics.Debug.Error(e);
            }
        }

        private void Update()
        {
            var startController = _container.GetInstance<GameStartController>();
            startController.Update();

            if (!_spawnSquadSent && _session.CurrentStep >= 0)
            {
                Delta.Diagnostics.Trace.Info("[LocalStart] Spawn squad");
                _session.Send(new SpawnSquadCommand(), _session.CurrentStep + 1);
                _spawnSquadSent = true;
            }
        }
    }
}
