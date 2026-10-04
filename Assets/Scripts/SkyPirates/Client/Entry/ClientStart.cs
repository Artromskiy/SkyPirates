using DVG.SkyPirates.Client.DI;
using DVG.SkyPirates.Shared.Commands;
using DVG.SkyPirates.Shared.Services.Netcode;
using Riptide;
using Riptide.Utils;
using SimpleInjector;
using System;

namespace DVG.SkyPirates.Client.Entry
{
    public class ClientStart : UnityEngine.MonoBehaviour
    {
        private Container _container = null!;

        private void Start()
        {
            Message.MaxPayloadSize = 256;
            RiptideLogger.Initialize(LogRiptideInfo, true);
            RiptideLogger.EnableLoggingFor(LogType.Debug, LogRiptideInfo);
            RiptideLogger.EnableLoggingFor(LogType.Info, LogRiptideInfo);
            RiptideLogger.EnableLoggingFor(LogType.Warning, LogRiptideWarning);
            RiptideLogger.EnableLoggingFor(LogType.Error, LogRiptideError);

            _container = new ClientContainer();

            _container.RegisterAndInjectViewModels();

            Connect();
        }

        private void Connect()
        {
            if (_container == null)
                return;

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

            var sessions = _container.GetInstance<SkyPiratesSessionProvider>();
            sessions.Ready += () => sessions.Send(new SpawnSquadCommand(), System.Math.Max(1, sessions.CurrentStep + 1));
            sessions.Start(new Delta.Netcode.AuthorId((uint)client.Id));

            Delta.Diagnostics.Trace.Info("Connected");
        }

        private void OnDisconnected(object sender, Riptide.DisconnectedEventArgs e)
        {
            Delta.Diagnostics.Trace.Info(e.Message.GetString());
            Delta.Diagnostics.Trace.Assert(false, context: $"Disconnected: {e.Reason}");
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
