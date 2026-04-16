using System;
using System.Collections.Generic;

namespace OverseerProtocol.GameAbstractions.Overrides;

public sealed class MoonOverrideReferenceCatalog
{
    public HashSet<string> ExportedMoonIds { get; } = new(StringComparer.Ordinal);
    public HashSet<string> RuntimeMoonIds { get; } = new(StringComparer.Ordinal);
    public HashSet<string> RoutePriceMoonIds { get; } = new(StringComparer.Ordinal);

    public bool HasExportedMoonCatalog => ExportedMoonIds.Count > 0;
    public bool HasRuntimeMoonCatalog => RuntimeMoonIds.Count > 0;
}
