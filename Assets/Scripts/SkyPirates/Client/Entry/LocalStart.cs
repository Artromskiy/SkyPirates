using DVG.Commands;
using DVG.Core;
using DVG.SkyPirates.Client.DI;
using DVG.SkyPirates.Client.Entry;
using DVG.SkyPirates.Client.IServices;
using DVG.SkyPirates.Shared.Commands;
using DVG.SkyPirates.Shared.Data;
using DVG.SkyPirates.Shared.IServices;
using DVG.SkyPirates.Shared.IServices.TickableExecutors;
using SimpleInjector;
using System;
using UnityEngine;

namespace DVG.SkyPirates.Local.Entry
{
    public class LocalStart : MonoBehaviour
    {
        private Container _container = null!;

        private void Start()
        {
            try
            {
                Delta.Diagnostics.Trace.Info("[LocalStart] Container creation");
                _container = new LocalContainer();
                Delta.Diagnostics.Trace.Info("[LocalStart] Container register and inject ViewModels");
                _container.RegisterAndInjectViewModels();

                Delta.Diagnostics.Trace.Info("[LocalStart] Container get instances");
                var comandReciever = _container.GetInstance<ICommandReciever>();
                var client = _container.GetInstance<IClientService>();
                var worldData = _container.GetInstance<IPathFactory<WorldData>>().Create("Configs/Maps/Map1");
                Delta.Diagnostics.Trace.Info("[LocalStart] Load map");
                var history = _container.GetInstance<IHistorySystem>();
                history.ApplySnapshot(worldData);
                history.SaveBaseline();
                Delta.Diagnostics.Trace.Info("[LocalStart] Spawn squad");
                comandReciever.InvokeCommand(new Command<SpawnSquadCommand>(client.Id, 5, new SpawnSquadCommand()));
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
        }
    }
}
