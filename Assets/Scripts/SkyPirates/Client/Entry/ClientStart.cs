using DVG.Core;
using SimpleInjector;
using SimpleInjector.Lifestyles;
using System;
using UnityEngine;

namespace DVG.SkyPirates.Client.Entry
{
    public class ClientStart : MonoBehaviour
    {
        [SerializeField]
        private GameObject[] _views;

        private Scope scope;
        protected void Start()
        {
            Container container = new ClientContainer(_views);
            scope = AsyncScopedLifestyle.BeginScope(container);
            scope.GetInstance<PresenterClient>();
            scope.GetInstance<IPlayerLoopSystem>().ExceptionHandler += Debug.LogException;
            var client = scope.GetInstance<Riptide.Client>();
            int port = 7777;
            string ip = "127.0.0.1";
            client.Connected += OnConnected;
            client.Disconnected += OnDisconnected;
            var connected = client.Connect($"{ip}:{port}");
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
            
            scope?.GetInstance<Riptide.Client>().Update();
            scope?.GetInstance<IPlayerLoopSystem>().Start();
            scope?.GetInstance<IPlayerLoopSystem>().Tick();
        }
        private void FixedUpdate()
        {
            scope?.GetInstance<IPlayerLoopSystem>().FixedTick();
        }
    }
}
