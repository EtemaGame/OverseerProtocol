using System.Collections.Generic;
using OverseerProtocol.Data.Models.Spawns;

namespace OverseerProtocol.Data.Models;

public sealed class SpawnOverrideCollection
{
    public int SchemaVersion { get; set; } = 1;
    public List<MoonSpawnOverride> Overrides { get; set; } = new();
}

public sealed class MoonSpawnOverride
{
    public string MoonId { get; set; } = "";
    
    // Nullable to support "omitted = keep vanilla" requirement
    public List<SpawnEntry>? InsideEnemies { get; set; }
    public List<SpawnEntry>? OutsideEnemies { get; set; }
    public List<SpawnEntry>? DaytimeEnemies { get; set; }
}
