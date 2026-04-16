namespace OverseerProtocol.Data.Models.Economy;

public sealed class RoutePriceNodeDefinition
{
    public string TerminalNodeId { get; set; } = "";
    public int BuyRerouteToMoon { get; set; }
    public int ItemCost { get; set; }
}
