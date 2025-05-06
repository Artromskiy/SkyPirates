using DVG.SkyPirates.Server.IFactories;
using DVG.SkyPirates.Server.Presenters;
using DVG.SkyPirates.OldShared.IViews;
using Unity.Netcode;

namespace DVG.SkyPirates.Server.Factories
{
    public class InputFactory : IInputFactory
    {
        public InputPm Create(ulong clientId)
        {
            var view = NetworkManager.Singleton.ConnectedClients[clientId].PlayerObject.GetComponent<IInputView>();
            return new(view);
        }
    }
}
