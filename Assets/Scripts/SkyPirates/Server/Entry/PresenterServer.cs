using DVG.SkyPirates.Server.IFactories;
using DVG.SkyPirates.Server.Presenters;
using DVG.SkyPirates.Shared.IFactories;
using DVG.SkyPirates.Shared.Models;
using System.Collections.Generic;
using Unity.Netcode;
using UnityEngine;


namespace DVG.SkyPirates.Server.Entry
{
    public class PresenterServer : IStartable, ITickable
    {
        private readonly IInputFactory _inputFactory;
        private readonly IUnitFactory _unitFactory;
        private readonly IPathFactory<SquadModel> _squadModelFactory;
        private readonly IPathFactory<PackedCirclesModel> _packedCirclesFactory;

        private readonly Dictionary<ulong, PlayerPm> _players = new();

        public PresenterServer(IInputFactory inputFactory, IUnitFactory unitFactory, IPathFactory<SquadModel> squadModelFactory, IPathFactory<PackedCirclesModel> packedCirclesFactory)
        {
            _inputFactory = inputFactory;
            _unitFactory = unitFactory;
            _squadModelFactory = squadModelFactory;
            _packedCirclesFactory = packedCirclesFactory;
        }

        public void Start()
        {
            Debug.Log("Start");
            NetworkManager.Singleton.OnClientConnectedCallback += OnClientConnected;
            NetworkManager.Singleton.StartServer();
        }

        public void Tick()
        {
            foreach (var item in _players)
                item.Value.Tick();
        }

        private void OnClientConnected(ulong clientId)
        {
            var input = _inputFactory.Create(clientId);
            var squad = new SquadPm(input, _packedCirclesFactory);
            var squadModel = _squadModelFactory.Create("Configs/SquadModels/DefaultSquadModel");
            var player = new PlayerPm(input, squad, squadModel, _unitFactory);
            _players.Add(clientId, player);
        }
    }
}
