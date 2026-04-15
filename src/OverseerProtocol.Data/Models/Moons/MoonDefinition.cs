using System.Collections.Generic;

namespace OverseerProtocol.Data.Models.Moons;

public sealed class MoonDefinition
{
    public string Id { get; set; } = "";
    public string Name { get; set; } = "";
    public int RoutePrice { get; set; }
    public int RiskLevel { get; set; }
    public bool IsWeatherEffectPermanent { get; set; }
    public int DungeonSizeMin { get; set; }
    public int DungeonSizeMax { get; set; }
    public List<string> DaytimeEnemies { get; set; } = new();
    public List<string> OutsideEnemies { get; set; } = new();
    public List<string> InsideEnemies { get; set; } = new();
}
