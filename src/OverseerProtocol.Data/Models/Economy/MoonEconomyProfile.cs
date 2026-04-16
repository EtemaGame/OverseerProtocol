using System.Collections.Generic;

namespace OverseerProtocol.Data.Models.Economy;

public sealed class MoonEconomyProfile
{
    public string MoonId { get; set; } = "";
    public string InternalName { get; set; } = "";
    public int LevelIndex { get; set; }
    public int RoutePrice { get; set; }
    public bool HasRouteNode { get; set; }
    public List<RoutePriceNodeDefinition> RouteNodes { get; set; } = new();
}
