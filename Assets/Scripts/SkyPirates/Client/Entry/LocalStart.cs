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
using System.Diagnostics;
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
                Trace.TraceInformation("[LocalStart] Container creation");
                _container = new LocalContainer();
                Trace.TraceInformation("[LocalStart] Container register and inject ViewModels");
                _container.RegisterAndInjectViewModels();

                Trace.TraceInformation("[LocalStart] Container get instances");
                var comandReciever = _container.GetInstance<ICommandReciever>();
                var client = _container.GetInstance<IClientService>();
                var worldData = _container.GetInstance<IPathFactory<WorldData>>().Create("Configs/Maps/Map1");
                Trace.TraceInformation("[LocalStart] Load map");
                var history = _container.GetInstance<IHistorySystem>();
                history.ApplySnapshot(worldData);
                history.SaveBaseline();
                Trace.TraceInformation("[LocalStart] Spawn squad");
                comandReciever.InvokeCommand(new Command<SpawnSquadCommand>(client.Id, 5, new SpawnSquadCommand()));
            }
            catch (Exception e)
            {
                Trace.TraceError($"[LocalStart] Start failed: {e.Message}\n{e.StackTrace}");
            }
        }

        private void Update()
        {
            try
            {
                var startController = _container.GetInstance<GameStartController>();
                startController.Update();
            }
            catch (Exception e)
            {
                UnityEngine.Debug.Break();
                Trace.TraceError($"[LocalStart] Update failed: {e.Message}\n{e.StackTrace}");
            }
        }
    }
}