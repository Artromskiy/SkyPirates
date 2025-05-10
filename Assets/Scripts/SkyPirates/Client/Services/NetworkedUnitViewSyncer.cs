#nullable enable
using DVG.SkyPirates.Client.IServices;
using DVG.SkyPirates.Shared.Commands;
using DVG.SkyPirates.Shared.Ids;
using DVG.SkyPirates.Shared.IFactories;
using DVG.SkyPirates.Shared.IViews;
using System.Collections.Generic;

namespace DVG.SkyPirates.Client.Services
{
    internal class NetworkedUnitViewSyncer : IUnitViewSyncer
    {
        private readonly ICommandRecieveService _messageRecieveService;
        private readonly IUnitViewFactory _unitViewFactory;

        private readonly Dictionary<int, IUnitView> _views = new Dictionary<int, IUnitView>();

        public NetworkedUnitViewSyncer(ICommandRecieveService messageRecieveService, IUnitViewFactory unitViewFactory)
        {
            _messageRecieveService = messageRecieveService;
            _unitViewFactory = unitViewFactory;

            _messageRecieveService.RegisterReciever<RegisterUnitCommand>(RegisterUnit);
            _messageRecieveService.RegisterReciever<UpdateUnitCommand>(UpdateUnit);
            _messageRecieveService.RegisterReciever<UnregisterUnitCommand>(UnregisterUnit);
        }

        private void UpdateUnit(UpdateUnitCommand cmd)
        {
            if (_views.TryGetValue(cmd.id, out var view))
            {
                view.Position = cmd.position;
                view.Rotation = cmd.rotation;
            }
        }

        private void RegisterUnit(RegisterUnitCommand cmd)
        {
            var view = _unitViewFactory.Create((new UnitId("Sailor"), 1, 1));
            _views.Add(cmd.id, view);
        }

        private void UnregisterUnit(UnregisterUnitCommand cmd)
        {
            _views.Remove(cmd.id);
        }

        public void Tick()
        {
            //throw new System.NotImplementedException();
        }
    }
}
