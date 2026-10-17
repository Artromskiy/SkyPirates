using SlyPirates.Client.Entry;
using SlyPirates.Shared.Data;
using SlyPirates.Shared.Ids;

public static class ClientSetupData
{
    public static string IP = "192.168.0.201";
    public static string Port = "7788";

    public static PlayerPack Pack { get; } = PlayerPack.CreateStarter();
    public static bool HasFullTeam => Pack.IsTeamComplete;
    public static UnitId[] TeamCards => Pack.GetTeamCards();
    public static UnitId[] CollectionCards => Pack.GetCollectionCards();
    public static UnitDeck StartingDeck => Pack.GetTeamDeck();
}
