using SlyPirates.Client.DI;
using Delta.ECS.Unity;
using SlyPirates.Shared.Commands;
using SlyPirates.Shared.Services.Netcode;
using Riptide;
using Riptide.Utils;
using SimpleInjector;
using System;

namespace SlyPirates.Client.Entry
{
    public class ClientStart : UnityEngine.MonoBehaviour
    {
        private Container _container = null!;
        private SkyPiratesSessionProvider _sessions = null!;
        private bool _spawnSquadSent;

        private void Start()
        {
            Message.MaxPayloadSize = 256;
            RiptideLogger.Initialize(LogRiptideInfo, true);
            RiptideLogger.EnableLoggingFor(LogType.Debug, LogRiptideInfo);
            RiptideLogger.EnableLoggingFor(LogType.Info, LogRiptideInfo);
            RiptideLogger.EnableLoggingFor(LogType.Warning, LogRiptideWarning);
            RiptideLogger.EnableLoggingFor(LogType.Error, LogRiptideError);

            var sceneWorld = SceneWorld.GetForScene(gameObject.scene);
            _container = new ClientContainer(sceneWorld, this);

            _container.RegisterAndInjectViewModels();

            Connect();
        }

        private void Connect()
        {
            if (_container == null)
            {
                return;
            }

            var client = _container.GetInstance<Riptide.Client>();

            client.Connected += OnConnected;
            client.Disconnected += OnDisconnected;

            string port = ClientSetupData.Port;
            string ip = ClientSetupData.IP;
            client.Connect($"{ip}:{port}", useMessageHandlers: false);
        }

        private void OnConnected(object sender, EventArgs e)
        {
            var client = _container.GetInstance<Riptide.Client>();
            client.Connection.CanQualityDisconnect = false;

            if (_sessions == null)
            {
                _sessions = _container.GetInstance<SkyPiratesSessionProvider>();
                _sessions.Ready += OnSessionReady;
            }

            _sessions.Start(new Delta.Netcode.AuthorId((uint)client.Id));

            Delta.Diagnostics.Trace.Info("Connected");
        }

        private void OnDisconnected(object sender, Riptide.DisconnectedEventArgs e)
        {
            if (_sessions != null)
            {
                _sessions.Disconnect();
            }

            Delta.Diagnostics.Trace.Info(e.Message.GetString());
            Delta.Diagnostics.Trace.Assert(false, context: $"Disconnected: {e.Reason}");
        }

        private void OnSessionReady()
        {
            if (_spawnSquadSent)
            {
                return;
            }

            _sessions.Send(new SpawnSquadCommand { Deck = ClientSetupData.StartingDeck },
                System.Math.Max(1, _sessions.CurrentStep + 1));
            _spawnSquadSent = true;
        }

        private static void LogRiptideInfo(string message) => Delta.Diagnostics.Trace.Info(message);

        private static void LogRiptideWarning(string message) => Delta.Diagnostics.Trace.Warn(message);

        private static void LogRiptideError(string message) => Delta.Diagnostics.Trace.Error(new Exception(message));

        private void Update()
        {
            var startController = _container.GetInstance<GameStartController>();
            startController.Update();
        }

    }
}
