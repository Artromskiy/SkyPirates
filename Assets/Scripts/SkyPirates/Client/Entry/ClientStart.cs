#nullable enable
using DVG.SkyPirates.Client.DI;
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

            DVG.Trace.Info("Connected");
        }

        private void OnDisconnected(object sender, Riptide.DisconnectedEventArgs e)
        {
            DVG.Trace.Info(e.Message.GetString());
            DVG.Trace.Assert(false, context: $"Disconnected: {e.Reason}");
        }

        private static void LogRiptideInfo(string message) => DVG.Trace.Info(message);

        private static void LogRiptideWarning(string message) => DVG.Trace.Warn(message);

        private static void LogRiptideError(string message) => DVG.Trace.Error(new Exception(message));

        private void Update()
        {
            var startController = _container.GetInstance<GameStartController>();
            startController.Update();
        }

    }
}
