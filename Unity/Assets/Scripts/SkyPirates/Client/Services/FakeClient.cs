using DVG.SkyPirates.Client.IServices;

namespace DVG.SkyPirates.Local.Services
{
    public class FakeClient : IClientService
    {
        public bool IsConnected => true;
        public int Id => 1;
        public void Tick(int tick) { }
    }
}