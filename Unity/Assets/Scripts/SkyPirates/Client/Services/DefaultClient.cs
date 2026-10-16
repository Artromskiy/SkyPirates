using SlyPirates.Client.IServices;

namespace SlyPirates.Client.Services
{
    internal class DefaultClient : IClientService
    {
        private readonly Riptide.Client _client;

        public DefaultClient(Riptide.Client client)
        {
            _client = client;
        }

        public bool IsConnected => _client.IsConnected;
        public int Id => IsConnected ? _client.Id : -1;
        public void Tick(int tick) => _client.Update();
    }
}
