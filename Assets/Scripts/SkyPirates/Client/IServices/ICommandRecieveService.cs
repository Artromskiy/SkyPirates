#nullable enable
using System;

namespace DVG.SkyPirates.Client.IServices
{
    internal interface ICommandRecieveService
    {
        public void RegisterReciever<T>(Action<T> reciever) where T : unmanaged;
        public void UnregisterReciever<T>(Action<T> reciever) where T : unmanaged;
    }
}
