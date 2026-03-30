#nullable enable
using DVG.Collections;
using DVG.Commands;
using DVG.SkyPirates.Shared.Commands;
using DVG.SkyPirates.Shared.Data;
using DVG.SkyPirates.Shared.IServices;
using System;
using System.Runtime.CompilerServices;

namespace DVG.SkyPirates.Local.Services
{
    public class LocalCommandReciever : ICommandReciever
    {
        private readonly IEntityRegistry _entityRegistry;
        private readonly GenericCollection _listeners = new();

        public LocalCommandReciever(IEntityRegistry entityRegistry)
        {
            _entityRegistry = entityRegistry;
        }

        public void InvokeCommand<T>(Command<T> command)
        {
            var type = typeof(T);

            if (typeof(SpawnSquadCommand) == type || typeof(SpawnUnitCommand) == type)
            {
                if (command is Command<SpawnSquadCommand> squadCmd)
                {
                    var castedCmd = squadCmd;
                    var syncId = _entityRegistry.Reserve();
                    var syncIdReserve = _entityRegistry.Reserve(10);
                    var randomSeed = new Random().Next();
                    var creationParameters = new EntityParameters(syncId, syncIdReserve, randomSeed);
                    castedCmd.Data.CreationData = creationParameters;
                    command = Unsafe.As<Command<SpawnSquadCommand>, Command<T>>(ref castedCmd);
                }
                if (command is Command<SpawnUnitCommand> unitCmd)
                {
                    var castedCmd = unitCmd;
                    var syncId = _entityRegistry.Reserve();
                    var syncIdReserve = _entityRegistry.Reserve(10);
                    var randomSeed = new Random().Next();
                    var creationParameters = new EntityParameters(syncId, syncIdReserve, randomSeed);
                    castedCmd.Data.CreationData = creationParameters;
                    command = Unsafe.As<Command<SpawnUnitCommand>, Command<T>>(ref castedCmd);
                }
            }

            if (_listeners.TryGet<Action<Command<T>>>(out var callback))
                callback.Invoke(command);
        }

        public void RegisterReciever<T>(Action<Command<T>> reciever)
        {
            if (!_listeners.TryGet<Action<Command<T>>>(out var callback))
                _listeners.Add(reciever);
            else
                _listeners.Add(callback + reciever);
        }

        public void UnregisterReciever<T>(Action<Command<T>> reciever)
        {
            if (!_listeners.TryGet<Action<Command<T>>>(out var recievers))
                return;
            recievers -= reciever;
            if (recievers == null)
                _listeners.Remove<Action<Command<T>>>();
            else
                _listeners.Add(reciever);
        }



        private class ActionContainer<T> : IActionContainer
        {
            public event Action<Command<T>>? Recievers;
            public bool HasTargets => Recievers?.GetInvocationList().Length > 0;

            public void Invoke(Command<T> cmd)
            {
                Recievers?.Invoke(cmd);
            }
        }

        private interface IActionContainer { }
    }
}