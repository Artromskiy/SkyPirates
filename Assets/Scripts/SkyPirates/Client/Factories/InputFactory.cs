using DVG.SkyPirates.Client.IFactories;
using DVG.SkyPirates.Client.Presenters;
using DVG.SkyPirates.OldShared.IViews;
using Unity.Netcode;
using UnityEngine.Scripting;

namespace DVG.SkyPirates.Client.Factories
{
    [Preserve]
    public class InputFactory : IInputFactory
    {
        public InputFactory() { }
        public InputPm Create()
        {
            var view = NetworkManager.Singleton.LocalClient.PlayerObject.GetComponent<IInputView>();
            return new(view);
        }
    }
}
