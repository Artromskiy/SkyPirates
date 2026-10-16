using SlyPirates.Server.Factories;
using SlyPirates.Shared.Core;
using SlyPirates.Shared;
using SlyPirates.Shared.Data;
using SlyPirates.Shared.IFactories;
using SlyPirates.Shared.IServices;
using SlyPirates.Shared.IServices.TickableExecutors;
using SlyPirates.Shared.Services.Netcode;
using Riptide;
using Riptide.Utils;
using SimpleInjector;
using System;
using System.Net;
using System.Net.Sockets;

namespace SlyPirates.Server
{
    internal static class Program
    {
        private static Container _container = null!;

        private static void Main(string[] args)
        {
            RiptideLogger.Initialize(Console.WriteLine, true);
            Message.MaxPayloadSize = 256;
            _container = new ServerContainer();
            LogIPs();

            var server = _container.GetInstance<Riptide.Server>();
            while (!(Console.KeyAvailable && Console.ReadKey(true).Key == ConsoleKey.Enter)) { }
            Console.WriteLine("Started");

            var worldData = _container.GetInstance<IPathFactory<WorldData>>().Create("Configs/Maps/Map1");
            var sessions = _container.GetInstance<SkyPiratesSessionProvider>();
            sessions.ApplySnapshot(worldData, -1);
            sessions.Start(new Delta.Netcode.AuthorId(0));
            server.ClientConnected += ClientConnected;
            _container.GetInstance<GameStartController>().Loop();
        }

        private static void ClientConnected(object? sender, ServerConnectedEventArgs args)
        {
            args.Client.CanQualityDisconnect = false;
            var sessions = _container.GetInstance<SkyPiratesSessionProvider>();
            sessions.Bind(args.Client.Id, new Delta.Netcode.AuthorId(args.Client.Id));
        }

        private static void LogIPs()
        {
            var host = Dns.GetHostEntry(Dns.GetHostName(), AddressFamily.InterNetwork);
            Console.WriteLine("IPs:");
            foreach (var ip in host.AddressList)
                Console.WriteLine(ip.ToString());
        }
    }
}
