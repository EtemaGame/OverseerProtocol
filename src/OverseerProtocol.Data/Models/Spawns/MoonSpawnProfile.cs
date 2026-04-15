using System.Collections.Generic;

namespace OverseerProtocol.Data.Models.Spawns;

public sealed class MoonSpawnProfile
{
    public string MoonId { get; set; } = "";
    public List<SpawnEntry> InsideEnemies { get; set; } = new();
    public List<SpawnEntry> OutsideEnemies { get; set; } = new();
    public List<SpawnEntry> DaytimeEnemies { get; set; } = new();
}
