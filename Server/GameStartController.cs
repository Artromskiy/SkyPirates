using DVG;
using DVG.SkyPirates.Shared.Services.Netcode;
using System;
using System.Diagnostics;
using System.Threading;

namespace DVG.SkyPirates.Server
{
    public sealed class GameStartController
    {
        private readonly Riptide.Server _server;
        private readonly SkyPiratesSessionProvider _session;
        private readonly SkyPiratesSessionTickLoop _sessionTickLoop;
        private readonly Stopwatch _clock = new();

        public GameStartController(
            Riptide.Server server,
            SkyPiratesSessionProvider session,
            SkyPiratesSessionTickLoop sessionTickLoop)
        {
            _server = server;
            _session = session;
            _sessionTickLoop = sessionTickLoop;
        }

        public void Loop()
        {
            _clock.Start();
            while (true)
            {
                long targetStep = _clock.Elapsed.Ticks * Constants.TicksPerSecond / TimeSpan.TicksPerSecond;
                if (_session.CurrentStep != targetStep)
                {
                    _server.Update();
                    _sessionTickLoop.Tick(targetStep);
                }

                Thread.Yield();
            }
        }
    }
}
