using DVG.Commands;
using DVG.SkyPirates.Shared.IServices;
using System;

namespace DVG.SkyPirates.Client.Services
{
    public class FakeCommandReciever : ICommandReciever
    {
        public void InvokeCommand<T>(Command<T> command)
        {

        }

        public void RegisterReciever<T>(Action<Command<T>> reciever)
        {

        }

        public void UnregisterReciever<T>(Action<Command<T>> reciever)
        {

        }
    }
}
