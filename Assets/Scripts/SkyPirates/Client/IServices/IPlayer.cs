namespace DVG.SkyPirates.Client.IServices
{
    public interface IPlayer
    {
        int? SquadEntityId { get; }
        int? ShipEntityId { get; }
        int? CurrentEntityId { get; }
    }
}
