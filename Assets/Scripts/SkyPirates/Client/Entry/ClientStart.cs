#nullable enable
using DVG.SkyPirates.Client.DI;
using Riptide;
using Riptide.Utils;
using SimpleInjector;
using System;
using System.Diagnostics;

namespace DVG.SkyPirates.Client.Entry
{
    public class ClientStart : UnityEngine.MonoBehaviour
    {
        private Container _container = null!;

        private void Start()
        {
            Message.MaxPayloadSize = 256;
            RiptideLogger.Initialize(UnityEngine.Debug.Log, true);
            RiptideLogger.EnableLoggingFor(LogType.Debug, UnityEngine.Debug.Log);
            RiptideLogger.EnableLoggingFor(LogType.Info, UnityEngine.Debug.Log);
            RiptideLogger.EnableLoggingFor(LogType.Warning, UnityEngine.Debug.LogWarning);
            RiptideLogger.EnableLoggingFor(LogType.Error, UnityEngine.Debug.LogError);

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

            Debug.WriteLine("Connected");
        }

        private void OnDisconnected(object sender, Riptide.DisconnectedEventArgs e)
        {
            Debug.WriteLine(e.Message);
            Debug.WriteLine(e.Reason);
            Debug.Fail("Disconnected");
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
                Trace.TraceError($"[LocalStart] Update failed: {e.Message} \n {e.StackTrace}");
            }
        }

    }
}
