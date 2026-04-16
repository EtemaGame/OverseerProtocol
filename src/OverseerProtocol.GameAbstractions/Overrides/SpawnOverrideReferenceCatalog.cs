using System;
using System.Collections.Generic;

namespace OverseerProtocol.GameAbstractions.Overrides;

public sealed class SpawnOverrideReferenceCatalog
{
    public HashSet<string> MoonIds { get; } = new(StringComparer.Ordinal);
    public HashSet<string> EnemyIds { get; } = new(StringComparer.Ordinal);

    public bool HasMoonCatalog => MoonIds.Count > 0;
    public bool HasEnemyCatalog => EnemyIds.Count > 0;
}
