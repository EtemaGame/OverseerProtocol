using System;
using System.Collections.Generic;
using OverseerProtocol.Data.Models.Enemies;
using OverseerProtocol.Data.Models.Items;
using OverseerProtocol.Data.Models.Moons;
using OverseerProtocol.Data.Models.Spawns;

namespace OverseerProtocol.Data.Models.UserConfig;

public sealed class UtilityCatalogFile
{
    public int SchemaVersion { get; set; } = 1;
    public string GeneratedUtc { get; set; } = DateTime.UtcNow.ToString("O");
    public string Notes { get; set; } = "Catalogo de referencia generado. Usa estos IDs en items.json y moons/*.json.";
    public List<ItemDefinition> Items { get; set; } = new();
    public List<MoonDefinition> Moons { get; set; } = new();
    public List<EnemyDefinition> Enemies { get; set; } = new();
    public List<MoonSpawnProfile> MoonSpawnProfiles { get; set; } = new();
}
