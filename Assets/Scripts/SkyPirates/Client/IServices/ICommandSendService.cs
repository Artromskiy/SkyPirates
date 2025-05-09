namespace DVG.SkyPirates.Client.IServices
{
    internal interface ICommandSendService
    {
        void SendCommand<T>(T data) where T : unmanaged;
    }
}
