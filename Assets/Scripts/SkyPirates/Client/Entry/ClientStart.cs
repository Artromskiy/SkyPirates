#nullable enable
using DVG.Core;
using Riptide.Utils;
using SimpleInjector;
using SimpleInjector.Lifestyles;
using System;
using UnityEngine;

namespace DVG.SkyPirates.Client.Entry
{
    public class ClientStart : MonoBehaviour
    {
        [SerializeField]
        private GameObject[] _views = null!;

        private Scope? _scope;
        private Container? _container;
        protected void Start()
        {
            RiptideLogger.Initialize(Debug.Log, true);

            _container = new ClientContainer(_views);
            _scope = AsyncScopedLifestyle.BeginScope(_container);
            _container.GetInstance<PresenterClient>();
            _container.GetInstance<IPlayerLoopSystem>().ExceptionHandler += Debug.LogException;
            Connect();
        }

        private void Connect()
        {
            if (_scope == null || _container == null)
                return;
            var client = _container.GetInstance<Riptide.Client>();
            var client2 = _container.GetInstance<Riptide.Client>();
            Debug.Log(client.GetHashCode());
            Debug.Log(client2.GetHashCode());
            int port = 7777;
            string ip = "127.0.0.1";
            client.Connected += OnConnected;
            client.Disconnected += OnDisconnected;
            client.Connect($"{ip}:{port}", useMessageHandlers: false);
        }

        private void OnConnected(object sender, EventArgs e)
        {
            Debug.Log("Connected");
        }

        private void OnDisconnected(object sender, Riptide.DisconnectedEventArgs e)
        {
            Debug.Log("Disconnected");
            Debug.Log(e.Message);
            Debug.Log(e.Reason);
        }

        private void Update()
        {
            if (_scope == null || _container == null)
                return;
            _container?.GetInstance<IPlayerLoopSystem>().Start();
            _container?.GetInstance<IPlayerLoopSystem>().Tick();
        }
        private void FixedUpdate()
        {
            if (_scope == null || _container == null)
                return;
            _container?.GetInstance<Riptide.Client>().Update();
            _container?.GetInstance<IPlayerLoopSystem>().FixedTick();
        }
    }
}
