using SlyPirates.Client.IServices;

namespace SlyPirates.Local.Services
{
    public class FakeClient : IClientService
    {
        public bool IsConnected => true;
        public int Id => 1;
        public void Tick(int tick) { }
    }
}